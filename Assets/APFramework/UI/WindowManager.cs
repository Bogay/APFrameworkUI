using System;
using System.Collections.Generic;
using System.Linq;
using ChosenConcept.APFramework.UI.Element;
using ChosenConcept.APFramework.UI.Input;
using ChosenConcept.APFramework.UI.Layout;
using ChosenConcept.APFramework.UI.Menu;
using ChosenConcept.APFramework.UI.Provider;
using ChosenConcept.APFramework.UI.Window;
using Godot;

namespace ChosenConcept.APFramework.UI
{
    public partial class WindowManager : Node, IMenuInputTarget
    {
        static WindowManager _instance;
        public static WindowManager instance => _instance ??= GetViewport().GetNode<WindowManager>("WindowManager");

        [Export] WindowUI _windowTemplate;
        [Export] CanvasLayer _layerTemplate;
        [Export] Control _layoutTemplate;
        [Export] TextureRect _backgroundTemplate;
        [Export] TextInputProvider _textInputProvider;
        [Export] SelectionProvider _selectionProvider;
        // [Export] ConfirmationProvider _confirmationProvider;
        // [Export] ContextMenuProvider _contextMenuProvider;
        [Export] Camera2D _interfaceCamera;
        [Export] List<WindowUI> _windows = new();
        [Export] List<SimpleMenu> _simpleMenus = new();
        [Export] List<LayoutAlignment> _layoutAlignments = new();
        [Export] Vector2 _lastMousePosition = Vector2.Inf * -1;
        Dictionary<MenuLayer, CanvasLayer> _layers = new();
        Dictionary<MenuLayer, CanvasLayer> _backgroundLayers = new();
        IMenuInputTarget _activeMenuTarget;
        IInputProvider _inputProvider;
        Vector2I _lastResolution = Vector2I.Zero;

        public IInputProvider inputProvider => _inputProvider;

        // public bool providerActive => _selectionProvider.active || _textInputProvider.active ||
        //   _confirmationProvider.active ||
        //   _contextMenuProvider.active;


        public void EnableGlobalVisibility(bool enable)
        {
            float opacity = enable ? 1f : 0.05f;
            foreach (SimpleMenu simpleMenu in _simpleMenus)
            {
                if (!simpleMenu.isDisplayActive)
                    continue;
                simpleMenu.SetOpacity(opacity);
            }

            // foreach (CompositeMenuMono compositeMenuMono in _compositeMenuMonos)
            // {
            //     if (!compositeMenuMono.isDisplayActive)
            //         continue;
            //     compositeMenuMono.SetOpacity(opacity);
            // }
        }

        public void LinkInputTarget(IMenuInputTarget menuInputTarget)
        {
            _activeMenuTarget = menuInputTarget;
        }

        public void UnlinkInput(IMenuInputTarget target)
        {
            if (_activeMenuTarget == target)
                LinkInputTarget(null);
        }

        // public void GetContextMenu(List<string> choices, List<Action> actions, Vector2 position, Action onClose)
        // {
        //     EnableGlobalVisibility(false);
        //     _contextMenuProvider.SetupMenu(choices, actions, position, onClose);
        // }

        // public void EndContextMenu()
        // {
        //     EnableGlobalVisibility(true);
        // }

        public void GetTextInput(IMenuInputTarget sourceUI, TextInputUI text)
        {
            _textInputProvider.GetTextInput(sourceUI, text);
        }

        public void GetSelectionInput(IMenuInputTarget sourceUI, List<string> choices,
            int currentChoice)
        {
            EnableGlobalVisibility(false);

            _selectionProvider.GetSelection(sourceUI, choices, currentChoice);
        }

        public void EndSelectionInput()
        {
            EnableGlobalVisibility(true);
        }

        void TriggerResolutionChange()
        {
            // foreach (CompositeMenuMono menu in _compositeMenuMonos)
            // {
            //     menu.TriggerResolutionChange();
            // }

            foreach (SimpleMenu menu in _simpleMenus)
            {
                menu.TriggerResolutionChange();
            }

            ClearAllWindowLocation();
        }

        public override void _Ready()
        {
            _instance ??= this;
            _inputProvider = new UnityInputProvider();
            _inputProvider.SetTarget(this);
            _inputProvider.EnableInput(true);
            // _contextMenuProvider.Initialize();
        }

