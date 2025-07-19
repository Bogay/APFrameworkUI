using System;
using System.Collections.Generic;
using System.Linq;
using ChosenConcept.APFramework.UI.Element;
using ChosenConcept.APFramework.UI.Layout;
using ChosenConcept.APFramework.UI.Utility;
using Cysharp.Text;
using Godot;

namespace ChosenConcept.APFramework.UI.Window
{
    [GlobalClass]
    public partial class WindowUI : Control
    {
        const int OUTLINE_PADDING = 1;

        [ExportGroup("Components")]
        // [Export] Control _transform;
        [Export] RichTextLabel _drawText;
        [Export] WindowOutline _outlineBuilder;
        [Export] WindowMask _windowMask;
        [Export] WindowBackground _background;
        [Export] Control _layout;

        [ExportGroup("Debug View")]
        [Export] string _windowName = string.Empty;
        [Export] string _windowTag;
        [Export] string _windowLabelCache;
        [Export] string _windowSubscriptCache;
        [Export] WindowSetup _setup;
        [Export] LayoutAlignment _layoutAlignment;
        [Export] int _endFillCount = 5;
        [Export] bool _maskReady;
        [Export] bool _outlineReady;
        [Export] int _extraWidth;
        [Export] bool _isDirty = true;
        [Export] Godot.Collections.Array<WindowElement> _elements = new();
        [Export] bool _positionCached;
        [Export] Vector2 _cachedPositionStart = Vector2.Zero;
        [Export] Vector2 _cachedPositionEnd = Vector2.Zero;
        [Export] bool _active;
        [Export] bool _isFocused;
        [Export] bool _available = true;
        [Export] bool _inInput;
        [Export] int _designatedWidth;
        [Export] int _designatedHeight;
        [Export] bool _awaitDeactivate;
        [Export] bool _preciseSizeSync;
        [Export] ulong _nextFunctionStringUpdate = ulong.MinValue;
        IStringLabel _windowLabel;
        IStringLabel _windowSubscript = new StringLabel("");
        List<WindowElement> _interactables = new();

        public bool isFocused => _isFocused;
        public bool positionCached => _positionCached;
        public List<WindowElement> interactables => _interactables;
        public bool isActive => _active;
        public (Vector2, Vector2) cachedPosition => (_cachedPositionStart, _cachedPositionEnd);
        public Vector2 cachedCenter => (_cachedPositionStart + _cachedPositionEnd) / 2f;
        public bool canNavigate => _active && _interactables.Count > 0;
        string windowName => _windowName;
        public string windowTag => _windowTag;
        public bool isSingleButtonWindow => _elements.Count == 1 && _elements[0] is not TextUI;

        public string windowLabel
        {
            get
            {
                if (string.IsNullOrEmpty(_windowLabelCache))
                {
                    _windowLabelCache = _windowLabel.GetValue();
                }

                return _windowLabelCache;
            }
        }

        int titlePreserveLength => Mathf.FloorToInt(TextUtility.WidthSensitiveLength(windowLabel));

        public string windowSubscript
        {
            get
            {
                if (string.IsNullOrEmpty(_windowSubscriptCache))
                {
                    _windowSubscriptCache = _windowSubscript.GetValue();
                }

                return _windowSubscriptCache;
            }
        }

        public WindowSetup setup => _setup;
        int contentWidth => _setup.width - 2;
        // TODO: performance issue?
        public List<WindowElement> elements => _elements.ToList();
        public Control layout => _layout;
        public LayoutAlignment layoutAlignment => _layoutAlignment;
        public bool hasTitle => setup.titleStyle != WindowTitleStyle.None;
        public bool hasTitleBar => setup.titleStyle == WindowTitleStyle.TitleBar;
        public bool hasEmbeddedTitle => setup.titleStyle == WindowTitleStyle.EmbeddedTitle;
        public bool hasOutline => setup.outlineStyle != WindowOutlineStyle.None;
        public bool isFullFrame => setup.outlineStyle == WindowOutlineStyle.FullFrame;

        private WindowBackground getBackground()
        {
            // if (_background == null)
            // {
            //     _background = new WindowBackground();
            //     _background.Name = "WindowBackground";
            //     AddChild(_background);
            // }
            return _background;
        }

