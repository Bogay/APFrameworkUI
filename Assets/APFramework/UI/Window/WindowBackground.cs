using Godot;

namespace ChosenConcept.APFramework.UI.Window
{
    [GlobalClass]
    public partial class WindowBackground : ColorRect
    {
        [Export] Color _bgColor = Colors.Transparent;

        public void SetColor(Color color, bool active)
        {
            _bgColor = color;
            if (!active)
                return;
            Modulate = _bgColor;
        }

        internal void SetActive(bool v)
        {
            if (_bgColor == Colors.Transparent)
                return;
            Modulate = v ? _bgColor : Colors.Transparent;
        }
    }
}