        public override void _Process(double delta)
        {
            _inputProvider.Update();
            Vector2I currentResolution = new Vector2I(DisplayServer.WindowGetSize().X, DisplayServer.WindowGetSize().Y);
            if (_lastResolution.X != currentResolution.X || _lastResolution.Y != currentResolution.Y)
            {
                _lastResolution = currentResolution;
                TriggerResolutionChange();
            }

            foreach (WindowUI window in _windows)
            {
                window.UpdateWindow();
            }

            // if (providerActive)
            // {
            //     _confirmationProvider.UpdateMenu();
            //     _contextMenuProvider.UpdateMenu();
            //     _selectionProvider.UpdateMenu();
            //     return;
            // }

            // // When any provider is active, disable interaction of menus
            // foreach (CompositeMenuMono system in _compositeMenuMonos)
            // {
            //     if (!system.ProcessMode.HasFlag(Node.ProcessModeEnum.Disabled))
            //         continue;
            //     system.UpdateMenu();
            // }

            UpdateSimpleMenuMouseFocus();

            foreach (SimpleMenu system in _simpleMenus)
            {
                system.UpdateMenu();
            }
        }

        void LateUpdate()
        {
            foreach (WindowUI window in _windows)
            {
                window.ContextLateUpdate();
            }
        }

        void UpdateSimpleMenuMouseFocus()
        {
            Vector2 mousePosition = GetViewport().GetMousePosition();
            if (_lastMousePosition == mousePosition)
                return;
            _lastMousePosition = mousePosition;
            bool any = false;
            foreach (SimpleMenu x in _simpleMenus)
            {
                if (x.focused && x.windowInstance.canNavigate && x.IsMouseInWindow(_lastMousePosition) ||
                    x.movingWindow || x.inElementInputMode)
                {
                    any = true;
                    break;
                }
            }

            if (_simpleMenus.Count == 0 || any)
                return;
            foreach (SimpleMenu menu in _simpleMenus)
            {
                if (!menu.isDisplayActive || !menu.isNavigationActive || !menu.windowInstance.canNavigate)
                    continue;
                menu.SetFocused(menu.IsMouseInWindow(mousePosition));
            }
        }

        public Vector2 UIBoundRetriever(Node2D windowTransform, Vector2 elementPosition)
        {
            // Convert to global coordinates and then to screen coordinates
            Vector2 globalPos = windowTransform.ToGlobal(elementPosition);
            return GetViewport().GetCamera2D().GetScreenCenterPosition() + globalPos;
        }

        public void WindowRefresh()
        {
            // foreach (CompositeMenuMono ui in _compositeMenuMonos)
            // {
            //     ui.RefreshWindows();
            // }

            foreach (SimpleMenu menu in _simpleMenus)
            {
                menu.Refresh();
            }
        }

        public CanvasLayer InstantiateBackgroundLayer(MenuLayer layer)
        {
            if (!_backgroundLayers.TryGetValue(layer, out CanvasLayer existingLayer))
            {
                CanvasLayer newLayer = _layerTemplate.Duplicate() as CanvasLayer;
                AddChild(newLayer);
                newLayer.Layer = (int)layer * 2;
                newLayer.Name = $"{layer} BG";
                newLayer.Visible = true;
                _backgroundLayers.Add(layer, newLayer);
                return newLayer;
            }

            return existingLayer;
        }

        public CanvasLayer InstantiateLayer(MenuLayer layer)
        {
            if (!_layers.TryGetValue(layer, out CanvasLayer existingLayer))
            {
                CanvasLayer newLayer = _layerTemplate.Duplicate() as CanvasLayer;
                AddChild(newLayer);
                newLayer.Layer = (int)layer * 2 + 1;
                newLayer.Name = layer.ToString();
                newLayer.Visible = true;
                _layers.Add(layer, newLayer);
                return newLayer;
            }

            return existingLayer;
        }

        public LayoutAlignment InstantiateLayout(LayoutSetup layoutSetup, string layoutName = "")
        {
            CanvasLayer targetLayer = InstantiateLayer(layoutSetup.MenuLayer);
            Control newLayout = _layoutTemplate.Duplicate() as Control;
            targetLayer.AddChild(newLayout);
            LayoutAlignment layoutAlignment = new LayoutAlignment();
            newLayout.AddChild(layoutAlignment);
            _layoutAlignments.Add(layoutAlignment);

            Container layoutGroup = layoutSetup.windowDirection switch
            {
                WindowDirection.Vertical => new VBoxContainer(),
                WindowDirection.Horizontal => new HBoxContainer(),
                _ => throw new NotImplementedException(),
            };

            newLayout.AddChild(layoutGroup);

            // Convert Unity TextAnchor to Godot alignment
            Control.GrowDirection growDirection = layoutSetup.windowAlignment switch
            {
                WindowAlignment.UpperLeft => Control.GrowDirection.End,
                WindowAlignment.UpperCenter => Control.GrowDirection.End,
                WindowAlignment.UpperRight => Control.GrowDirection.End,
                WindowAlignment.MiddleLeft => Control.GrowDirection.Both,
                WindowAlignment.MiddleCenter => Control.GrowDirection.Both,
                WindowAlignment.MiddleRight => Control.GrowDirection.Both,
                WindowAlignment.LowerLeft => Control.GrowDirection.Begin,
                WindowAlignment.LowerCenter => Control.GrowDirection.Begin,
                WindowAlignment.LowerRight => Control.GrowDirection.Begin,
                _ => throw new System.NotImplementedException(),
            };

            layoutAlignment.Initialize(layoutGroup, layoutSetup);
            if (layoutName != string.Empty)
                newLayout.Name = layoutName;
            newLayout.Visible = true;
            newLayout.Scale = Vector2.One;
            return layoutAlignment;
        }

