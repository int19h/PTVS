using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ApplicationCapabilities.Publish.Contracts;

namespace Microsoft.PythonTools.Publishing {
    [Export(typeof(IProfileVisualFactoryProvider))]
    internal class PythonAppServiceProfileVisualFactoryProvider : IProfileVisualFactoryProvider {
        public IProfileVisualFactory CreateFactory(IPublishServiceProvider provider) =>
            new PythonAppServiceProfileVisualFactory(provider);
    }
}
