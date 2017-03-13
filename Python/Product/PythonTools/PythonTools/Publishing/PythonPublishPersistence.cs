using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.PythonTools.Infrastructure;
using Microsoft.PythonTools.Project;
using Microsoft.VisualStudio.ApplicationCapabilities.Publish.Contracts;

namespace Microsoft.PythonTools.Publishing {
    internal class PythonPublishPersistence : IPublishPersistence {
        /// <summary>
        /// Extension of the profile file that will be loaded and persisted in the profiles folder.
        /// </summary>
        public const string ProfileFileExtension = ".pubxml";

        /// <summary>
        /// Extension of the user file that will be loaded and persisted in the profiles folder.
        /// </summary>
        public const string ProfileUserFileExtension = ".pubxml.user";

        public const string LastSelectedProfileIdProperty = "LastSelectedProfileId";

        /// <summary>
        /// Location relative to the root of the current project where profiles will be persisted to and loaded from.
        /// </summary>
        public const string RelativePathFromProjectToProfileFolder = @"Properties\PublishProfiles";

        private readonly PythonProjectNode _projectNode;

        public PythonPublishPersistence(PythonProjectNode projectNode) {
            _projectNode = projectNode;
        }

        private string ProfileFolder =>
            Path.Combine(_projectNode.ProjectFolder, RelativePathFromProjectToProfileFolder);

        private string GetProfilePath(string id, bool isPrivate) =>
            Path.Combine(ProfileFolder, id + (isPrivate ? ProfileUserFileExtension : ProfileFileExtension));
        
        public string LastSelectedProfileId {
            get => _projectNode.GetMSBuildProjectInstance().GetPropertyValue(LastSelectedProfileIdProperty);
            set => _projectNode.GetMSBuildProjectInstance().SetProperty(LastSelectedProfileIdProperty, value);
        }

        public bool DeleteProfile(string id) {
            string profile = GetProfilePath(id, isPrivate: true);
            string userProfile = GetProfilePath(id, isPrivate: false);

            try {
                File.Delete(profile);
                File.Delete(userProfile);
            } catch (Exception ex) when (!ex.IsCriticalException()) {
                return false;
            }

            return true;
        }

        public IList<string> GetAllProfileIds() {
            var profileDir = new DirectoryInfo(ProfileFolder);
            if (!profileDir.Exists) {
                return new string[0];
            }

            IEnumerable<FileInfo> profileFiles;
            try {
                profileFiles = profileDir.EnumerateFiles("*" + ProfileFileExtension);
            } catch (IOException) {
                return new string[0];
            }

            return profileFiles.Select(file => Path.GetFileNameWithoutExtension(file.Name)).ToList();
        }

        public string GetNewProfileId(string profileName) => profileName;

        public string GetProfileName(string id) => id;

        public Stream GetProfile(string id, bool isPrivate) =>
            new FileStream(GetProfilePath(id, isPrivate), FileMode.Open, FileAccess.Read, FileShare.None);


        public string GetUniqueProfileName(string baseProfileName) {
            // Try baseProfileName as is first, and then append numeric suffixes starting from 1 and counting up.
            var suffixes = new[] { "" }.Concat(Enumerable.Range(1, int.MaxValue).Select(i => i.ToString()));
            return (from suffix in suffixes
                    let id = baseProfileName + suffix
                    let path = GetProfilePath(id, isPrivate: false)
                    where !File.Exists(path) && !Directory.Exists(path)
                    select id
                   ).First();
        }

        public void SetProfile(string id, Stream profile, bool isPrivate) {
            if (!Directory.Exists(ProfileFolder)) {
                Directory.CreateDirectory(ProfileFolder);
            }

            using (var stream = new FileStream(GetProfilePath(id, isPrivate), FileMode.Create, FileAccess.Write, FileShare.None)) {
                profile.CopyTo(stream);
            }
        }
    }
}