        private WindowOutline getOutline()
        {
            // if (_outlineBuilder == null)
            // {
            //     _outlineBuilder = new WindowOutline();
            //     _outlineBuilder.Name = "WindowOutline";
            //     AddChild(_outlineBuilder);
            // }
            return _outlineBuilder;
        }

        private WindowMask getMask()
        {
            // if (_windowMask == null)
            // {
            //     _windowMask = new WindowMask();
            //     _windowMask.Name = "WindowMask";
            //     AddChild(_windowMask);
            // }
            return _windowMask;
        }

        public bool ContainsPosition(Vector2 position)
        {
            if (!_positionCached)
                return false;
            if (_cachedPositionStart == Vector2.Zero && _cachedPositionEnd == Vector2.Zero)
                return false;
            Vector2 topLeftDelta = position - _cachedPositionStart;
            if (topLeftDelta.X <= 0 || topLeftDelta.Y <= 0)
                return false;
            Vector2 bottomRightDelta = position - _cachedPositionEnd;
            if (bottomRightDelta.X >= 0 || bottomRightDelta.Y >= 0)
                return false;
            return true;
        }

        public (Vector2, Vector2) SelectableBound(int index)
        {
            if (index < 0 || index > _interactables.Count || _interactables[index].firstCharacterIndex == -1 ||
                _interactables[index].lastCharacterIndex == -1 ||
                _interactables[index].cachedPosition == (Vector2.Zero, Vector2.Zero))
                return (Vector2.Zero, Vector2.Zero);
            return _interactables[index].cachedPosition;
        }

        public bool InteractableContainsPosition(int index, Vector2 position)
        {
            (Vector2 bottomLeft, Vector2 topRight) = SelectableBound(index);
            if (bottomLeft == Vector2.Zero && topRight == Vector2.Zero)
                return false;
            Vector2 bottomLeftDelta = position - bottomLeft;
            if (bottomLeftDelta.X <= 0 || bottomLeftDelta.Y <= 0)
                return false;
            Vector2 topRightDelta = position - topRight;
            if (topRightDelta.X >= 0 || topRightDelta.Y >= 0)
                return false;
            return true;
        }

        public void ClearElementsFocus()
        {
            foreach (WindowElement element in _interactables)
            {
                element.ClearFocus();
            }
        }

        public void ClearWindowFocus()
        {
            SetFocus(false);
        }

        void ClearCachedValue()
        {
            _windowLabelCache = null;
            _windowSubscriptCache = null;
            foreach (WindowElement element in _interactables)
            {
                element.ClearCache();
            }
        }

        void CheckFunctionStringLabelDirty()
        {
            foreach (WindowElement element in _elements)
            {
                if (element.rawContent is FunctionStringLabel content)
                {
                    string result = ((IStringLabel)content).GetValue();
                    if (result != element.content)
                        element.SetContentCache(result);
                }

                if (element.rawLabel is FunctionStringLabel label)
                {
                    string result = ((IStringLabel)label).GetValue();
                    if (result != element.label)
                        element.SetLabelCache(result);
                }
            }
        }


        public void SetLocalizedByTag()
        {
            SetLabel(new LocalizedStringLabel(_windowTag));
            foreach (WindowElement element in _elements)
            {
                element.SetLocalizedByTag();
            }
        }

        public void UpdateWindow()
        {
            if (_active && _nextFunctionStringUpdate < Engine.GetProcessFrames())
            {
                // GD.Print($"{Name}: Update window: {Name}");
                _nextFunctionStringUpdate = Engine.GetProcessFrames() + _setup.functionStringUpdateInterval;
                CheckFunctionStringLabelDirty();
                if (_setup.syncActiveValueAutomatically)
                {
                    SyncActiveValue();
                }
            }

            if (!_active && !_awaitDeactivate)
                return;
            this.getMask().ContextUpdate();
            if (_awaitDeactivate && !this.getMask().needUpdate)
                SetVisible(false);
        }

        public void TriggerSelectionUpdate()
        {
            foreach (WindowElement element in _elements)
            {
                if (element is ButtonUI button)
                {
                    button.CancelAwait();
                }
            }
        }

        public void SetOpacity(float alpha)
        {
            if (!_active)
                return;
            _drawText.Modulate = new Color(1, 1, 1, Mathf.Clamp(alpha, 0, 1));
            this.getOutline().SetOpacity(alpha);
        }

