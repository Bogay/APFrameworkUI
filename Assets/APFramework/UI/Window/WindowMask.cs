using ChosenConcept.APFramework.UI.Utility;
using Cysharp.Text;
using Godot;

namespace ChosenConcept.APFramework.UI.Window
{
    public partial class WindowMask : Node
    {
        enum FadeType
        {
            FadeIn,
            FadeOut,
            GlitchVFX,
            DamageGlitchVFX,
        }

        [Export] WindowTransition _windowTransitionIn = WindowTransition.Full;
        [Export] WindowTransition _windowTransitionOut = WindowTransition.FromLeftLagged;
        [Export] FadeType _currentFadeType = FadeType.FadeIn;
        [Export] RichTextLabel _mask;
        public RichTextLabel mask => _mask;
        string _maskText = TextUtility.FADE_IN;
        float _maskAnimationStep = 0.005f;
        int[,] _maskIndex;
        string _maskString = TextUtility.FADE_IN;
        int _fillLine;
        [Export] int _widthCount;
        [Export] int _heightCount;
        [Export] double _nextUpdate = Mathf.Inf;
        [Export] int _endStep;
        [Export] int _currentStep = -1;
        [Export] bool _initialized;
        public bool needUpdate => _nextUpdate < Mathf.Inf;

        public void Initialize()
        {
            mask.Modulate = Colors.White * 1.5f;
        }

        public void ContextUpdate()
        {
            if (!_initialized || Time.GetUnixTimeFromSystem() < _nextUpdate || _currentStep == -1)
                return;
            _nextUpdate = Time.GetUnixTimeFromSystem() + _maskAnimationStep;
            UpdateMaskIndex();
            SetMaskIndex();
            _currentStep++;
            if (_currentStep <= _endStep)
                return;
            _endStep = 0;
            _currentStep = -1;
            mask.Text = string.Empty;
            _nextUpdate = Mathf.Inf;
        }

        void UpdateMaskIndex()
        {
            for (int i = 0; i < _maskIndex.GetLength(0); i++)
            {
                for (int j = 0; j < _maskIndex.GetLength(1); j++)
                {
                    switch (CurrentTransition)
                    {
                        case WindowTransition.Noise:
                            _maskIndex[i, j] = GD.RandRange(0, TextUtility.FADE_IN.Length - 1);
                            break;
                        case WindowTransition.Glitch:
                            if (GD.Randf() > 0.5f)
                                _maskIndex[i, j] +=
                                    Mathf.FloorToInt(GetProcessDeltaTime() / _maskAnimationStep);
                            break;
                        case WindowTransition.DamageGlitch:
                            if (GD.Randf() > 0.5f)
                                _maskIndex[i, j] +=
                                    Mathf.FloorToInt(GetProcessDeltaTime() / _maskAnimationStep);
                            break;
                        case WindowTransition.Random:
                            if (GD.Randf() > 0.25f)
                                _maskIndex[i, j] += Mathf.FloorToInt(GD.RandRange(1, TextUtility.FADE_IN.Length - 1) *
                                    GetProcessDeltaTime() / _maskAnimationStep);
                            break;
                        default:
                            _maskIndex[i, j] += Mathf.FloorToInt(GetProcessDeltaTime() / _maskAnimationStep);
                            break;
                    }
                }
            }
        }

        WindowTransition CurrentTransition => _currentFadeType switch
        {
            FadeType.FadeIn => _windowTransitionIn,
            FadeType.FadeOut => _windowTransitionOut,
            FadeType.GlitchVFX => WindowTransition.Glitch,
            FadeType.DamageGlitchVFX => WindowTransition.DamageGlitch,
            _ => _windowTransitionIn
        };

        void SetMaskIndex()
        {
            using (var windowStringBuilder = ZString.CreateStringBuilder())
            {
                for (int j = 0; j < _maskIndex.GetLength(1); j++)
                {
                    for (int i = 0; i < _maskIndex.GetLength(0); i++)
                    {
                        if (j < _fillLine || j >= _maskIndex.GetLength(1) - _fillLine)
                            windowStringBuilder.Append(' ');
                        else
                        {
                            int targetIndex = _maskIndex[i, j];
                            targetIndex = Mathf.Clamp(targetIndex, 0, _maskString.Length - 1);
                            windowStringBuilder.Append(_maskString[targetIndex]);
                        }
                    }

                    windowStringBuilder.Append(TextUtility.LineBreaker);
                }

                mask.Text = windowStringBuilder.ToString();
            }
        }

        public void Setup(int widthCount, int heightCount, WindowSetup setup)
        {
            _windowTransitionIn = setup.transitionIn;
            _windowTransitionOut = setup.transitionOut;
            _widthCount = widthCount;
            _heightCount = heightCount;
            using (var windowStringBuilder = ZString.CreateStringBuilder())
            {
                for (int i = 0; i < heightCount; i++)
                {
                    windowStringBuilder.Append(LineFill(TextUtility.FADE_IN[0], _widthCount));
                }

                _maskText = windowStringBuilder.ToString();
            }

            _maskIndex = new int[_widthCount, _heightCount];
            for (int j = 0; j < _maskIndex.GetLength(1); j++)
            {
                for (int i = 0; i < _maskIndex.GetLength(0); i++)
                {
                    _maskIndex[i, j] = -1;
                }
            }

            if (setup.titleStyle != WindowTitleStyle.TitleBar)
                _fillLine = 1;
            if (setup.outlineStyle == WindowOutlineStyle.None)
                _fillLine = 2;
            SetActive(false);
        }


