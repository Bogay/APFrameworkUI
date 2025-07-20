using System;
using System.Collections.Generic;
using ChosenConcept.APFramework.UI.Element;
using ChosenConcept.APFramework.UI.Layout;
using ChosenConcept.APFramework.UI.Window;
using Godot;

namespace ChosenConcept.APFramework.UI.Menu
{
    [Serializable]
    [GlobalClass]
    public partial class SimpleMenu : Control, IMenuInputTarget
    {
        [Export]
        string _menuName;

        [Export] MenuSetup _menuSetup;
        [Export] MenuStyling _menuStyling;
        [Export] WindowUI _windowInstance;
        [Export] LayoutAlignment _layoutAlignmentInstance;
        [Export] protected bool _displayActive;
        [Export] bool _navigationActive;
        [Export] ulong _linkFrame = ulong.MaxValue;
        [Export] float _nextNavigationUpdate = Mathf.Inf;
        [Export] int _currentSelection = -1;
        [Export] float _holdStart = Mathf.Inf;
        [Export] float _holdNavigationNext = Mathf.Inf;
        [Export] Vector2 _move = Vector2.Zero;
        [Export] Vector2 _mouseScroll = Vector2.Zero;
        [Export] Vector2 _lastMousePosition = -Vector2.Inf;
        [Export] bool _mouseSelectionTargetExists;
        [Export] bool _hoverOnDecrease;
        [Export] bool _hoverOnIncrease;
        [Export] bool _inElementInputMode;
        [Export] bool _selectionUpdated;
        [Export] bool _focused;
        [Export] bool _movingWindow;
        Action _menuCloseAction;

        public bool canBeClosedByOutOfFocusClick =>
            _menuSetup.allowCloseOnClick is MenuCloseOnClickBehavior.Both
                or MenuCloseOnClickBehavior.OutOfFocus;

        public bool movingWindow => _movingWindow;
        bool windowPositionCached => _windowInstance.positionCached;

        public WindowElement currentSelectable
        {
            get
            {
                if (GetSelectable(_currentSelection) == null)
                    return null;
                return GetSelectable(_currentSelection);
            }
        }

        public string menuTag => $"Interface.{_menuName}";
        public bool isDisplayActive => _displayActive;
        public bool isNavigationActive => _navigationActive;
        public bool canNavigate => _windowInstance.canNavigate;
        public WindowUI windowInstance => _windowInstance;
        public bool focused => _focused;
        public bool MouseSelectionTargetExists => _mouseSelectionTargetExists;
        public bool inElementInputMode => _inElementInputMode;

        public SimpleMenu(string name)
        {
            _menuName = name;
            _menuSetup = MenuSetup.defaultSetup;
            _menuStyling = MenuStyling.defaultStyling;
            InitNewLayout();
        }

        public SimpleMenu(string name, MenuSetup menuSetup)
        {
            _menuName = name;
            _menuSetup = menuSetup;
            _menuStyling = MenuStyling.defaultStyling;
        }

        public SimpleMenu(string name, MenuSetup menuSetup, MenuStyling menuStyling)
        {
            _menuName = name;
            _menuSetup = menuSetup;
            _menuStyling = menuStyling;
        }

        public SimpleMenu(string name, MenuSetup menuSetup, WindowSetup windowSetup, LayoutSetup layoutSetup)
        {
            _menuName = name;
            _menuSetup = menuSetup;
            _menuStyling = MenuStyling.defaultStyling;
            _menuStyling.SetWindowSetup(windowSetup);
            _menuStyling.SetLayoutSetup(layoutSetup);
        }

        public SimpleMenu(string name, MenuSetup menuSetup, WindowSetup windowSetup, LayoutAlignment layoutAlignment)
        {
            _menuName = name;
            _menuSetup = menuSetup;
            _menuStyling = MenuStyling.defaultStyling;
            _menuStyling.SetWindowSetup(windowSetup);
            _layoutAlignmentInstance = layoutAlignment;
        }