        public bool UpdateElementPosition(WindowElement element)
        {
            if (element.firstCharacterIndex >= _drawText.GetParsedText().Length)
            {
                return false;
            }

            (Vector2, Vector2) result = (Vector2.Zero, Vector2.Zero);
            // Note: Godot's RichTextLabel doesn't have the same character info system as Unity's TextMeshPro
            // This would need to be adapted based on the actual text rendering system used
            for (int i = element.firstCharacterIndex; i <= element.lastCharacterIndex; i++)
            {
                // This is a placeholder - Godot's text positioning would need different implementation
                Vector2 rangeBottomLeft = Vector2.Zero; // _drawText.GetCharacterPosition(i);
                Vector2 rangeTopRight = Vector2.Zero;   // _drawText.GetCharacterPosition(i) + _drawText.GetCharacterSize(i);

                if (result.Item1.X > rangeBottomLeft.X || result.Item1.X == 0)
                    result.Item1.X = rangeBottomLeft.X;
                if (result.Item1.Y > rangeBottomLeft.Y || result.Item1.Y == 0)
                    result.Item1.Y = rangeBottomLeft.Y;
                if (result.Item2.X < rangeTopRight.X || result.Item2.X == 0)
                    result.Item2.X = rangeTopRight.X;
                if (result.Item2.Y < rangeTopRight.Y || result.Item2.Y == 0)
                    result.Item2.Y = rangeTopRight.Y;
            }

            result.Item1 = WindowManager.instance.UIBoundRetriever(this, result.Item1);
            result.Item2 = WindowManager.instance.UIBoundRetriever(this, result.Item2);
            element.SetCachedPosition(result);

            // ...existing code for slider handling...

            return true;
        }

        public void UpdateWindowPosition()
        {
            (Vector2, Vector2) result = (Vector2.Zero, Vector2.Zero);
            if (hasOutline && setup.outlineDisplayStyle == WindowOutlineDisplayStyle.Always)
            {
                // Note: This would need adaptation for Godot's text system
                for (int i = 0; i <= this.getOutline().GetParsedText().Length - 1; i++)
                {
                    // Placeholder for character position retrieval in Godot
                    Vector2 rangeBottomLeft = Vector2.Zero;
                    Vector2 rangeTopRight = Vector2.Zero;

                    if (result.Item1.X > rangeBottomLeft.X || result.Item1.X == 0)
                        result.Item1.X = rangeBottomLeft.X;
                    if (result.Item1.Y > rangeBottomLeft.Y || result.Item1.Y == 0)
                        result.Item1.Y = rangeBottomLeft.Y;
                    if (result.Item2.X < rangeTopRight.X || result.Item2.X == 0)
                        result.Item2.X = rangeTopRight.X;
                    if (result.Item2.Y < rangeTopRight.Y || result.Item2.Y == 0)
                        result.Item2.Y = rangeTopRight.Y;
                }
            }

            for (int i = 0; i <= _drawText.GetParsedText().Length - 1; i++)
            {
                // Placeholder for character position retrieval in Godot
                Vector2 rangeBottomLeft = Vector2.Zero;
                Vector2 rangeTopRight = Vector2.Zero;

                if (result.Item1.X > rangeBottomLeft.X || result.Item1.X == 0)
                    result.Item1.X = rangeBottomLeft.X;
                if (result.Item1.Y > rangeBottomLeft.Y || result.Item1.Y == 0)
                    result.Item1.Y = rangeBottomLeft.Y;
                if (result.Item2.X < rangeTopRight.X || result.Item2.X == 0)
                    result.Item2.X = rangeTopRight.X;
                if (result.Item2.Y < rangeTopRight.Y || result.Item2.Y == 0)
                    result.Item2.Y = rangeTopRight.Y;
            }

            result.Item1 = WindowManager.instance.UIBoundRetriever(this, result.Item1);
            result.Item2 = WindowManager.instance.UIBoundRetriever(this, result.Item2);

            _cachedPositionStart = result.Item1;
            _cachedPositionEnd = result.Item2;
        }


        public void UpdateElementsAndWindowPosition()
        {
            if (!_active || _positionCached)
                return;
            bool quickOut = false;
            foreach (WindowElement element in _elements)
            {
                quickOut = quickOut || !UpdateElementPosition(element);
            }

            if (quickOut)
                return;
            UpdateWindowPosition();
            _positionCached = true;
        }

