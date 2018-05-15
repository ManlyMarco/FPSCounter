using BepInEx;
using UnityEngine;

namespace FPSCounter
{
    [BepInPlugin("B6C411AF-D2BA-4CAE-B5A9-94C9CC0BF9A1", "FPS Counter", "1.0")]
    public class FPSCounter : BaseUnityPlugin
    {
        private readonly float avgFraction = 0.994f;
        private readonly int screenOffset = 20;

        private float deltaTime;
        private float lastAvg;
        private float minFps, maxFps;

        private bool showFps;

        private Rect screenRect;
        private GUIStyle style = new GUIStyle();

        private void OnGUI()
        {
            if (!showFps) return;

            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;

            lastAvg = avgFraction * lastAvg + (1 - avgFraction) * fps;

            if (fps < minFps) minFps = fps;
            if (fps > maxFps) maxFps = fps;

            string text = string.Format("{1:0.} FPS, {0:0.0}ms\nAvg: {2:0.}, Min: {3:0.}, Max: {4:0.}", msec, fps, lastAvg, minFps, maxFps);
            GUI.Label(screenRect, text, style);
        }

        private void ResetValues()
        {
            minFps = float.MaxValue;
            maxFps = 0;
            lastAvg = 0;

            int w = Screen.width, h = Screen.height;
            screenRect = new Rect(screenOffset, screenOffset, w - screenOffset * 2, h - screenOffset * 2);

            style.fontSize = h / 50;

            BepInLogger.Log($"FPS Counter {(showFps ? "enabled" : "disabled")}. Screen size: {w}x{h}px");
        }

        private void Start()
        {
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.white;

            if (showFps)
                ResetValues();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Equals))
            {
                showFps = !showFps;
                ResetValues();
                Manager.Game.Instance.Player.hentai = 100;
                Manager.Game.Instance.Player.intellect = 100;
                Manager.Game.Instance.Player.physical = 100;
            }

            if (!showFps) return;

            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }
    }
}