using ChosenConcept.APFramework.UI.Menu;
using Godot;
using System;

namespace ChosenConcept.APFramework.UI.Input
{
    public partial class GodotInputProvider : Node, IInputProvider
    {
        bool _inputEnabled;
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
            if (Input.IsActionPressed("move_up") || Input.IsKeyPressed(Key.W))
                movement.Y = 1.0f;
            if (Input.IsActionPressed("move_down") || Input.IsKeyPressed(Key.S))
                movement.Y = -1.0f;
            if (Input.IsActionPressed("move_left") || Input.IsKeyPressed(Key.A))
                movement.X = -1.0f;
            if (Input.IsActionPressed("move_right") || Input.IsKeyPressed(Key.D))
                movement.X = 1.0f;

            // Handle gamepad input (if available)
            Vector2 leftStick = Input.GetVector("move_left", "move_right", "move_up", "move_down");
            if (leftStick.LengthSquared() > movement.LengthSquared())
                movement = leftStick;

            // Handle confirm/cancel input
            if (Input.IsActionJustPressed("ui_accept") || Input.IsKeyPressed(Key.Space) || Input.IsKeyPressed(Key.Enter))
                _activeTarget?.OnConfirm();

            if (Input.IsActionJustPressed("ui_cancel") || Input.IsKeyPressed(Key.Escape))
                _activeTarget?.OnCancel();

            // Handle mouse input
            Vector2 currentMousePosition = GetViewport().GetMousePosition();
            _mouseDelta = currentMousePosition - _lastMousePosition;
            _lastMousePosition = currentMousePosition;

            if (Input.IsActionJustPressed("mouse_left") || Input.IsMouseButtonPressed(MouseButton.Left))
                _activeTarget?.OnMouseConfirmPressed();
            if (Input.IsActionJustReleased("mouse_left") || !Input.IsMouseButtonPressed(MouseButton.Left))
                _activeTarget?.OnMouseConfirmReleased();
            if (Input.IsActionJustPressed("mouse_right") || Input.IsMouseButtonPressed(MouseButton.Right))
                _activeTarget?.OnMouseCancel();

            // Handle scroll input
            Vector2 scrollDelta = Vector2.Zero;
            if (Input.IsActionJustPressed("scroll_up"))
                scrollDelta.Y = 1.0f;
            if (Input.IsActionJustPressed("scroll_down"))
                scrollDelta.Y = -1.0f;

            if (scrollDelta.LengthSquared() > 0)
                _activeTarget?.OnScroll(scrollDelta);

            // Handle movement changes
            if (_lastMovement != movement)
            {
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