        public void ClearCachedPosition()
        {
            foreach (WindowElement element in _elements)
            {
                element.ClearCachedPosition();
            }

            _cachedPositionStart = Vector2.Zero;
            _cachedPositionEnd = Vector2.Zero;
            _positionCached = false;
        }

        void UpdateContent()
        {
            GD.Print($"{Name}: Update content");
            if (_designatedWidth == 0 || _designatedHeight == 0)
            {
                RefreshSize();
            }

            int count = _elements.Count;
            int characterCount = 0;
            using (Utf16ValueStringBuilder windowStringBuilder = ZString.CreateStringBuilder())
            {
                // Build window title
                if (!hasTitle)
                {
                    windowStringBuilder.Append(TextUtility.LineBreaker);
                    windowStringBuilder.Append(TextUtility.LineBreaker);
                }
                else if (hasTitleBar)
                {
                    windowStringBuilder.Append(TextUtility.TitleOpener);
                    windowStringBuilder.Append((_elements.Count == 1) switch
                    {
                        true => _isFocused switch
                        {
                            false => _available
                                ? StyleUtility.StringBold(windowLabel.ToUpper())
                                : StyleUtility.StringColored(StyleUtility.StringBold(windowLabel.ToUpper()),
                                    StyleUtility.disabled),
                            true => StyleUtility.StringColored(StyleUtility.StringBold(windowLabel.ToUpper()),
                                _available ? StyleUtility.selected : StyleUtility.disableSelected),
                        },
                        false => StyleUtility.StringBold(windowLabel.ToUpper()),
                    });
                    windowStringBuilder.Append(TextUtility.LineBreaker);
                    windowStringBuilder.Append(TextUtility.LineBreaker);
                }
                else if (hasEmbeddedTitle)
                {
                    windowStringBuilder.Append(" ");
                    windowStringBuilder.Append((_elements.Count == 1) switch
                    {
                        true => _isFocused switch
                        {
                            false => _available
                                ? StyleUtility.StringBold(windowLabel.ToUpper())
                                : StyleUtility.StringColored(StyleUtility.StringBold(windowLabel.ToUpper()),
                                    StyleUtility.disabled),
                            true => StyleUtility.StringColored(StyleUtility.StringBold(windowLabel.ToUpper()),
                                _available ? StyleUtility.selected : StyleUtility.disableSelected),
                        },
                        false => StyleUtility.StringBold(windowLabel.ToUpper()),
                    });
                    windowStringBuilder.Append(TextUtility.LineBreaker);
                }
                else
                {
                    windowStringBuilder.Append(TextUtility.LineBreaker);
                }

                if (count == 0)
                {
                    TextUI text = AddText("DummyBlankText");
                    text.SetLabel(new StringLabel(TextUtility.Repeat(' ', 10)));
                    AutoResize();
                }

                for (int i = 0; i < count; i++)
                {
                    string[] texts = _elements[i].GetSplitDisplayText(contentWidth);
                    for (int k = 0; k < texts.Length; k++)
                    {
                        string text = texts[k];
                        if (TextUtility.WidthSensitiveLength(text) > contentWidth)
                        {
                            List<string> splitString =
                                TextUtility.StringCutter(text, contentWidth);
                            for (int j = 0; j < splitString.Count; j++)
                            {
                                windowStringBuilder.Append(TextUtility.FULL_WIDTH_SPACE);
                                if (j == 0 && k == 0)
                                    _elements[i].SetFirstCharacterIndex(
                                        TextUtility.RichTagsStrippedLength(windowStringBuilder));
                                windowStringBuilder.Append(splitString[j]);
                                if (j == splitString.Count - 1 && k == texts.Length - 1)
                                    _elements[i].SetLastCharacterIndex(
                                        TextUtility.RichTagsStrippedLength(windowStringBuilder) - 1);
                                windowStringBuilder.Append(TextUtility.LINE_BREAK);
                            }
                        }
                        else
                        {
                            windowStringBuilder.Append(TextUtility.FULL_WIDTH_SPACE);
                            if (k == 0)
                                _elements[i].SetFirstCharacterIndex(
                                    TextUtility.RichTagsStrippedLength(windowStringBuilder));
                            windowStringBuilder.Append(text);
                            if (k == texts.Length - 1)
                                _elements[i].SetLastCharacterIndex(
                                    TextUtility.RichTagsStrippedLength(windowStringBuilder) - 1);
                            windowStringBuilder.Append(TextUtility.LINE_BREAK);
                        }
                    }
                }

                int compensate = 0;
                if (!hasTitle)
                    compensate = -1;
                for (int i = 0; i < _endFillCount; i++)
                {
                    if (windowSubscript != string.Empty && i == _endFillCount - 1 + compensate && !isFullFrame)
                    {
                        windowStringBuilder.Append(TextUtility.FULL_WIDTH_SPACE);
                        windowStringBuilder.Append(TextUtility.PlaceHolder(contentWidth +
                                                                           2 -
                                                                           TextUtility.WidthSensitiveLength(
                                                                               windowSubscript)));
                        if (_isFocused)
                            windowStringBuilder.Append(StyleUtility.StringColored(windowSubscript,
                                _available ? StyleUtility.selected : StyleUtility.disableSelected));
                        else
                            windowStringBuilder.Append(_available
                                ? windowSubscript
                                : StyleUtility.StringColored(windowSubscript, StyleUtility.disabled));
                        windowStringBuilder.Append(TextUtility.LineBreaker);
                    }
                    else if (windowSubscript != string.Empty && i == _endFillCount - 1 + compensate &&
                             isFullFrame)
                    {
                        windowStringBuilder.Append(TextUtility.FULL_WIDTH_SPACE);
                        windowStringBuilder.Append(TextUtility.PlaceHolder(contentWidth +
                                                                           2 -
                                                                           TextUtility.WidthSensitiveLength(
                                                                               windowSubscript)));
                        if (_isFocused)
                            windowStringBuilder.Append(StyleUtility.StringColored(windowSubscript,
                                _available ? StyleUtility.selected : StyleUtility.disableSelected));
                        else
                            windowStringBuilder.Append(_available
                                ? windowSubscript
                                : StyleUtility.StringColored(windowSubscript, StyleUtility.disabled));
                        windowStringBuilder.Append(TextUtility.LineBreaker);
                    }
                    else if (i == _endFillCount - 2 + compensate && !isFullFrame)
                        windowStringBuilder.Append(TextUtility.LineBreaker);
                    else
                        windowStringBuilder.Append(TextUtility.LineBreaker);
                }

                var content = windowStringBuilder.ToString();
                // GD.Print($"{Name}: set text: {content}");
                _drawText.Text = content;
            }
        }

