using System.Collections.Generic;
using Microsoft.PythonTools.Project;
using Microsoft.VisualStudio.ApplicationCapabilities.Publish.Contracts;

namespace Microsoft.PythonTools.Publishing {
    internal class PythonBuildAndPublish : IBuildAndPublish {
        private readonly PythonProjectNode _projectNode;

        public PythonBuildAndPublish(PythonProjectNode projectNode) {
            _projectNode = projectNode;
            DefaultPublishSteps = new PythonPublishSteps(_projectNode);
        }

        public IPublishSteps DefaultPublishSteps { get; }

        // Appears to be unused.
        public IDictionary<string, string> CorePublishProperties { get; } = new Dictionary<string, string>();
    }
}
