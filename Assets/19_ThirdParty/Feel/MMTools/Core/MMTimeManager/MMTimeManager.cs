using MoreMountains.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
    /// <summary>
    /// A list of the methods available to change the current score
    /// </summary>
    public enum MMTimeScaleMethods
    {
        For,
        Reset,
        Unfreeze
    }

    /// <summary>
    /// The possible modes with which to interpolate time scale. Speed is a legacy mode, the recommended mode if you want to lerp timescale would be Duration, which offers the most options and accuracy
    /// </summary>
    public enum MMTimeScaleLerpModes { Speed, Duration, NoInterpolation }

    /// <summary>
    /// The different settings you can play with on a time scale event
    /// </summary>
    public struct TimeScaleProperties
    {
        public float TimeScale;
        public float Duration;
        public bool TimeScaleLerp;
        public float LerpSpeed;
        public bool Infinite;
        public MMTimeScaleLerpModes TimeScaleLerpMode;
        public MMTweenType TimeScaleLerpCurve;
        public float TimeScaleLerpDuration;
        public bool TimeScaleLerpOnUnfreeze;
        public MMTweenType TimeScaleLerpCurveOnUnfreeze;
        public float TimeScaleLerpDurationOnUnfreeze;
        public override string ToString() => $"REQUESTED ts={TimeScale} time={Duration} lerp={TimeScaleLerp} speed={LerpSpeed} keep={Infinite}";
    }

    public struct MMTimeScaleEvent
    {
        static private event Delegate OnEvent;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
        static public void Register(Delegate callback) { OnEvent += callback; }
        static public void Unregister(Delegate callback) { OnEvent -= callback; }

        public delegate void Delegate(MMTimeScaleMethods timeScaleMethod, float timeScale, float duration, bool lerp, float lerpSpeed, bool infinite,
            MMTimeScaleLerpModes timeScaleLerpMode = MMTimeScaleLerpModes.Speed, MMTweenType timeScaleLerpCurve = null, float timeScaleLerpDuration = 0.2f,
            bool timeScaleLerpOnUnfreeze = false, MMTweenType timeScaleLerpCurveOnUnfreeze = null, float timeScaleLerpDurationOnUnfreeze = 0.2f);

        static public void Trigger(MMTimeScaleMethods timeScaleMethod, float timeScale, float duration, bool lerp, float lerpSpeed, bool infinite,
            MMTimeScaleLerpModes timeScaleLerpMode = MMTimeScaleLerpModes.Speed, MMTweenType timeScaleLerpCurve = null, float timeScaleLerpDuration = 0.2f,
            bool timeScaleLerpOnUnfreeze = false, MMTweenType timeScaleLerpCurveOnUnfreeze = null, float timeScaleLerpDurationOnUnfreeze = 0.2f)
        {
            OnEvent?.Invoke(timeScaleMethod, timeScale, duration, lerp, lerpSpeed, infinite, timeScaleLerpMode, timeScaleLerpCurve, timeScaleLerpDuration, timeScaleLerpOnUnfreeze, timeScaleLerpCurveOnUnfreeze, timeScaleLerpDurationOnUnfreeze);
        }

        static public void Unfreeze()
        {
            OnEvent?.Invoke(MMTimeScaleMethods.Unfreeze, 0f, 0f, false, 0f, false);
        }

        static public void Reset()
        {
            OnEvent?.Invoke(MMTimeScaleMethods.Reset, 0f, 0f, false, 0f, false);
        }
    }

    public struct MMFreezeFrameEvent
    {
        static private event Delegate OnEvent;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
        static public void Register(Delegate callback) { OnEvent += callback; }
        static public void Unregister(Delegate callback) { OnEvent -= callback; }

        public delegate void Delegate(float duration);

        static public void Trigger(float duration)
        {
            OnEvent?.Invoke(duration);
        }
    }

    /// <summary>
    /// Put this component in your scene and it'll catch MMFreezeFrameEvents and MMTimeScaleEvents, allowing you to control the flow of time.
    /// </summary>
    [AddComponentMenu("More Mountains/Feedbacks/Shakers/Various/MM Time Manager")]
    public class MMTimeManager : MMSingleton<MMTimeManager>
    {
        [Header("Default Values")]
        /// The reference time scale, to which the system will go back to after all time is changed
        [Tooltip("The reference time scale, to which the system will go back to after all time is changed")]
        public float NormalTimeScale = 1f;

        [Header("Impacted Values")]
        /// whether or not to update Time.timeScale when changing time scale
        [Tooltip("whether or not to update Time.timeScale when changing time scale")]
        public bool UpdateTimescale = true;
        /// whether or not to update Time.fixedDeltaTime when changing time scale
        [Tooltip("whether or not to update Time.fixedDeltaTime when changing time scale")]
        public bool UpdateFixedDeltaTime = true;
        /// whether or not to update Time.maximumDeltaTime when changing time scale
        [Tooltip("whether or not to update Time.maximumDeltaTime when changing time scale")]
        public bool UpdateMaximumDeltaTime = true;

        [Header("Debug")]
        /// the current, real time, time scale
        [Tooltip("the current, real time, time scale")]
        [MMReadOnly]
        public float CurrentTimeScale = 1f;
        /// the time scale the system is lerping towards
        [Tooltip("the time scale the system is lerping towards")]
        [MMReadOnly]
        public float TargetTimeScale = 1f;

        [MMInspectorButton("TestButtonToSlowDownTime")]
        /// a test button for the inspector
        public bool TestButton;

        protected Stack<TimeScaleProperties> TimeScaleProperties;
        protected TimeScaleProperties CurrentProperty;
        protected TimeScaleProperties ResetProperty;
        protected float InitialFixedDeltaTime = 0f;
        protected float InitialMaximumDeltaTime = 0f;
        protected float StartedAt;
        protected bool LerpingBackToNormal = false;
        protected float TimeScaleLastTime = float.NegativeInfinity;
        protected float InitialTimeScale = 1f;

        /// <summary>
        /// Statics initialization to support enter play modes
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        protected static void InitializeStatics()
        {
            _instance = null;
        }

        /// <summary>
        /// A method used from the inspector to test the system
        /// </summary>
        protected virtual void TestButtonToSlowDownTime()
        {
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0.5f, 3f, true, 1f, false);
        }

        /// <summary>
        /// On start we initialize our stack 
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            PreInitialization();
        }

        /// <summary>
        /// We initialize our stack
        /// </summary>
        public virtual void PreInitialization()
        {
            TimeScaleProperties = new Stack<TimeScaleProperties>();
        }

        /// <summary>
        /// On Start we apply our timescale
        /// </summary>
        protected virtual void Start()
        {
            Initialization();
        }

        /// <summary>
        /// On init we store our initial timescales and apply the normal timescale
        /// </summary>
        public virtual void Initialization()
        {
            TargetTimeScale = NormalTimeScale;
            InitialFixedDeltaTime = Time.fixedDeltaTime;
            InitialMaximumDeltaTime = Time.maximumDeltaTime;
            ApplyTimeScale(NormalTimeScale);
        }

        /// <summary>
        /// On Update, applies the time scale and resets it if needed
        /// </summary>
        protected virtual void Update()
        {
            // if we have things in our stack, we handle them, otherwise we reset to the normal time scale
            while (TimeScaleProperties.Count > 0)
            {
                CurrentProperty = TimeScaleProperties.Peek();
                TargetTimeScale = CurrentProperty.TimeScale;
                CurrentProperty.Duration -= Time.unscaledDeltaTime;

                TimeScaleProperties.Pop();
                TimeScaleProperties.Push(CurrentProperty);

                if (CurrentProperty.Duration > 0f || CurrentProperty.Infinite)
                {
                    break; // keep current property values
                }
                else
                {
                    Unfreeze(); // pop current property
                }
            }

            if (TimeScaleProperties.Count == 0)
            {
                TargetTimeScale = NormalTimeScale;
            }

            // we apply our time scale
            if (CurrentProperty.TimeScaleLerp)
            {
                if (CurrentProperty.TimeScaleLerpMode == MMTimeScaleLerpModes.Speed)
                {
                    if (CurrentProperty.LerpSpeed <= 0) { CurrentProperty.LerpSpeed = 1; }
                    ApplyTimeScale(Mathf.Lerp(Time.timeScale, TargetTimeScale, Time.unscaledDeltaTime * CurrentProperty.LerpSpeed));
                }
                else if (CurrentProperty.TimeScaleLerpMode == MMTimeScaleLerpModes.Duration)
                {
                    float timeSinceStart = Time.unscaledTime - StartedAt;

                    if (timeSinceStart < CurrentProperty.TimeScaleLerpDuration)
                    {
                        float progress = MMMaths.Remap(timeSinceStart, 0f, CurrentProperty.TimeScaleLerpDuration, 0f,
                            1f);
                        float delta = CurrentProperty.TimeScaleLerpCurve.Evaluate(progress);
                        float newValue = MMMaths.Remap(delta, 0f, 1f, InitialTimeScale, TargetTimeScale);
                        ApplyTimeScale(newValue);
                    }
                    else
                    {
                        ApplyTimeScale(TargetTimeScale);
                        if (LerpingBackToNormal)
                        {
                            LerpingBackToNormal = false;
                            TimeScaleProperties.Pop();
                        }
                    }
                }
            }
            else
            {
                ApplyTimeScale(TargetTimeScale);
            }
        }

        /// <summary>
        /// Modifies the time scale and time attributes to match the new time scale
        /// </summary>
        /// <param name="newValue"></param>
        protected virtual void ApplyTimeScale(float newValue)
        {
            // if the new timescale is the same as last time, we don't bother updating it
            if (newValue == TimeScaleLastTime)
            {
                return;
            }

            newValue = Mathf.Clamp(newValue, 0f, 100f);

            if (UpdateTimescale)
            {
                Time.timeScale = newValue;
            }

            if (UpdateFixedDeltaTime && (newValue != 0))
            {
                Time.fixedDeltaTime = InitialFixedDeltaTime * newValue;
            }

            if (UpdateMaximumDeltaTime)
            {
                Time.maximumDeltaTime = InitialMaximumDeltaTime * newValue;
            }

            CurrentTimeScale = Time.timeScale;
            TimeScaleLastTime = CurrentTimeScale;
        }

        /// <summary>
        /// Resets all stacked time scale changes and simply sets the time scale, until further changes
        /// </summary>
        /// <param name="newTimeScale">New time scale.</param>
        protected virtual void SetTimeScale(float newTimeScale)
        {
            TimeScaleProperties.Clear();
            ApplyTimeScale(newTimeScale);
        }

        /// <summary>
        /// Sets the time scale for the specified properties (duration, time scale, lerp or not, and lerp speed)
        /// </summary>
        /// <param name="timeScaleProperties">Time scale properties.</param>
        protected virtual void SetTimeScale(TimeScaleProperties timeScaleProperties)
        {
            if (timeScaleProperties.TimeScaleLerp &&
                timeScaleProperties.TimeScaleLerpMode == MMTimeScaleLerpModes.Duration)
            {
                timeScaleProperties.Duration = timeScaleProperties.Duration + timeScaleProperties.TimeScaleLerpDuration;
            }
            StartedAt = Time.unscaledTime;
            TimeScaleProperties.Push(timeScaleProperties);
        }

        /// <summary>
        /// Resets the time scale to the stored normal time scale
        /// </summary>
        public virtual void ResetTimeScale()
        {
            SetTimeScale(NormalTimeScale);
        }

        /// <summary>
        /// Resets the time scale to the last saved time scale.
        /// </summary>
        public virtual void Unfreeze()
        {
            if (TimeScaleProperties.Count > 0)
            {
                ResetProperty = TimeScaleProperties.Peek();
                TimeScaleProperties.Pop();
            }

            if (TimeScaleProperties.Count == 0)
            {
                if (ResetProperty.TimeScaleLerp && ResetProperty.TimeScaleLerpMode == MMTimeScaleLerpModes.Duration && ResetProperty.TimeScaleLerpOnUnfreeze)
                {
                    LerpingBackToNormal = true;
                    MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, NormalTimeScale, ResetProperty.TimeScaleLerpDurationOnUnfreeze, ResetProperty.TimeScaleLerp,
                        ResetProperty.LerpSpeed, true, MMTimeScaleLerpModes.Duration, ResetProperty.TimeScaleLerpCurveOnUnfreeze, ResetProperty.TimeScaleLerpDurationOnUnfreeze);
                }
                else
                {
                    ResetTimeScale();
                }
            }
        }

        /// <summary>
        /// Sets the time scale to the specified value, instantly
        /// </summary>
        /// <param name="newNormalTimeScale">New normal time scale.</param>
        public virtual void SetTimeScaleTo(float newNormalTimeScale)
        {
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, newNormalTimeScale, 0f, false, 0f, true);
        }

        /// <summary>
        /// Catches TimeScaleEvents and acts on them
        /// </summary>
        /// <param name="timeScaleEvent">MMTimeScaleEvent event.</param>
        public virtual void OnTimeScaleEvent(MMTimeScaleMethods timeScaleMethod, float timeScale, float duration, bool lerp, float lerpSpeed, bool infinite,
            MMTimeScaleLerpModes timeScaleLerpMode = MMTimeScaleLerpModes.Speed, MMTweenType timeScaleLerpCurve = null, float timeScaleLerpDuration = 0.2f,
            bool timeScaleLerpOnUnfreeze = false, MMTweenType timeScaleLerpCurveOnUnfreeze = null, float timeScaleLerpDurationOnUnfreeze = 0.2f)
        {
            TimeScaleProperties timeScaleProperty = new TimeScaleProperties();
            timeScaleProperty.TimeScale = timeScale;
            timeScaleProperty.Duration = duration;
            timeScaleProperty.TimeScaleLerp = lerp;
            timeScaleProperty.LerpSpeed = lerpSpeed;
            timeScaleProperty.Infinite = infinite;
            timeScaleProperty.TimeScaleLerpOnUnfreeze = timeScaleLerpOnUnfreeze;
            timeScaleProperty.TimeScaleLerpCurveOnUnfreeze = timeScaleLerpCurveOnUnfreeze;
            timeScaleProperty.TimeScaleLerpDurationOnUnfreeze = timeScaleLerpDurationOnUnfreeze;
            timeScaleProperty.TimeScaleLerpMode = timeScaleLerpMode;
            timeScaleProperty.TimeScaleLerpCurve = timeScaleLerpCurve;
            timeScaleProperty.TimeScaleLerpDuration = timeScaleLerpDuration;
            InitialTimeScale = Time.timeScale;

            switch (timeScaleMethod)
            {
                case MMTimeScaleMethods.Reset:
                    ResetTimeScale();
                    break;

                case MMTimeScaleMethods.For:
                    SetTimeScale(timeScaleProperty);
                    break;

                case MMTimeScaleMethods.Unfreeze:
                    Unfreeze();
                    break;
            }
        }

        /// <summary>
        /// When getting a freeze frame event we stop the time
        /// </summary>
        /// <param name="freezeFrameEvent">Freeze frame event.</param>
        public virtual void OnMMFreezeFrameEvent(float duration)
        {
            TimeScaleProperties properties = new TimeScaleProperties();
            properties.Duration = duration;
            properties.TimeScaleLerp = false;
            properties.LerpSpeed = 0f;
            properties.TimeScale = 0f;
            SetTimeScale(properties);
        }

        /// <summary>
        /// On enable, starts listening for FreezeFrame events
        /// </summary>
        void OnEnable()
        {
            MMFreezeFrameEvent.Register(OnMMFreezeFrameEvent);
            MMTimeScaleEvent.Register(OnTimeScaleEvent);
        }

        /// <summary>
        /// On disable, stops listening for FreezeFrame events
        /// </summary>
        void OnDisable()
        {
            MMFreezeFrameEvent.Unregister(OnMMFreezeFrameEvent);
            MMTimeScaleEvent.Unregister(OnTimeScaleEvent);
        }
    }
}