        public void ContextLateUpdate()
        {
            CheckDirty();
        }

        void CheckDirty()
        {
            if (_isDirty)
            {
                UpdateContent();
                _isDirty = false;
            }
        }

        public void ClearElements()
        {
            _elements.Clear();
            _interactables.Clear();
        }

        public void Initialize(string elementName, string parent, WindowSetup windowSetup)
        {
            _windowName = elementName;
            _windowTag = ZString.Concat(parent, ".", elementName);
            _windowLabel = new StringLabel(_windowName);
            _setup = windowSetup;

            // Convert Unity font size to Godot
            var theme = new Theme();
            var fontFile = new FontFile();
            theme.SetFontSize("normal_font_size", "RichTextLabel", (int)_setup.fontSize);
            _drawText.Theme = theme;
            this.getOutline().Theme = theme;
            this.getMask().Theme = theme;

            SetActive(false);
            if (windowSetup.width != 0 && windowSetup.height != 0)
                Resize(windowSetup.width, windowSetup.height);
            else if (windowSetup.width != 0 && windowSetup.height == 0)
                Resize(windowSetup.width);
            this.getBackground().SetColor(windowSetup.backgroundColor);
        }

        public void SetBackgroundColor(Color color)
        {
            this.getBackground().SetColor(color, _active);
        }

        public void ChangeSetup(WindowSetup windowSetup)
        {
            _setup = windowSetup;
        }

        void SetLayout(int widthCount, int heightCount)
        {
            // FIXME: hard-coded values
            const float widthFactor = 0.635f;
            const float heightFactor = 1.05f * 2;
            CustomMinimumSize = new Vector2(
                setup.fontSize * widthFactor * widthCount,
                setup.fontSize * heightFactor * heightCount);
            _layoutAlignment.UpdateLayout();
            GD.Print($"{Name}: Set size: {CustomMinimumSize}");
            SetDeferred(Control.PropertyName.Size, CustomMinimumSize);
        }

