using Microsoft.VisualStudio.ApplicationCapabilities.Publish.Contracts;

namespace Microsoft.PythonTools.Publishing {
    internal class PythonPublishUserInterface : IPublishUserInterface {
        public string PublishTitleText => "Python";

        public string PublishProfileHeadingText => "Header";

        public string PublishOptionsLinkText => "OptionsLink";

        public string PublishOptionsLinkUrl => "http://localhost";
    }
}
