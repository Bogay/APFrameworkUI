using ChosenConcept.APFramework.UI.Utility;
using ChosenConcept.APFramework.UI.Window;
using Godot;

namespace ChosenConcept.APFramework.UI.Element
{
    [GlobalClass]
    public partial class TextUI : WindowElement
    {
        public TextUI(string name, WindowUI parent) : base(name, parent)
        {
        }
        public override string displayText => _available switch
        {
            true => formattedContent,
            false => StyleUtility.StringColored(formattedContent, StyleUtility.disabled)
        };
    }
}