        public bool IsMouseInWindow(Vector2 mousePosition)
        {
            return _windowInstance.ContainsPosition(mousePosition);
        }

        public bool ExistsSelectable(int i) => _windowInstance.interactables.Count > i && i >= 0;

        public WindowElement GetSelectable(int i) =>
            i >= 0 && i < _windowInstance.interactables.Count ? _windowInstance.interactables[i] : null;

        public void UpdateMenu()
        {
            if (!_displayActive)
                return;
            if (!_navigationActive && (this.currentTime()) >= _nextNavigationUpdate)
                _navigationActive = true;
            UpdateNavigation();
        }

        void UpdateNavigation()
        {
            if (!_windowInstance.canNavigate || _linkFrame == Engine.GetProcessFrames())
                return;
#if AUTOPANIC_GODOT_MOUSE_ENABLED
            if (_movingWindow)
            {
                _windowInstance.Move(WindowManager.instance.inputProvider.mouseDelta);
                return;
            }
#endif

            UpdateMouseNavigation();
            var currentTime = this.currentTime();
            if (currentTime < _nextNavigationUpdate)
            {
                // GD.Print($"{Name}: UpdateNavigation (skipped) - next update {_nextNavigationUpdate} / {currentTime}");
                return;
            }
            UpdateWindowPosition();
            if (float.IsPositiveInfinity(_holdStart) || _holdNavigationNext <= currentTime)
            {
                UpdateSelection();
            }

            ResetDirection();

            if (_selectionUpdated)
            {
                _selectionUpdated = false;
                OnSelectionUpdate();
            }
        }

        public void ForceUpdateDisplayContent()
        {
            _windowInstance.InvokeUpdate();
        }

        public void SetOpacity(float opacity)
        {
            _windowInstance.SetOpacity(opacity);
        }

        public LayoutAlignment InitNewLayout()
        {
            LayoutAlignment layout =
                WindowManager.instance.InstantiateLayout(_menuStyling.layoutSetup,
                    menuTag);
            _layoutAlignmentInstance = layout;
            return layout;
        }

        /// <summary>
        /// A simple method to spawn window
        /// </summary>
        public WindowUI NewWindow(string windowName)
        {
            if (_layoutAlignmentInstance == null)
                InitNewLayout();
            WindowUI window =
                WindowManager.instance.NewWindow(windowName, _layoutAlignmentInstance, _menuStyling.windowSetup,
                    menuTag);
            _windowInstance = window;
            return window;
        }

        /// <summary>
        /// A simple method to spawn single text UI element window
        /// </summary>
        public TextUI AddText(string elementName)
        {
            if (_windowInstance == null)
                NewWindow(elementName);
            return _windowInstance.AddText(elementName);
        }

        public void AddGap()
        {
            AddText("Blank")
                .SetLabel(new StringLabel("　"));
        }

        public void RemoveElement(WindowElement element)
        {
            element.Remove();
        }

        public void RemoveWindowsAndLayoutGroups()
        {
            ClearWindow(true);
            // with shared instance, it is possible that another menu destroys the layout
            if (_layoutAlignmentInstance != null)
            {
                _layoutAlignmentInstance.QueueFree();
                _layoutAlignmentInstance = null;
            }
        }

        public void ClearWindow(bool removeWindow)
        {
            ClearWindowLocation();
            _currentSelection = -1;
            _windowInstance?.ClearElements();
            if (removeWindow)
            {
                _windowInstance?.Close();
                _windowInstance = null;
            }
        }

        public void SetWindowLocalizedByTag()
        {
            _windowInstance.SetLocalizedByTag();
        }

        public void AutoResizeWindow(int extraWidth = 0, bool sizeFixed = false)
        {
            _windowInstance.AutoResize(extraWidth, sizeFixed);
        }

        public void Refresh()
        {
            _windowInstance.InvokeUpdate();
            _windowInstance.RefreshSize();
        }

        public void TriggerResolutionChange()
        {
            _layoutAlignmentInstance.ContextResolutionChange();
        }

