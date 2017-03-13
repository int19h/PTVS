using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PythonTools.Project;
using Microsoft.VisualStudio.ApplicationCapabilities.Publish.Contracts;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Web.WindowsAzure.CommonContracts;
using Microsoft.VisualStudio.WindowsAzure.MicrosoftWeb;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudioTools.Project.Automation;
using Microsoft.WindowsAzure.Client.Entities;
using Microsoft.WindowsAzure.Client.MicrosoftWeb;
using Microsoft.WindowsAzure.Client.MicrosoftWeb.Entities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.PythonTools.Publishing {
    internal class PythonAppServiceProfileVisualFactory : IProfileVisualFactory {
        private static readonly IReadOnlyList<string> _tags = new[] { "Azure", "Web", "AppService" };

        private readonly IPublishServiceProvider _publishServiceProvider;
        private readonly IComponentModel _componentModel;
        private Action<string, bool> _statusUpdater;

        public PythonAppServiceProfileVisualFactory(IPublishServiceProvider publishServiceProvider) {
            _publishServiceProvider = publishServiceProvider;

            _componentModel = (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
            _componentModel.DefaultCompositionService.SatisfyImportsOnce(this);
        }

        [Import]
        private IAzureShoppingCartDeploymentDialogFactory DeploymentDialogFactory { get; set; }

        [Import]
        private IAzureServiceSelectionDialogFactory ServiceSelectionDialogFactory { get; set; }

        public int ExistingProfileHandlingPriority => 100;

        public object Icon => null;

        public string DisplayName => "Python Azure Service";

        public Func<Task<bool>> ImportProfilesAsync => null;

        public Func<string, bool> FilterProfiles => id => true;

        public bool IsApplicableTo(IVsHierarchy hierarchy, out IReadOnlyList<string> tags) { 
            if (GetProjectNode(hierarchy) != null) {
                tags = _tags;
                return true;
            } else {
                tags = null;
                return false;
            }
        }

        public object GetOptionsControl(IVsHierarchy hierarchy, Action<string, bool> statusUpdater) {
            _statusUpdater = statusUpdater;
            return new PythonAppServiceOptionsControl();
        }

        public Task<IProfileVisual> CreateProfileAsync(IVsHierarchy hierarchy, object optionsControl) {
            var control = optionsControl as PythonAppServiceOptionsControl;
            if (control == null) {
                return Task.FromResult<IProfileVisual>(null);
            }

            var projectNode = GetProjectNode(hierarchy);
            if (projectNode == null) {
                return Task.FromResult<IProfileVisual>(null);
            }

            // TODO: other kinds are "api" and "mobileapp" - .NET publish uses NuGet package references to
            // auto-detect them; we will probably need an explicit project property if we support those.
            string kind = "WebApp";

            return control.CreateNewService
                ? CreateProfileWithNewAppServiceAsync(hierarchy, projectNode, kind)
                : CreateProfileWithExistingAppServiceAsync(hierarchy, projectNode, kind);
        }

        private async Task<IProfileVisual> CreateProfileWithNewAppServiceAsync(IVsHierarchy hierarchy, PythonProjectNode projectNode, string kind) {
            var dialog = await DeploymentDialogFactory.CreateAsync(
                "Microsoft.Web/sites",
                new Dictionary<string, object>
                {
                    { WellKnownLaunchParameter.Launcher, "AppServicePublish" },
                    { WellKnownLaunchParameter.ProjectName, projectNode.GetProjectName() },
                    { "AppServiceKind", kind }
                });

            if (dialog.ShowModal().GetValueOrDefault()) {
                return await ImportProfileFromAppServiceAsync(hierarchy, dialog.PrimaryEntity);
            }

            return null;
        }

        private async Task<IProfileVisual> CreateProfileWithExistingAppServiceAsync(IVsHierarchy hierarchy, PythonProjectNode projectNode, string kind) {
            var dialog = ServiceSelectionDialogFactory.CreateDialog(AppServiceAssetSelectionParameters.Default);
            if (dialog.ShowModal().GetValueOrDefault()) {
                return await ImportProfileFromAppServiceAsync(hierarchy, dialog.Result);
            }

            return null;
        }

        private async Task<IProfileVisual> ImportProfileFromAppServiceAsync(IVsHierarchy hierarchy, IEntity entity) {
            _statusUpdater?.Invoke("Downloading publish settings", false);
            try {
                //IProfileList list;
                //if (!_publishServiceProvider.TryGetService(out list)) {
                //    return null;
                //}

                string publishSettingsFileContents = null;
                IWebsite siteTarget = entity as IWebsite;
                if (siteTarget != null) {
                    publishSettingsFileContents = await siteTarget.GetPublishXmlAsync();
                } else {
                    IWebsiteDeploymentSlot slotTarget = entity as IWebsiteDeploymentSlot;
                    if (slotTarget != null) {
                        publishSettingsFileContents = await slotTarget.GetPublishXmlAsync();
                    }
                }
                if (string.IsNullOrEmpty(publishSettingsFileContents)) {
                    return null;
                }

                if (!_publishServiceProvider.TryGetService(out IPublishRequirements publishRequirements)) {
                    return null;
                }

                var persistence = publishRequirements.PublishPersistence;
                string profileName = persistence.GetUniqueProfileName(entity.Name);
                string profileId = persistence.GetNewProfileId(profileName);

                var profile = new MemoryStream(Encoding.UTF8.GetBytes(publishSettingsFileContents));
                persistence.SetProfile(profileId, profile, isPrivate: false);

                return new PythonAppServiceProfileVisual(hierarchy, profileId);
            } finally {
                _statusUpdater?.Invoke(null, true);
            }
        }

        public bool TryGetProfileVisual(string profileId, out IProfileVisual profileVisual) {
            profileVisual = null;
            if (!_publishServiceProvider.TryGetService(out IPublishRequirements publish) ||
                !(publish is PythonPublishRequirements pythonPublish)) {
                return false;
            }

            profileVisual = new PythonAppServiceProfileVisual(pythonPublish.Hierarchy, profileId);
            return true;
        }

        private static PythonProjectNode GetProjectNode(IVsHierarchy hierarchy) =>
            (hierarchy.GetProject() as OAProject)?.Project as PythonProjectNode;
    }
}
