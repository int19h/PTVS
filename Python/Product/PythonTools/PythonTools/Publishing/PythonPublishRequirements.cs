using System.ComponentModel.Composition;
using Microsoft.PythonTools.Project;
using Microsoft.VisualStudio.ApplicationCapabilities.Publish.Contracts;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudioTools.Project.Automation;

namespace Microsoft.PythonTools.Publishing {
    [Export(typeof(IPublishRequirements))]
    internal class PythonPublishRequirements : IPublishRequirements {
        public IVsHierarchy Hierarchy { get; private set; }

        public PythonProjectNode ProjectNode { get; private set; }

        public IPublishUserInterface PublishUserInterface { get; } = new PythonPublishUserInterface();

        public IPublishPersistence PublishPersistence { get; private set; }

        public IBuildAndPublish SolutionBuildManagerPublish { get; private set; }

        public bool IsApplicableTo(IVsHierarchy hierarchy) {
            if ((hierarchy.GetProject() as OAProject)?.Project is PythonProjectNode projectNode) {
                Hierarchy = hierarchy;
                ProjectNode = projectNode;
                PublishPersistence = new PythonPublishPersistence(projectNode);
                SolutionBuildManagerPublish = new PythonBuildAndPublish(projectNode);
                return true;
            }

            PublishPersistence = null;
            return false;
        }
    }
}