        void UpdateMouseNavigation()
        {
#if !AUTOPANIC_GODOT_MOUSE_ENABLED
            return;
#endif

            if (!_displayActive || !WindowManager.instance.inputProvider.inputEnabled ||
                _lastMousePosition == WindowManager.instance.inputProvider.mousePosition ||
                !WindowManager.instance.inputProvider.hasMouse || _windowInstance == null
                || !_navigationActive)
            {
                return;
            }

            _lastMousePosition = WindowManager.instance.inputProvider.mousePosition;
            _mouseSelectionTargetExists = false;
            if (!_inElementInputMode)
            {
                // if the window is a single button window, perform window check only
                if (_windowInstance.isSingleButtonWindow)
                {
                    if (!IsMouseInWindow(_lastMousePosition))
                    {
                        SetFocused(false);
                        return;
                    }

                    _currentSelection = 0;
                    SetFocused(true);
                    _selectionUpdated = true;
                    _mouseSelectionTargetExists = true;
                }
                else
                {
                    if (!IsMouseInWindow(_lastMousePosition))
                    {
                        SetFocused(false);
                        return;
                    }

                    SetFocused(true);
                    for (int i = 0; i < _windowInstance.interactables.Count; i++)
                    {
                        if (!_windowInstance.InteractableContainsPosition(i, _lastMousePosition))
                            continue;
                        if (i != _currentSelection)
                        {
                            _selectionUpdated = true;
                            currentSelectable?.SetFocus(false);
                            _currentSelection = i;
                            currentSelectable?.SetFocus(true);
                        }

                        _mouseSelectionTargetExists = true;
                        break;
                    }

                    if (!_mouseSelectionTargetExists)
                        ClearSelection();
                }
            }
            // if input mode is active
            else
            {
                if (currentSelectable is ISlider slider)
                {
                    (_hoverOnDecrease, _hoverOnIncrease) = slider.HoverOnArrow(_lastMousePosition);
                }

                if (currentSelectable is ScrollableTextUI scrollableTextUI)
                {
                    (_hoverOnDecrease, _hoverOnIncrease) = scrollableTextUI.HoverOnArrow(_lastMousePosition);
                }
            }
        }

        void UpdateWindowPosition()
        {
            if (!windowPositionCached)
            {
                _windowInstance.UpdateElementsAndWindowPosition();
            }
        }

        public void LinkInput()
        {
            GD.Print($"{Name}: Link input");
            _linkFrame = Engine.GetProcessFrames();
            ResetInput();
            WindowManager.instance.LinkInputTarget(this);
        }

        public void UnlinkInput()
        {
            ResetInput();
            WindowManager.instance.UnlinkInput(this);
        }

        void ResetInput()
        {
            _move = Vector2.Zero;
            _mouseScroll = Vector2.Zero;
        }

        public void ResetSelection()
        {
            ResetInput();
            foreach (WindowElement element in _windowInstance.interactables)
            {
                element.SetFocus(false);
            }

            _currentSelection = -1;
            if (GetSelectable(_currentSelection) == null && _windowInstance.canNavigate)
            {
                _currentSelection = 0;
            }

            GetSelectable(_currentSelection)?.SetFocus(true);
        }

        // Used to set all windows in unselected state
        public void ClearSelection()
        {
            foreach (WindowElement element in _windowInstance.interactables)
            {
                element.SetFocus(false);
            }

            _currentSelection = -1;
        }

        public void SetCurrentSelection(int x)
        {
            GetSelectable(_currentSelection)?.SetFocus(false);
            _currentSelection = x;
            GetSelectable(_currentSelection)?.SetFocus(true);
        }

        public void SetNavigationActive(bool active)
        {
            if (active)
            {
                LinkInput();
                DelayInput(_menuSetup.menuOpenInputDelay);
            }
            else
            {
                UnlinkInput();
                _nextNavigationUpdate = Mathf.Inf;
            }
        }

