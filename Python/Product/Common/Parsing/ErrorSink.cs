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
// MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using Microsoft.PythonTools.Common.Core.Text;
using Microsoft.PythonTools.Common.Parsing;

namespace Microsoft.PythonTools.Common.Parsing {
    public class ErrorSink {
        public static readonly ErrorSink Null = new ErrorSink();

        internal void Add(string message, NewLineLocation[] lineLocations, int startIndex, int endIndex, int errorCode, Severity severity) => Add(
                message,
                NewLineLocation.IndexToLocation(lineLocations, startIndex),
                NewLineLocation.IndexToLocation(lineLocations, endIndex),
                errorCode,
                severity
            );

        internal void Add(string message, SourceLocation startSourceLoc, SourceLocation endSourceLoc, int errorCode, Severity severity) => Add(
                message,
                new SourceSpan(startSourceLoc, endSourceLoc),
                errorCode,
                severity
            );

        public virtual void Add(string message, SourceSpan span, int errorCode, Severity severity) {
        }
    }
}
