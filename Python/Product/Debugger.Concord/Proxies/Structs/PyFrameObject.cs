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
using System.Diagnostics;
using Microsoft.PythonTools.Parsing;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.PythonTools.Debugger.Concord.Proxies.Structs {
    internal class PyFrameObject : PyVarObject {
        public class Fields_27_35 {
            public StructField<PointerProxy<PyCodeObject>> f_code;
            public StructField<PointerProxy<PyDictObject>> f_globals;
            public StructField<PointerProxy<PyDictObject>> f_locals;
            public StructField<Int32Proxy> f_lineno;
            public StructField<ArrayProxy<PointerProxy<PyObject>>> f_localsplus;
        }

        public class Fields_36 {
            public StructField<PointerProxy<PyFrameObject>> f_back;
            public StructField<PointerProxy<PyCodeObject>> f_code;
            public StructField<PointerProxy<PyDictObject>> f_globals;
            public StructField<PointerProxy<PyDictObject>> f_locals;
            public StructField<Int32Proxy> f_lineno;
            public StructField<ArrayProxy<PointerProxy<PyObject>>> f_localsplus;
        }

        private readonly object _fields;

        public PyFrameObject(DkmProcess process, ulong address)
            : base(process, address) {
            var pythonInfo = process.GetPythonRuntimeInfo();
            if (pythonInfo.LanguageVersion <= PythonLanguageVersion.V35) {
                Fields_27_35 fields;
                InitializeStruct(this, out fields);
                _fields = fields;
            } else {
                Fields_36 fields;
                InitializeStruct(this, out fields);
                _fields = fields;
            }
            CheckPyType<PyFrameObject>();
        }

        private static bool IsInSameFunction(DkmStackWalkFrame frame, ulong addr) =>
            addr != 0 && frame.InstructionAddress.IsInSameFunction(frame.Process.CreateNativeInstructionAddress(addr));

        private static NativeFrameKind GetNativeFrameKind(DkmStackWalkFrame frame) {
            var pythonInfo = frame.Process.GetPythonRuntimeInfo();
            var frameKind = NativeFrameKind.Other;
            if (frame.ModuleInstance == pythonInfo.DLLs.Python) {
                if (IsInSameFunction(frame, pythonInfo.DLLs.Python.GetFunctionAddress("PyEval_EvalFrameEx"))) {
                    frameKind = NativeFrameKind.PyEval_EvalFrameEx;
                } else if (IsInSameFunction(frame, pythonInfo.DLLs.Python.GetFunctionAddress("_PyEval_EvalFrameDefault"))) {
                    frameKind = NativeFrameKind.PyEval_EvalFrameDefault;
                }
            } else if (frame.ModuleInstance == pythonInfo.DLLs.DebuggerHelper) {
                if (IsInSameFunction(frame, pythonInfo.DLLs.DebuggerHelper.GetFunctionAddress("EvalFrameFunc"))) {
                    frameKind = NativeFrameKind.EvalFrameFunc;
                }
            }
            return frameKind;
        }

        public static PyFrameObject TryCreate(DkmStackWalkFrame frame) => TryCreate(frame, out _);

        public static unsafe PyFrameObject TryCreate(DkmStackWalkFrame frame, out NativeFrameKind nativeFrameKind) {
            var addr = TryGetAddress(frame, out nativeFrameKind);
            return addr == 0 ? null : new PyFrameObject(frame.Process, addr);
        }

        public static unsafe ulong TryGetAddress(DkmStackWalkFrame frame, out NativeFrameKind nativeFrameKind) {
            var process = frame.Process;

            nativeFrameKind = NativeFrameKind.Other;
            if (frame.InstructionAddress == null) {
                return 0;
            }

            nativeFrameKind = GetNativeFrameKind(frame);
            if (frame.RuntimeInstance.Id.RuntimeType != Guids.PythonRuntimeTypeGuid &&
                nativeFrameKind != NativeFrameKind.PyEval_EvalFrameDefault &&
                nativeFrameKind != NativeFrameKind.PyEval_EvalFrameEx &&
                nativeFrameKind != NativeFrameKind.EvalFrameFunc) {
                return 0;
            }

            var cppLanguage = DkmLanguage.Create("C++", new DkmCompilerId(Guids.MicrosoftVendorGuid, Guids.CppLanguageGuid));
            var inspectionSession = DkmInspectionSession.Create(process, null);
            var inspectionContext = DkmInspectionContext.Create(inspectionSession, process.GetNativeRuntimeInstance(), frame.Thread, 0,
                DkmEvaluationFlags.TreatAsExpression | DkmEvaluationFlags.NoSideEffects, DkmFuncEvalFlags.None, 10, cppLanguage, null);

            CppExpressionEvaluator cppEval;
            try {
                cppEval = new CppExpressionEvaluator(inspectionContext, frame);
            } catch (ArgumentException) {
                Debug.Fail("Failed to create C++ expression evaluator while obtaining PyFrameObject from a native frame.");
                return 0;
            }

            try {
                return cppEval.EvaluateUInt64("f");
            } catch (CppEvaluationException) {
                Debug.Fail("Failed to evaluate the 'f' parameter to PyEval_EvalFrameEx while obtaining PyFrameObject from a native frame.");
                return 0;
            }
        }

        public PointerProxy<PyFrameObject> f_back {
            get { return GetFieldProxy((_fields as Fields_36)?.f_back); }
        }

        public PointerProxy<PyCodeObject> f_code {
            get { return GetFieldProxy((_fields as Fields_36)?.f_code ?? (_fields as Fields_27_35)?.f_code); }
        }

        public PointerProxy<PyDictObject> f_globals {
            get { return GetFieldProxy((_fields as Fields_36)?.f_globals ?? (_fields as Fields_27_35)?.f_globals); }
        }

        public PointerProxy<PyDictObject> f_locals {
            get { return GetFieldProxy((_fields as Fields_36)?.f_locals ?? (_fields as Fields_27_35)?.f_locals); }
        }

        public Int32Proxy f_lineno {
            get { return GetFieldProxy((_fields as Fields_36)?.f_lineno ?? (_fields as Fields_27_35)?.f_lineno); }
        }

        public ArrayProxy<PointerProxy<PyObject>> f_localsplus {
            get { return GetFieldProxy((_fields as Fields_36)?.f_localsplus ?? (_fields as Fields_27_35)?.f_localsplus); }
        }
    }

    internal enum NativeFrameKind {
        Other,
        Python = 1000, // for conditionals: (kind >= NativeFrameKind.Python)
        PyEval_EvalFrameEx = Python,
        PyEval_EvalFrameDefault,
        EvalFrameFunc,
    }
}
