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
        private ConfigWrapper<State> CurrentState { get; }

        private State lastState;

        private float deltaTime;
        private float lastAvg;
        private float minFps, maxFps;

        private Rect screenRect;
        private GUIStyle style = new GUIStyle();

        public FPSCounter()
        {
            ShowCounter = new SavedKeyboardShortcut("Toggle FPS counter", this, new KeyboardShortcut(KeyCode.U, false, false, true));
            Position = new ConfigWrapper<TextAnchor>("Counter screen position", this, TextAnchor.LowerLeft);
            CurrentState = new ConfigWrapper<State>("State of the counter", this, State.VisibleWhite);
        }

        private enum State
        {
            Hidden,
            VisibleWhite,
            VisibleBlack,
        }

        private void OnGUI()
        {
            if (CurrentState.Value == State.Hidden) return;

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

            int w = Screen.width, h = Screen.height;
            screenRect = new Rect(screenOffset, screenOffset, w - screenOffset * 2, h - screenOffset * 2);

            style.fontSize = h / 50;

            if (CurrentState.Value == State.VisibleBlack)
                style.normal.textColor = Color.black;
            else if (CurrentState.Value == State.VisibleWhite)
                style.normal.textColor = Color.white;
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
                lastState = CurrentState.Value.Next();
                CurrentState.Value = lastState;
            }

            var state = CurrentState.Value;

            if (state != lastState)
            {
                ResetValues();
                lastState = state;
            }

            if (state == State.Hidden) return;

            style.alignment = Position.Value;
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }
    }
}