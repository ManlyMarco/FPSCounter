using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSCounter
{
    [BepInPlugin("MarC0.FPSCounter", "FPS Counter", "1.3")]
    public class FpsCounter : BaseUnityPlugin
    {
        private readonly float _avgFraction = 0.994f;
        private readonly int _screenOffset = 20;
        private readonly GUIStyle _style = new GUIStyle();

        private Rect _screenRect;

        private float _deltaTime;
        private float _lastAvg;
        private float _minFps, _maxFps;

        public SavedKeyboardShortcut ShowCounter { get; }
        public ConfigWrapper<CounterColors> CounterColor { get; }
        public ConfigWrapper<TextAnchor> Position { get; }
        public ConfigWrapper<bool> Shown { get; }

        public FpsCounter()
        {
            ShowCounter = new SavedKeyboardShortcut("Toggle counter and reset stats", this, new KeyboardShortcut(KeyCode.U, KeyCode.LeftShift));
            Position = new ConfigWrapper<TextAnchor>("Screen position", this, TextAnchor.LowerLeft);
            Shown = new ConfigWrapper<bool>("!Display the counter", this, false);
            CounterColor = new ConfigWrapper<CounterColors>("Color of the text", this, CounterColors.White);
        }

        private void OnGUI()
        {
            if (!Shown.Value) return;

            var msec = _deltaTime * 1000.0f;
            var fps = 1.0f / _deltaTime;

            _lastAvg = _avgFraction * _lastAvg + (1 - _avgFraction) * fps;

            if (fps < _minFps) _minFps = fps;
            if (fps > _maxFps)
            {
                _maxFps = fps;
                // Prevent scene loading etc from bogging the framerate down
                _minFps = fps;
            }

            var text = string.Format("{1:0.} FPS, {0:0.0}ms\nAvg: {2:0.}, Min: {3:0.}, Max: {4:0.}", msec, fps, _lastAvg, _minFps, _maxFps);

            switch (CounterColor.Value)
            {
                case CounterColors.Outline:
                    ShadowAndOutline.DrawOutline(_screenRect, text, _style, Color.black, Color.white, 1);
                    break;

                case CounterColors.White:
                    _style.normal.textColor = Color.white;
                    goto default;
                case CounterColors.Black:
                    _style.normal.textColor = Color.black;
                    goto default;
                default:
                    GUI.Label(_screenRect, text, _style);
                    break;
            }
        }

        private void OnLevelFinishedLoading(Scene sc, LoadSceneMode mode)
        {
            ResetValues();
        }

        private void ResetValues()
        {
            _minFps = float.MaxValue;
            _maxFps = 0;
            _lastAvg = 0;
        }

        private void Start()
        {
            ResetValues();

            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        private void Update()
        {
            if (ShowCounter.IsDown())
            {
                if (!Shown.Value)
                {
                    CounterColor.Value = CounterColors.Outline;
                    Shown.Value = true;
                }
                else if (CounterColor.Value == CounterColors.Outline)
                    CounterColor.Value = CounterColors.White;
                else if (CounterColor.Value == CounterColors.White)
                    CounterColor.Value = CounterColors.Black;
                else
                    Shown.Value = false;

                ResetValues();
            }

            if (Shown.Value)
            {
                UpdateLooks();
                _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            }
        }

        private void UpdateLooks()
        {
            int w = Screen.width, h = Screen.height;
            _screenRect = new Rect(_screenOffset, _screenOffset, w - _screenOffset * 2, h - _screenOffset * 2);

            _style.alignment = Position.Value;
            _style.fontSize = h / 50;
        }
    }
}
