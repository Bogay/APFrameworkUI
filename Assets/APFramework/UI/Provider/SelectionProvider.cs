using System.Collections.Generic;
using ChosenConcept.APFramework.UI.Element;
using ChosenConcept.APFramework.UI.Layout;
using ChosenConcept.APFramework.UI.Menu;
using Godot;

namespace ChosenConcept.APFramework.UI.Provider
{
    [GlobalClass]
    public partial class SelectionProvider : SimpleMenu
    {
        LayoutAlignment _layout;
        IMenuInputTarget _target;
        int _currentChoice;
        public bool active => _displayActive;

        public SelectionProvider() : base("SelectionProvider")
        {
            _layout = InitNewLayout();
        }

        void UpdateChoices(List<string> choices)
        {
            ClearWindow(true);
            foreach (string choice in choices)
            {
                ButtonUI button = AddButton(choice);
                button.SetLabel(new StringLabel(choice));
                button.SetAction(CompleteInput);
                GD.Print($"{Name}: Added choice: {choice}");
            }
        }

        public void GetSelection(IMenuInputTarget target, List<string> choices, int currentChoice)
        {
            UpdateChoices(choices);
            // take away the input from the sourceUI
            _target = target;
            OpenMenu();
            ForceUpdateDisplayContent();
            // currentWindow?.SetFocus(false);
            currentSelectable?.SetFocus(false);
            _currentChoice = currentChoice;
            // _currentSelection[0] = _currentChoice;
            // _currentSelection[1] = 0;
            currentSelectable?.SetFocus(true);
            // currentWindow?.SetFocus(true);
        }

        void CompleteInput()
        {
            WindowManager.instance.EndSelectionInput();
            CloseMenu();
            // _target.SetSelection(_currentSelection[0]);
            _target = null;
        }

        // protected override bool CancelOut()
        // {
        //     currentWindow?.SetFocus(false);
        //     currentSelectable?.SetFocus(false);
        //     _currentSelection[0] = _currentChoice;
        //     _currentSelection[1] = 0;
        //     currentSelectable?.SetFocus(true);
        //     currentWindow?.SetFocus(true);
        //     CompleteInput();
        //     return true;
        // }
    }
}