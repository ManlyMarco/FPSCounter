using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace FPSCounter
{
    [BepInPlugin(GUID, "FPS Counter", Version)]
    public class FrameCounter : BaseUnityPlugin
    {
        public const string Version = "3.1";
        public const string GUID = "MarC0.FPSCounter";

        private static ConfigEntry<KeyboardShortcut> _showCounter;
        private static ConfigEntry<CounterColors> _counterColor;
        private static ConfigEntry<TextAnchor> _position;
        private static ConfigEntry<bool> _shown;
        private static ConfigEntry<bool> _showPluginStats;
        private static ConfigEntry<bool> _showUnityMethodStats;
        private static ConfigEntry<bool> _measureMemory;

        internal static new ManualLogSource Logger;

        private void Start()
        {
            Logger = base.Logger;

            _showCounter = Config.Bind("General", "Toggle counter and reset stats", new KeyboardShortcut(KeyCode.U, KeyCode.LeftShift), "Key to enable and disable the plugin.");
            _shown = Config.Bind("General", "Enable", false, "Monitor performance statistics and show them on the screen. When disabled the plugin has no effect on performance.");
            _showPluginStats = Config.Bind("General", "Enable monitoring plugins", true, "Count time each plugin takes every frame to execute. Only detects MonoBehaviour event methods, so results might be lower than expected. Has a small performance penalty.");
            _showUnityMethodStats = Config.Bind("General", "Show detailed frame stats", true, "Show how much time was spent by Unity in each part of the frame, for example how long it took to run all Update methods.");

            try
            {
                var procMem = MemoryInfo.QueryProcessMemStatus();
                var memorystatus = MemoryInfo.QuerySystemMemStatus();
                if (procMem.WorkingSetSize <= 0 || memorystatus.ullTotalPhys <= 0)
                    throw new IOException("Empty data was returned");

                _measureMemory = Config.Bind("General", "Show memory and GC stats", true,
                    "Show memory usage of the process, free available physical memory and garbage collector statistics (if available).");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Memory statistics are not available - " + ex.Message);
            }

            _position = Config.Bind("Interface", "Screen position", TextAnchor.LowerRight, "Which corner of the screen to display the statistics in.");
            _counterColor = Config.Bind("Interface", "Color of the text", CounterColors.White, "Color of the displayed stats. Outline has a performance hit but it always easy to see.");

            _position.SettingChanged += (sender, args) => UpdateLooks();
            _counterColor.SettingChanged += (sender, args) => UpdateLooks();
            _shown.SettingChanged += (sender, args) =>
            {
                UpdateLooks();
                SetCapturingEnabled(_shown.Value);
            };
            _showPluginStats.SettingChanged += (sender, args) => SetCapturingEnabled(_shown.Value);

            OnEnable();
        }

        private void Update()
        {
            if (_showCounter.Value.IsDown())
                _shown.Value = !_shown.Value;
        }

        #region Helpers

        private readonly MonoBehaviour[] _helpers = new MonoBehaviour[2];

        private void SetCapturingEnabled(bool enableCapturing)
        {
            if (!enableCapturing)
            {
                PluginCounter.Stop();
                Destroy(_helpers[0]);
                Destroy(_helpers[1]);
            }
            else
            {
                if (_helpers[0] == null) _helpers[0] = gameObject.AddComponent<FrameCounterHelper>();
                if (_helpers[1] == null) _helpers[1] = gameObject.AddComponent<FrameCounterHelper.FrameCounterHelper2>();

                if (_showPluginStats.Value)
                    PluginCounter.Start(_helpers[0], this);
                else
                    PluginCounter.Stop();
            }
        }

        private void OnEnable()
        {
            if (_shown != null && _shown.Value)
            {
                UpdateLooks();
                SetCapturingEnabled(true);
            }
        }

        private void OnDisable()
        {
            SetCapturingEnabled(false);
        }

        private void OnDestroy()
        {
            SetCapturingEnabled(false);
        }

        #endregion

        #region UI
        const int MAX_STRING_SIZE = 499;

        private static readonly GUIStyle _style = new GUIStyle();
        private static Rect _screenRect;
        private const int ScreenOffset = 10;

        private static MutableString fString = new MutableString(MAX_STRING_SIZE, true);
        private static string _frameOutputText;




        private static void DrawCounter()
        {
            if (_counterColor.Value == CounterColors.Outline)
                ShadowAndOutline.DrawOutline(_screenRect, _frameOutputText, _style, Color.black, Color.white, 1.5f);
            else
                GUI.Label(_screenRect, _frameOutputText, _style);
        }

        private static void UpdateLooks()
        {
            if (_counterColor.Value == CounterColors.White)
                _style.normal.textColor = Color.white;
            if (_counterColor.Value == CounterColors.Black)
                _style.normal.textColor = Color.black;

            int w = Screen.width, h = Screen.height;
            _screenRect = new Rect(ScreenOffset, ScreenOffset, w - ScreenOffset * 2, h - ScreenOffset * 2);

            _style.alignment = _position.Value;
            _style.fontSize = h / 65;
        }

        #endregion

        /// <summary>
        /// Code that actually captures the frame times
        /// int.MinValue makes all events on this script execute first in the scene
        /// </summary>
        [DefaultExecutionOrder(int.MinValue)]
        internal sealed class FrameCounterHelper : MonoBehaviour
        {
            #region Measurements

            /// <summary>
            /// https://docs.unity3d.com/Manual/ExecutionOrder.html
            /// Measure order:
            /// 1 FixedUpdate (all iterations this frame, includes physics)
            /// 2 Update
            /// 3 Yield null (includes physics and yield null / waitseconds)
            /// 4 LateUpdate
            /// 5 Scene rendering (from last LateUpdate to first OnGUI call)
            /// 6 OnGUI (measured from first redraw until first WaitForEndOfFrame)
            /// </summary>
            private static readonly MovingAverage _fixedUpdateTime = new MovingAverage();
            private static readonly MovingAverage _updateTime = new MovingAverage();
            private static readonly MovingAverage _yieldTime = new MovingAverage();
            private static readonly MovingAverage _lateUpdateTime = new MovingAverage();
            private static readonly MovingAverage _renderTime = new MovingAverage();
            private static readonly MovingAverage _onGuiTime = new MovingAverage();
            private static readonly MovingAverage _gcAddedSize = new MovingAverage(60);

            /// <summary>
            /// Measure frame time separately to get the true value
            /// TODO measure vsync sleeping?
            /// </summary>
            private static readonly MovingAverage _frameTime = new MovingAverage();

            #endregion

            #region Timing

            private static Stopwatch _measurementStopwatch;

            private static long TakeMeasurement()
            {
                var result = _measurementStopwatch.ElapsedTicks;
                _measurementStopwatch.Reset();
                _measurementStopwatch.Start();
                return result;
            }

            #endregion

            #region Capture

            internal static bool CanProcessOnGui;
            private static bool _onGuiHit;

            private static readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
            private static readonly KVPluginDataComparer _comparer = new KVPluginDataComparer();

            private IEnumerator Start()
            {
                _measurementStopwatch = new Stopwatch();
                var totalStopwatch = new Stopwatch();
                var nanosecPerTick = (float)(1000 * 1000 * 100) / Stopwatch.Frequency;
                var msScale = 1f / (nanosecPerTick * 1000f);
                var gcPreviousAmount = 0L;

                while (true)
                {
                    // Waits until right after last Update
                    yield return null;

                    _updateTime.Sample(TakeMeasurement());

                    // Waits until right after last OnGUI
                    yield return _waitForEndOfFrame;

                    // If no OnGUI was executed somehow, make sure to log the render time
                    if (!_onGuiHit)
                    {
                        _renderTime.Sample(TakeMeasurement());
                        _onGuiHit = true;
                    }
                    CanProcessOnGui = false;

                    _onGuiTime.Sample(TakeMeasurement());
                    // Stop until FixedUpdate so it gets counted accurately (skip other end of frame stuff)
                    _measurementStopwatch.Reset();

                    // Get actual frame round-time
                    _frameTime.Sample(totalStopwatch.ElapsedTicks);
                    totalStopwatch.Reset();
                    totalStopwatch.Start();

                    // Calculate only once at end of frame so all data is from a single frame
                    var avgFrame = _frameTime.GetAverage();
                    var fps = 1000000f / (avgFrame / nanosecPerTick);


                    fString.Append(fps, 2, 2).Append(" FPS");

                    if (_showUnityMethodStats.Value)
                    {
                        var avgFixed = _fixedUpdateTime.GetAverage();
                        var avgUpdate = _updateTime.GetAverage();
                        var avgYield = _yieldTime.GetAverage();
                        var avgLate = _lateUpdateTime.GetAverage();
                        var avgRender = _renderTime.GetAverage();
                        var avgGui = _onGuiTime.GetAverage();

                        var totalCapturedTicks = avgFixed + avgUpdate + avgYield + avgLate + avgRender + avgGui;
                        var otherTicks = avgFrame - totalCapturedTicks;

                        // Print floats with 1 decimal precision i.e. XX.X and padding of 2,
                        // meaning we assume we always get XX.X value
                        fString.Append(", ").Append(avgFrame * msScale, 2, 2);
                        fString.Append("ms\nFixed: ").Append(avgFixed * msScale, 2, 2);
                        fString.Append("ms\nUpdate: ").Append(avgUpdate * msScale, 2, 2);
                        fString.Append("ms\nYield/anim: ").Append(avgYield * msScale, 2, 2);
                        fString.Append("ms\nLate: ").Append(avgLate * msScale, 2, 2);
                        fString.Append("ms\nRender/VSync: ").Append(avgRender * msScale, 2, 2);
                        fString.Append("ms\nOnGUI: ").Append(avgGui * msScale, 2, 2);
                        fString.Append("ms\nOther: ").Append(otherTicks * msScale, 2, 2).Append("ms");
                    }

                    if (_measureMemory != null && _measureMemory.Value)
                    {
                        var procMem = MemoryInfo.QueryProcessMemStatus();
                        var currentMem = procMem.WorkingSetSize / 1024 / 1024;

                        var memorystatus = MemoryInfo.QuerySystemMemStatus();
                        var freeMem = memorystatus.ullAvailPhys / 1024 / 1024;

                        fString.Append("\nRAM: ").Append((uint)currentMem).Append("MB used, ");
                        fString.Append((uint)freeMem).Append("MB free");

                        var totalGcMemBytes = GC.GetTotalMemory(false);
                        if (totalGcMemBytes != 0)
                        {
                            var gcDelta = totalGcMemBytes - gcPreviousAmount;
                            var totalGcMem = totalGcMemBytes / 1024 / 1024;
                            _gcAddedSize.Sample(gcDelta);

                            fString.Append("\nGC: ").Append((int)totalGcMem).Append("MB (");
                            fString.Append(_gcAddedSize.GetAverageFloat() / 1024, 2, 4).Append("KB/s)");
                            //fString.Append(Mathf.RoundToInt(_gcAddedSize.GetAverage() * fps / 1024)).Append("KB/s)");

                            gcPreviousAmount = totalGcMemBytes;
                        }

                        // Check if current GC supports generations
                        var gcGens = GC.MaxGeneration;
                        if (gcGens > 0)
                        {
                            fString.Append("\nGC hits:");
                            for (var g = 0; g < gcGens; g++)
                            {
                                var collections = GC.CollectionCount(g);
                                fString.Append(' ').Append(g).Append(':').Append(collections);
                            }
                        }
                    }

                    var plugList = PluginCounter.SlowPlugins;
                    if (plugList != null)
                    {
                        if (plugList.Count > 0)
                        {
                            plugList.Sort(_comparer);
                            int len = plugList.Count;
                            for (int i = 0; i < len && i < 5; i++)
                            {
                                var kvav = plugList[i];
                                var maxName = kvav.Key.Length > 20 ? 20 : kvav.Key.Length;
                                fString.Append("\n[").Append(kvav.Key, 0, maxName).Append(": ").Append(kvav.Value * msScale, 1, 2).Append("ms]");
                            }
                        }
                        else
                        {
                            fString.Append("\nNo slow plugins");
                        }
                    }

                    _frameOutputText = fString.Finalize();
                    _measurementStopwatch.Reset();
                }
            }

            private void FixedUpdate()
            {
                // If fixed doesn't run at all this frame, stopwatch won't get started and tick count will stay at 0
                _measurementStopwatch.Start();
            }

            private void Update()
            {
                _fixedUpdateTime.Sample(TakeMeasurement());
            }

            private void LateUpdate()
            {
                _yieldTime.Sample(TakeMeasurement());
            }

            /// <summary>
            /// Needed to measure LateUpdate, int.MaxValue makes it run as the last LateUpdate call in scene.
            /// It's the last possible time to do it without listening for render events on a Camera, which is less reliable
            /// </summary>
            [DefaultExecutionOrder(int.MaxValue)]
            internal sealed class FrameCounterHelper2 : MonoBehaviour
            {
                private void LateUpdate()
                {
                    _lateUpdateTime.Sample(TakeMeasurement());

                    _onGuiHit = false;
                    CanProcessOnGui = true;
                }
            }

            private void OnGUI()
            {
                // Dragging the mouse will mess with event ordering and give bad results, so we have to reset this flag at the last point before OnGUI we can (end of lateupdate)
                if (!_onGuiHit)
                {
                    _renderTime.Sample(TakeMeasurement());
                    _onGuiHit = true;
                }

                if (Event.current.type == EventType.Repaint)
                    DrawCounter();
            }

            #endregion
        }
    }
}
