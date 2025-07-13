using System;
using Godot;

namespace ChosenConcept.APFramework.UI.Menu
{
    [Serializable]
    [GlobalClass]
    public partial class MenuSetup : Resource
    {
        [Export] bool _allowCycleWithinWindow;
        [Export] bool _allowCycleBetweenWindows;
        [Export] bool _allowCloseMenuWithCancelAction;
        [Export] MenuCloseOnClickBehavior _allowCloseOnClick;
        [Export] MenuResetOnOpenBehavior _resetOnOpen;
        [Export] bool _allowDraggingWithMouse;
        [Export] bool _allowNavigationOnOpen;
        [Export] float _menuOpenInputDelay;
        [Export] bool _singlePressOnly;
        [Export] float _holdNavigationDelay;
        [Export] float _holdNavigationInterval;
        [Export] float _holdNavigationSpeedUpInterval;
        [Export] float _functionStringLabelUpdateInterval;

        public bool allowCycleWithinWindow => _allowCycleWithinWindow;
        public bool allowCycleBetweenWindows => _allowCycleBetweenWindows;
        public bool allowCloseMenuWithCancelAction => _allowCloseMenuWithCancelAction;
        public MenuCloseOnClickBehavior allowCloseOnClick => _allowCloseOnClick;
        public MenuResetOnOpenBehavior resetOnOpen => _resetOnOpen;
        public bool allowDraggingWithMouse => _allowDraggingWithMouse;
        public bool allowNavigationOnOpen => _allowNavigationOnOpen;
        public float menuOpenInputDelay => _menuOpenInputDelay;
        public bool singlePressOnly => _singlePressOnly;
        public float holdNavigationDelay => _holdNavigationDelay;
        public float holdNavigationInterval => _holdNavigationInterval;
        public float holdNavigationSpeedUpInterval => _holdNavigationSpeedUpInterval;
        public float functionStringLabelUpdateInterval => _functionStringLabelUpdateInterval;


        public static MenuSetup defaultSetup => new()
        {
            _allowCycleWithinWindow = false,
            _allowCycleBetweenWindows = false,
            _allowCloseMenuWithCancelAction = false,
            _allowCloseOnClick = MenuCloseOnClickBehavior.Disable,
            _resetOnOpen = MenuResetOnOpenBehavior.ClearSelection,
            _allowNavigationOnOpen = true,
            _allowDraggingWithMouse = false,
            _menuOpenInputDelay = .2f,
            _singlePressOnly = false,
            _holdNavigationDelay = 0.5f,
            _holdNavigationInterval = 0.2f,
            _holdNavigationSpeedUpInterval = 2f,
            _functionStringLabelUpdateInterval = .1f,
        };

        public MenuSetup SetAllowCycleWithinWindow(bool allowCycleWithinWindow)
        {
            _allowCycleWithinWindow = allowCycleWithinWindow;
            return this;
        }

        public MenuSetup SetAllowCycleBetweenWindows(bool allowCycleBetweenWindows)
        {
            _allowCycleBetweenWindows = allowCycleBetweenWindows;
            return this;
        }

        public MenuSetup SetAllowCloseMenuWithCancelAction(bool allowCloseMenuWithCancelAction)
        {
            _allowCloseMenuWithCancelAction = allowCloseMenuWithCancelAction;
            return this;
        }

        public MenuSetup SetAllowCloseOnClick(MenuCloseOnClickBehavior behavior)
        {
            _allowCloseOnClick = behavior;
            return this;
        }

        public MenuSetup SetAllowNavigationOnOpen(bool allowNavigationOnOpen)
        {
            _allowNavigationOnOpen = allowNavigationOnOpen;
            return this;
        }

        public MenuSetup SetAllowDraggingWithMouse(bool allowDraggingWithMouse)
        {
            _allowDraggingWithMouse = allowDraggingWithMouse;
            return this;
        }

        public MenuSetup SetSinglePressOnly(bool singlePressOnly)
        {
            _singlePressOnly = singlePressOnly;
            return this;
        }
    }
}