using System;
using Godot;

namespace ChosenConcept.APFramework.UI.Window
{
    [GlobalClass]
    [System.Serializable]
    public partial class WindowSetup : Resource
    {
        [Export] int _width;
        [Export] int _height;
        [Export] WindowTitleStyle _titleStyle;
        [Export] WindowOutlineStyle _outlineStyle;
        [Export] WindowOutlineDisplayStyle _outlineDisplayStyle;
        [Export] WindowThickenStyle _thickenStyle;
        [Export] WindowLabelStyle _labelStyle;
        [Export] WindowTransition _transitionIn;
        [Export] WindowTransition _transitionOut;
        [Export] float _fontSize;
        [Export] Color _backgroundColor;
        [Export] float _functionStringUpdateInterval;
        [Export] bool _syncActiveValueAutomatically;
        public bool syncActiveValueAutomatically => _syncActiveValueAutomatically;
        public float functionStringUpdateInterval => _functionStringUpdateInterval;
        public int width => _width;
        public int height => _height;
        public WindowTitleStyle titleStyle => _titleStyle;
        public WindowOutlineStyle outlineStyle => _outlineStyle;
        public WindowOutlineDisplayStyle outlineDisplayStyle => _outlineDisplayStyle;
        public WindowThickenStyle thickenStyle => _thickenStyle;
        public WindowLabelStyle labelStyle => _labelStyle;
        public WindowTransition transitionIn => _transitionIn;
        public WindowTransition transitionOut => _transitionOut;
        public float fontSize => _fontSize;
        public Color backgroundColor => _backgroundColor;

        public static WindowSetup defaultSetup => new()
        {
            _width = 0,
            _height = 0,
            _titleStyle = WindowTitleStyle.None,
            _outlineStyle = WindowOutlineStyle.CornerOnly,
            _outlineDisplayStyle = WindowOutlineDisplayStyle.WhenSelected,
            _thickenStyle = WindowThickenStyle.None,
            _labelStyle = WindowLabelStyle.None,
            _transitionIn = WindowTransition.Glitch,
            _transitionOut = WindowTransition.Glitch,
            _fontSize = 30,
            _backgroundColor = Color.Color8(0, 0, 0, 0),
            _functionStringUpdateInterval = 0,
            _syncActiveValueAutomatically = true
        };

        public WindowSetup() { }

        public WindowSetup(WindowSetup setup)
        {
            _width = setup._width;
            _height = setup._height;
            _titleStyle = setup._titleStyle;
            _outlineStyle = setup._outlineStyle;
            _outlineDisplayStyle = setup._outlineDisplayStyle;
            _thickenStyle = setup._thickenStyle;
            _labelStyle = setup._labelStyle;
            _transitionIn = setup._transitionIn;
            _transitionOut = setup._transitionOut;
            _fontSize = setup._fontSize;
            _backgroundColor = setup._backgroundColor;
            _functionStringUpdateInterval = setup.functionStringUpdateInterval;
            _syncActiveValueAutomatically = setup.syncActiveValueAutomatically;
        }

        public WindowSetup SetSize(int w, int h = 0)
        {
            _width = w;
            _height = h;
            return this;
        }

        public WindowSetup SetWidth(int targetWidth)
        {
            _width = targetWidth;
            return this;
        }

        public WindowSetup SetHeight(int targetHeight)
        {
            _height = targetHeight;
            return this;
        }

        public WindowSetup SetTitleStyle(WindowTitleStyle style)
        {
            _titleStyle = style;
            return this;
        }

        public WindowSetup SetFontSize(float font)
        {
            _fontSize = font;
            return this;
        }

        public WindowSetup SetOutlineDisplayStyle(WindowOutlineDisplayStyle style)
        {
            _outlineDisplayStyle = style;
            return this;
        }

        public WindowSetup SetThickenStyle(WindowThickenStyle style)
        {
            _thickenStyle = style;
            return this;
        }

        public WindowSetup SetLabelStyle(WindowLabelStyle style)
        {
            _labelStyle = style;
            return this;
        }

        public WindowSetup SetTransitionIn(WindowTransition transition)
        {
            _transitionIn = transition;
            return this;
        }

        public WindowSetup SetTransitionOut(WindowTransition transition)
        {
            _transitionOut = transition;
            return this;
        }

        public WindowSetup SetStringUpdateInterval(float updateInterval)
        {
            _functionStringUpdateInterval = updateInterval;
            return this;
        }

        public WindowSetup SetUpdateActiveValue(bool value)
        {
            _syncActiveValueAutomatically = value;
            return this;
        }
    }
}