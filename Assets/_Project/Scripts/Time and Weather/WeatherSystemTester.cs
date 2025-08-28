using UnityEngine;
using TMPro;

namespace Sol
{
    /// <summary>
    /// Simple monitoring tool for the weather system
    /// Displays current weather status and basic statistics with countdown timers
    /// </summary>
    public class WeatherSystemTester : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private WeatherManager weatherManager;
        [SerializeField] private TimeManager timeManager;
        
        [Header("UI Display")]
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Settings")]
        [SerializeField] private float updateFrequency = 0.1f; // More frequent for countdown
        [SerializeField] private bool logWeatherChanges = true;
        [SerializeField] private bool logDetailedTiming = true; // New: detailed timing logs
        
        private float _lastUpdateTime;
        private WeatherState _lastWeatherState = WeatherState.Clear;
        private float _lastStateChangeTime;

        private void Start()
        {
            // Auto-find components if not assigned
            if (weatherManager == null)
                weatherManager = FindObjectOfType<WeatherManager>();
                
            if (timeManager == null)
                timeManager = FindObjectOfType<TimeManager>();

            // Subscribe to weather changes
            if (weatherManager != null)
                weatherManager.OnWeatherChanged += OnWeatherChanged;

            // Initialize
            _lastWeatherState = weatherManager?.CurrentState ?? WeatherState.Clear;
            _lastStateChangeTime = timeManager?.CelestialTime ?? 0f;
        }

        private void Update()
        {
            if (Time.time - _lastUpdateTime >= updateFrequency)
            {
                UpdateDisplay();
                _lastUpdateTime = Time.time;
            }
        }

        private void UpdateDisplay()
        {
            if (statusText == null || weatherManager == null || timeManager == null) return;

            string status = $"<b>Weather System Status</b>\n\n";
            status += $"Current Weather: <color={(weatherManager.CurrentState == WeatherState.Snowing ? "cyan" : "yellow")}>{weatherManager.CurrentState}</color>\n";
            status += $"Is Snowing: {(weatherManager.IsSnowing ? "<color=green>Yes</color>" : "<color=yellow>No</color>")}\n";
            status += $"Season: <color=orange>{timeManager.CurrentSeason}</color>\n";
            status += $"Day: {timeManager.CurrentDay}/{timeManager.DaysPerYear}\n";
            status += $"Time Scale: {timeManager.TimeScale:F1}x\n\n";

            // Add exact countdown timer
            status += GetExactCountdownInfo();
            status += "\n";

            // Current seasonal weather data
            SeasonalData seasonData = timeManager.GetCurrentSeasonalData();
            if (seasonData != null && seasonData.HasWeather && seasonData.WeatherData != null)
            {
                WeatherData weatherData = seasonData.WeatherData;
                status += $"<b>Season Weather Settings:</b>\n";
                status += $"Weather Name: {weatherData.WeatherName}\n";
                status += $"Snow Chance: <color={(weatherData.SnowChance > 0.5f ? "cyan" : "yellow")}>{weatherData.SnowChance * 100f:F1}%</color>\n";
                status += $"Snow Duration: {weatherData.MinSnowDuration:F1}-{weatherData.MaxSnowDuration:F1}h\n";
                status += $"Clear Duration: {weatherData.MinClearDuration:F1}-{weatherData.MaxClearDuration:F1}h\n";
                status += $"Emission Rate: {weatherData.MinSnowEmissionRate:F0}-{weatherData.MaxSnowEmissionRate:F0} particles/sec\n";
                
                if (weatherData.WindSound != null)
                {
                    status += $"Wind Volume: {weatherData.WindVolume * 100f:F0}%\n";
                }
            }
            else
            {
                status += $"<color=gray>No weather data for {timeManager.CurrentSeason}</color>";
            }

            statusText.text = status;
        }

        private string GetExactCountdownInfo()
        {
            float currentTime = timeManager.CelestialTime;
            float timeRemaining = weatherManager.TimeRemaining;
            float totalDuration = weatherManager.CurrentStateDuration;
            float elapsedTime = totalDuration - timeRemaining;
            
            string countdownInfo = $"<b>Current State Timing:</b>\n";
            countdownInfo += $"Total Duration: <color=white>{totalDuration / 60f:F1} minutes</color>\n";
            countdownInfo += $"Elapsed: <color=orange>{elapsedTime / 60f:F1} minutes</color>\n";
            countdownInfo += $"Remaining: <color=cyan>{timeRemaining / 60f:F1} minutes</color>\n";
            
            // Progress bar
            float progress = elapsedTime / totalDuration;
            countdownInfo += $"Progress: <color=yellow>{progress * 100f:F0}%</color>\n";
            
            // Real-time remaining
            float realTimeRemaining = timeRemaining / timeManager.TimeScale;
            countdownInfo += $"Real-time remaining: <color=blue>{realTimeRemaining / 60f:F1} minutes</color>\n";
            
            // Status
            if (timeRemaining > 60f) // More than 1 minute
            {
                countdownInfo += $"Status: <color=green>Active</color>";
            }
            else if (timeRemaining > 0f)
            {
                countdownInfo += $"Status: <color=yellow>Ending soon</color>";
            }
            else
            {
                countdownInfo += $"Status: <color=red>Should change now</color>";
            }

            return countdownInfo;
        }

        private void OnWeatherChanged(WeatherState newWeatherState)
        {
            float currentTime = timeManager.CelestialTime;
            float previousStateDuration = currentTime - _lastStateChangeTime;
            
            if (logWeatherChanges)
            {
                Debug.Log($"[WeatherTester] Weather changed: {_lastWeatherState} → {newWeatherState}");
                Debug.Log($"[WeatherTester] Previous {_lastWeatherState} lasted: {previousStateDuration / 60f:F1} minutes");
            }

            if (logDetailedTiming)
            {
                LogNewStateDetails(newWeatherState);
            }
            
            _lastWeatherState = newWeatherState;
            _lastStateChangeTime = currentTime;
        }
        