        /// <summary>
        /// To automatically fit UI according to content size
        /// </summary>
        /// <param name="extraWidth">Extra amount of width to preserve.</param>
        public void AutoResize(int extraWidth = 0, bool sizeFixed = false)
        {
            _extraWidth = extraWidth;
            int targetHeight = GetAutoResizeHeight();
            int targetWidth = GetAutoResizeWidth(extraWidth);
            if (_setup.width == targetWidth && _setup.height == targetHeight)
                return;
            if (sizeFixed)
            {
                _designatedHeight = targetHeight;
                _designatedWidth = targetWidth;
            }

            _endFillCount = GetEndFillCount();
            _setup.SetWidth(targetWidth);
            _setup.SetHeight(targetHeight);
            SetupMask(targetWidth, targetHeight + 2, _setup);
            if (hasOutline)
            {
                int subscriptLength = TextUtility.WidthSensitiveLength(windowSubscript);
                SetupOutline(targetWidth + OUTLINE_PADDING, targetHeight, _setup, titlePreserveLength + 2,
                    subscriptLength);
                SetLayout(targetWidth + OUTLINE_PADDING, targetHeight);
            }
            else
            {
                SetLayout(targetWidth, targetHeight);
            }

            _positionCached = false;
        }

        public int GetAutoResizeWidth(int extraWidth)
        {
            int count = _elements.Count;
            int minimumWidth = 0;
            if (hasTitleBar)
                minimumWidth = titlePreserveLength + 2;
            if (hasEmbeddedTitle)
                minimumWidth = titlePreserveLength;
            if (windowSubscript != string.Empty)
                minimumWidth = Mathf.Max(minimumWidth, TextUtility.WidthSensitiveLength(windowSubscript) + 1);
            for (int i = 0; i < count; i++)
            {
                if (!_elements[i].flexible || count == 1)
                {
                    int elementLength = _elements[i].getMaxLength;
                    if (elementLength > minimumWidth)
                        minimumWidth = elementLength;
                }
            }

            int targetWidth = minimumWidth + 2 + extraWidth;
            return targetWidth;
        }

        public int GetAutoResizeHeight()
        {
            int count = _elements.Count;
            int minimumHeight = 0;
            for (int i = 0; i < count; i++)
            {
                string text = _elements[i].displayText;
                if (!text.Contains('\n'))
                    minimumHeight += 1;
                else
                    minimumHeight += text.Split('\n').Length;
            }

            if (hasTitleBar)
                minimumHeight += 1;
            int targetHeight = minimumHeight + 2;
            return targetHeight;
        }

        public int GetEndFillCount()
        {
            if (hasEmbeddedTitle)
                return 1;
            return 2;
        }

        /// <summary>
        /// To resize UI with specified width. Height will be automatically adjusted.
        /// </summary>
        public void Resize(int width, bool sizeFixed = false)
        {
            _extraWidth = 0;
            int minimumHeight = 0;
            int count = _elements.Count;
            for (int i = 0; i < count; i++)
            {
                minimumHeight += _elements[i].GetSplitDisplayTextTotalHeight(width);
            }

            _endFillCount = 2;
            if (hasTitleBar)
                minimumHeight += 3;
            else if (hasEmbeddedTitle)
                _endFillCount = 1;
            int targetWidth = width + 4;
            int targetHeight = minimumHeight + 2;
            if (_setup.width == targetWidth && _setup.height == targetHeight)
                return;
            _designatedWidth = width;
            if (sizeFixed)
                _designatedHeight = targetHeight;
            _setup.SetWidth(targetWidth);
            _setup.SetHeight(targetHeight);
            SetupMask(targetWidth, targetHeight + 2, _setup);
            if (hasOutline)
            {
                int subscriptLength = TextUtility.WidthSensitiveLength(windowSubscript);
                SetupOutline(targetWidth + OUTLINE_PADDING, targetHeight, _setup, titlePreserveLength + 2,
                    subscriptLength);
                SetLayout(targetWidth + OUTLINE_PADDING, targetHeight);
            }
            else
            {
                SetLayout(targetWidth, targetHeight);
            }

            _positionCached = false;
        }

