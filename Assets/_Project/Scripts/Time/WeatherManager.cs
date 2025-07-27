using UnityEngine;
using System.Collections;

namespace Sol
{
    /// <summary>
    /// Enumeration of possible weather states
    /// </summary>
    public enum WeatherState
    {
        Clear,
        Snowing,
        // Future expansion: Rain, Storm, Fog, etc.
    }

    /// <summary>
    /// Manages dynamic weather patterns based on seasonal data and time progression
    /// Handles weather state transitions, probability calculations, and seasonal weather variations
    /// Updated with proper state management and transition handling
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        [Header("Weather System Configuration")]
        [SerializeField] private bool enableWeatherSystem = true;
        [SerializeField] private float weatherTransitionDuration = 30f;
        [SerializeField] private AnimationCurve weatherTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool logWeatherChanges = true;
        [SerializeField] private bool logWeatherEvents = false;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem snowParticleSystem;
        [SerializeField] private bool autoFindParticleSystem = true;
        [SerializeField] private string snowParticleSystemName = "SnowParticleSystem";
        
        private ParticleSystem.EmissionModule _snowEmission;
        private bool _particleSystemInitialized = false;

        // Core references
        private ITimeManager _timeManager;
        
        // Weather state management
        private WeatherState _currentWeatherState = WeatherState.Clear;
        private WeatherState _targetWeatherState = WeatherState.Clear;
        
        // Transition state management
        private bool _isTransitioning = false;
        private WeatherState _transitionFromState = WeatherState.Clear;
        private WeatherState _transitionToState = WeatherState.Clear;
        private float _transitionStartTime = 0f;
        private float _transitionProgress = 0f;
        
        // Weather timing
        private float _currentWeatherStartTime = 0f;
        private float _currentWeatherDuration = 3600f; // Default 1 hour
        private float _nextWeatherCheckTime = 0f;
        
        // Weather intensity (for future expansion)
        private float _currentWeatherIntensity = 0f;
        private float _targetWeatherIntensity = 0f;

        // Events
        public System.Action<WeatherState> OnWeatherChanged;
        public System.Action<WeatherState, WeatherState, float> OnWeatherTransitionUpdate;
        public System.Action<float> OnWeatherIntensityChanged;

        // Public properties
        public WeatherState CurrentWeatherState => _currentWeatherState;
        public WeatherState TargetWeatherState => _targetWeatherState;
        public bool IsTransitioning => _isTransitioning;
        public float TransitionProgress => _transitionProgress;
        public float CurrentWeatherIntensity => _currentWeatherIntensity;
        public bool WeatherSystemEnabled => enableWeatherSystem;

