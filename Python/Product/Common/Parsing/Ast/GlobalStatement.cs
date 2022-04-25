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

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PythonTools.Common.Core;
using Microsoft.PythonTools.Common.Core.Collections;

namespace Microsoft.PythonTools.Common.Parsing.Ast {
    public class GlobalStatement : Statement {
        public GlobalStatement(ImmutableArray<NameExpression> names) {
            Names = names;
        }

        public ImmutableArray<NameExpression> Names { get; }

        public override IEnumerable<Node> GetChildNodes() => Names;

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                foreach (var n in Names) {
                    n.Walk(walker);
                }
            }
            walker.PostWalk(this);
        }

        public override async Task WalkAsync(PythonWalkerAsync walker, CancellationToken cancellationToken = default) {
            if (await walker.WalkAsync(this, cancellationToken)) {
                foreach (var n in Names) {
                    await n.WalkAsync(walker, cancellationToken);
                }
            }
            await walker.PostWalkAsync(this, cancellationToken);
        }

        internal override void AppendCodeStringStmt(StringBuilder res, PythonAst ast, CodeFormattingOptions format) {
            var namesWhiteSpace = this.GetNamesWhiteSpace(ast);

            if (namesWhiteSpace != null) {
                ListExpression.AppendItems(res, ast, format, "global", "", this, Names.Count, (i, sb) => {
                    sb.Append(namesWhiteSpace[i]);
                    Names[i].AppendCodeString(res, ast, format);
                });
            } else {
                ListExpression.AppendItems(res, ast, format, "global", "", this, Names.Count, (i, sb) => Names[i].AppendCodeString(sb, ast, format));
            }
        }
    }
}
