// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.PythonTools.Debugger.Concord.Proxies;
using Microsoft.PythonTools.Debugger.Concord.Proxies.Structs;
using Microsoft.PythonTools.Parsing;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.CustomRuntimes;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Native;

namespace Microsoft.PythonTools.Debugger.Concord {
    internal class CallStackFilter : DkmDataItem {
        private class StackWalkContextData : DkmDataItem {
            public bool? IsLastFrameNative { get; set; }
            public NativeFrameKind LastFrameKind { get; set; }

            private PyFrameObject[] _knownFrames;

            public PyFrameObject[] GetKnownFrames(DkmProcess process) {
                if (_knownFrames == null) {
                    _knownFrames = PyThreadState.GetThreadStates(process).SelectMany(tstate => tstate.GetFrames()).ToArray();
                }
                return _knownFrames;
            }
        }

        private readonly DkmProcess _process;
        private readonly PythonRuntimeInfo _pyrtInfo;

        public CallStackFilter(DkmProcess process) {
            _process = process;
            _pyrtInfo = process.GetPythonRuntimeInfo();
        }

        public DkmStackWalkFrame[] FilterNextFrame(DkmStackContext stackContext, DkmStackWalkFrame nativeFrame) {
            PyFrameObject pythonFrame = null;
            var nativeModuleInstance = nativeFrame.ModuleInstance;
            if (nativeModuleInstance == _pyrtInfo.DLLs.DebuggerHelper) {
                if (_pyrtInfo.LanguageVersion < PythonLanguageVersion.V36 ||
                    (pythonFrame = PyFrameObject.TryCreate(nativeFrame)) == null) {
                    return DebuggerOptions.ShowNativePythonFrames ? new[] { nativeFrame } : new DkmStackWalkFrame[0];
                }
            }

            var result = new List<DkmStackWalkFrame>();

            if (pythonFrame == null) {
                var stackWalkData = stackContext.GetDataItem<StackWalkContextData>();
                if (stackWalkData == null) {
                    stackWalkData = new StackWalkContextData();
                    stackContext.SetDataItem(DkmDataCreationDisposition.CreateNew, stackWalkData);
                }
                bool? wasLastFrameNative = stackWalkData.IsLastFrameNative;

                if (nativeModuleInstance != _pyrtInfo.DLLs.Python && nativeModuleInstance != _pyrtInfo.DLLs.CTypes) {
                    stackWalkData.IsLastFrameNative = true;
                    if (wasLastFrameNative == false) {
                        result.Add(DkmStackWalkFrame.Create(nativeFrame.Thread, null, nativeFrame.FrameBase, nativeFrame.FrameSize,
                            DkmStackWalkFrameFlags.NonuserCode, Strings.DebugCallStackNativeToPythonTransition, null, null));
                    } else {
                        stackWalkData.IsLastFrameNative = true;
                    }
                    result.Add(nativeFrame);
                    return result.ToArray();
                }

                stackWalkData.IsLastFrameNative = false;
                if (wasLastFrameNative == true) {
                    result.Add(DkmStackWalkFrame.Create(nativeFrame.Thread, null, nativeFrame.FrameBase, nativeFrame.FrameSize,
                        DkmStackWalkFrameFlags.NonuserCode, Strings.DebugCallStackPythonToNativeTransition, null, null));
                }

                var frameAddr = PyFrameObject.TryGetAddress(nativeFrame, out var frameKind);
                if (frameKind == NativeFrameKind.PyEval_EvalFrameEx) {
                    // On 3.6+, PyEval_EvalFrameEx cannot be used to retrieve PyFrameObject reliably.
                    if (_pyrtInfo.LanguageVersion >= PythonLanguageVersion.V36) {
                        frameAddr = 0;
                    }
                } else if (frameKind == NativeFrameKind.PyEval_EvalFrameDefault) {
                    // Only present in 3.6+.
                    if (stackWalkData.LastFrameKind == NativeFrameKind.EvalFrameFunc) {
                        // If the previous frame was EvalFrameFunc, we have already retrieved PyFrameObject
                        // from that one, and there's no need to try to do so here; just skip it. 
                        frameAddr = 0;
                    } else {
                        // If the previous frame was not EvalFrameFunc, then this is our last chance to retrieve
                        // PyFrameObject. This can happen if we have just attached to an object, and are filtering
                        // an existing stack that was constructed before our eval hook was in the picture - thus, 
                        // it will have PyEval_EvalFrameEx invoking _PyEval_EvalFrameDefault directly.
                        //
                        // Because the object pointer may be optimized away, we can only do best effort here.
                        // The assumption is that the pointer is mangled by making it point to some member of
                        // PyFrameObject, rather than the object itself. To detect that, we look at all known
                        // Python frame objects, retrieved via interpreter/thread state, and compare our pointer
                        // to their boundaries in memory. Worst case, we match a wrong (but still valid) frame
                        // object, or find no matching frame at all.

                        var knownFrames = stackWalkData.GetKnownFrames(nativeFrame.Process);
                        pythonFrame = knownFrames.Where(f => f.ContainsAddress(frameAddr)).FirstOrDefault();
                        frameAddr = 0;
                    }

                    if (frameAddr != 0) {
                        pythonFrame = new PyFrameObject(nativeFrame.Process, frameAddr);
                    }
                }
                stackWalkData.LastFrameKind = frameKind;
            }

            if (pythonFrame == null) {
                if (DebuggerOptions.ShowNativePythonFrames) {
                    result.Add(nativeFrame);
                }
                return result.ToArray();
            }

            PyCodeObject code = pythonFrame.f_code.Read();
            var loc = new SourceLocation(
                code.co_filename.Read().ToStringOrNull(),
                pythonFrame.f_lineno.Read(),
                code.co_name.Read().ToStringOrNull(),
                nativeFrame.InstructionAddress as DkmNativeInstructionAddress);

            var pythonRuntime = _process.GetPythonRuntimeInstance();
            var pythonModuleInstances = pythonRuntime.GetModuleInstances().OfType<DkmCustomModuleInstance>();
            var pyModuleInstance = pythonModuleInstances.Where(m => m.FullName == loc.FileName).FirstOrDefault();
            if (pyModuleInstance == null) {
                pyModuleInstance = pythonModuleInstances.Single(m => m.Module.Id.Mvid == Guids.UnknownPythonModuleGuid);
            }

            var encodedLocation = loc.Encode();
            var instrAddr = DkmCustomInstructionAddress.Create(pythonRuntime, pyModuleInstance, encodedLocation, 0, encodedLocation, null);
            var frame = DkmStackWalkFrame.Create(
                nativeFrame.Thread,
                instrAddr,
                nativeFrame.FrameBase,
                nativeFrame.FrameSize,
                DkmStackWalkFrameFlags.None,
                null,
                nativeFrame.Registers,
                nativeFrame.Annotations);
            result.Add(frame);

            if (DebuggerOptions.ShowNativePythonFrames) {
                result.Add(nativeFrame);
            }
            return result.ToArray();
        }

        public void GetFrameName(DkmInspectionContext inspectionContext, DkmWorkList workList, DkmStackWalkFrame frame, DkmVariableInfoFlags argumentFlags, DkmCompletionRoutine<DkmGetFrameNameAsyncResult> completionRoutine) {
            var insAddr = frame.InstructionAddress as DkmCustomInstructionAddress;
            if (insAddr == null) {
                Debug.Fail("GetFrameName called on a Python frame without a proper instruction address.");
                throw new InvalidOperationException();
            }

            var loc = new SourceLocation(insAddr.AdditionalData, frame.Process);
            completionRoutine(new DkmGetFrameNameAsyncResult(loc.FunctionName));
        }

        public void GetFrameReturnType(DkmInspectionContext inspectionContext, DkmWorkList workList, DkmStackWalkFrame frame, DkmCompletionRoutine<DkmGetFrameReturnTypeAsyncResult> completionRoutine) {
            completionRoutine(new DkmGetFrameReturnTypeAsyncResult(null));
        }
    }
}
