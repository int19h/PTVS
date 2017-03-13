using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PythonTools.Project;
using Microsoft.VisualStudio.ApplicationCapabilities.Publish.Contracts;

namespace Microsoft.PythonTools.Publishing {
    internal class PythonPublishSteps : IPublishSteps {
        private readonly PythonProjectNode _projectNode;

        public PythonPublishSteps(PythonProjectNode projectNode) {
            _projectNode = projectNode;
        }

        public Func<string, Task<bool>> PrePublish { get; set; } = DoNothing;

        public Func<string, Task<bool>> CorePublish { get; set; } = Publish;

        public Func<string, Task<bool>> PostPublish { get; set; } = DoNothing;

        private static Task<bool> DoNothing(string profileId) => Task.FromResult(true);

        private static Task<bool> Publish(string profileId) {
            return Task.FromResult(true);
        }
    }
}