        /// <summary>
        /// To resize UI with specified width and height.
        /// </summary>
        public void Resize(int width, int height)
        {
            _extraWidth = 0;
            _endFillCount = 2;
            if (hasEmbeddedTitle)
                _endFillCount = 1;
            int targetWidth = width + 4;
            int targetHeight = height + 2;
            if (_setup.width == targetWidth && _setup.height == targetHeight)
                return;
            _designatedWidth = targetWidth;
            _designatedHeight = targetHeight;
            _setup.SetWidth(targetWidth);
            _setup.SetHeight(targetHeight);
            SetupMask(targetWidth, targetHeight + 2, _setup);
            if (hasOutline)
            {
                int subscriptLength = TextUtility.WidthSensitiveLength(windowSubscript);
                SetupOutline(targetWidth + OUTLINE_PADDING, targetHeight, _setup, titlePreserveLength + 2,
                    subscriptLength);
                SetLayout(targetWidth + OUTLINE_PADDING, targetHeight);
            }
            else
            {
                SetLayout(targetWidth, targetHeight);
            }

            _positionCached = false;
        }

        void SetupOutline(int width, int height, WindowSetup windowSetup, int titleOverride, int subLength)
        {
            this.getOutline().SetOutline(width, height, windowSetup, titleOverride, subLength);
            _outlineReady = true;
        }

        void SetupMask(int width, int height, WindowSetup windowSetup)
        {
            this.getMask().Setup(width, height, windowSetup);
            _maskReady = true;
        }

        public void Close()
        {
            _layoutAlignment.UnregisterWindow(this);
            WindowManager.instance.DelistWindow(this);
        }

        public void MoveElement(WindowElement element, int targetIndex)
        {
            _elements.Remove(element);
            _elements.Insert(targetIndex, element);
            if (element is ButtonUI)
            {
                UpdateSelectables();
            }
        }

        public void UpdateSelectables()
        {
            _interactables.Clear();
            _elements.Where(x => x is ButtonUI).ToList().ForEach(x => _interactables.Add(x as ButtonUI));
        }

        public void AddElement(WindowElement element)
        {
            _elements.Add(element);
            if (element is not TextUI)
                _interactables.Add(element);
        }

        public void RemoveElement(WindowElement element)
        {
            _elements.Remove(element);
            if (_interactables.Contains(element))
                _interactables.Remove(element);
        }

        public TextUI AddText(string elementName)
        {
            TextUI text = new(elementName, this);
            AddElement(text);
            return text;
        }

        public void AddGap()
        {
            AddText("Blank")
                .SetLabel(new StringLabel("　"));
        }

        public ButtonUI AddButton(string elementName, Action action = null)
        {
            ButtonUI button = new(elementName, this);
            button.SetAction(action);
            AddElement(button);
            return button;
        }

        public ScrollableTextUI AddScrollableText(string elementName, Action action = null)
        {
            ScrollableTextUI button = new(elementName, this);
            button.SetAction(action);
            AddElement(button);
            return button;
        }

        public ToggleUI AddToggle(string elementName, Action<bool> action = null)
        {
            ToggleUI toggle = new(elementName, this);
            toggle.SetAction(action);
            AddElement(toggle);
            return toggle;
        }

        public SliderUI AddSlider(string elementName, Action<string> action = null)
        {
            SliderUI slider = new SliderUI(elementName, this);
            slider.SetAction(action);
            AddElement(slider);
            return slider;
        }

        public QuickSelectionUI AddQuickSelectionUI(string elementName, Action<string> action = null)
        {
            QuickSelectionUI selection = new(elementName, this);
            selection.SetAction(action);
            AddElement(selection);
            return selection;
        }

        public SelectionUI AddSingleSelection(string elementName, Action<string> action = null)
        {
            SelectionUI selection = new SelectionUI(elementName, this);
            selection.SetAction(action);
            AddElement(selection);
            return selection;
        }

        public TextInputUI AddTextInput(string elementName, Action<string> action = null)
        {
            TextInputUI textInput = new(elementName, this);
            textInput.SetAction(action);
            AddElement(textInput);
            return textInput;
        }