        public void FadeIn()
        {
            if (_windowTransitionIn == WindowTransition.None)
                return;
            _currentFadeType = FadeType.FadeIn;
            _maskString = TextUtility.FADE_IN;
            SetupTransition(CurrentTransition);
            _initialized = true;
        }

        public float FadeOut()
        {
            if (!_initialized || _windowTransitionOut == WindowTransition.None)
                return 0f;
            _currentFadeType = FadeType.FadeOut;
            return SetupTransition(CurrentTransition);
        }

        float SetupTransition(WindowTransition transitionSetup, bool toSyncGameObject = false)
        {
            _nextUpdate = Mathf.NegInf;
            _currentStep = 0;
            int counter = 0;
            switch (transitionSetup)
            {
                case WindowTransition.Noise:
                    for (int j = 0; j < _maskIndex.GetLength(1); j++)
                    {
                        for (int i = 0; i < _maskIndex.GetLength(0); i++)
                        {
                            _maskIndex[i, j] = GD.RandRange(0, TextUtility.FADE_IN.Length - 1);
                        }
                    }

                    _endStep = Mathf.CeilToInt(0.02f / _maskAnimationStep);
                    break;
                case WindowTransition.FromLeft:
                    for (int i = 0; i < _maskIndex.GetLength(0); i++)
                    {
                        for (int j = 0; j < _maskIndex.GetLength(1); j++)
                        {
                            _maskIndex[i, j] = counter;
                        }

                        counter--;
                    }

                    _endStep = _maskIndex.GetLength(0);
                    break;
                case WindowTransition.FromLeftLagged:
                    _maskString = TextUtility.FADE_IN;
                    for (int i = 0; i < _maskIndex.GetLength(0); i++)
                    {
                        for (int j = 0; j < _maskIndex.GetLength(1); j++)
                        {
                            _maskIndex[i, j] = counter - j;
                        }

                        counter--;
                    }

                    _endStep = _maskIndex.GetLength(0) + _maskIndex.GetLength(1);
                    break;
                case WindowTransition.FromRight:
                    for (int i = _maskIndex.GetLength(0) - 1; i >= 0; i--)
                    {
                        for (int j = 0; j < _maskIndex.GetLength(1); j++)
                        {
                            _maskIndex[i, j] = counter;
                        }

                        counter--;
                    }

                    _endStep = _maskIndex.GetLength(0);
                    break;
                case WindowTransition.FromRightLagged:
                    _maskString = TextUtility.FADE_IN;
                    for (int i = _maskIndex.GetLength(0) - 1; i >= 0; i--)
                    {
                        for (int j = 0; j < _maskIndex.GetLength(1); j++)
                        {
                            _maskIndex[i, j] = counter - j;
                        }

                        counter--;
                    }

                    _endStep = _maskIndex.GetLength(0) + _maskIndex.GetLength(1);
                    break;
                case WindowTransition.Full:
                    for (int j = 0; j < _maskIndex.GetLength(1); j++)
                    {
                        for (int i = 0; i < _maskIndex.GetLength(0); i++)
                        {
                            _maskIndex[i, j] = 0;
                        }
                    }

                    _endStep = Mathf.CeilToInt(0.3f / _maskAnimationStep);
                    break;
                case WindowTransition.Random:
                    for (int j = 0; j < _maskIndex.GetLength(1); j++)
                    {
                        for (int i = 0; i < _maskIndex.GetLength(0); i++)
                        {
                            _maskIndex[i, j] = 0;
                        }
                    }

                    _endStep = 10;
                    break;
                case WindowTransition.Glitch:
                    for (int j = 0; j < _maskIndex.GetLength(1); j++)
                    {
                        for (int i = 0; i < _maskIndex.GetLength(0); i++)
                        {
                            if (GD.Randf() > 0.8f)
                                _maskIndex[i, j] = GD.RandRange(0, TextUtility.FADE_IN.Length - 1);
                            else
                                _maskIndex[i, j] = TextUtility.FADE_IN.Length - 1;
                        }
                    }

                    _endStep = Mathf.CeilToInt(0.05f / _maskAnimationStep);
                    break;
                case WindowTransition.DamageGlitch:
                    for (int j = 0; j < _maskIndex.GetLength(1); j++)
                    {
                        for (int i = 0; i < _maskIndex.GetLength(0); i++)
                        {
                            if (GD.Randf() > 0.95f)
                                _maskIndex[i, j] = GD.RandRange(0, TextUtility.FADE_IN.Length - 1);
                        }
                    }

                    _endStep = Mathf.CeilToInt(0.05f / _maskAnimationStep);
                    break;
            }

            return _endStep * _maskAnimationStep;
        }

        string LineFill(char pattern, int count) =>
            ZString.Concat(TextUtility.Repeat(pattern, count), TextUtility.LineBreaker);

        public void SetActive(bool active)
        {
            if (active)
            {
                mask.Text = _maskText;
                _nextUpdate = Mathf.NegInf;
            }
            else
            {
                mask.Text = string.Empty;
                _nextUpdate = Mathf.Inf;
            }
        }

        public void SetColor(Color color)
        {
            mask.Modulate = color;
        }


        public void TriggerGlitch()
        {
            _currentFadeType = FadeType.GlitchVFX;
            SetupTransition(CurrentTransition);
        }

        public void TriggerEffect(WindowTransition type)
        {
            SetupTransition(type);
        }

        public void TriggerDamageGlitch()
        {
            _currentFadeType = FadeType.DamageGlitchVFX;
            SetupTransition(CurrentTransition);
        }
    }
}