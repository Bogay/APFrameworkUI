
using ChosenConcept.APFramework.UI.Layout;
using ChosenConcept.APFramework.UI.Window;
using Godot;

namespace ChosenConcept.APFramework.UI.Menu
{
    [System.Serializable]
    [GlobalClass]
    public partial class MenuStyling : Resource
    {
        [Export] WindowSetup _windowSetup;
        [Export] LayoutSetup _layoutSetup;
        public WindowSetup windowSetup => _windowSetup;
        public LayoutSetup layoutSetup => _layoutSetup;

        public static MenuStyling defaultStyling => new()
        {
            _windowSetup = WindowSetup.defaultSetup,
            _layoutSetup = LayoutSetup.defaultLayout,
        };

        public MenuStyling SetWindowSetup(WindowSetup windowSetup)
        {
            _windowSetup = windowSetup;
            return this;
        }

        public MenuStyling SetLayoutSetup(LayoutSetup layoutSetup)
        {
            _layoutSetup = layoutSetup;
            return this;
        }
    }
}