using System;
using System.Collections.Generic;
using ChosenConcept.APFramework.UI.Window;
using Godot;

namespace ChosenConcept.APFramework.UI.Layout
{
    [GlobalClass]
    public partial class LayoutAlignment : BoxContainer
    {
        [Export] Container _layoutGroup;
        [Export] LayoutSetup _layoutSetup;
        [Export] Godot.Collections.Array<WindowUI> _windows = new();
        static Vector2I _referenceResolution = new(1920, 1080);

        public void Initialize(Container layoutGroup, LayoutSetup layoutSetup)
        {
            _layoutGroup = layoutGroup;
            _layoutSetup = layoutSetup;
            UpdateLayout();
        }

        public void UpdateLayout()
        {
            Vertical = _layoutSetup.windowDirection == WindowDirection.Vertical;
            if (_layoutSetup.offsetSource == OffsetSource.CenterOfScreen)
            {
                // consider the content size of all windows
                float accumulatedWidth = 0;
                float accumulatedHeight = 0;
                if (_layoutSetup.windowDirection == WindowDirection.Horizontal)
                {
                    foreach (WindowUI window in _windows)
                    {
                        accumulatedWidth += window.layout.CustomMinimumSize.X;

                        if (window.layout.CustomMinimumSize.Y > accumulatedHeight)
                            accumulatedHeight = window.layout.CustomMinimumSize.Y;
                    }
                }
                else
                {
                    foreach (WindowUI window in _windows)
                    {
                        if (window.layout.CustomMinimumSize.X > accumulatedWidth)
                            accumulatedWidth = window.layout.CustomMinimumSize.X;

                        accumulatedHeight += window.layout.CustomMinimumSize.Y;
                    }
                }

                Vector2I screenSize = DisplayServer.WindowGetSize();
                float referenceMultiplier = screenSize.Y / (float)_referenceResolution.Y;
                float ratio = screenSize.X / (float)screenSize.Y;
                int width = 0;
                int height = 0;

                // Set margins based on alignment (Godot uses margin_* properties)
                if (_layoutGroup is MarginContainer marginContainer)
                {
                    int topMargin = _layoutSetup.windowAlignment switch
                    {
                        WindowAlignment.UpperLeft or
                            WindowAlignment.UpperCenter or
                            WindowAlignment.UpperRight => _referenceResolution.Y / 2 - height,
                        _ => 0
                    };

                    int bottomMargin = _layoutSetup.windowAlignment switch
                    {
                        WindowAlignment.LowerLeft or
                            WindowAlignment.LowerCenter or
                            WindowAlignment.LowerRight => _referenceResolution.Y / 2 - height,
                        _ => 0
                    };

                    int leftMargin = _layoutSetup.windowAlignment switch
                    {
                        WindowAlignment.UpperLeft or
                            WindowAlignment.MiddleLeft or
                            WindowAlignment.LowerLeft => (int)(_referenceResolution.Y * ratio / 2 - width),
                        _ => 0
                    };

                    int rightMargin = _layoutSetup.windowAlignment switch
                    {
                        WindowAlignment.UpperRight or
                            WindowAlignment.MiddleRight or
                            WindowAlignment.LowerRight => (int)(_referenceResolution.Y * ratio / 2 - width),
                        _ => 0
                    };

                    marginContainer.AddThemeConstantOverride("margin_top", topMargin);
                    GD.Print($"{Name}: Set top margin: {topMargin}");
                    marginContainer.AddThemeConstantOverride("margin_bottom", bottomMargin);
                    GD.Print($"{Name}: Set bottom margin: {bottomMargin}");
                    marginContainer.AddThemeConstantOverride("margin_left", leftMargin);
                    GD.Print($"{Name}: Set left margin: {leftMargin}");
                    marginContainer.AddThemeConstantOverride("margin_right", rightMargin);
                    GD.Print($"{Name}: Set right margin: {rightMargin}");

                    Vector2 multiplier = _layoutSetup.offsetType switch
                    {
                        OffsetType.Percentage => _referenceResolution / 2,
                        OffsetType.Pixel => Vector2.One * referenceMultiplier,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    if (_layoutSetup.offset.X > 0)
                        marginContainer.AddThemeConstantOverride("margin_top", topMargin - (int)(_layoutSetup.offset.X * multiplier.Y));
                    if (_layoutSetup.offset.Y > 0)
                        marginContainer.AddThemeConstantOverride("margin_bottom", bottomMargin - (int)(_layoutSetup.offset.Y * multiplier.Y));
                    if (_layoutSetup.offset.Z > 0)
                        marginContainer.AddThemeConstantOverride("margin_left", leftMargin - (int)(_layoutSetup.offset.Z * multiplier.X));
                    if (_layoutSetup.offset.W > 0)
                        marginContainer.AddThemeConstantOverride("margin_right", rightMargin - (int)(_layoutSetup.offset.W * multiplier.X));
                }
            }
            else
            {
                Vector2I screenSize = DisplayServer.WindowGetSize();
                float referenceMultiplier = screenSize.Y / (float)_referenceResolution.Y;
                Vector2 multiplier = _layoutSetup.offsetType switch
                {
                    OffsetType.Percentage => _referenceResolution / 2,
                    OffsetType.Pixel => Vector2.One * referenceMultiplier,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (_layoutGroup is MarginContainer marginContainer)
                {
                    marginContainer.AddThemeConstantOverride("margin_top", (int)(_layoutSetup.offset.X * multiplier.Y));
                    marginContainer.AddThemeConstantOverride("margin_bottom", (int)(_layoutSetup.offset.Y * multiplier.Y));
                    marginContainer.AddThemeConstantOverride("margin_left", (int)(_layoutSetup.offset.Z * multiplier.X));
                    marginContainer.AddThemeConstantOverride("margin_right", (int)(_layoutSetup.offset.W * multiplier.X));
                }
            }

            // Set separation for box containers
            if (_layoutGroup is BoxContainer boxContainer)
            {
                boxContainer.AddThemeConstantOverride("separation", _layoutSetup.spacing);
            }

            // Force layout update
            _layoutGroup.QueueRedraw();
        }

        public void RegisterWindow(WindowUI window)
        {
            _windows.Add(window);
        }
        public void UnregisterWindow(WindowUI window)
        {
            _windows.Remove(window);
        }
        public void ContextResolutionChange()
        {
            UpdateLayout();
        }
        public void MoveWindowToIndex(WindowUI window, int index)
        {
            _layoutGroup.MoveChild(window, index);
        }
    }
}
