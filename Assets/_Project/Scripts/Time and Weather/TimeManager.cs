using UnityEngine;
using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// Central time management system with calendar support
    /// Manages celestial time, seasonal transitions, and day/year progression
    /// </summary>
    public class TimeManager : MonoBehaviour, ITimeManager
    {
        [Header("Calendar System")]
        [SerializeField] private float dayLengthInSeconds = 7200f;
        [SerializeField] private int daysPerYear = 200;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int currentYear = 1;
        
        [Header("Time Configuration")]
        [SerializeField] private float timeScale = 1f;
        [SerializeField] private float celestialTimeMultiplier = 1f;
        [SerializeField] private bool pauseTime = false;
        
        [Header("Seasonal Data")]
        [SerializeField] private SeasonalData[] seasonalDataArray = new SeasonalData[5];
        [SerializeField] private Season currentSeason = Season.PolarSummer;
        [SerializeField] private Season targetSeason = Season.PolarSummer;
        
        [Header("Seasonal Transitions")]
        [SerializeField] private bool enableSeasonalTransitions = true;
        [SerializeField] private float seasonTransitionDuration = 10f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool logSeasonChanges = true;
        [SerializeField] private bool logDayChanges = true;

        // Time state
        private float _celestialTime = 0f;
        private float _realTimeAtStart;
        private float _dayStartTime = 0f;
        
        // Seasonal transition state
        private bool _isTransitioning = false;
        private float _transitionStartTime;
        private float _transitionProgress = 0f;
        private Season _transitionFromSeason;
        private Season _transitionToSeason;
        
        // Cached seasonal data for performance
        private Dictionary<Season, SeasonalData> _seasonalDataCache;

        // Calendar properties
        public float DayLengthInSeconds => dayLengthInSeconds;
        public int DaysPerYear => daysPerYear;
        public int CurrentDay => currentDay;
        public int CurrentYear => currentYear;
        public int DaysPerSeason => seasonalDataArray.Length > 0 ? daysPerYear / seasonalDataArray.Length : 40;
        public float SeasonDurationInSeconds => DaysPerSeason * dayLengthInSeconds;
        public float DayProgress => (_celestialTime - _dayStartTime) / dayLengthInSeconds;

        // Events - implemented as auto-properties to match interface
        public System.Action<Season> OnSeasonChanged { get; set; }
        public System.Action<Season, Season, float> OnSeasonTransitionUpdate { get; set; }
        public System.Action OnTimeScaleChanged { get; set; }
        public System.Action<int, int> OnDayChanged { get; set; } // day, year
        public System.Action<int> OnYearChanged { get; set; }

        // ITimeManager implementation
        public float CelestialTime => _celestialTime;
        public float TimeScale => timeScale;
        public Season CurrentSeason => currentSeason;
        public Season TargetSeason => targetSeason;
        public float SeasonTransitionProgress => _transitionProgress;
        public bool IsTransitioning => _isTransitioning;

        private void Awake()
        {
            _realTimeAtStart = Time.time;
            _dayStartTime = 0f;
            
            BuildSeasonalDataCache();
            ValidateSeasonalData();
            ValidateCalendarSettings();
        }

        private void Start()
        {
            if (!_isTransitioning)
            {
                targetSeason = currentSeason;
                _transitionProgress = 1f;
            }
    
            if (logSeasonChanges)
            {
                Debug.Log($"TimeManager initialized. Season: {currentSeason}, Day: {currentDay}, Year: {currentYear}");
            }
        }

        private void Update()
        {
            if (!pauseTime)
            {
                UpdateCelestialTime();
                UpdateDayProgression();
            }
            
            if (enableSeasonalTransitions)
            {
                UpdateSeasonalTransitions();
            }
            
            UpdateAutoSeasonProgression();
            
            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }

        private void UpdateCelestialTime()
        {
            float deltaTime = Time.deltaTime * timeScale * celestialTimeMultiplier;
            _celestialTime += deltaTime;
        }

        private void UpdateDayProgression()
        {
            // Check if a day has passed
            if (_celestialTime - _dayStartTime >= dayLengthInSeconds)
            {
                AdvanceDay();
            }
        }

        private void AdvanceDay()
        {
            currentDay++;
            _dayStartTime = _celestialTime;
            
            // Check for year rollover
            if (currentDay > daysPerYear)
            {
                currentDay = 1;
                currentYear++;
                OnYearChanged?.Invoke(currentYear);
                
                if (logDayChanges)
                {
                    Debug.Log($"New Year! Year {currentYear} has begun.");
                }
            }
            
            OnDayChanged?.Invoke(currentDay, currentYear);
            
            if (logDayChanges)
            {
                Debug.Log($"Day {currentDay}, Year {currentYear} - Season: {currentSeason}");
            }
        }

        private void UpdateSeasonalTransitions()
        {
            if (!_isTransitioning) return;
            
            float elapsedTime = Time.time - _transitionStartTime;
            float rawProgress = elapsedTime / seasonTransitionDuration;
            
            _transitionProgress = transitionCurve.Evaluate(Mathf.Clamp01(rawProgress));
            
            OnSeasonTransitionUpdate?.Invoke(_transitionFromSeason, _transitionToSeason, _transitionProgress);
            
            if (rawProgress >= 1f)
            {
                CompleteSeasonTransition();
            }
        }

        private void UpdateAutoSeasonProgression()
        {
            if (_isTransitioning) return;
            
            // Calculate which season we should be in based on current day
            int seasonIndex = Mathf.FloorToInt((currentDay - 1) / (float)DaysPerSeason);
            seasonIndex = Mathf.Clamp(seasonIndex, 0, seasonalDataArray.Length - 1);
            
            Season expectedSeason = GetSeasonByIndex(seasonIndex);
            
            if (expectedSeason != currentSeason)
            {
                StartSeasonTransition(expectedSeason);
            }
        }

        private Season GetSeasonByIndex(int index)
        {
            if (seasonalDataArray.Length == 0) return Season.PolarSummer;
            
            // Map array index to season enum
            Season[] seasons = { Season.PolarSummer, Season.Transition1, Season.Equinox, Season.Transition2, Season.LongNight };
            return seasons[Mathf.Clamp(index, 0, seasons.Length - 1)];
        }

        private void ValidateCalendarSettings()
        {
            if (dayLengthInSeconds <= 0f)
            {
                Debug.LogWarning("Day length must be positive! Setting to default 2 hours.");
                dayLengthInSeconds = 7200f;
            }
            
            if (daysPerYear <= 0)
            {
                Debug.LogWarning("Days per year must be positive! Setting to default 200.");
                daysPerYear = 200;
            }
            
            if (seasonalDataArray.Length == 0)
            {
                Debug.LogWarning("No seasonal data configured! Add seasonal data assets.");
            }
        }

        private void CompleteSeasonTransition()
        {
            currentSeason = targetSeason;
            _isTransitioning = false;
            _transitionProgress = 1f;
            
            OnSeasonChanged?.Invoke(currentSeason);
            
            if (logSeasonChanges)
            {
                Debug.Log($"Season transition completed. New season: {currentSeason}");
            }
        }

        private void BuildSeasonalDataCache()
        {
            _seasonalDataCache = new Dictionary<Season, SeasonalData>();
            
            foreach (SeasonalData data in seasonalDataArray)
            {
                if (data != null)
                {
                    _seasonalDataCache[data.Season] = data;
                }
            }
        }

        private void ValidateSeasonalData()
        {
            Season[] allSeasons = System.Enum.GetValues(typeof(Season)) as Season[];
            
            foreach (Season season in allSeasons)
            {
                if (!_seasonalDataCache.ContainsKey(season))
                {
                    Debug.LogWarning($"Missing seasonal data for {season}. Some features may not work correctly.");
                }
                else if (!_seasonalDataCache[season].IsValid())
                {
                    Debug.LogWarning($"Invalid seasonal data for {season}. Check configuration.");
                }
            }
        }

        private void DisplayDebugInfo()
        {
            Debug.Log($"[TimeManager] Day {currentDay}/{daysPerYear}, Year {currentYear}, " +
                     $"Season: {currentSeason}, Day Progress: {DayProgress:F2}, " +
                     $"Celestial Time: {_celestialTime:F2}");
        }

        // Public API Implementation
        public void SetTimeScale(float newTimeScale)
        {
            timeScale = Mathf.Max(0f, newTimeScale);
            OnTimeScaleChanged?.Invoke();
        }

        public void SetCelestialTimeMultiplier(float multiplier)
        {
            celestialTimeMultiplier = Mathf.Max(0f, multiplier);
        }

        public void PauseTime()
        {
            pauseTime = true;
        }

        public void ResumeTime()
        {
            pauseTime = false;
        }

        public void SetCelestialTime(float time)
        {
            _celestialTime = time;
        }

        public void StartSeasonTransition(Season newSeason)
        {
            if (newSeason == currentSeason && !_isTransitioning) return;
            
            _transitionFromSeason = currentSeason;
            _transitionToSeason = newSeason;
            targetSeason = newSeason;
            _isTransitioning = true;
            _transitionStartTime = Time.time;
            _transitionProgress = 0f;
            
            if (logSeasonChanges)
            {
                Debug.Log($"Starting season transition from {_transitionFromSeason} to {_transitionToSeason}");
            }
        }

        public void SetSeason(Season season, bool immediate = false)
        {
            if (immediate)
            {
                currentSeason = season;
                targetSeason = season;
                _isTransitioning = false;
                _transitionProgress = 1f;
                
                OnSeasonChanged?.Invoke(currentSeason);
                
                if (logSeasonChanges)
                {
                    Debug.Log($"Season changed immediately to: {currentSeason}");
                }
            }
            else
            {
                StartSeasonTransition(season);
            }
        }

        public SeasonalData GetCurrentSeasonalData()
        {
            return _seasonalDataCache.ContainsKey(currentSeason) ? _seasonalDataCache[currentSeason] : null;
        }

        public SeasonalData GetTargetSeasonalData()
        {
            return _seasonalDataCache.ContainsKey(targetSeason) ? _seasonalDataCache[targetSeason] : null;
        }

        public SeasonalData GetSeasonalData(Season season)
        {
            return _seasonalDataCache.ContainsKey(season) ? _seasonalDataCache[season] : null;
        }

        // Calendar API methods
        public void SetDay(int day, int year)
        {
            currentDay = Mathf.Clamp(day, 1, daysPerYear);
            currentYear = Mathf.Max(1, year);
            _dayStartTime = _celestialTime - (DayProgress * dayLengthInSeconds);
        }

        public void SetDayLength(float lengthInSeconds)
        {
            dayLengthInSeconds = Mathf.Max(1f, lengthInSeconds);
        }

        // Editor validation
        private void OnValidate()
        {
            timeScale = Mathf.Max(0f, timeScale);
            celestialTimeMultiplier = Mathf.Max(0f, celestialTimeMultiplier);
            seasonTransitionDuration = Mathf.Max(0.1f, seasonTransitionDuration);
            dayLengthInSeconds = Mathf.Max(1f, dayLengthInSeconds);
            daysPerYear = Mathf.Max(1, daysPerYear);
            currentDay = Mathf.Clamp(currentDay, 1, daysPerYear);
            currentYear = Mathf.Max(1, currentYear);
    
            if (Application.isPlaying && _seasonalDataCache != null)
            {
                BuildSeasonalDataCache();
            }
        }
    }
}