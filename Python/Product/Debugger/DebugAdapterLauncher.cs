﻿// Python Tools for Visual Studio
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
using System.Runtime.InteropServices;
using Microsoft.PythonTools.Debugger.Remote;
using Microsoft.VisualStudio.Debugger.DebugAdapterHost.Interfaces;
using Newtonsoft.Json.Linq;

namespace Microsoft.PythonTools.Debugger {
    [ComVisible(true)]
    [Guid(DebugAdapterLauncherCLSIDNoBraces)]
    public sealed class DebugAdapterLauncher : IAdapterLauncher {
        public const string DebugAdapterLauncherCLSIDNoBraces = "C2990BF1-A87B-4459-9478-322482C535D6";
        public const string DebugAdapterLauncherCLSID = "{" + DebugAdapterLauncherCLSIDNoBraces + "}";
        public const string VSCodeDebugEngineId = "{86432F39-ADFD-4C56-AA8F-AF8FCDC66039}";
        public static Guid VSCodeDebugEngine = new Guid(VSCodeDebugEngineId);

        public DebugAdapterLauncher() { }

        public void Initialize(IDebugAdapterHostContext context) {
        }

        public ITargetHostProcess LaunchAdapter(IAdapterLaunchInfo launchInfo, ITargetHostInterop targetInterop) {
            switch (launchInfo.LaunchType) {
                case LaunchType.Launch:
                    return DebugAdapterProcess.Start(launchInfo.LaunchJson);
                case LaunchType.Attach:
                    if (launchInfo.LaunchLocation == LaunchLocation.Local) {
                        return DebugAdapterProcess.Attach(launchInfo.AttachProcessId);
                    } else if (launchInfo.DebugPort is PythonRemoteDebugPort) {
                        return DebugAdapterRemoteProcess.Attach(launchInfo.LaunchJson);
                    } else {
                        goto default;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public void UpdateLaunchOptions(IAdapterLaunchInfo launchInfo) {
            if (launchInfo.LaunchType == LaunchType.Attach) {
                launchInfo.DebugPort.GetPortName(out string uri);
                JObject obj = new JObject {
                    ["remote"] = uri
                };
                launchInfo.LaunchJson = obj.ToString();
            }
        }
    }
}