        public void DelistWindow(WindowUI window)
        {
            _windows.Remove(window);
            window.QueueFree();
        }

        public WindowUI NewWindow(string windowName, LayoutSetup layoutSetup, WindowSetup setup,
            string menuName)
        {
            WindowUI window = InstantiateWindow(windowName, layoutSetup);
            window.Initialize(windowName, menuName, setup);
            _windows.Add(window);
            window.Visible = false;
            return window;
        }

        public WindowUI NewWindow(string windowName, LayoutAlignment layout, WindowSetup setup,
            string menuName)
        {
            WindowUI window = InstantiateWindow(windowName, layout);
            window.Initialize(windowName, menuName, setup);
            _windows.Add(window);
            window.Visible = false;
            return window;
        }


        WindowUI InstantiateWindow(string windowName, LayoutSetup layoutSetup)
        {
            LayoutAlignment layout = InstantiateLayout(layoutSetup);
            WindowUI window = _windowTemplate.Duplicate() as WindowUI;
            layout.AddChild(window);
            layout.RegisterWindow(window);
            layout.Name = windowName;
            window.Name = windowName;
            window.Scale = Vector2.One;
            window.RegisterLayout(layout);
            return window;
        }

        WindowUI InstantiateWindow(string windowName, LayoutAlignment layout)
        {
            WindowUI window = _windowTemplate.Duplicate() as WindowUI;
            layout.AddChild(window);
            layout.RegisterWindow(window);
            window.Name = windowName;
            window.Scale = Vector2.One;
            window.RegisterLayout(layout);
            return window;
        }

        // public void RegisterMenu(CompositeMenuMono menu)
        // {
        //     if (_compositeMenuMonos.Contains(menu))
        //         return;
        //     _compositeMenuMonos.Add(menu);
        // }

        public void RegisterMenu(SimpleMenu menu)
        {
            if (_simpleMenus.Contains(menu))
                return;
            _simpleMenus.Add(menu);
        }

        public void ClearAllWindowLocation()
        {
            float inputDelayDuration = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.ExclusiveFullscreen ? 1.5f : 0.5f;
            // foreach (CompositeMenuMono menu in _compositeMenuMonos)
            // {
            //     menu.ClearWindowLocation(inputDelayDuration);
            // }

            foreach (SimpleMenu menu in _simpleMenus)
            {
                menu.ClearWindowLocation(inputDelayDuration);
            }
        }

        // public void GetConfirm(string title, string message, string confirm,
        //     string cancel = null, Action onConfirm = null, Action onCancel = null,
        //     ConfirmationDefaultChoice defaultChoice = ConfirmationDefaultChoice.None)
        // {
        //     EnableGlobalVisibility(false);

        //     _confirmationProvider.GetConfirm(title, message, confirm, cancel, onConfirm, onCancel,
        //         defaultChoice);
        // }

        // public void GetConfirm(string title, string message, string confirm,
        //     Action onConfirm = null, ConfirmationDefaultChoice defaultChoice = ConfirmationDefaultChoice.None)
        // {
        //     EnableGlobalVisibility(false);

        //     _confirmationProvider.GetConfirm(title, message, confirm, null, onConfirm, null, defaultChoice);
        // }

        public void EndConfirm()
        {
            EnableGlobalVisibility(true);
        }