        private void Awake()
        {
            _timeManager = FindObjectOfType<TimeManager>();
            if (_timeManager == null)
            {
                Debug.LogError("WeatherManager requires a TimeManager in the scene!");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            InitializeWeatherSystem();
        }

        private void Update()
        {
            if (!enableWeatherSystem) return;

            CheckForWeatherChanges();
            
            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }

        private void InitializeWeatherSystem()
        {
            _currentWeatherState = WeatherState.Clear;
            _targetWeatherState = WeatherState.Clear;
            _currentWeatherStartTime = _timeManager.CelestialTime;
            _isTransitioning = false;
            _transitionProgress = 1f;
            _currentWeatherIntensity = 0f;
            _targetWeatherIntensity = 0f;
    
            // Initialize particle system
            InitializeParticleSystem();
    
            // Schedule first weather check
            ScheduleNextWeatherCheck();
    
            if (logWeatherChanges)
            {
                Debug.Log("Weather system initialized - Starting with clear weather");
            }
        }
        
        private void InitializeParticleSystem()
        {
            // Auto-find particle system if not assigned
            if (snowParticleSystem == null && autoFindParticleSystem)
            {
                // Try to find by name first
                GameObject snowObject = GameObject.Find(snowParticleSystemName);
                if (snowObject != null)
                {
                    snowParticleSystem = snowObject.GetComponent<ParticleSystem>();
                }
        
                // If not found by name, try to find any ParticleSystem
                if (snowParticleSystem == null)
                {
                    snowParticleSystem = FindObjectOfType<ParticleSystem>();
                }
            }
    
            if (snowParticleSystem != null)
            {
                _snowEmission = snowParticleSystem.emission;
                _particleSystemInitialized = true;
        
                // Set initial state
                UpdateParticleSystemEmission();
        
                if (logWeatherChanges)
                {
                    Debug.Log($"Snow particle system initialized: {snowParticleSystem.name}");
                }
            }
            else
            {
                if (logWeatherChanges)
                {
                    Debug.LogWarning("Snow particle system not found! Visual snow effects will not work.");
                }
            }
        }

        private void CheckForWeatherChanges()
        {
            // Priority 1: Handle ongoing transitions
            if (_isTransitioning)
            {
                UpdateWeatherTransition();
                return; // Don't evaluate other changes during transition
            }

            // Priority 2: Check if current weather period has naturally ended
            if (_timeManager.CelestialTime >= _currentWeatherStartTime + _currentWeatherDuration)
            {
                StartNextWeatherPeriod();
                return;
            }

            // Priority 3: Only check for early weather changes if we're past minimum duration AND check time
            if (_timeManager.CelestialTime >= _nextWeatherCheckTime && CanWeatherChangeEarly())
            {
                EvaluateEarlyWeatherChange();
                ScheduleNextWeatherCheck();
            }
        }

        private void UpdateWeatherTransition()
        {
            float elapsedTime = _timeManager.CelestialTime - _transitionStartTime;
            float rawProgress = elapsedTime / weatherTransitionDuration;
    
            _transitionProgress = weatherTransitionCurve.Evaluate(Mathf.Clamp01(rawProgress));
    
            // Update weather intensity during transition
            if (_transitionFromState == WeatherState.Clear && _transitionToState == WeatherState.Snowing)
            {
                _currentWeatherIntensity = _transitionProgress;
            }
            else if (_transitionFromState == WeatherState.Snowing && _transitionToState == WeatherState.Clear)
            {
                _currentWeatherIntensity = 1f - _transitionProgress;
            }
    
            // Update particle system during transition
            UpdateParticleSystemEmission();
    
            OnWeatherTransitionUpdate?.Invoke(_transitionFromState, _transitionToState, _transitionProgress);
            OnWeatherIntensityChanged?.Invoke(_currentWeatherIntensity);
    
            if (rawProgress >= 1f)
            {
                CompleteWeatherTransition();
            }
        }

        private void CompleteWeatherTransition()
        {
            _currentWeatherState = _transitionToState;
            _targetWeatherState = _transitionToState;
            _isTransitioning = false;
            _transitionProgress = 1f;
    
            // Set final intensity
            _currentWeatherIntensity = _currentWeatherState == WeatherState.Snowing ? 1f : 0f;
            _targetWeatherIntensity = _currentWeatherIntensity;
    
            // Update particle system for final state
            UpdateParticleSystemEmission();
    
            OnWeatherChanged?.Invoke(_currentWeatherState);
            OnWeatherIntensityChanged?.Invoke(_currentWeatherIntensity);
    
            if (logWeatherChanges)
            {
                Debug.Log($"Weather transition completed: {_currentWeatherState}");
            }
        }
        
        private void UpdateParticleSystemEmission()
        {
            if (!_particleSystemInitialized || snowParticleSystem == null) return;
    
            bool shouldEmit = _currentWeatherState == WeatherState.Snowing || 
                              (_isTransitioning && _transitionToState == WeatherState.Snowing);
    
            if (_snowEmission.enabled != shouldEmit)
            {
                _snowEmission.enabled = shouldEmit;
        
                if (logWeatherChanges)
                {
                    Debug.Log($"Snow particle emission: {(shouldEmit ? "Enabled" : "Disabled")}");
                }
            }
    
            // Optional: Adjust emission rate based on weather intensity
            if (shouldEmit && _snowEmission.enabled)
            {
                var rateOverTime = _snowEmission.rateOverTime;
                float baseRate = rateOverTime.constant;
                
                rateOverTime.constant = baseRate * _currentWeatherIntensity;
                _snowEmission.rateOverTime = rateOverTime;
            }
        }

        /// <summary>
        /// Checks if weather can change before its scheduled end time
        /// </summary>
        private bool CanWeatherChangeEarly()
        {
            SeasonalData seasonData = _timeManager.GetCurrentSeasonalData();
            if (seasonData == null) return false;

            float timeSinceWeatherStart = _timeManager.CelestialTime - _currentWeatherStartTime;
            
            // Must be past minimum duration
            if (_currentWeatherState == WeatherState.Snowing)
            {
                float minDuration = seasonData.GetMinSnowDurationSeconds(_timeManager.DayLengthInSeconds);
                return timeSinceWeatherStart >= minDuration;
            }
            else if (_currentWeatherState == WeatherState.Clear)
            {
                float minDuration = seasonData.GetMinClearDurationSeconds(_timeManager.DayLengthInSeconds);
                return timeSinceWeatherStart >= minDuration;
            }
            
            return false;
        }

        /// <summary>
        /// Evaluates whether weather should change before its scheduled end time
        /// Uses much lower probability than natural period endings
        /// </summary>
        private void EvaluateEarlyWeatherChange()
        {
            SeasonalData currentSeasonData = _timeManager.GetCurrentSeasonalData();
            if (currentSeasonData == null || !currentSeasonData.WeatherEnabled)
            {
                if (_currentWeatherState != WeatherState.Clear)
                {
                    StartWeatherTransition(WeatherState.Clear);
                }
                return;
            }

            // Very low chance for early weather changes (creates weather stability)
            float earlyChangeChance = 0.05f; // 5% chance per check
            
            // Even lower chance if weather just started recently
            float timeSinceStart = _timeManager.CelestialTime - _currentWeatherStartTime;
            float totalDuration = _currentWeatherDuration;
            float progressThroughPeriod = timeSinceStart / totalDuration;
            
            // Reduce chance if we're early in the weather period
            if (progressThroughPeriod < 0.5f) // First half of period
            {
                earlyChangeChance *= 0.2f; // Much lower chance
            }
            
            float randomValue = Random.Range(0f, 1f);
            
            if (randomValue < earlyChangeChance)
            {
                // Early weather change triggered
                if (_currentWeatherState == WeatherState.Clear)
                {
                    // Chance to start snowing early
                    float snowChance = GetCurrentSnowChance() * 0.1f; // 10% of normal daily chance
                    if (Random.Range(0f, 1f) < snowChance)
                    {
                        StartWeatherTransition(WeatherState.Snowing);
                        if (logWeatherChanges)
                        {
                            Debug.Log($"Early snow start triggered (progress: {progressThroughPeriod:F2}, chance: {earlyChangeChance:F3})");
                        }
                    }
                }
                else if (_currentWeatherState == WeatherState.Snowing)
                {
                    // Chance to stop snowing early
                    StartWeatherTransition(WeatherState.Clear);
                    if (logWeatherChanges)
                    {
                        Debug.Log($"Early snow stop triggered (progress: {progressThroughPeriod:F2}, chance: {earlyChangeChance:F3})");
                    }
                }
            }
        }

        /// <summary>
        /// Starts the next weather period when current period naturally ends
        /// This is the primary way weather changes - not through early checks
        /// </summary>
        private void StartNextWeatherPeriod()
        {
            SeasonalData seasonData = _timeManager.GetCurrentSeasonalData();
            if (seasonData == null || !seasonData.WeatherEnabled)
            {
                SetWeatherPeriod(WeatherState.Clear, 3600f);
                return;
            }

            WeatherState nextState;
            float duration;

            if (_currentWeatherState == WeatherState.Clear)
            {
                // Currently clear, decide if we should start snowing
                float snowChance = GetCurrentSnowChance();
                
                // Use full daily probability for natural period transitions
                float periodTransitionChance = snowChance * 0.3f; // 30% of daily chance when period ends
                
                if (Random.Range(0f, 1f) < periodTransitionChance)
                {
                    nextState = WeatherState.Snowing;
                    duration = Random.Range(
                        seasonData.GetMinSnowDurationSeconds(_timeManager.DayLengthInSeconds),
                        seasonData.GetMaxSnowDurationSeconds(_timeManager.DayLengthInSeconds)
                    );
                    
                    if (logWeatherChanges)
                    {
                        Debug.Log($"Natural period transition: Starting snow (duration: {duration/60f:F1} minutes, chance: {periodTransitionChance:F3})");
                    }
                }
                else
                {
                    nextState = WeatherState.Clear;
                    duration = Random.Range(
                        seasonData.GetMinClearDurationSeconds(_timeManager.DayLengthInSeconds),
                        seasonData.GetMaxClearDurationSeconds(_timeManager.DayLengthInSeconds)
                    );
                    
                    if (logWeatherChanges)
                    {
                        Debug.Log($"Natural period transition: Continuing clear weather (duration: {duration/60f:F1} minutes)");
                    }
                }
            }
            else
            {
                // Currently snowing, return to clear (snow periods always end naturally)
                nextState = WeatherState.Clear;
                duration = Random.Range(
                    seasonData.GetMinClearDurationSeconds(_timeManager.DayLengthInSeconds),
                    seasonData.GetMaxClearDurationSeconds(_timeManager.DayLengthInSeconds)
                );
                
                if (logWeatherChanges)
                {
                    Debug.Log($"Natural period transition: Snow ended, returning to clear (duration: {duration/60f:F1} minutes)");
                }
            }

            SetWeatherPeriod(nextState, duration);
            
            // Reset the check timer for the new period
            ScheduleNextWeatherCheck();
        }

        /// <summary>
        /// Sets a new weather period with proper state management
        /// </summary>
        private void SetWeatherPeriod(WeatherState newState, float duration)
        {
            if (newState != _currentWeatherState)
            {
                StartWeatherTransition(newState);
            }
            
            // Set the new period timing
            _currentWeatherDuration = duration;
            _currentWeatherStartTime = _timeManager.CelestialTime;
            
            if (showDebugInfo)
            {
                Debug.Log($"Weather period set: {newState} for {duration/60f:F1} minutes");
            }
        }

        /// <summary>
        /// Schedules the next weather check with improved timing logic
        /// </summary>
        private void ScheduleNextWeatherCheck()
        {
            SeasonalData seasonData = _timeManager.GetCurrentSeasonalData();
            if (seasonData == null) return;
            
            float baseCheckInterval = seasonData.GetWeatherCheckIntervalSeconds(_timeManager.DayLengthInSeconds);
            
            // Never check more often than 25% of the minimum duration
            float minDuration = _currentWeatherState == WeatherState.Snowing 
                ? seasonData.GetMinSnowDurationSeconds(_timeManager.DayLengthInSeconds)
                : seasonData.GetMinClearDurationSeconds(_timeManager.DayLengthInSeconds);
            
            float safeCheckInterval = Mathf.Max(baseCheckInterval, minDuration * 0.25f);
            
            // Add some randomness to prevent predictable patterns
            float randomVariation = Random.Range(0.8f, 1.2f);
            safeCheckInterval *= randomVariation;
            
            _nextWeatherCheckTime = _timeManager.CelestialTime + safeCheckInterval;
            
            if (showDebugInfo)
            {
                Debug.Log($"Next weather check in {safeCheckInterval/60f:F1} minutes (base: {baseCheckInterval/60f:F1}, min duration: {minDuration/60f:F1})");
            }
        }
        
                /// <summary>
        /// Starts a weather transition with proper state validation
        /// </summary>
        private void StartWeatherTransition(WeatherState newState)
        {
            if (newState == _currentWeatherState && !_isTransitioning)
            {
                return; // No change needed
            }
            
            // Cancel any existing transition
            if (_isTransitioning)
            {
                if (logWeatherChanges)
                {
                    Debug.Log($"Cancelling transition from {_transitionFromState} to {_transitionToState} (progress: {_transitionProgress:F2})");
                }
            }
            
            _transitionFromState = _currentWeatherState;
            _transitionToState = newState;
            _targetWeatherState = newState;
            _isTransitioning = true;
            _transitionStartTime = _timeManager.CelestialTime;
            _transitionProgress = 0f;
            
            if (logWeatherChanges)
            {
                Debug.Log($"Starting weather transition: {_transitionFromState} → {_transitionToState}");
            }
        }

        /// <summary>
        /// Gets the current snow chance based on seasonal data and seasonal transitions
        /// </summary>
        private float GetCurrentSnowChance()
        {
            SeasonalData currentSeasonData = _timeManager.GetCurrentSeasonalData();
            if (currentSeasonData == null || !currentSeasonData.WeatherEnabled)
                return 0f;

            float baseChance = currentSeasonData.SnowChancePerDay;
            
            // If we're transitioning between seasons, blend the snow chances
            if (_timeManager.IsTransitioning)
            {
                SeasonalData targetSeasonData = _timeManager.GetTargetSeasonalData();
                if (targetSeasonData != null && targetSeasonData.WeatherEnabled)
                {
                    float targetChance = targetSeasonData.SnowChancePerDay;
                    baseChance = Mathf.Lerp(baseChance, targetChance, _timeManager.SeasonTransitionProgress);
                    
                    if (logWeatherEvents)
                    {
                        Debug.Log($"Blending snow chances: {currentSeasonData.SnowChancePerDay:F3} → {targetChance:F3} = {baseChance:F3} (progress: {_timeManager.SeasonTransitionProgress:F2})");
                    }
                }
            }
            
            return Mathf.Clamp01(baseChance);
        }

        private void DisplayDebugInfo()
        {
            string transitionInfo = _isTransitioning 
                ? $"Transitioning: {_transitionFromState} → {_transitionToState} ({_transitionProgress:F2})"
                : "Not transitioning";
                
            float timeInCurrentWeather = _timeManager.CelestialTime - _currentWeatherStartTime;
            float remainingTime = _currentWeatherDuration - timeInCurrentWeather;
            
            Debug.Log($"[WeatherManager] State: {_currentWeatherState}, " +
                     $"Intensity: {_currentWeatherIntensity:F2}, " +
                     $"Time in weather: {timeInCurrentWeather/60f:F1}min, " +
                     $"Remaining: {remainingTime/60f:F1}min, " +
                     $"{transitionInfo}");
        }

        // Public API methods
        public void SetWeatherSystemEnabled(bool enabled)
        {
            enableWeatherSystem = enabled;
            
            if (!enabled && _currentWeatherState != WeatherState.Clear)
            {
                StartWeatherTransition(WeatherState.Clear);
            }
        }

        public void ForceWeatherState(WeatherState state, bool immediate = false)
        {
            if (immediate)
            {
                _currentWeatherState = state;
                _targetWeatherState = state;
                _isTransitioning = false;
                _transitionProgress = 1f;
                _currentWeatherIntensity = state == WeatherState.Snowing ? 1f : 0f;
                _targetWeatherIntensity = _currentWeatherIntensity;
        
                // Update particle system immediately
                UpdateParticleSystemEmission();
        
                OnWeatherChanged?.Invoke(_currentWeatherState);
                OnWeatherIntensityChanged?.Invoke(_currentWeatherIntensity);
        
                if (logWeatherChanges)
                {
                    Debug.Log($"Weather forced immediately to: {_currentWeatherState}");
                }
            }
            else
            {
                StartWeatherTransition(state);
            }
    
            // Reset timing for forced weather
            _currentWeatherStartTime = _timeManager.CelestialTime;
            SeasonalData seasonData = _timeManager.GetCurrentSeasonalData();
            if (seasonData != null)
            {
                if (state == WeatherState.Snowing)
                {
                    _currentWeatherDuration = Random.Range(
                        seasonData.GetMinSnowDurationSeconds(_timeManager.DayLengthInSeconds),
                        seasonData.GetMaxSnowDurationSeconds(_timeManager.DayLengthInSeconds)
                    );
                }
                else
                {
                    _currentWeatherDuration = Random.Range(
                        seasonData.GetMinClearDurationSeconds(_timeManager.DayLengthInSeconds),
                        seasonData.GetMaxClearDurationSeconds(_timeManager.DayLengthInSeconds)
                    );
                }
            }
    
            ScheduleNextWeatherCheck();
        }

        public void SetWeatherTransitionDuration(float duration)
        {
            weatherTransitionDuration = Mathf.Max(1f, duration);
        }

        // Editor validation
        private void OnValidate()
        {
            weatherTransitionDuration = Mathf.Max(1f, weatherTransitionDuration);
        }
    }
}