        public void ClearElementsFocus()
        {
            _windowInstance.ClearElementsFocus();
        }

        public void ClearWindowFocus()
        {
            _windowInstance.ClearWindowFocus();
        }

        void UpdateSelection()
        {
            // GD.Print($"{Name}: UpdateSelection @ {this.currentTime()}");
            if (!_navigationActive)
                return;

            bool mouseScrollOverride = false;
#if AUTOPANIC_GODOT_MOUSE_ENABLED
            if (_move.LengthSquared() < _mouseScroll.LengthSquared())
            {
                _move = _mouseScroll;
                mouseScrollOverride = true;
            }
#endif

            if (_move.Length() < .5f)
                return;
            // if all elements are deselected, reset to the first element first
            if (_currentSelection < 0)
            {
                ResetSelection();
            }
            else
            {
                UpdateSelectionByMovement(_move.Normalized());
                // Ignore when mouse scroll to prevent scrolling out of a window
                if (!_inElementInputMode && !_selectionUpdated && !mouseScrollOverride)
                {
                    GD.Print($"{Name}: UpdateSelection - checking closest directional match");
                    WindowManager.instance.CheckClosestDirectionalMatch(
                        this,
                        currentSelectable.cachedPosition.Item1,
                        _move.Normalized(),
                        _menuSetup.allowCycleBetweenWindows);
                }

                _move = Vector2.Zero;
            }

            if (mouseScrollOverride)
            {
                _move = Vector2.Zero;
                _mouseScroll = Vector2.Zero;
            }
        }

        void UpdateSelectionByMovement(Vector2 move)
        {
            if (!_inElementInputMode)
            {
                currentSelectable?.SetFocus(false);
                int yBefore = _currentSelection;
                int offset = Mathf.RoundToInt(move.Y) switch
                {
                    1 => -1,
                    -1 => 1,
                    _ => 0
                };
                _currentSelection += offset;
                int count = _windowInstance.interactables.Count;
                if (_menuSetup.allowCycleWithinWindow)
                    _currentSelection = _currentSelection >= 0 ? _currentSelection % count : count - 1;
                else
                    _currentSelection = Mathf.Clamp(_currentSelection, 0, count - 1);

                if (yBefore != _currentSelection)
                {
                    _selectionUpdated = true;
                }


                currentSelectable?.SetFocus(true);
            }
            else
            {
                int result = currentSelectable.count;
                int offset = Mathf.RoundToInt(move.X) switch
                {
                    1 => 1,
                    -1 => -1,
                    _ => 0
                };
                if (currentSelectable is ScrollableTextUI scrollableText)
                {
                    if (move.Y != 0)
                    {
                        offset = Mathf.RoundToInt(move.Y) switch
                        {
                            1 => -1,
                            -1 => 1,
                            _ => 0
                        };
                    }
                    else
                    {
                        offset *= scrollableText.contentHeight;
                    }
                }

                result += offset;
                currentSelectable.count = result;
                if (offset != 0 && currentSelectable.count == result)
                {
                    _selectionUpdated = true;
                    ClearWindowLocation();
                }
            }
        }

        void ResetDirection()
        {
            if (!_menuSetup.singlePressOnly && _move.LengthSquared() > 0)
            {
                if (float.IsPositiveInfinity(_holdStart))
                {
                    _holdStart = (this.currentTime());
                }
                else if ((this.currentTime()) - _holdStart >= _menuSetup.holdNavigationDelay &&
                         (float.IsPositiveInfinity(_holdNavigationNext) ||
                          (this.currentTime()) >= _holdNavigationNext))
                {
                    _holdNavigationNext = (this.currentTime()) + _menuSetup.holdNavigationInterval /
                        Mathf.CeilToInt(((this.currentTime()) - _holdStart - _menuSetup.holdNavigationDelay) /
                                        _menuSetup.holdNavigationSpeedUpInterval);
                }
            }
            else
            {
                ResetHold();
            }

            DelayInput(0.005f);
        }

