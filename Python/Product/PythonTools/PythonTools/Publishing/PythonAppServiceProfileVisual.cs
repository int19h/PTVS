using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ApplicationCapabilities.Publish.Contracts;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Web.Publish.Contracts;

namespace Microsoft.PythonTools.Publishing {
    internal class PythonAppServiceProfileVisual : IProfileVisual {
        private readonly IVsHierarchy _hierarchy;

        public PythonAppServiceProfileVisual(IVsHierarchy hierarchy, string profileId) {
            _hierarchy = hierarchy;
            ProfileId = profileId;
        }

        public string ProfileId { get; }

        public object ProfileIcon => null;

        public string ProfileName => ProfileId;

        public IReadOnlyList<IProfileCommand> ProfileCommands {
            get {
                // To reuse the existing UI for the implementation of these commands, we need IProfileManager.
                // Its actual implementation is class VsWebProjectPublish in assembly Microsoft.VisualStudio.Web.Publish,
                // obtained via IVsWebProjectPublishService globally proffered service. That code assumes that it's dealing
                // with .NET (or .NET Core, now) projects. See ProjectPublishData and PublishService.GetProjectPublishData
                // for the kinds of things it expects from the project system.
                // Otherwise, we need to provide our own UI to edit the profile, and to do publish preview.

                var publishService = (IVsWebProjectPublishService)Package.GetGlobalService(typeof(IVsWebProjectPublishService));
                var publish = publishService.GetWebProjectPublish(_hierarchy);
                if (!(publish is Microsoft.VisualStudio.Web.Publish.Core.Contracts.IProfileManager profileManager)) {
                    return new IProfileCommand[0];
                }

                return new IProfileCommand[] {
                    new PythonProfileCommand("Settings...", null, async() => {
                        await profileManager.ShowProfileUIAsync(ProfileName, profileManager.GetSettingsPublishOptions());
                    }),
                    new PythonProfileCommand("Preview...", null, async() => {
                        await profileManager.ShowProfileUIAsync(ProfileName, profileManager.GetPreviewPublishOptions());
                    }),
                };
            }
        }


        // These are displayed as key-value pairs in UI. For .NET service publish, the following pubxml elements are displayed:
        // - SiteUrlToLaunchAfterPublish
        // - ResourceGroup
        // - LastUsedBuildConfiguration
        // - UserName
        // - EncryptedPassword
        public IReadOnlyList<KeyValuePair<string, object>> SummaryEntries { get; } = new List<KeyValuePair<string, object>>();

        // .NET publish sets:
        // - PublishProvider
        // - PublishMethod
        // - ResourceId
        //
        // TODO: figure out what exactly this does, how it's tracked, and whether we can use
        // a custom property name here to distinguish Python publish. 
        public IReadOnlyDictionary<string, object> TelemetryData { get; } = new Dictionary<string, object>();

        public bool IsReadyToPublish => true;

        public string InformationMessage => null;

        public void ConfigurePublishSteps(IPublishSteps publishSteps) {
        }
    }
}
