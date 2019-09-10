using System.Collections;
using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace FPSCounter
{
    [BepInPlugin("MarC0.FPSCounter", "FPS Counter", Version)]
    public class FrameCounter : BaseUnityPlugin
    {
        public const string Version = "2.0";

        public static ConfigWrapper<KeyboardShortcut> ShowCounter { get; private set; }
        public static ConfigWrapper<CounterColors> CounterColor { get; private set; }
        public static ConfigWrapper<TextAnchor> Position { get; private set; }
        public static ConfigWrapper<bool> Shown { get; private set; }

        private void Awake()
        {
            ShowCounter = Config.GetSetting("General", "Toggle counter and reset stats", new KeyboardShortcut(KeyCode.U, KeyCode.LeftShift));
            Shown = Config.GetSetting("General", "Enabled", false);
            Position = Config.GetSetting("Interface", "Screen position", TextAnchor.LowerRight);
            CounterColor = Config.GetSetting("Interface", "Color of the text", CounterColors.White);

            Position.SettingChanged += (sender, args) => UpdateLooks();
            CounterColor.SettingChanged += (sender, args) => UpdateLooks();
            Shown.SettingChanged += (sender, args) =>
            {
                UpdateLooks();
                SetCapturingEnabled(Shown.Value, true);
            };
        }

        private void Start()
        {
            if (Shown.Value)
            {
                UpdateLooks();
                SetCapturingEnabled(Shown.Value, true);
            }
        }

        private void Update()
        {
            if (ShowCounter.Value.IsDown())
                Shown.Value = !Shown.Value;
        }

        #region Helpers

        private readonly MonoBehaviour[] _helpers = new MonoBehaviour[2];

        private void SetCapturingEnabled(bool enableCapturing, bool create)
        {
            if (_helpers[0] == null)
            {
                if (!create || !enableCapturing) return;

                _helpers[0] = gameObject.AddComponent<FrameCounterHelper>();
                _helpers[1] = gameObject.AddComponent<FrameCounterHelper.FrameCounterHelper2>();
            }

            _helpers[0].enabled = enableCapturing;
            _helpers[1].enabled = enableCapturing;
        }

        private void OnEnable()
        {
            SetCapturingEnabled(true, false);
        }

        private void OnDisable()
        {
            SetCapturingEnabled(false, false);
        }

        private void OnDestroy()
        {
            Destroy(_helpers[0]);
            Destroy(_helpers[1]);
        }

        #endregion

        #region UI

        private static readonly GUIStyle _style = new GUIStyle();
        private static Rect _screenRect;
        private const int ScreenOffset = 10;

        private static string _outputText;

        private static void DrawCounter()
        {
            GUI.Label(_screenRect, _outputText, _style);

            if (CounterColor.Value == CounterColors.Outline)
                ShadowAndOutline.DrawOutline(_screenRect, _outputText, _style, Color.black, Color.white, 1.5f);
            else
                GUI.Label(_screenRect, _outputText, _style);
        }

        private static void UpdateLooks()
        {
            if (CounterColor.Value == CounterColors.White)
                _style.normal.textColor = Color.white;
            if (CounterColor.Value == CounterColors.Black)
                _style.normal.textColor = Color.black;

            int w = Screen.width, h = Screen.height;
            _screenRect = new Rect(ScreenOffset, ScreenOffset, w - ScreenOffset * 2, h - ScreenOffset * 2);

            _style.alignment = Position.Value;
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

            /// <summary>
            /// Measure frame time separately to get the true value
            /// TODO measure vsync sleeping?
            /// </summary>
            private static readonly MovingAverage _frameTime = new MovingAverage();

            #endregion

            #region Timing

            private static Stopwatch _stopwatch;
            private static Stopwatch _stopwatchFrame;
            private static long _nanosecPerTick;

            private static long TakeMeasurement()
            {
                var result = _stopwatch.ElapsedTicks;
                _stopwatch.Reset();
                _stopwatch.Start();
                return result;
            }

            #endregion

            private void Awake()
            {
                _stopwatch = new Stopwatch();
                _stopwatchFrame = new Stopwatch();
                _nanosecPerTick = 1000L * 1000L * 100L / Stopwatch.Frequency;
            }

            #region Capture

            private static bool _onGuiHit;

            private IEnumerator Start()
            {
                while (true)
                {
                    // Waits until right after last Update
                    yield return null;

                    _updateTime.Sample(TakeMeasurement());

                    // Waits until right after last OnGUI
                    yield return new WaitForEndOfFrame();

                    // If no OnGUI was executed somehow, make sure to log the render time
                    if (!_onGuiHit)
                    {
                        _renderTime.Sample(TakeMeasurement());
                        _onGuiHit = true;
                    }

                    _onGuiTime.Sample(TakeMeasurement());
                    // Stop until FixedUpdate so it gets counted accurately
                    _stopwatch.Stop();
                    _stopwatch.Reset();

                    // Get actual frame round-time
                    _frameTime.Sample(_stopwatchFrame.ElapsedTicks);
                    _stopwatchFrame.Reset();
                    _stopwatchFrame.Start();

                    // Calculate only once at end of frame so all data is from a single frame
                    var avgFrame = _frameTime.GetAverage();
                    var avgFixed = _fixedUpdateTime.GetAverage();
                    var avgUpdate = _updateTime.GetAverage();
                    var avgYield = _yieldTime.GetAverage();
                    var avgLate = _lateUpdateTime.GetAverage();
                    var avgRender = _renderTime.GetAverage();
                    var avgGui = _onGuiTime.GetAverage();

                    var frameTimeScaled = avgFrame / _nanosecPerTick;
                    var fps = 1000000f / frameTimeScaled;

                    var totalCapturedTicks = avgFixed + avgUpdate + avgYield + avgLate + avgRender + avgGui;
                    var otherTicks = avgFrame - totalCapturedTicks;

                    var msScale = 1f / (_nanosecPerTick * 1000f);

                    _outputText = $"{fps:0.0} FPS, {avgFrame * msScale,5:0.00}ms\nFixed: {avgFixed * msScale,5:0.00}ms\nUpdate: {avgUpdate * msScale,5:0.00}ms\nYield/anim: {avgYield * msScale,5:0.00}ms\nLate: {avgLate * msScale,5:0.00}ms\nRender/VSync: {avgRender * msScale,5:0.00}ms\nOnGUI: {avgGui * msScale,5:0.00}ms\nOther: {otherTicks * msScale,5:0.00}ms";
                }
            }

            private void FixedUpdate()
            {
                // If fixed doesn't run at all this frame, stopwatch won't get started tick count will stay at 0
                _stopwatch.Start();
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

                DrawCounter();
            }

            #endregion
        }
    }
}