        void ResetHold()
        {
            _holdStart = Mathf.Inf;
            _holdNavigationNext = Mathf.Inf;
            _move = Vector2.Zero;
        }

        public void RefocusAtNearestElement(Vector2 referenceLocation)
        {
            if (!_displayActive)
                return;
            if (!windowPositionCached)
                UpdateWindowPosition();
            float minDistance = Mathf.Inf;
            currentSelectable?.SetFocus(false);
            if (!_windowInstance.canNavigate)
                return;
            for (int i = 0; i < _windowInstance.interactables.Count; i++)
            {
                Vector2 selectableLocation = _windowInstance.interactables[i].cachedPosition.Item1;
                float distance = (selectableLocation - referenceLocation).LengthSquared();
                if (distance < minDistance)
                {
                    minDistance = distance;
                    _currentSelection = i;
                }
            }

            _currentSelection = Mathf.Clamp(_currentSelection, 0, _windowInstance.interactables.Count - 1);

            currentSelectable?.SetFocus(true);
        }

        public ButtonUI AddButton(string elementName, Action action = null)
        {
            if (_windowInstance == null)
                NewWindow(elementName);
            return _windowInstance.AddButton(elementName, action);
        }

        public ScrollableTextUI AddScrollableText(string elementName, Action action = null)
        {
            if (_windowInstance == null)
                NewWindow(elementName);
            return _windowInstance.AddScrollableText(elementName, action);
        }

        public SelectionUI AddSingleSelection(string elementName,
            Action<string> action = null)
        {
            if (_windowInstance == null)
                NewWindow(elementName);
            return _windowInstance.AddSingleSelection(elementName, action);
        }

        public SelectionUI
            AddSingleSelection(string elementName, WindowUI window, Action<string> action = null) =>
            window.AddSingleSelection(elementName, action);

        public TextInputUI AddTextInput(string elementName, Action<string> action = null)
        {
            if (_windowInstance == null)
                NewWindow(elementName);
            return _windowInstance.AddTextInput(elementName, action);
        }

        public ToggleUI AddToggle(string elementName,
            Action<bool> action = null)
        {
            if (_windowInstance == null)
                NewWindow(elementName);
            return _windowInstance.AddToggle(elementName, action);
        }

        public SliderUI AddSlider(string elementName,
            Action<string> action = null)
        {
            if (_windowInstance == null)
                NewWindow(elementName);
            return _windowInstance.AddSlider(elementName, action);
        }

        bool BaseConfirmAction()
        {
            bool result = currentSelectable switch
            {
                ToggleUI toggle => ToggleAction(toggle),
                IQuickSelect button => QuickSelectionAction(button, true),
                TextInputUI textInput => TextInputAction(textInput),
                ISelectable selection => SelectionAction(selection),
                ISlider input => InputAction(input, !_inElementInputMode),
                ScrollableTextUI scrollableText => ScrollableTextAction(scrollableText, !_inElementInputMode),
                ButtonUI button => ButtonAction(button),
                _ => false,
            };
            return result;
        }

        public void SetTextInput(string text)
        {
            if (currentSelectable is TextInputUI textInput)
            {
                textInput.SetInputContent(text);
            }

            _inElementInputMode = false;
            LinkInput();
        }

        void IMenuInputTarget.SetSelection(int count)
        {
            if (currentSelectable is ISelectable button)
            {
                button.SetCount(count);
            }

            LinkInput();
        }

        void IMenuInputTarget.OnMove(Vector2 vector2)
        {
            GD.Print($"{Name}: OnMove {vector2}");
            _move = vector2;
        }

        void IMenuInputTarget.OnScroll(Vector2 vector2)
        {
            _mouseScroll = vector2;
        }

        void IMenuInputTarget.OnCancel()
        {
            GD.Print($"{Name}: OnCancel");
            CancelSelection();
        }

        void IMenuInputTarget.OnConfirm()
        {
            GD.Print($"{Name}: OnConfirm");
            ConfirmSelection();
        }

