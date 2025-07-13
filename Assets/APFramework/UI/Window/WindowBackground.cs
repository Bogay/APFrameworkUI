using Godot;

namespace ChosenConcept.APFramework.UI.Window
{
    public partial class WindowBackground : Node
    {
        [Export] TextureRect _background;
        [Export] Color _bgColor = Colors.Transparent;
        public TextureRect background => _background;

        internal void SetColor(Color color)
        {
            _bgColor = color;
        }

        public void SetColor(Color color, bool active)
        {
            _bgColor = color;
            if (!active)
                return;
            _background.Modulate = _bgColor;
        }

        internal void SetActive(bool v)
        {
            if (_bgColor == Colors.Transparent)
                return;
            _background.Modulate = v ? _bgColor : Colors.Transparent;
        }
    }
}