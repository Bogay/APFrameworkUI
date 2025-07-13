using System;
using System.Collections.Generic;
using System.Linq;
using ChosenConcept.APFramework.UI.Utility;
using ChosenConcept.APFramework.UI.Window;
using Cysharp.Text;
using Godot;

namespace ChosenConcept.APFramework.UI.Element
{
    // TODO: support generic
    [GlobalClass]
    public partial class SliderUI : WindowElement, ISlider, IValueSyncTarget
    {
        bool _inInput;
        List<string> _choiceListContentCache = new();
        List<IStringLabel> _choiceList = new();
        List<string> _choiceValueList = new();
        Action<string> _action;
        Func<string> _activeValueGetter;

        (Vector2, Vector2) _cachedArrowPosition = (Vector2.Zero, Vector2.Zero);
        int ISlider.firstSliderArrowIndex => firstCharacterIndex + labelPrefix.Length;
        int ISlider.lastSliderArrowIndex => lastCharacterIndex;
        public (Vector2, Vector2) cachedArrowPosition => _cachedArrowPosition;

        public override int count
        {
            get => _count;
            set
            {
                _count = Mathf.Clamp(value, 0, _choiceList.Count - 1);
                if (_count != value)
                    return;
                _parentWindow?.InvokeUpdate();
                TriggerAction();
            }
        }

        public override string displayText
        {
            get
            {
                if (_inInput)
                    return ZString.Concat(labelPrefix, SliderText());
                return base.displayText;
            }
        }

        public string currentChoice => choiceListContent.Count > 0 ? choiceListContent[_count] : TextUtility.NA;

        public List<string> choiceListContent
        {
            get
            {
                if (_choiceListContentCache.Count == 0)
                {
                    if (_choiceList.Count == 0)
                    {
                        return _choiceListContentCache;
                    }

                    _choiceListContentCache.AddRange(_choiceList.Select(x => x.GetValue()));
                }

                return _choiceListContentCache;
            }
        }

        public override int getMaxLength
        {
            get
            {
                if (_choiceList.Count == 0)
                    return TextUtility.WidthSensitiveLength(labelPrefix) + 2;
                return TextUtility.WidthSensitiveLength(labelPrefix) + maxContentLength + 2;
            }
        }

        public override string formattedContent => ZString.Concat(labelPrefix, currentChoice);

        public int maxContentLength
        {
            get
            {
                if (choiceListContent.Count == 0)
                {
                    return 0;
                }

                int count = 0;
                foreach (string choice in choiceListContent)
                {
                    int choiceLength = TextUtility.WidthSensitiveLength(choice);
                    if (choiceLength > count)
                    {
                        count = choiceLength;
                    }
                }

                return count;
            }
        }

        public SliderUI(string label, WindowUI parent) : base(label, parent)
        {
        }

        public SliderUI SetAction(Action<string> action)
        {
            _action = action;
            return this;
        }

        public SliderUI SetActiveValue(string value)
        {
            int index = _choiceValueList.IndexOf(value);
            if (index < 0)
            {
                return this;
            }

            if (_count == index)
                return this;
            _count = index;
            _parentWindow?.InvokeUpdate();
            return this;
        }

        public SliderUI SetActiveValue(Func<string> valueGetter)
        {
            _activeValueGetter = valueGetter;
            int index = _choiceValueList.IndexOf(valueGetter());
            if (index < 0)
            {
                return this;
            }

            if (_count == index)
                return this;
            _count = index;
            _parentWindow?.InvokeUpdate();
            return this;
        }

        public void TriggerAction()
        {
            if (_action == null)
                return;
            _action.Invoke(_choiceValueList[_count]);
        }

        public void ClearChoice()
        {
            _choiceListContentCache.Clear();
            _choiceList.Clear();
        }

        public SliderUI SetChoice(List<IStringLabel> choice, List<string> value)
        {
            if (choice.Count != value.Count)
            {
                GD.PrintErr($"Mismatch amount of {choice.Count} and {value.Count}");
                return this;
            }

            ClearChoice();
            _choiceList.AddRange(choice);
            _choiceValueList.AddRange(value);
            return this;
        }

        public SliderUI SetChoice(List<string> choice, List<string> value)
        {
            if (choice.Count != value.Count)
            {
                GD.PrintErr($"Mismatch amount of {choice.Count} and {value.Count}");
                return this;
            }

            ClearChoice();
            for (int i = 0; i < choice.Count; i++)
            {
                AddChoice(choice[i], value[i]);
            }

            return this;
        }

