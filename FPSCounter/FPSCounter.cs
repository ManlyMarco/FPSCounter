using BepInEx;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        private Rect screenRect;
        private GUIStyle style = new GUIStyle();

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

        private static readonly string ConfigFileLocation;

        static FPSCounter()
        {
            ConfigFileLocation = Assembly.GetExecutingAssembly().Location + ".config";
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

        private void OnLevelFinishedLoading(Scene sc, LoadSceneMode mode)
        {
            ResetValues();
        }

        private bool initial;

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

        State currentState = State.Normal;

        private enum State
        {
            Hidden,
            Normal,
            NormalWhite,
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
    }

    public static class Extensions
    {
        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argumnent {0} is not an Enum", typeof(T).FullName));

            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }
    }
}