        public void SetActive(bool v, bool showMaskAnimation = true, bool syncGameObject = true)
        {
            if (_active == v)
                return;
            _active = v;
            GD.Print($"{Name}: Set active: {_active}");
            if (_active)
            {
                RefreshSize();
                if (syncGameObject)
                    SetVisible(true);
                _awaitDeactivate = false;
                ResetAllWindowElement();
                this.getOutline().SetActive(true);
                if (showMaskAnimation)
                    this.getMask().FadeIn();
                this.getBackground().SetActive(true);
                _drawText.Visible = true;
                SyncActiveValue();
                InvokeUpdate();
            }
            else
            {
                this.getOutline().SetActive(false);
                if (showMaskAnimation)
                    this.getMask().FadeOut();
                _awaitDeactivate = true;
                ResetAllWindowElement();
                this.getBackground().SetActive(false);
                // Cancel out the next update
                _isDirty = false;
                _drawText.Text = string.Empty;
            }
        }

        void ResetAllWindowElement()
        {
            foreach (WindowElement element in _elements)
            {
                element.Reset();
            }
        }

        // public void SetVisible(bool v)
        // {
        //     Visible = v;
        // }

        public void SetLabel(string label)
        {
            _windowLabelCache = null;
            _windowLabel = new StringLabel(label);
            _isDirty = true;
        }

        public void SetLabel(IStringLabel label)
        {
            _windowLabelCache = null;
            _windowLabel = label;
            _isDirty = true;
        }

        public void SetSubscript(string subscript)
        {
            _windowSubscriptCache = null;
            _windowSubscript = new StringLabel(subscript);
            _isDirty = true;
        }

        public void SetSubscript(IStringLabel label)
        {
            _windowSubscriptCache = null;
            _windowSubscript = label;
            _isDirty = true;
        }

        public void RefreshSize()
        {
            if (_designatedWidth == 0)
                AutoResize(_extraWidth);
            else if (_designatedHeight == 0)
                Resize(_designatedWidth);
        }

        public void InvokeUpdate()
        {
            _isDirty = true;
        }

        public void TriggerGlitch()
        {
            if (_maskReady) this.getMask().TriggerGlitch();
        }

        public void TriggerEffect(WindowTransition transitionSetup)
        {
            if (_maskReady) this.getMask().TriggerEffect(transitionSetup);
        }

        internal void SetMaskColor(Color color)
        {
            this.getMask().SetColor(color);
        }

        public void SetInput(bool inInput)
        {
            if (_inInput == inInput)
                return;
            _inInput = inInput;
            this.getOutline().SetFocusAndAvailable(isSingleButtonWindow, _isFocused, _available, _inInput);
            _isDirty = true;
        }

        internal void SetFocus(bool inFocus)
        {
            if (inFocus == _isFocused)
                return;
            _isFocused = inFocus;
            this.getOutline().SetFocusAndAvailable(isSingleButtonWindow, _isFocused, _available, _inInput);
            _isDirty = true;
        }

        internal void SetAvailable(bool available)
        {
            if (_available == available)
                return;
            _available = available;
            this.getOutline().SetFocusAndAvailable(isSingleButtonWindow, _isFocused, _available, _inInput);
            _isDirty = true;
        }

        public void RegisterLayout(LayoutAlignment layout)
        {
            _layoutAlignment = layout;
        }

        public void Move(Vector2 delta)
        {
            if (GetParent() == _layoutAlignment)
                Reparent(_layoutAlignment.GetParent());
            Position += delta;
        }

        public void MoveTo(Vector2 position)
        {
            if (GetParent() == _layoutAlignment)
                Reparent(_layoutAlignment.GetParent());
            Position = position;
        }

        public void RevertAlignment()
        {
            Reparent(_layoutAlignment);
        }

        public IEnumerable<string> ExportLocalizationTag()
        {
            List<string> tags = new List<string>();
            tags.Add(_windowTag);
            foreach (WindowElement element in _elements)
            {
                if (element.rawLabel is LocalizedStringLabel label)
                    tags.Add(label.tag);
                if (element.rawContent is LocalizedStringLabel content)
                    tags.Add(content.tag);
            }

            return tags;
        }

        public void SyncActiveValue()
        {
            foreach (WindowElement element in _elements)
            {
                if (element is IValueSyncTarget valurTarget && valurTarget.needSync)
                    valurTarget.SyncValue();
                if (element is IAvailabilitySyncTarget availabilityTarget && availabilityTarget.needSync)
                    availabilityTarget.SyncAvailability();
            }
        }
    }
}