        public SliderUI SetChoiceByValue(IEnumerable<string> value)
        {
            ClearChoice();
            foreach (string choice in value)
            {
                AddChoice(choice.ToString(), choice);
            }

            return this;
        }

        public SliderUI SetLocalizedChoiceByValue(IEnumerable<string> value)
        {
            ClearChoice();
            foreach (string item in value)
            {
                AddChoice(new LocalizedStringLabel(_tag, item.ToString()), item);
            }

            return this;
        }


        public SliderUI AddLocalizedChoice(string tag, string value)
        {
            return AddChoice(new LocalizedStringLabel(_tag, tag), value);
        }

        public SliderUI AddChoice(string choice, string value)
        {
            return AddChoice(new StringLabel(choice), value);
        }

        public SliderUI AddChoice(IStringLabel choice, string value)
        {
            _choiceListContentCache.Clear();
            _choiceList.Add(choice);
            _choiceValueList.Add(value);
            return this;
        }

        public void RemoveChoiceAt(int index)
        {
            _choiceListContentCache.Clear();
            _choiceList.RemoveAt(index);
            _choiceValueList.RemoveAt(index);
        }

        public SliderUI AddChoiceByValue(string choice)
        {
            _choiceList.Add(new StringLabel(choice.ToString()));
            _choiceValueList.Add(choice);
            return this;
        }

        public void RemoveValue(string value)
        {
            _choiceListContentCache.Clear();
            int index = _choiceValueList.IndexOf(value);
            if (index < 0)
                return;
            _choiceList.RemoveAt(index);
            _choiceValueList.RemoveAt(index);
        }

        public string SliderText()
        {
            string optionString = currentChoice;
            if (_count == 0)
                return StyleUtility.StringColored(ZString.Concat(" ", OptionFillString(optionString), "›"),
                    StyleUtility.selected);
            if (_count == _choiceList.Count - 1)
                return StyleUtility.StringColored(ZString.Concat("‹", OptionFillString(optionString), " "),
                    StyleUtility.selected);
            return StyleUtility.StringColored(ZString.Concat("‹", OptionFillString(optionString), "›"),
                StyleUtility.selected);
        }

        public override void ClearCache()
        {
            base.ClearCache();
            _choiceListContentCache.Clear();
        }

        public virtual string OptionFillString(string activeOption)
        {
            int totalLengthRequired = maxContentLength - TextUtility.WidthSensitiveLength(activeOption);
            return ZString.Concat(TextUtility.Repeat(' ', totalLengthRequired - totalLengthRequired / 2), activeOption,
                TextUtility.Repeat(' ', totalLengthRequired / 2));
        }

        void ISlider.SetCachedArrowPosition((Vector2, Vector2) position) => _cachedArrowPosition = position;

        void ISlider.SetInput(bool inInput)
        {
            if (_inInput == inInput)
                return;
            _inInput = inInput;
            parentWindow?.InvokeUpdate();
        }

        public override void ClearCachedPosition()
        {
            base.ClearCachedPosition();
            _cachedArrowPosition = (Vector2.Zero, Vector2.Zero);
        }

        (bool, bool) ISlider.HoverOnArrow(Vector2 position)
        {
            bool hoverOnDecrease = false;
            bool hoverOnIncrease = false;
            float fontSize = _parentWindow.setup.fontSize;
            Vector2 leftArrowDelta = position - _cachedArrowPosition.Item1;
            Vector2 rightArrowDelta = position - _cachedArrowPosition.Item2;
            if (leftArrowDelta.LengthSquared() < rightArrowDelta.LengthSquared() &&
                Mathf.Abs(leftArrowDelta.X) < fontSize && Mathf.Abs(leftArrowDelta.Y) < fontSize)
            {
                hoverOnDecrease = true;
            }
            else if (Mathf.Abs(rightArrowDelta.X) < fontSize && Mathf.Abs(rightArrowDelta.Y) < fontSize)
            {
                hoverOnIncrease = true;
            }

            return (hoverOnDecrease, hoverOnIncrease);
        }

        public override void ContextLanguageChange()
        {
            base.ContextLanguageChange();
            _choiceListContentCache.Clear();
        }

        public override IEnumerable<string> ExportLocalizationTag()
        {
            List<string> tags = new();
            tags.AddRange(base.ExportLocalizationTag());
            foreach (IStringLabel item in _choiceList)
            {
                if (item is LocalizedStringLabel label)
                    tags.Add(label.tag);
            }

            return tags;
        }

        bool IValueSyncTarget.needSync => _activeValueGetter != null;

        void IValueSyncTarget.SyncValue()
        {
            SetActiveValue(_activeValueGetter());
        }
    }
}