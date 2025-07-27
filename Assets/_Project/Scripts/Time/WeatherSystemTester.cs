using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sol
{
    /// <summary>
    /// Testing and debugging tool for the weather system
    /// Provides real-time monitoring and statistics without manual intervention
    /// Updated to focus on natural weather behavior observation
    /// </summary>
    public class WeatherSystemTester : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private WeatherManager weatherManager;
        [SerializeField] private TimeManager timeManager;
        
        [Header("UI Display Elements")]
        [SerializeField] private TextMeshProUGUI weatherInfoText;
        [SerializeField] private TextMeshProUGUI timeInfoText;
        [SerializeField] private TextMeshProUGUI seasonInfoText;
        [SerializeField] private TextMeshProUGUI weatherStatsText;
        [SerializeField] private TextMeshProUGUI transitionInfoText;
        
        [Header("Testing Configuration")]
        [SerializeField] private bool enableContinuousLogging = false;
        [SerializeField] private bool logWeatherEvents = true;
        [SerializeField] private bool logTimeScaleChanges = true;
        [SerializeField] private float uiUpdateFrequency = 0.5f;
        
        [Header("Statistics Tracking")]
        [SerializeField] private bool trackWeatherStatistics = true;
        [SerializeField] private bool resetStatsOnSeasonChange = true;
        
        // Weather statistics tracking
        private float _totalTestTime = 0f;
        private float _totalSnowTime = 0f;
        private float _totalClearTime = 0f;
        private int _snowPeriodCount = 0;
        private int _clearPeriodCount = 0;
        private float _lastWeatherChangeTime = 0f;
        private WeatherState _lastWeatherState = WeatherState.Clear;
        private Season _lastSeason = Season.LongNight;
        
        // UI update timing
        private float _lastUIUpdateTime = 0f;
        
        // Weather duration tracking
        private float _currentPeriodStartTime = 0f;
        private float _shortestSnowPeriod = float.MaxValue;
        private float _longestSnowPeriod = 0f;
        private float _shortestClearPeriod = float.MaxValue;
        private float _longestClearPeriod = 0f;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (weatherManager == null)
                weatherManager = FindObjectOfType<WeatherManager>();
                
            if (timeManager == null)
                timeManager = FindObjectOfType<TimeManager>();
                
            ValidateComponents();
        }

        private void Start()
        {
            InitializeTester();
            SubscribeToEvents();
        }

        private void Update()
        {
            if (!ValidateComponents()) return;
            
            UpdateStatistics();
            UpdateUI();
            
            if (enableContinuousLogging)
            {
                LogContinuousInfo();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private bool ValidateComponents()
        {
            if (weatherManager == null)
            {
                Debug.LogError("WeatherSystemTester: WeatherManager not found!");
                return false;
            }
            
            if (timeManager == null)
            {
                Debug.LogError("WeatherSystemTester: TimeManager not found!");
                return false;
            }
            
            return true;
        }

        private void InitializeTester()
        {
            _totalTestTime = 0f;
            _totalSnowTime = 0f;
            _totalClearTime = 0f;
            _snowPeriodCount = 0;
            _clearPeriodCount = 0;
            _lastWeatherChangeTime = timeManager.CelestialTime;
            _lastWeatherState = weatherManager.CurrentWeatherState;
            _lastSeason = timeManager.CurrentSeason;
            _currentPeriodStartTime = timeManager.CelestialTime;
            
            ResetDurationTracking();
            
            if (logWeatherEvents)
            {
                Debug.Log($"WeatherSystemTester initialized - Starting weather: {_lastWeatherState}, Season: {_lastSeason}");
            }
        }

        private void SubscribeToEvents()
        {
            if (weatherManager != null)
            {
                weatherManager.OnWeatherChanged += OnWeatherChanged;
                weatherManager.OnWeatherTransitionUpdate += OnWeatherTransitionUpdate;
                weatherManager.OnWeatherIntensityChanged += OnWeatherIntensityChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (weatherManager != null)
            {
                weatherManager.OnWeatherChanged -= OnWeatherChanged;
                weatherManager.OnWeatherTransitionUpdate -= OnWeatherTransitionUpdate;
                weatherManager.OnWeatherIntensityChanged -= OnWeatherIntensityChanged;
            }
        }

        private void UpdateStatistics()
        {
            if (!trackWeatherStatistics) return;
            
            float deltaTime = Time.deltaTime * timeManager.TimeScale;
            _totalTestTime += deltaTime;
            
            // Track time in current weather state
            if (weatherManager.CurrentWeatherState == WeatherState.Snowing)
            {
                _totalSnowTime += deltaTime;
            }
            else
            {
                _totalClearTime += deltaTime;
            }
            
            // Check for season changes
            if (timeManager.CurrentSeason != _lastSeason)
            {
                OnSeasonChanged(timeManager.CurrentSeason);
            }
        }

        private void UpdateUI()
        {
            if (Time.time - _lastUIUpdateTime < uiUpdateFrequency) return;
            _lastUIUpdateTime = Time.time;
            
            UpdateWeatherInfo();
            UpdateTimeInfo();
            UpdateSeasonInfo();
            UpdateWeatherStats();
            UpdateTransitionInfo();
        }

        private void UpdateWeatherInfo()
        {
            if (weatherInfoText == null) return;
            
            string weatherInfo = $"<b>Weather System Status</b>\n";
            weatherInfo += $"Current State: <color={(weatherManager.CurrentWeatherState == WeatherState.Snowing ? "cyan" : "yellow")}>{weatherManager.CurrentWeatherState}</color>\n";
            weatherInfo += $"System Enabled: {(weatherManager.WeatherSystemEnabled ? "<color=green>Yes</color>" : "<color=red>No</color>")}\n";
            weatherInfo += $"Weather Intensity: {weatherManager.CurrentWeatherIntensity:F2}\n";
            
            // Current period info
            float currentPeriodDuration = timeManager.CelestialTime - _currentPeriodStartTime;
            weatherInfo += $"Current Period: {currentPeriodDuration / 60f:F1} minutes\n";
            
            // Expected durations from seasonal data
            SeasonalData seasonData = timeManager.GetCurrentSeasonalData();
            if (seasonData != null && seasonData.WeatherEnabled)
            {
                if (weatherManager.CurrentWeatherState == WeatherState.Snowing)
                {
                    float minDuration = seasonData.GetMinSnowDurationSeconds(timeManager.DayLengthInSeconds) / 60f;
                    float maxDuration = seasonData.GetMaxSnowDurationSeconds(timeManager.DayLengthInSeconds) / 60f;
                    weatherInfo += $"Expected Snow: {minDuration:F1}-{maxDuration:F1} min\n";
                }
                else
                {
                    float minDuration = seasonData.GetMinClearDurationSeconds(timeManager.DayLengthInSeconds) / 60f;
                    float maxDuration = seasonData.GetMaxClearDurationSeconds(timeManager.DayLengthInSeconds) / 60f;
                    weatherInfo += $"Expected Clear: {minDuration:F1}-{maxDuration:F1} min\n";
                }
                
                weatherInfo += $"Snow Chance/Day: {seasonData.SnowChancePerDay * 100f:F1}%";
            }
            
            weatherInfoText.text = weatherInfo;
        }

        private void UpdateTimeInfo()
        {
            if (timeInfoText == null) return;
            
            float realDayMinutes = (timeManager.DayLengthInSeconds / timeManager.TimeScale) / 60f;
            
            string timeInfo = $"<b>Time System Status</b>\n";
            timeInfo += $"Day {timeManager.CurrentDay}/{timeManager.DaysPerYear}, Year {timeManager.CurrentYear}\n";
            timeInfo += $"Day Progress: {timeManager.DayProgress * 100f:F0}%\n";
            timeInfo += $"Time Scale: <color=orange>{timeManager.TimeScale:F1}x</color>\n";
            timeInfo += $"Real Day Length: {realDayMinutes:F1} minutes\n";
            timeInfo += $"Game Day Length: {timeManager.DayLengthInSeconds / 3600f:F1} hours";
            
            timeInfoText.text = timeInfo;
        }
        
        private void UpdateSeasonInfo()
        {
            if (seasonInfoText == null) return;
    
            string seasonInfo = $"<b>Season Information</b>\n";
            seasonInfo += $"Current Season: <color=cyan>{timeManager.CurrentSeason}</color>\n";
    
            if (timeManager.IsTransitioning)
            {
                seasonInfo += $"Transitioning to: <color=yellow>{timeManager.TargetSeason}</color>\n";
                seasonInfo += $"Transition Progress: {timeManager.SeasonTransitionProgress * 100f:F1}%\n";
            }
            else
            {
                // Calculate season progress manually
                float seasonProgress = CalculateSeasonProgress();
                seasonInfo += $"Season Progress: {seasonProgress * 100f:F1}%\n";
            }
    
            seasonInfo += $"Days in Season: {timeManager.DaysPerSeason}\n";
            seasonInfo += $"Season Duration: {timeManager.SeasonDurationInSeconds / 3600f:F1} hours";
    
            seasonInfoText.text = seasonInfo;
        }

        /// <summary>
        /// Calculates the current progress through the season (0.0 to 1.0)
        /// </summary>
        private float CalculateSeasonProgress()
        {
            // Calculate which day we are in the current season
            int dayInSeason = ((timeManager.CurrentDay - 1) % timeManager.DaysPerSeason) + 1;
    
            // Add the current day progress
            float totalProgress = (dayInSeason - 1 + timeManager.DayProgress) / timeManager.DaysPerSeason;
    
            return Mathf.Clamp01(totalProgress);
        }

        private void UpdateWeatherStats()
        {
            if (weatherStatsText == null || !trackWeatherStatistics) return;
            
            string statsInfo = $"<b>Weather Statistics</b>\n";
            statsInfo += $"Total Test Time: {_totalTestTime / 60f:F1} minutes\n";
            
            if (_totalTestTime > 0)
            {
                float snowPercentage = (_totalSnowTime / _totalTestTime) * 100f;
                float clearPercentage = (_totalClearTime / _totalTestTime) * 100f;
                
                statsInfo += $"Snow Time: {snowPercentage:F1}% ({_totalSnowTime / 60f:F1} min)\n";
                statsInfo += $"Clear Time: {clearPercentage:F1}% ({_totalClearTime / 60f:F1} min)\n";
            }
            
            statsInfo += $"Snow Periods: {_snowPeriodCount}\n";
            statsInfo += $"Clear Periods: {_clearPeriodCount}\n";
            
            // Duration statistics
            if (_snowPeriodCount > 0)
            {
                statsInfo += $"Snow Duration: {_shortestSnowPeriod / 60f:F1}-{_longestSnowPeriod / 60f:F1} min\n";
            }
            
            if (_clearPeriodCount > 0)
            {
                statsInfo += $"Clear Duration: {_shortestClearPeriod / 60f:F1}-{_longestClearPeriod / 60f:F1} min";
            }
            
            weatherStatsText.text = statsInfo;
        }

        private void UpdateTransitionInfo()
        {
            if (transitionInfoText == null) return;
            
            string transitionInfo = $"<b>Transition Status</b>\n";
            
            if (weatherManager.IsTransitioning)
            {
                transitionInfo += $"<color=yellow>Weather Transitioning</color>\n";
                transitionInfo += $"Progress: {weatherManager.TransitionProgress * 100f:F1}%\n";
            }
            else
            {
                transitionInfo += $"<color=green>Weather Stable</color>\n";
            }
            
            if (timeManager.IsTransitioning)
            {
                transitionInfo += $"<color=cyan>Season Transitioning</color>\n";
                transitionInfo += $"Progress: {timeManager.SeasonTransitionProgress * 100f:F1}%";
            }
            else
            {
                transitionInfo += $"<color=green>Season Stable</color>";
            }
            
            transitionInfoText.text = transitionInfo;
        }

        private void LogContinuousInfo()
        {
            // Log every 30 seconds of real time
            if (Time.time % 30f < Time.deltaTime)
            {
                Debug.Log($"[WeatherTester] Weather: {weatherManager.CurrentWeatherState}, " +
                         $"Intensity: {weatherManager.CurrentWeatherIntensity:F2}, " +
                         $"Season: {timeManager.CurrentSeason}, " +
                         $"Day: {timeManager.CurrentDay}, " +
                         $"Time Scale: {timeManager.TimeScale:F1}x");
            }
        }

        private void ResetDurationTracking()
        {
            _shortestSnowPeriod = float.MaxValue;
            _longestSnowPeriod = 0f;
            _shortestClearPeriod = float.MaxValue;
            _longestClearPeriod = 0f;
        }

        // Event handlers
        private void OnWeatherChanged(WeatherState newState)
        {
            float currentTime = timeManager.CelestialTime;
            float periodDuration = currentTime - _currentPeriodStartTime;
            
            // Track the duration of the period that just ended
            if (_lastWeatherState == WeatherState.Snowing)
            {
                _snowPeriodCount++;
                _shortestSnowPeriod = Mathf.Min(_shortestSnowPeriod, periodDuration);
                _longestSnowPeriod = Mathf.Max(_longestSnowPeriod, periodDuration);
            }
            else if (_lastWeatherState == WeatherState.Clear)
            {
                _clearPeriodCount++;
                _shortestClearPeriod = Mathf.Min(_shortestClearPeriod, periodDuration);
                _longestClearPeriod = Mathf.Max(_longestClearPeriod, periodDuration);
            }
            
            if (logWeatherEvents)
            {
                Debug.Log($"[WeatherTester] Weather changed: {_lastWeatherState} → {newState} " +
                         $"(Previous period: {periodDuration / 60f:F1} minutes)");
            }
            
            _lastWeatherState = newState;
            _lastWeatherChangeTime = currentTime;
            _currentPeriodStartTime = currentTime;
        }

        private void OnWeatherTransitionUpdate(WeatherState fromState, WeatherState toState, float progress)
        {
            // Optional: Log transition updates if needed for debugging
            if (enableContinuousLogging && progress % 0.25f < 0.05f) // Log at 25%, 50%, 75%
            {
                Debug.Log($"[WeatherTester] Transition {fromState} → {toState}: {progress * 100f:F0}%");
            }
        }

        private void OnWeatherIntensityChanged(float intensity)
        {
            // Optional: React to intensity changes
        }
        
                private void OnSeasonChanged(Season newSeason)
        {
            if (logWeatherEvents)
            {
                Debug.Log($"[WeatherTester] Season changed: {_lastSeason} → {newSeason}");
            }
            
            if (resetStatsOnSeasonChange)
            {
                ResetStatistics();
                if (logWeatherEvents)
                {
                    Debug.Log("[WeatherTester] Weather statistics reset for new season");
                }
            }
            
            _lastSeason = newSeason;
        }

        // Public methods for external control
        public void ResetStatistics()
        {
            _totalTestTime = 0f;
            _totalSnowTime = 0f;
            _totalClearTime = 0f;
            _snowPeriodCount = 0;
            _clearPeriodCount = 0;
            _currentPeriodStartTime = timeManager.CelestialTime;
            ResetDurationTracking();
            
            if (logWeatherEvents)
            {
                Debug.Log("[WeatherTester] All statistics reset");
            }
        }

        public void SetTimeScale(float scale)
        {
            if (timeManager != null)
            {
                timeManager.SetTimeScale(scale);
                
                if (logTimeScaleChanges)
                {
                    float realDayMinutes = (timeManager.DayLengthInSeconds / scale) / 60f;
                    Debug.Log($"[WeatherTester] Time scale set to {scale:F1}x - Days now take {realDayMinutes:F1} minutes");
                }
            }
        }

        public void ToggleContinuousLogging()
        {
            enableContinuousLogging = !enableContinuousLogging;
            Debug.Log($"[WeatherTester] Continuous logging: {(enableContinuousLogging ? "Enabled" : "Disabled")}");
        }

        public void ToggleStatisticsTracking()
        {
            trackWeatherStatistics = !trackWeatherStatistics;
            Debug.Log($"[WeatherTester] Statistics tracking: {(trackWeatherStatistics ? "Enabled" : "Disabled")}");
        }

        // Quick time scale presets for testing
        public void SetNormalSpeed() => SetTimeScale(1f);
        public void SetFastTesting() => SetTimeScale(10f);
        public void SetVeryFastTesting() => SetTimeScale(60f);
        public void SetExtremelyFastTesting() => SetTimeScale(120f);

        // Utility methods for getting current statistics
        public float GetSnowTimePercentage()
        {
            return _totalTestTime > 0 ? (_totalSnowTime / _totalTestTime) * 100f : 0f;
        }

        public float GetClearTimePercentage()
        {
            return _totalTestTime > 0 ? (_totalClearTime / _totalTestTime) * 100f : 0f;
        }

        public float GetAverageSnowDuration()
        {
            return _snowPeriodCount > 0 ? (_longestSnowPeriod + _shortestSnowPeriod) / 2f / 60f : 0f;
        }

        public float GetAverageClearDuration()
        {
            return _clearPeriodCount > 0 ? (_longestClearPeriod + _shortestClearPeriod) / 2f / 60f : 0f;
        }

        // Debug methods
        public void LogCurrentStatus()
        {
            Debug.Log($"=== Weather System Status ===");
            Debug.Log($"Weather: {weatherManager.CurrentWeatherState} (Intensity: {weatherManager.CurrentWeatherIntensity:F2})");
            Debug.Log($"Season: {timeManager.CurrentSeason} (Day {timeManager.CurrentDay}/{timeManager.DaysPerYear})");
            Debug.Log($"Time Scale: {timeManager.TimeScale:F1}x");
            Debug.Log($"Total Test Time: {_totalTestTime / 60f:F1} minutes");
            Debug.Log($"Snow Time: {GetSnowTimePercentage():F1}% ({_snowPeriodCount} periods)");
            Debug.Log($"Clear Time: {GetClearTimePercentage():F1}% ({_clearPeriodCount} periods)");
            
            if (_snowPeriodCount > 0)
            {
                Debug.Log($"Snow Duration Range: {_shortestSnowPeriod / 60f:F1}-{_longestSnowPeriod / 60f:F1} minutes");
            }
            
            if (_clearPeriodCount > 0)
            {
                Debug.Log($"Clear Duration Range: {_shortestClearPeriod / 60f:F1}-{_longestClearPeriod / 60f:F1} minutes");
            }
            
            Debug.Log($"=============================");
        }

        public void LogSeasonalWeatherData()
        {
            SeasonalData seasonData = timeManager.GetCurrentSeasonalData();
            if (seasonData == null)
            {
                Debug.Log("No seasonal data available");
                return;
            }
            
            Debug.Log($"=== {timeManager.CurrentSeason} Season Weather Data ===");
            Debug.Log($"Weather Enabled: {seasonData.WeatherEnabled}");
            
            if (seasonData.WeatherEnabled)
            {
                Debug.Log($"Snow Chance Per Day: {seasonData.SnowChancePerDay * 100f:F1}%");
                
                float minSnow = seasonData.GetMinSnowDurationSeconds(timeManager.DayLengthInSeconds) / 60f;
                float maxSnow = seasonData.GetMaxSnowDurationSeconds(timeManager.DayLengthInSeconds) / 60f;
                Debug.Log($"Snow Duration: {minSnow:F1}-{maxSnow:F1} minutes");
                
                float minClear = seasonData.GetMinClearDurationSeconds(timeManager.DayLengthInSeconds) / 60f;
                float maxClear = seasonData.GetMaxClearDurationSeconds(timeManager.DayLengthInSeconds) / 60f;
                Debug.Log($"Clear Duration: {minClear:F1}-{maxClear:F1} minutes");
                
                float checkInterval = seasonData.GetWeatherCheckIntervalSeconds(timeManager.DayLengthInSeconds) / 60f;
                Debug.Log($"Check Interval: {checkInterval:F1} minutes");
            }
            
            Debug.Log($"=======================================");
        }

        // Editor validation
        private void OnValidate()
        {
            uiUpdateFrequency = Mathf.Max(0.1f, uiUpdateFrequency);
        }

        // Context menu methods for easy testing in editor
        [ContextMenu("Reset Statistics")]
        private void ContextMenuResetStats()
        {
            ResetStatistics();
        }

        [ContextMenu("Log Current Status")]
        private void ContextMenuLogStatus()
        {
            LogCurrentStatus();
        }

        [ContextMenu("Log Seasonal Data")]
        private void ContextMenuLogSeasonalData()
        {
            LogSeasonalWeatherData();
        }

        [ContextMenu("Set Fast Testing (10x)")]
        private void ContextMenuFastTesting()
        {
            SetFastTesting();
        }

        [ContextMenu("Set Very Fast Testing (60x)")]
        private void ContextMenuVeryFastTesting()
        {
            SetVeryFastTesting();
        }

        [ContextMenu("Set Normal Speed (1x)")]
        private void ContextMenuNormalSpeed()
        {
            SetNormalSpeed();
        }
    }
}