        void IMenuInputTarget.OnMouseConfirmPressed()
        {
            MouseConfirmSelection();
            if (_menuSetup.allowDraggingWithMouse && !_movingWindow && _currentSelection == -1 &&
                _windowInstance.ContainsPosition(WindowManager.instance.inputProvider.mousePosition))
            {
                _movingWindow = true;
                _windowInstance.Move(Vector2.Zero);
            }
            else if (_menuSetup.allowCloseOnClick is MenuCloseOnClickBehavior.Both
                         or MenuCloseOnClickBehavior.InFocus && _currentSelection == -1)
            {
                CloseMenu();
            }
        }

        void IMenuInputTarget.OnMouseConfirmReleased()
        {
            if (!_movingWindow)
                return;
            _movingWindow = false;
            _windowInstance.ClearCachedPosition();
            _windowInstance.UpdateElementsAndWindowPosition();
        }

        void IMenuInputTarget.OnMouseCancel()
        {
            CancelSelection();
        }

        void IMenuInputTarget.OnKeyboardEscape()
        {
        }

        bool ConfirmSelection()
        {
            if (!_displayActive)
                return false;
            if (!_navigationActive || _linkFrame == Engine.GetProcessFrames())
                return false;
            if (currentSelectable == null)
                return false;
            if (!currentSelectable.available)
                return false;
            return BaseConfirmAction();
        }

        bool MouseConfirmSelection()
        {
            if (!_displayActive || !_mouseSelectionTargetExists)
            {
                return false;
            }

            if (!_navigationActive || _linkFrame == Engine.GetProcessFrames())
            {
                return false;
            }

            if (currentSelectable == null)
            {
                return false;
            }

            if (!currentSelectable.available)
            {
                return false;
            }

            if (!_inElementInputMode)
            {
                return BaseConfirmAction();
            }

            if (!_hoverOnDecrease && !_hoverOnIncrease)
                return false;
            ClearWindowLocation();
            if (_hoverOnDecrease)
            {
                int initialCount = currentSelectable.count;
                currentSelectable.count -= 1;
                if (initialCount != currentSelectable.count)
                    _selectionUpdated = true;
            }
            else if (_hoverOnIncrease)
            {
                int initialCount = currentSelectable.count;
                currentSelectable.count += 1;
                if (initialCount != currentSelectable.count)
                    _selectionUpdated = true;
            }

            return true;
        }

        bool ButtonAction(ButtonUI button)
        {
            button.TriggerAction();
            return true;
        }

        bool ToggleAction(ToggleUI toggle)
        {
            if (toggle.available)
                toggle.Toggle();
            return true;
        }

        bool QuickSelectionAction(IQuickSelect button, bool increment)
        {
            if (increment)
                button.CycleForward();
            else
                button.CycleBackward();
            return true;
        }

        bool TextInputAction(TextInputUI textInput)
        {
            WindowManager.instance.GetTextInput(this,
                textInput);
            _inElementInputMode = true;
            textInput.SetInput(true);
            return true;
        }

        bool SelectionAction(ISelectable selection)
        {
            UnlinkInput();
            WindowManager.instance.GetSelectionInput(
                this,
                selection.values,
                selection.activeCount);
            return true;
        }

        bool ScrollableTextAction(ScrollableTextUI scrollableText, bool setInput)
        {
            if (setInput && !_inElementInputMode)
            {
                scrollableText.SetScrolling(true);
                ClearWindowLocation();
                _inElementInputMode = true;
            }
            else if (!setInput && _inElementInputMode)
            {
                _inElementInputMode = false;
                scrollableText.SetScrolling(false);
                ClearWindowLocation();
            }

            return true;
        }

        bool InputAction(ISlider inputElement, bool setInput)
        {
            if (setInput && !_inElementInputMode)
            {
                inputElement.SetInput(true);
                ClearWindowLocation();
                _inElementInputMode = true;
            }
            else if (!setInput && _inElementInputMode)
            {
                _inElementInputMode = false;
                inputElement.SetInput(false);
                ClearWindowLocation();
            }

            return true;
        }

