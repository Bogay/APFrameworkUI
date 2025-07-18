using ChosenConcept.APFramework.UI.Menu;
using Godot;
using System;
using GodotInput = Godot.Input;

namespace ChosenConcept.APFramework.UI.Input
{
    [GlobalClass]
    public partial class GodotInputProvider : Node, IInputProvider
    {
        [Export] bool _inputEnabled;
        Vector2 _lastMovement;
        Vector2 _mouseDelta;
        Vector2 _lastMousePosition;

        public bool hasMouse => true; // Godot always has mouse support
        public Vector2 mouseDelta => _mouseDelta;
        public Vector2 mousePosition => GetViewport().GetMousePosition();
        public bool inputEnabled => _inputEnabled;
        IMenuInputTarget _activeTarget;

        void IInputProvider.SetTarget(IMenuInputTarget target)
        {
            _activeTarget = target;
        }

        void IInputProvider.EnableInput(bool enable)
        {
            _inputEnabled = enable;
        }

        void IInputProvider.Update()
        {
            if (!_inputEnabled)
                return;

            Vector2 movement = Vector2.Zero;

            // Handle keyboard input
            if (GodotInput.IsActionPressed("ui_up"))
                movement.Y = 1.0f;
            if (GodotInput.IsActionPressed("ui_down"))
                movement.Y = -1.0f;
            if (GodotInput.IsActionPressed("ui_left"))
                movement.X = -1.0f;
            if (GodotInput.IsActionPressed("ui_right"))
                movement.X = 1.0f;

            // Handle gamepad input (if available)
            Vector2 leftStick = GodotInput.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            if (leftStick.LengthSquared() > movement.LengthSquared())
                movement = leftStick;

            // Handle confirm/cancel input
            if (GodotInput.IsActionJustPressed("ui_accept"))
            {
                GD.Print($"{Name}: OnConfirm");
                _activeTarget?.OnConfirm();
            }

            if (GodotInput.IsActionJustPressed("ui_cancel"))
            {
                GD.Print($"{Name}: OnCancel");
                _activeTarget?.OnCancel();
            }

            // Handle mouse input
            Vector2 currentMousePosition = GetViewport().GetMousePosition();
            _mouseDelta = currentMousePosition - _lastMousePosition;
            _lastMousePosition = currentMousePosition;

            if (
                // GodotInput.IsActionJustPressed("mouse_left") ||
                GodotInput.IsMouseButtonPressed(MouseButton.Left))
                _activeTarget?.OnMouseConfirmPressed();
            if (
                // GodotInput.IsActionJustReleased("mouse_left") ||
                !GodotInput.IsMouseButtonPressed(MouseButton.Left))
                _activeTarget?.OnMouseConfirmReleased();
            if (
                // GodotInput.IsActionJustPressed("mouse_right") ||
                GodotInput.IsMouseButtonPressed(MouseButton.Right))
                _activeTarget?.OnMouseCancel();

            // Handle scroll input
            Vector2 scrollDelta = Vector2.Zero;
            if (GodotInput.IsActionJustPressed("ui_text_scroll_up"))
                scrollDelta.Y = 1.0f;
            if (GodotInput.IsActionJustPressed("ui_text_scroll_down"))
                scrollDelta.Y = -1.0f;

            if (scrollDelta.LengthSquared() > 0)
                _activeTarget?.OnScroll(scrollDelta);

            // Handle movement changes
            if (_lastMovement != movement)
            {
                GD.Print($"{Name}: OnMove {movement}");
                if (movement.Length() > 0.5f)
                {
                    _activeTarget?.OnMove(movement);
                }
                else
                {
                    _activeTarget?.OnMove(Vector2.Zero);
                }
                _lastMovement = movement;
            }
        }
    }
}
