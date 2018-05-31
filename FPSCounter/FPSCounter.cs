using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSCounter
{
    [BepInPlugin("MarC0.FPSCounter", "FPS Counter", "1.2")]
    public class FPSCounter : BaseUnityPlugin
    {
        private readonly float avgFraction = 0.994f;
        private readonly int screenOffset = 20;

        public SavedKeyboardShortcut ShowCounter { get; }

        public ConfigWrapper<TextAnchor> Position { get; }
        private ConfigWrapper<CounterColors> CounterColor { get; }
        private ConfigWrapper<bool> Shown { get; }

        private float deltaTime;
        private float lastAvg;
        private float minFps, maxFps;

        private Rect screenRect;
        private GUIStyle style = new GUIStyle();

        public FPSCounter()
        {
            ShowCounter = new SavedKeyboardShortcut("Toggle counter and reset stats", this, new KeyboardShortcut(KeyCode.U, KeyCode.LeftShift));
            Position = new ConfigWrapper<TextAnchor>("Screen position", this, TextAnchor.LowerLeft);
            Shown = new ConfigWrapper<bool>("!Display the counter", this, false);
            CounterColor = new ConfigWrapper<CounterColors>("Color of the text", this, CounterColors.White);
        }

        private enum CounterColors
        {
            White,
            Black
        }

        private void OnGUI()
        {
            if (!Shown.Value) return;

            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;

            lastAvg = avgFraction * lastAvg + (1 - avgFraction) * fps;

            if (fps < minFps) minFps = fps;
            if (fps > maxFps) maxFps = fps;

            string text = string.Format("{1:0.} FPS, {0:0.0}ms\nAvg: {2:0.}, Min: {3:0.}, Max: {4:0.}", msec, fps, lastAvg, minFps, maxFps);

            GUI.Label(screenRect, text, style);
        }

        private void OnLevelFinishedLoading(Scene sc, LoadSceneMode mode)
        {
            ResetValues();
        }

        private void ResetValues()
        {
            minFps = float.MaxValue;
            maxFps = 0;
            lastAvg = 0;
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
                    CounterColor.Value = CounterColors.White;
                    Shown.Value = true;
                }
                else if (CounterColor.Value == CounterColors.White)
                    CounterColor.Value = CounterColors.Black;
                else
                    Shown.Value = false;

                ResetValues();
            }

            if (!Shown.Value) return;

            UpdateLooks();

            style.alignment = Position.Value;
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        private void UpdateLooks()
        {
            int w = Screen.width, h = Screen.height;
            screenRect = new Rect(screenOffset, screenOffset, w - screenOffset * 2, h - screenOffset * 2);

            style.fontSize = h / 50;

            switch (CounterColor.Value)
            {
                case CounterColors.White:
                    style.normal.textColor = Color.white;
                    break;
                case CounterColors.Black:
                    style.normal.textColor = Color.black;
                    break;
            }
        }
    }
}