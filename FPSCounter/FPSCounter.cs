using BepInEx;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSCounter
{
    [BepInPlugin("FPSCounter", "FPS Counter", "1.1")]
    public class FPSCounter : BaseUnityPlugin
    {
        private static readonly string ConfigFileLocation;
        private readonly float avgFraction = 0.994f;
        private readonly int screenOffset = 20;

        private State currentState = State.Normal;
        private float deltaTime;
        private bool initial;
        private float lastAvg;
        private float minFps, maxFps;

        private Rect screenRect;
        private GUIStyle style = new GUIStyle();

        static FPSCounter()
        {
            ConfigFileLocation = Assembly.GetExecutingAssembly().Location + ".config";
        }

        private enum State
        {
            Hidden,
            Normal,
            NormalWhite,
        }

        private System.Collections.IEnumerator HideKeybindInfo()
        {
            yield return new WaitForSecondsRealtime(15);

            initial = false;
        }

        private void LoadConfig()
        {
            if (File.Exists(ConfigFileLocation))
            {
                initial = false;
                try
                {
                    currentState = (State)int.Parse(File.ReadAllText(ConfigFileLocation));
                }
                catch (Exception ex)
                {
                    BepInLogger.Log("Failed to load FPSCounter config: " + ex.Message);
                }
            }
            else
            {
                initial = true;
            }
        }

        private void OnGUI()
        {
            if (currentState == State.Hidden) return;

            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;

            lastAvg = avgFraction * lastAvg + (1 - avgFraction) * fps;

            if (fps < minFps) minFps = fps;
            if (fps > maxFps) maxFps = fps;

            string text = string.Format("{1:0.} FPS, {0:0.0}ms\nAvg: {2:0.}, Min: {3:0.}, Max: {4:0.}", msec, fps, lastAvg, minFps, maxFps);

            if (initial)
                text += "\nPress U to toggle/reset the counters.\nMade by MarC0";

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

            if (currentState == State.Normal)
                style.normal.textColor = Color.black;
            else if (currentState == State.NormalWhite)
                style.normal.textColor = Color.white;
        }

        private void SaveConfig()
        {
            try
            {
                File.WriteAllText(ConfigFileLocation, ((int)currentState).ToString());
            }
            catch (Exception ex)
            {
                BepInLogger.Log("Failed to save FPSCounter config: " + ex.Message);
            }
        }

        private void Start()
        {
            style.alignment = TextAnchor.UpperLeft;

            LoadConfig();

            ResetValues();

            if (initial)
                StartCoroutine(HideKeybindInfo());

            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                currentState = currentState.Next();
                initial = false;

                ResetValues();

                SaveConfig();
            }

            if (currentState == State.Hidden) return;

            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }
    }
}