                private void LogNewStateDetails(WeatherState newState)
        {
            float exactDuration = weatherManager.CurrentStateDuration;
            float realTimeDuration = exactDuration / timeManager.TimeScale;
            
            if (newState == WeatherState.Snowing)
            {
                Debug.Log($"[WeatherTester] ❄️ SNOW STARTED");
                Debug.Log($"[WeatherTester] Exact duration: {exactDuration / 60f:F1} minutes ({exactDuration / 3600f:F2} hours)");
                Debug.Log($"[WeatherTester] Real-time duration: {realTimeDuration / 60f:F1} minutes at {timeManager.TimeScale:F1}x speed");
                Debug.Log($"[WeatherTester] Will end at celestial time: {weatherManager.StateEndTime:F1}");
                
                SeasonalData seasonData = timeManager.GetCurrentSeasonalData();
                if (seasonData?.WeatherData != null)
                {
                    WeatherData weatherData = seasonData.WeatherData;
                    Debug.Log($"[WeatherTester] Emission range: {weatherData.MinSnowEmissionRate:F0}-{weatherData.MaxSnowEmissionRate:F0} particles/sec");
                    
                    if (weatherData.WindSound != null)
                    {
                        Debug.Log($"[WeatherTester] Wind sound: {weatherData.WindSound.name} at {weatherData.WindVolume * 100f:F0}% volume");
                    }
                }
            }
            else
            {
                Debug.Log($"[WeatherTester] ☀️ CLEAR WEATHER");
                Debug.Log($"[WeatherTester] Exact duration: {exactDuration / 60f:F1} minutes ({exactDuration / 3600f:F2} hours)");
                Debug.Log($"[WeatherTester] Real-time duration: {realTimeDuration / 60f:F1} minutes at {timeManager.TimeScale:F1}x speed");
                Debug.Log($"[WeatherTester] Will end at celestial time: {weatherManager.StateEndTime:F1}");
                
                SeasonalData seasonData = timeManager.GetCurrentSeasonalData();
                if (seasonData?.WeatherData != null)
                {
                    float snowChance = seasonData.WeatherData.SnowChance;
                    Debug.Log($"[WeatherTester] Next snow chance: {snowChance * 100f:F1}%");
                }
            }
        }

        private void OnDestroy()
        {
            if (weatherManager != null)
                weatherManager.OnWeatherChanged -= OnWeatherChanged;
        }

        // Simple public methods for testing
        [ContextMenu("Log Current Status")]
        public void LogCurrentStatus()
        {
            if (weatherManager == null || timeManager == null) return;

            float currentTime = timeManager.CelestialTime;
            float timeRemaining = weatherManager.TimeRemaining;
            float totalDuration = weatherManager.CurrentStateDuration;

            Debug.Log($"=== Weather System Status ===");
            Debug.Log($"Weather: {weatherManager.CurrentState} (Is Snowing: {weatherManager.IsSnowing})");
            Debug.Log($"Season: {timeManager.CurrentSeason}");
            Debug.Log($"Day: {timeManager.CurrentDay}");
            Debug.Log($"Time Scale: {timeManager.TimeScale:F1}x");
            Debug.Log($"Total duration: {totalDuration / 60f:F1} minutes");
            Debug.Log($"Time remaining: {timeRemaining / 60f:F1} minutes");
            Debug.Log($"Real-time remaining: {(timeRemaining / timeManager.TimeScale) / 60f:F1} minutes");
            Debug.Log($"Will end at celestial time: {weatherManager.StateEndTime:F1}");
            
            SeasonalData seasonData = timeManager.GetCurrentSeasonalData();
            if (seasonData?.HasWeather == true && seasonData.WeatherData != null)
            {
                WeatherData weatherData = seasonData.WeatherData;
                Debug.Log($"Snow Chance: {weatherData.SnowChance * 100f:F1}%");
            }
            
            Debug.Log($"============================");
        }

        [ContextMenu("Set Fast Testing (10x)")]
        public void SetFastTesting()
        {
            if (timeManager != null)
            {
                timeManager.SetTimeScale(10f);
                Debug.Log("[WeatherTester] Time scale set to 10x - weather changes should be much faster!");
            }
        }

        [ContextMenu("Set Very Fast Testing (60x)")]
        public void SetVeryFastTesting()
        {
            if (timeManager != null)
            {
                timeManager.SetTimeScale(60f);
                Debug.Log("[WeatherTester] Time scale set to 60x - weather changes should be very fast!");
            }
        }

        [ContextMenu("Set Normal Speed (1x)")]
        public void SetNormalSpeed()
        {
            if (timeManager != null)
            {
                timeManager.SetTimeScale(1f);
                Debug.Log("[WeatherTester] Time scale set to 1x");
            }
        }

        [ContextMenu("Force Snow (1 hour)")]
        public void ForceSnow()
        {
            if (weatherManager != null)
            {
                weatherManager.ForceWeatherState(WeatherState.Snowing, 1f);
                Debug.Log("[WeatherTester] Forced snow for 1 hour");
            }
        }

        [ContextMenu("Force Clear (1 hour)")]
        public void ForceClear()
        {
            if (weatherManager != null)
            {
                weatherManager.ForceWeatherState(WeatherState.Clear, 1f);
                Debug.Log("[WeatherTester] Forced clear weather for 1 hour");
            }
        }
    }
}