using System;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Core time management system for Sol planetary time simulation.
    /// Handles day/night cycles, seasonal progression, and calendar integration.
    /// </summary>
    public class TimeManager : MonoBehaviour, ITimeManager
    {
        [Header("Configuration")]
        [SerializeField] public WorldTimeData worldTimeData;
        
        [Header("Starting Values")]
        [SerializeField] private float startingCelestialTime = 0.5f;
        [SerializeField] private int startingDay = 1;
        [SerializeField] private int startingYear = 1;
        
        [Header("Time Control")]
        [SerializeField] private float timeScale = 1f;
        [SerializeField] private bool pauseTime = false;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = false;

        // Core time state
        private float celestialTime;
        private int currentDay;
        private int currentYear;
        
        // Season state
        private int currentSeasonIndex;
        private string currentSeasonName = "";
        private WorldTimeData.SeasonRange currentSeasonRange;
        
        // Transition state
        private bool isInSeasonTransition;
        private int targetSeasonIndex = -1;
        private string targetSeasonName = "";
        private float seasonTransitionProgress;
        
        // Calendar state
        private Month currentMonth;
        private int currentDayOfMonth;
        
        // Time of day state
        private TimeOfDay currentTimeOfDay;
        
        // Cached data
        private GameTime cachedGameTime;
        private float lastCacheUpdateTime;

        // Events
        public event Action<float> OnCelestialTimeChanged;
        public event Action<int> OnDayChanged;
        public event Action<int> OnYearChanged;
        public event Action<int, string> OnSeasonChanged;
        public event Action<TimeOfDay> OnTimeOfDayChanged;
        public event Action<Month> OnMonthChanged;

        #region Properties

        public float CelestialTime => celestialTime;
        public int CurrentDay => currentDay;
        public int CurrentYear => currentYear;
        public int CurrentSeasonIndex => currentSeasonIndex;
        public string CurrentSeasonName => currentSeasonName;
        public int TargetSeasonIndex => targetSeasonIndex;
        public string TargetSeasonName => targetSeasonName;
        public bool IsInSeasonTransition => isInSeasonTransition;
        public float SeasonTransitionProgress => seasonTransitionProgress;
        public TimeOfDay CurrentTimeOfDay => currentTimeOfDay;
        public Month CurrentMonth => currentMonth;
        public int CurrentDayOfMonth => currentDayOfMonth;
        public float TimeScale => timeScale;
        public bool IsPaused => pauseTime;
        public GameTime CurrentGameTime => cachedGameTime;

        public int DaysInCurrentSeason => currentSeasonRange.duration;
        public float SeasonProgress => currentSeasonRange.GetProgressForDay(currentDay);
        public float YearProgress => worldTimeData != null ? (float)currentDay / worldTimeData.GetTotalSeasonDays() : 0f;

        public string CurrentTimeDisplay
        {
            get
            {
                if (worldTimeData == null) return "00:00:00";
                
                var gameTime = GetCurrentGameTime();
                return $"{gameTime.hours:D2}:{gameTime.minutes:D2}:{gameTime.seconds:D2}";
            }
        }

        public string CurrentTimeDisplay12Hour
        {
            get
            {
                if (worldTimeData == null) return "12:00:00 AM";
                
                var gameTime = GetCurrentGameTime();
                int displayHour = gameTime.hours == 0 ? 12 : (gameTime.hours > 12 ? gameTime.hours - 12 : gameTime.hours);
                string ampm = gameTime.hours < 12 ? "AM" : "PM";
                return $"{displayHour:D2}:{gameTime.minutes:D2}:{gameTime.seconds:D2} {ampm}";
            }
        }

        public string CurrentDateDisplay => $"Day {currentDay}, Year {currentYear}, {currentSeasonName}";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeTimeManager();
        }

        private void Start()
        {
            ValidateConfiguration();
            UpdateAllTimeInfo();
            
            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Initialized - Day {currentDay}, Year {currentYear}, Season {currentSeasonName}");
            }
        }

        private void Update()
        {
            if (pauseTime || worldTimeData == null) return;

            UpdateCelestialTime();
            UpdateCachedGameTime();
            UpdateAllTimeInfo();
        }

        #endregion

        #region Initialization

        private void InitializeTimeManager()
        {
            celestialTime = Mathf.Clamp01(startingCelestialTime);
            currentDay = Mathf.Max(1, startingDay);
            currentYear = Mathf.Max(1, startingYear);

            cachedGameTime = new GameTime();
            isInSeasonTransition = false;
            targetSeasonIndex = -1;
            targetSeasonName = "";
            seasonTransitionProgress = 0f;

            InitializeCalendarState();
        }

        private void InitializeCalendarState()
        {
            if (worldTimeData == null || worldTimeData.MonthsPerYear == 0) return;

            try
            {
                currentMonth = worldTimeData.GetMonthForDay(currentDay);
                currentDayOfMonth = worldTimeData.GetDayOfMonth(currentDay);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TimeManager] Failed to initialize calendar state: {ex.Message}");
                currentMonth = new Month("Unknown", 0, worldTimeData.daysPerMonth);
                currentDayOfMonth = 1;
            }
        }

        #endregion

        #region Time Updates

        private void UpdateCelestialTime()
        {
            float deltaTime = Time.deltaTime * timeScale;
            float timeIncrement = deltaTime / worldTimeData.dayLengthInSeconds;
            
            float previousCelestialTime = celestialTime;
            celestialTime += timeIncrement;

            if (celestialTime >= 1f)
            {
                celestialTime -= 1f;
                AdvanceDay();
            }

            if (Mathf.Abs(celestialTime - previousCelestialTime) > 0.001f)
            {
                OnCelestialTimeChanged?.Invoke(celestialTime);
            }
        }

        private void AdvanceDay()
        {
            currentDay++;
            
            int totalDays = worldTimeData.GetTotalSeasonDays();
            if (totalDays > 0 && currentDay > totalDays)
            {
                currentDay = 1;
                currentYear++;
                OnYearChanged?.Invoke(currentYear);
            }

            OnDayChanged?.Invoke(currentDay);
            UpdateCalendarState();
        }

        private void UpdateAllTimeInfo()
        {
            UpdateTimeOfDay();
            UpdateSeasonInfo();
            UpdateCalendarState();
        }

        #endregion

        #region Season Management

        private void UpdateSeasonInfo()
        {
            if (worldTimeData == null) return;

            int newSeasonIndex = worldTimeData.GetSeasonIndexForDay(currentDay);
            string newSeasonName = worldTimeData.GetSeasonName(newSeasonIndex);
            
            if (newSeasonIndex != currentSeasonIndex)
            {
                HandleSeasonChange(newSeasonIndex, newSeasonName);
            }
            else
            {
                currentSeasonRange = worldTimeData.GetSeasonRange(currentSeasonIndex);
            }

            UpdateSeasonTransition();
        }

        private void HandleSeasonChange(int newSeasonIndex, string newSeasonName)
        {
            currentSeasonIndex = newSeasonIndex;
            currentSeasonName = newSeasonName;
            currentSeasonRange = worldTimeData.GetSeasonRange(currentSeasonIndex);

            OnSeasonChanged?.Invoke(currentSeasonIndex, currentSeasonName);
            
            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Season Changed to: {currentSeasonName}");
            }
        }

        #endregion
                #region Season Transitions

        private void UpdateSeasonTransition()
        {
            if (worldTimeData == null) return;

            int transitionDays = worldTimeData.seasonTransitionDays;
            int halfTransition = transitionDays / 2;

            // Reset transition state
            isInSeasonTransition = false;
            targetSeasonIndex = currentSeasonIndex;
            targetSeasonName = currentSeasonName;
            seasonTransitionProgress = 0f;

            int totalSeasons = worldTimeData.GetSeasonCount();
            if (totalSeasons == 0) return;

            var seasonRanges = worldTimeData.SeasonRanges;
            
            foreach (var seasonRange in seasonRanges)
            {
                // Check transition at end of season
                int daysFromSeasonEnd = seasonRange.endDay - currentDay;

                if (daysFromSeasonEnd >= 0 && daysFromSeasonEnd <= halfTransition)
                {
                    isInSeasonTransition = true;
                    int nextSeasonIndex = (seasonRange.seasonIndex + 1) % totalSeasons;
                    targetSeasonIndex = nextSeasonIndex;
                    targetSeasonName = worldTimeData.GetSeasonName(nextSeasonIndex);
                    seasonTransitionProgress = 0.5f + (0.5f * (halfTransition - daysFromSeasonEnd) / halfTransition);
                    return;
                }

                // Check transition at start of season
                int daysFromSeasonStart = currentDay - seasonRange.startDay;

                if (daysFromSeasonStart >= 0 && daysFromSeasonStart < halfTransition)
                {
                    isInSeasonTransition = true;
                    targetSeasonIndex = seasonRange.seasonIndex;
                    targetSeasonName = seasonRange.seasonName;
                    seasonTransitionProgress = 0.5f * (daysFromSeasonStart / halfTransition);
                    return;
                }
            }

            HandleYearBoundaryTransition(halfTransition, totalSeasons);
        }

        private void HandleYearBoundaryTransition(int halfTransition, int totalSeasons)
        {
            if (totalSeasons == 0) return;
            
            var seasonRanges = worldTimeData.SeasonRanges;
            if (seasonRanges.Length == 0) return;
            
            var lastSeason = seasonRanges[seasonRanges.Length - 1];
            var firstSeason = seasonRanges[0];
            
            // Check if approaching year end
            if (currentDay >= lastSeason.startDay)
            {
                int daysFromYearEnd = worldTimeData.GetTotalSeasonDays() - currentDay;
                if (daysFromYearEnd <= halfTransition)
                {
                    isInSeasonTransition = true;
                    targetSeasonIndex = 0;
                    targetSeasonName = firstSeason.seasonName;
                    seasonTransitionProgress = 0.5f + (0.5f * (halfTransition - daysFromYearEnd) / halfTransition);
                    return;
                }
            }

            // Check if coming from previous year
            if (currentDay <= firstSeason.endDay && currentDay <= halfTransition)
            {
                isInSeasonTransition = true;
                targetSeasonIndex = 0;
                targetSeasonName = firstSeason.seasonName;
                seasonTransitionProgress = 0.5f * (currentDay / halfTransition);
            }
        }

        #endregion

        #region Calendar Management

        private void UpdateCalendarState()
        {
            if (worldTimeData == null || worldTimeData.MonthsPerYear == 0) return;

            try
            {
                Month newMonth = worldTimeData.GetMonthForDay(currentDay);
                int newDayOfMonth = worldTimeData.GetDayOfMonth(currentDay);

                if (newMonth.monthIndex != currentMonth.monthIndex)
                {
                    currentMonth = newMonth;
                    OnMonthChanged?.Invoke(currentMonth);
                }

                currentDayOfMonth = newDayOfMonth;
            }
            catch (Exception ex)
            {
                if (enableDebugLogging)
                {
                    Debug.LogError($"[TimeManager] Calendar update failed: {ex.Message}");
                }
            }
        }

        public string GetFormattedDate()
        {
            if (worldTimeData == null) return $"Day {currentDay}";
            return worldTimeData.FormatDate(currentDay);
        }

        public string GetFormattedFullDate()
        {
            if (worldTimeData == null) return $"Day {currentDay}, Year {currentYear}";
            return $"{worldTimeData.FormatFullDate(currentDay)}, Year {currentYear}";
        }

        #endregion

        #region Time of Day Management

        private void UpdateTimeOfDay()
        {
            TimeOfDay newTimeOfDay = CalculateTimeOfDay(celestialTime);
            
            if (newTimeOfDay != currentTimeOfDay)
            {
                currentTimeOfDay = newTimeOfDay;
                OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
            }
        }

        private TimeOfDay CalculateTimeOfDay(float celestialTime)
        {
            if (celestialTime >= 0.0f && celestialTime < 0.22f) return TimeOfDay.EarlyMorning;
            if (celestialTime >= 0.2f && celestialTime < 0.4f) return TimeOfDay.Morning;
            if (celestialTime >= 0.4f && celestialTime < 0.6f) return TimeOfDay.Midday;
            if (celestialTime >= 0.6f && celestialTime < 0.8f) return TimeOfDay.Afternoon;
            return TimeOfDay.Evening;
        }
        
        #endregion

        #region Game Time Management

        private void UpdateCachedGameTime()
        {
            if (worldTimeData == null) return;

            float cacheUpdateInterval = worldTimeData.dayLengthInSeconds / 86400f;

            if (Time.time - lastCacheUpdateTime >= cacheUpdateInterval)
            {
                if (cachedGameTime == null)
                {
                    cachedGameTime = new GameTime();
                }

                worldTimeData.UpdateGameTimeFromCelestialTime(cachedGameTime, celestialTime, currentDay);

                cachedGameTime.seasonTransition = isInSeasonTransition ? seasonTransitionProgress : 0f;
                cachedGameTime.nextSeasonIndex = targetSeasonIndex;
                cachedGameTime.nextSeasonName = targetSeasonName;

                cachedGameTime.totalGameTime = (currentDay - 1) * worldTimeData.dayLengthInSeconds + 
                                               (celestialTime * worldTimeData.dayLengthInSeconds);

                var currentSeasonRange = worldTimeData.GetSeasonRange(currentSeasonIndex);
                cachedGameTime.DaysRemainingInSeason = currentSeasonRange.GetDaysRemainingForDay(currentDay);
                cachedGameTime.TotalDaysInSeason = currentSeasonRange.duration;

                lastCacheUpdateTime = Time.time;
            }
        }

        public GameTime GetCurrentGameTime()
        {
            if (cachedGameTime == null)
            {
                UpdateCachedGameTime();
            }
            return cachedGameTime;
        }

        #endregion

        #region Public API

        public void SetTimeScale(float newTimeScale)
        {
            timeScale = Mathf.Max(0f, newTimeScale);
        }

        public void PauseTime()
        {
            pauseTime = true;
        }

        public void ResumeTime()
        {
            pauseTime = false;
        }

        public void TogglePause()
        {
            pauseTime = !pauseTime;
        }

        public void SetCelestialTime(float newCelestialTime)
        {
            celestialTime = Mathf.Clamp01(newCelestialTime);
            UpdateAllTimeInfo();
            OnCelestialTimeChanged?.Invoke(celestialTime);
        }

        public void SetCurrentDay(int newDay)
        {
            int totalDays = worldTimeData != null ? worldTimeData.GetTotalSeasonDays() : 365;
            currentDay = Mathf.Clamp(newDay, 1, totalDays > 0 ? totalDays : int.MaxValue);
            UpdateAllTimeInfo();
            OnDayChanged?.Invoke(currentDay);
        }

        public void SetCurrentYear(int newYear)
        {
            currentYear = Mathf.Max(1, newYear);
            OnYearChanged?.Invoke(currentYear);
        }

        public void AdvanceDays(int days)
        {
            if (days <= 0) return;

            for (int i = 0; i < days; i++)
            {
                AdvanceDay();
            }
            UpdateAllTimeInfo();
        }

        #endregion
        #region Seasonal Data Access

        public SeasonalData GetCurrentSeasonalData()
        {
            if (worldTimeData == null) return null;
            return worldTimeData.GetSeasonalData(currentSeasonIndex);
        }

        public SeasonalData GetSeasonalData(int seasonIndex)
        {
            if (worldTimeData == null) return null;
            return worldTimeData.GetSeasonalData(seasonIndex);
        }

        public int GetSeasonIndexForDay(int dayOfYear)
        {
            if (worldTimeData == null) return 0;
            return worldTimeData.GetSeasonIndexForDay(dayOfYear);
        }

        public WorldTimeData.SeasonRange GetSeasonRange(int seasonIndex)
        {
            if (worldTimeData == null) return new WorldTimeData.SeasonRange();
            return worldTimeData.GetSeasonRange(seasonIndex);
        }

        public string GetSeasonName(int seasonIndex)
        {
            if (worldTimeData == null) return "Unknown Season";
            return worldTimeData.GetSeasonName(seasonIndex);
        }

        #endregion

        #region Validation

        private void ValidateConfiguration()
        {
            if (worldTimeData == null)
            {
                Debug.LogError("[TimeManager] WorldTimeData is not assigned! Please assign a WorldTimeData ScriptableObject.");
                enabled = false;
                return;
            }

            if (worldTimeData.dayLengthInSeconds <= 0)
            {
                Debug.LogError("[TimeManager] Day length must be greater than 0!");
                enabled = false;
                return;
            }

            // Validate season configuration
            if (worldTimeData.GetSeasonCount() == 0)
            {
                Debug.LogWarning("[TimeManager] No seasons configured! Add seasons to WorldTimeData to enable seasonal system.");
            }
            else
            {
                // Check if all seasons have seasonal data assigned
                for (int i = 0; i < worldTimeData.GetSeasonCount(); i++)
                {
                    var seasonalData = worldTimeData.GetSeasonalData(i);
                    if (seasonalData == null)
                    {
                        var seasonConfig = worldTimeData.GetSeasonConfiguration(i);
                        Debug.LogWarning($"[TimeManager] Season '{seasonConfig?.seasonName ?? "Unknown"}' has no SeasonalData assigned!");
                    }
                }
            }

            // Validate starting values
            int totalDays = worldTimeData.GetTotalSeasonDays();
            if (totalDays > 0 && startingDay > totalDays)
            {
                Debug.LogWarning($"[TimeManager] Starting day ({startingDay}) exceeds total days in year ({totalDays}). Clamping to valid range.");
                currentDay = totalDays;
            }

            // Validate calendar configuration
            if (worldTimeData.MonthsPerYear > 0)
            {
                if (worldTimeData.totalDaysInYear != worldTimeData.MonthsPerYear * worldTimeData.daysPerMonth)
                {
                    Debug.LogWarning($"[TimeManager] Calendar configuration mismatch: {worldTimeData.totalDaysInYear} total days doesn't match {worldTimeData.MonthsPerYear} months × {worldTimeData.daysPerMonth} days per month");
                }
            }

            if (enableDebugLogging)
            {
                Debug.Log("[TimeManager] Configuration validation completed successfully.");
            }
        }

        public void ValidateTimeManagerState()
        {
            bool isValid = true;
            System.Text.StringBuilder issues = new System.Text.StringBuilder();

            // Check basic configuration
            if (worldTimeData == null)
            {
                issues.AppendLine("- WorldTimeData is not assigned");
                isValid = false;
            }
            else
            {
                // Check day bounds
                int totalDays = worldTimeData.GetTotalSeasonDays();
                if (totalDays > 0 && (currentDay < 1 || currentDay > totalDays))
                {
                    issues.AppendLine($"- Current day ({currentDay}) is outside valid range (1-{totalDays})");
                    isValid = false;
                }

                // Check celestial time bounds
                if (celestialTime < 0f || celestialTime > 1f)
                {
                    issues.AppendLine($"- Celestial time ({celestialTime:F3}) is outside valid range (0.0-1.0)");
                    isValid = false;
                }

                // Check season consistency
                int expectedSeasonIndex = worldTimeData.GetSeasonIndexForDay(currentDay);
                if (expectedSeasonIndex != currentSeasonIndex)
                {
                    string expectedSeasonName = worldTimeData.GetSeasonName(expectedSeasonIndex);
                    issues.AppendLine($"- Season mismatch: Expected {expectedSeasonName} for day {currentDay}, but current is {currentSeasonName}");
                    isValid = false;
                }

                // Check calendar consistency if enabled
                if (worldTimeData.MonthsPerYear > 0)
                {
                    try
                    {
                        Month expectedMonth = worldTimeData.GetMonthForDay(currentDay);
                        int expectedDayOfMonth = worldTimeData.GetDayOfMonth(currentDay);

                        if (expectedMonth.monthIndex != currentMonth.monthIndex)
                        {
                            issues.AppendLine($"- Month mismatch: Expected {expectedMonth.name} for day {currentDay}, but current is {currentMonth.name}");
                            isValid = false;
                        }

                        if (expectedDayOfMonth != currentDayOfMonth)
                        {
                            issues.AppendLine($"- Day of month mismatch: Expected {expectedDayOfMonth} for day {currentDay}, but current is {currentDayOfMonth}");
                            isValid = false;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        issues.AppendLine($"- Calendar validation error: {ex.Message}");
                        isValid = false;
                    }
                }
            }

            // Check year bounds
            if (currentYear < 1)
            {
                issues.AppendLine($"- Current year ({currentYear}) must be at least 1");
                isValid = false;
            }

            // Report results
            if (isValid)
            {
                if (enableDebugLogging)
                {
                    Debug.Log("[TimeManager] Validation passed: All systems are functioning correctly");
                }
            }
            else
            {
                Debug.LogWarning($"[TimeManager] Validation failed:\n{issues.ToString()}");
            }
        }

        #endregion

        #region Unity Editor Support

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp values in editor
            startingCelestialTime = Mathf.Clamp01(startingCelestialTime);
            startingDay = Mathf.Max(1, startingDay);
            startingYear = Mathf.Max(1, startingYear);
            timeScale = Mathf.Max(0f, timeScale);

            // Update current values if playing
            if (Application.isPlaying)
            {
                celestialTime = Mathf.Clamp01(celestialTime);
                currentDay = Mathf.Max(1, currentDay);
                currentYear = Mathf.Max(1, currentYear);
            }
        }

        [ContextMenu("Validate State")]
        private void EditorValidateState()
        {
            ValidateTimeManagerState();
        }

        [ContextMenu("Reset to Starting Values")]
        private void EditorResetToStartingValues()
        {
            celestialTime = startingCelestialTime;
            currentDay = startingDay;
            currentYear = startingYear;
            
            if (Application.isPlaying)
            {
                UpdateAllTimeInfo();
                Debug.Log("[TimeManager] Reset to starting values");
            }
        }
#endif

        #endregion
    }
}
    