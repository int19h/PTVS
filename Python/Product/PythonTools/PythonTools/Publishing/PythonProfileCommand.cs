using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ApplicationCapabilities.Publish.Contracts;

namespace Microsoft.PythonTools.Publishing {
    internal class PythonProfileCommand : IProfileCommand {
        public PythonProfileCommand(string linkText, string toolTip, Action linkClickAction) {
            LinkText = linkText;
            ToolTip = toolTip;
            LinkClickAction = linkClickAction;
        }

        public string LinkText { get; }

        public string ToolTip { get; }

        public Action LinkClickAction { get; }

        public bool IsEnabled => true;

        public bool IsVisible => true;
    }
}