        bool CancelSelection()
        {
            if (!_displayActive)
                return false;
            if (!_navigationActive || _linkFrame == Engine.GetProcessFrames())
                return false;
            if (currentSelectable == null)
            {
                if (_menuSetup.allowCloseMenuWithCancelAction)
                    return CancelOut();
                return false;
            }

            bool result = currentSelectable switch
            {
                ISlider slider when _inElementInputMode => InputAction(slider, false),
                IQuickSelect button => QuickSelectionAction(button, false),
                ScrollableTextUI scrollableText when _inElementInputMode => ScrollableTextAction(scrollableText, false),
                _ when _menuSetup.allowCloseMenuWithCancelAction => CancelOut(),
                _ => false
            };
            return result;
        }

        public void UpdateWindows()
        {
            _windowInstance.InvokeUpdate();
        }

        public void ClearWindowLocation(float delay = .1f)
        {
            GD.Print($"{Name}: ClearWindowLocation");
            if (_nextNavigationUpdate < this.currentTime() + delay)
            {
                DelayInput(delay);
            }

            _windowInstance?.ClearCachedPosition();
        }

        public bool OpenMenu()
        {
            return OpenMenu(_menuSetup.allowNavigationOnOpen);
        }

        public bool OpenMenu(bool enableNavigation)
        {
            _displayActive = true;
            _selectionUpdated = true;
            if (enableNavigation)
                DelayInput(_menuSetup.menuOpenInputDelay);
            ResetHold();
            switch (_menuSetup.resetOnOpen)
            {
                case MenuResetOnOpenBehavior.ResetSelection:
                    ResetSelection();
                    break;
                case MenuResetOnOpenBehavior.ClearSelection:
                    ClearSelection();
                    break;
                case MenuResetOnOpenBehavior.Disable:
                    break;
            }

            _mouseSelectionTargetExists = false;
            currentSelectable?.SetFocus(true);
            _windowInstance.SetActive(true);

            return true;
        }

        public bool CloseMenu()
        {
            if (!_displayActive)
                return false;
            UnlinkInput();
            _nextNavigationUpdate = Mathf.Inf;

            if (_inElementInputMode)
            {
                _inElementInputMode = false;
                if (currentSelectable is ISlider slider)
                    slider.SetInput(false);
                if (currentSelectable is ScrollableTextUI scrollableText)
                    scrollableText.SetScrolling(false);
            }

            ClearWindowFocus();
            ClearElementsFocus();
            _focused = false;
            _displayActive = false;
            _navigationActive = false;
            _nextNavigationUpdate = Mathf.Inf;
            _windowInstance.SetActive(false);
            _menuCloseAction?.Invoke();

            return true;
        }

        void OnSelectionUpdate()
        {
            _windowInstance.TriggerSelectionUpdate();
        }

        bool CancelOut()
        {
            CloseMenu();
            return true;
        }

        public void DelayInput(float delay)
        {
            // GD.Print($"{Name}: DelayInput {delay}");
            _nextNavigationUpdate = (this.currentTime()) + delay;
        }

        public void SetFocused(bool focused)
        {
            if (_focused == focused)
                return;
            _focused = focused;
            if (!focused)
            {
                _currentSelection = -1;
                ClearWindowFocus();
                ClearElementsFocus();
                UnlinkInput();
            }
            else
            {
                _windowInstance.SetFocus(true);
                LinkInput();
            }
        }

        public void SetMenuCloseAction(Action action)
        {
            _menuCloseAction = action;
        }

        public IEnumerable<string> ExportLocalizationTag()
        {
            List<string> tags = new List<string>();
            tags.Add(menuTag);
            tags.AddRange(_windowInstance.ExportLocalizationTag());
            return tags;
        }

        private float currentTime() => Time.GetTicksMsec() / 1000.0f;
    }
}