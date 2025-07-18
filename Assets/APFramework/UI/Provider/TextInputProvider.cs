using ChosenConcept.APFramework.UI.Element;
using ChosenConcept.APFramework.UI.Menu;
using Godot;
using GodotInput = Godot.Input;

namespace ChosenConcept.APFramework.UI.Provider
{
    [GlobalClass]
    public partial class TextInputProvider : Node, IMenuInputTarget
    {
        [Export] LineEdit _inputField;
        IMenuInputTarget _target;
        TextInputUI _textInputUI;
        string _originalText = string.Empty;
        bool _active = false;
        public bool active => _active;

        public void GetTextInput(IMenuInputTarget sourceUI, TextInputUI text)
        {
            _target = sourceUI;
            GetTextInput(text);
        }

        public void GetTextInput(TextInputUI text)
        {
            _active = true;
            _textInputUI = text;
            _originalText = _textInputUI.inputContent;
            // take away the input from the sourceUI
            WindowManager.instance.LinkInputTarget(null);

            // show the text UI
            _inputField.Text = text.inputContent;
            _inputField.Visible = true;
            _inputField.TextChanged += OnValueChanged;
            _inputField.TextSubmitted += OnSubmit;

            // focus on Godot's text UI
            _inputField.GrabFocus();
            _textInputUI.SetCaretPosition(_inputField.CaretColumn);
#if AUTOPANIC_STEAMWORK
        bool success = GameContext.platform.steam.ShowGamepadTextInput(this,
            GameContext.localization.GetLocalizedValue(text.rawContent),
            text.inputContent);
        if (success)
            return;
#endif
            WindowManager.instance.LinkInputTarget(this);
        }

        // XXX: to don't use Update
        public override void _Process(double delta)
        {
            if (_textInputUI == null)
                return;
            _textInputUI.SetCaretPosition(_inputField.CaretColumn);
            // This naive solution is required because InputSystem isn't triggered properly
            if (GodotInput.IsActionJustPressed("ui_tab"))
                TriggerAutoComplete();
        }

        void OnSubmit(string arg0)
        {
            CompleteInput();
        }

        void OnValueChanged(string value)
        {
            _textInputUI.SetActiveInputContent(value);
            _textInputUI.SetCaretPosition(_inputField.CaretColumn);
            _textInputUI.SetSelectionRange(0, 0);
        }

        void CompleteInput()
        {
            _active = false;
            _inputField.TextChanged -= OnValueChanged;
            _inputField.TextSubmitted -= OnSubmit;

            // remove focus from Godot's text UI
            _inputField.ReleaseFocus();

            // close the text UI
            _inputField.Visible = false;

            // give back the input to the target
            WindowManager.instance.LinkInputTarget(null);
            _target.SetTextInput(_inputField.Text);
            _target = null;
            _textInputUI = null;
        }

        public void SetTextAndConfirm(string submittedText)
        {
            _inputField.Text = submittedText;
            _textInputUI.SetActiveInputContent(submittedText);
            _textInputUI.SetCaretPosition(_inputField.CaretColumn);
            _textInputUI.SetSelectionRange(0, 0);
            CompleteInput();
        }

        public void CancelInput()
        {
            SetTextAndConfirm(_originalText);
        }

        void TriggerAutoComplete()
        {
            if (_textInputUI.TriggerAutoComplete())
            {
                _inputField.Text = _textInputUI.inputContent;
                _inputField.CaretColumn = _inputField.Text.Length;
            }
        }


        void IMenuInputTarget.OnConfirm()
        {
        }

        void IMenuInputTarget.OnCancel()
        {
            CompleteInput();
        }

        void IMenuInputTarget.OnMove(Vector2 move)
        {
        }

        void IMenuInputTarget.OnScroll(Vector2 scroll)
        {
        }

        void IMenuInputTarget.OnMouseConfirmPressed()
        {
        }

        void IMenuInputTarget.OnMouseConfirmReleased()
        {
        }

        void IMenuInputTarget.OnMouseCancel()
        {
            CompleteInput();
        }

        void IMenuInputTarget.OnKeyboardEscape()
        {
            CompleteInput();
        }

        void IMenuInputTarget.SetSelection(int i)
        {
        }

        void IMenuInputTarget.SetTextInput(string inputFieldText)
        {
        }
    }
}