        public bool CheckClosestDirectionalMatch(SimpleMenu source, Vector2 currentPosition, Vector2 inputDirection,
            bool allowCycleBetweenWindows)
        {
            float minScore = Mathf.Infinity;
            SimpleMenu nearestMenu = source;
            foreach (SimpleMenu menu in _simpleMenus)
            {
                if (menu == source || !menu.isDisplayActive || !menu.isNavigationActive ||
                    !menu.windowInstance.canNavigate)
                    continue;
                Vector2 windowCenter =
                    (menu.windowInstance.cachedPosition.Item1 + menu.windowInstance.cachedPosition.Item2) / 2f;
                Vector2 direction = windowCenter - currentPosition;
                float distance = direction.sqrMagnitude;
                Vector2 directionNormalized = direction.normalized;
                float dotProduct = Vector2.Dot(inputDirection, directionNormalized);
                if (dotProduct < .3f)
                    continue;
                // Favoring both shorter distance and better directional match
                float score = distance * (2 - dotProduct);
                if (score < minScore)
                {
                    minScore = score;
                    nearestMenu = menu;
                }
            }

            if (nearestMenu != source)
            {
                int nearestInteractableIndex = -1;
                minScore = Mathf.Infinity;
                for (int i = 0; i < nearestMenu.windowInstance.interactables.Count; i++)
                {
                    // Using only the starting position for consistency
                    Vector2 position1 = nearestMenu.windowInstance.interactables[i].cachedPosition.Item1;
                    Vector2 selectableLocation = position1;
                    Vector2 direction = selectableLocation - currentPosition;
                    float distance = direction.sqrMagnitude;
                    if (distance < minScore)
                    {
                        minScore = distance;
                        nearestInteractableIndex = i;
                    }
                }

                source.SetFocused(false);
                nearestMenu.SetFocused(true);
                nearestMenu.SetCurrentSelection(nearestInteractableIndex);
                return true;
            }

            if (allowCycleBetweenWindows)
            {
                // TODO: maybe?
            }

            return false;
        }

        void IMenuInputTarget.OnConfirm()
        {
            if (_activeMenuTarget != null)
                _activeMenuTarget.OnConfirm();
        }

        void IMenuInputTarget.OnCancel()
        {
            if (_activeMenuTarget != null)
                _activeMenuTarget.OnCancel();
        }

        void IMenuInputTarget.OnMove(Vector2 move)
        {
            if (_activeMenuTarget != null)
            {
                _activeMenuTarget.OnMove(move);
                return;
            }

            if (_simpleMenus.Count == 0 || Mathf.IsZeroApprox(move.LengthSquared()))
                return;
            foreach (SimpleMenu menu in _simpleMenus)
            {
                if (!menu.isDisplayActive || !menu.isNavigationActive)
                    continue;
                menu.SetFocused(true);
                menu.SetCurrentSelection(0);
                break;
            }
        }

        void IMenuInputTarget.OnScroll(Vector2 scroll)
        {
            if (_activeMenuTarget != null)
                _activeMenuTarget.OnScroll(scroll);
        }

        void IMenuInputTarget.OnMouseConfirmPressed()
        {
            IEnumerable<SimpleMenu> list = _simpleMenus.Where(x =>
                x.canBeClosedByOutOfFocusClick && x.isDisplayActive && x.isNavigationActive && !x.focused);
            foreach (SimpleMenu menu in list)
            {
                menu.CloseMenu();
            }

            if (_activeMenuTarget != null)
                _activeMenuTarget.OnMouseConfirmPressed();
        }

        void IMenuInputTarget.OnMouseConfirmReleased()
        {
            if (_activeMenuTarget != null)
                _activeMenuTarget.OnMouseConfirmReleased();
        }

        void IMenuInputTarget.OnMouseCancel()
        {
            if (_activeMenuTarget != null)
                _activeMenuTarget.OnMouseCancel();
        }

        void IMenuInputTarget.OnKeyboardEscape()
        {
            if (_activeMenuTarget != null)
                _activeMenuTarget.OnKeyboardEscape();
        }

        void IMenuInputTarget.SetSelection(int i)
        {
        }

        void IMenuInputTarget.SetTextInput(string inputFieldText)
        {
        }

        public T GetMenu<T>()
        {
            // foreach (CompositeMenuMono system in _compositeMenuMonos)
            // {
            //     if (system is T t)
            //     {
            //         return t;
            //     }
            // }

            throw new Exception($"Menu {typeof(T)} not found");
        }

        public string ExportLocalizationTag()
        {
            List<string> tags = new List<string>();
            foreach (SimpleMenu simpleMenu in _simpleMenus)
            {
                tags.AddRange(simpleMenu.ExportLocalizationTag());
            }

            // foreach (CompositeMenuMono system in _compositeMenuMonos)
            // {
            //     tags.AddRange(system.ExportLocalizationTag());
            // }

            return string.Join("\n", tags);
        }
    }
}