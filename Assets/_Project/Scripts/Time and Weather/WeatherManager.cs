// using UnityEngine;
// using System.Collections;
// using UnityEngine.Rendering;
//
// namespace Sol
// {
//     public enum WeatherState
//     {
//         Clear,
//         Snowing
//     }
//
//     /// <summary>
//     /// Simple weather system that manages snow/clear states based on seasonal data
//     /// </summary>
//     public class WeatherManager : MonoBehaviour
//     {
//         [Header("System References")]
//         [SerializeField] private ParticleSystem snowParticleSystem; // Only this one!
//         
//         [Header("Transition Settings")]
//         [SerializeField] private float emissionTransitionDuration = 5f; // Initial fade in/out
//         [SerializeField] private AnimationCurve emissionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
//         
//         [Header("Snow Progression Settings")]
//         [SerializeField] private float snowIntensificationDuration = 300f; // 5 minutes to reach max intensity
//         [SerializeField] private AnimationCurve snowProgressionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
//         
//         [Header("Debug")]
//         [SerializeField] private bool enableLogging = true;
//
//         // Events
//         public System.Action<WeatherState> OnWeatherChanged;
//         
//         // Core state
//         private WeatherState _currentState = WeatherState.Clear;
//         private float _stateEndTime = 0f;
//         private float _currentStateDuration = 0f;
//         private float _stateStartTime = 0f;
//         private bool _isForced = false;
//         
//         // Particle system
//         private ParticleSystem.EmissionModule _emission;
//         private Coroutine _emissionCoroutine;
//         
//         // Dependencies
//         private ITimeManager _timeManager;
//
//         // Properties
//         public WeatherState CurrentState => _currentState;
//         public bool IsSnowing => _currentState == WeatherState.Snowing;
//         public float StateEndTime => _stateEndTime;
//         public float CurrentStateDuration => _currentStateDuration;
//         public float TimeRemaining => Mathf.Max(0f, _stateEndTime - _timeManager.CelestialTime);
//         public float StateProgress => _currentStateDuration > 0f ? (_timeManager.CelestialTime - _stateStartTime) / _currentStateDuration : 0f;
//
//         private void Awake()
//         {
//             _timeManager = FindObjectOfType<TimeManager>();
//             InitializeParticleSystem();
//         }
//
//         private void Start()
//         {
//             ScheduleNextStateChange();
//         }
//
//         private void Update()
//         {
//             // Check if current state should end
//             if (!_isForced && _timeManager.CelestialTime >= _stateEndTime)
//             {
//                 ChangeToNextState();
//             }
//             
//             // Update emission rate for snow over time (separate from transition)
//             if (_currentState == WeatherState.Snowing && snowParticleSystem != null)
//             {
//                 UpdateSnowEmissionProgression();
//             }
//         }
//
//         private void InitializeParticleSystem()
//         {
//             if (snowParticleSystem != null)
//             {
//                 _emission = snowParticleSystem.emission;
//                 _emission.enabled = false;
//                 _emission.rateOverTime = 0f;
//         
//                 // Ensure particle system starts properly
//                 if (snowParticleSystem.isPlaying)
//                 {
//                     snowParticleSystem.Stop();
//                 }
//                 snowParticleSystem.Play();
//             }
//         }
//
//         private void ChangeToNextState()
//         {
//             SeasonalData seasonData = _timeManager.GetCurrentSeasonalData();
//             if (seasonData == null || !seasonData.HasWeather)
//             {
//                 SetWeatherState(WeatherState.Clear, 3600f); // Default 1 hour
//                 return;
//             }
//
//             WeatherState nextState;
//     
//             if (_currentState == WeatherState.Clear)
//             {
//                 // Roll for snow using weather data
//                 bool shouldSnow = Random.Range(0f, 1f) < seasonData.WeatherData.SnowChance;
//                 nextState = shouldSnow ? WeatherState.Snowing : WeatherState.Clear;
//             }
//             else
//             {
//                 // Snow always returns to clear
//                 nextState = WeatherState.Clear;
//             }
//
//             float duration = GetRandomDuration(nextState);
//             SetWeatherState(nextState, duration);
//         }
//
//         private float GetRandomDuration(WeatherState state)
//         {
//             SeasonalData seasonData = _timeManager.GetCurrentSeasonalData();
//             if (seasonData?.WeatherData == null) return 3600f; // 1 hour default
//
//             WeatherData weather = seasonData.WeatherData;
//     
//             if (state == WeatherState.Snowing)
//             {
//                 return Random.Range(weather.MinSnowDuration, weather.MaxSnowDuration) * 3600f;
//             }
//             else
//             {
//                 return Random.Range(weather.MinClearDuration, weather.MaxClearDuration) * 3600f;
//             }
//         }
//
//         private void SetWeatherState(WeatherState newState, float duration)
//         {
//             if (newState == _currentState && !_isForced) return;
//
//             _currentState = newState;
//             _currentStateDuration = duration;
//             _stateStartTime = _timeManager.CelestialTime;
//             _stateEndTime = _timeManager.CelestialTime + duration;
//             
//             StartEmissionTransition();
//             OnWeatherChanged?.Invoke(_currentState);
//
//             if (enableLogging)
//             {
//                 Debug.Log($"Weather changed to {_currentState} for exactly {duration/60f:F1} minutes (until {_stateEndTime:F1})");
//                 
//                 if (newState == WeatherState.Snowing)
//                 {
//                     SeasonalData seasonData = _timeManager.GetCurrentSeasonalData();
//                     if (seasonData?.WeatherData != null)
//                     {
//                         Debug.Log($"Snow will transition to {seasonData.WeatherData.MinSnowEmissionRate:F0} particles/sec over {emissionTransitionDuration:F1}s, then intensify to {seasonData.WeatherData.MaxSnowEmissionRate:F0} over {snowIntensificationDuration/60f:F1} minutes");
//                     }
//                 }
//             }
//         }
//
//         private void ScheduleNextStateChange()
//         {
//             float duration = GetRandomDuration(_currentState);
//             _currentStateDuration = duration;
//             _stateStartTime = _timeManager.CelestialTime;
//             _stateEndTime = _timeManager.CelestialTime + duration;
//             
//             if (enableLogging)
//             {
//                 Debug.Log($"Current {_currentState} will last exactly {duration/60f:F1} minutes (until {_stateEndTime:F1})");
//             }
//         }
//
//         private void StartEmissionTransition()
//         {
//             if (snowParticleSystem == null) return;
//
//             if (_emissionCoroutine != null)
//             {
//                 StopCoroutine(_emissionCoroutine);
//             }
//
//             _emissionCoroutine = StartCoroutine(TransitionEmission());
//         }
//
//         private IEnumerator TransitionEmission()
//         {
//             SeasonalData seasonData = _timeManager.GetCurrentSeasonalData();
//             float targetRate = 0f;
//     
//             if (_currentState == WeatherState.Snowing && seasonData?.WeatherData != null)
//             {
//                 // Transition to minimum emission rate first
//                 targetRate = seasonData.WeatherData.MinSnowEmissionRate;
//                 
//                 if (enableLogging)
//                 {
//                     Debug.Log($"Snow emission transitioning to: {targetRate:F0} particles/sec over {emissionTransitionDuration:F1}s");
//                 }
//             }
//
//             float startRate = _emission.rateOverTime.constant;
//             _emission.enabled = true;
//
//             float elapsed = 0f;
//             while (elapsed < emissionTransitionDuration)
//             {
//                 elapsed += Time.deltaTime;
//                 float progress = emissionCurve.Evaluate(elapsed / emissionTransitionDuration);
//                 float currentRate = Mathf.Lerp(startRate, targetRate, progress);
//
//                 _emission.rateOverTime = currentRate;
//                 yield return null;
//             }
//
//             _emission.rateOverTime = targetRate;
//             _emission.enabled = targetRate > 0f;
//
//             if (enableLogging)
//             {
//                 Debug.Log($"Emission transition complete: {targetRate:F0} particles/sec. Snow will now intensify over time.");
//             }
//
//             _emissionCoroutine = null;
//         }
//
//         private void UpdateSnowEmissionProgression()
//         {
//             SeasonalData seasonData = _timeManager.GetCurrentSeasonalData();
//             if (seasonData?.WeatherData == null) return;
//
//             WeatherData weatherData = seasonData.WeatherData;
//             
//             // Calculate progression over the intensification duration (not the entire snow duration)
//             float timeSinceSnowStart = _timeManager.CelestialTime - _stateStartTime;
//             float intensificationProgress = Mathf.Clamp01(timeSinceSnowStart / snowIntensificationDuration);
//             
//             // Apply curve to the progression
//             float curvedProgress = snowProgressionCurve.Evaluate(intensificationProgress);
//
//             // Lerp from min to max emission rate over the intensification period
//             float targetEmissionRate = Mathf.Lerp(weatherData.MinSnowEmissionRate, weatherData.MaxSnowEmissionRate, curvedProgress);
//             
//             // Only update if we're past the initial transition
//             if (_emissionCoroutine == null) // Transition is complete
//             {
//                 _emission.rateOverTime = targetEmissionRate;
//
//                 // Log progression periodically
//                 if (enableLogging && Time.frameCount % 600 == 0 && intensificationProgress < 1f) // Every 10 seconds at 60fps
//                 {
//                     Debug.Log($"Snow intensifying: {targetEmissionRate:F0} particles/sec (progress: {intensificationProgress * 100f:F0}% over {snowIntensificationDuration/60f:F1} min)");
//                 }
//             }
//         }
//
//         // Public API
//         public void ForceWeatherState(WeatherState state, float durationHours)
//         {
//             _isForced = true;
//             float durationSeconds = durationHours * 3600f;
//             SetWeatherState(state, durationSeconds);
//             
//             // Schedule return to normal after forced period
//             StartCoroutine(EndForcedWeather(durationSeconds));
//         }
//
//         private IEnumerator EndForcedWeather(float duration)
//         {
//             yield return new WaitForSeconds(duration);
//             _isForced = false;
//             ChangeToNextState();
//         }
//     }
// }