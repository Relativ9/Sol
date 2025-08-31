using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Core time management system that handles game time progression, season tracking, calendar system, and temporal events.
    /// Implements ITimeManager interface and serves as the central authority for all time-related calculations.
    /// Follows Single Responsibility Principle by focusing on time management while delegating display formatting to WorldTimeData.
    /// </summary>
    public class TimeManager : MonoBehaviour, ITimeManager
    {
        #region Inspector Fields

        [Header("Time Configuration")]
        [Tooltip("World time data configuration containing all temporal settings and seasonal references")]
        [SerializeField] private WorldTimeData worldTimeData;

        [Tooltip("Starting celestial time value (0.0 = day start, 1.0 = day end)")]
        [SerializeField] private float startingCelestialTime = 0.5f;

        [Tooltip("Starting day of the year (1-based)")]
        [SerializeField] private int startingDay = 1;

        [Tooltip("Starting year number (1-based)")]
        [SerializeField] private int startingYear = 1;

        [Header("Time Control")]
        [Tooltip("Time scale multiplier for time progression speed (0 = paused, 1 = normal, >1 = accelerated)")]
        [SerializeField] private float timeScale = 1f;

        [Tooltip("Whether time should automatically progress or remain static")]
        [SerializeField] private bool autoProgressTime = true;

        [Header("Debug Settings")]
        [Tooltip("Enable debug logging for time progression and season changes")]
        [SerializeField] private bool enableDebugLogging = false;

        [Tooltip("Enable detailed logging for season transition calculations")]
        [SerializeField] private bool enableTransitionLogging = false;

        [Tooltip("Display current time information in inspector (read-only)")]
        [SerializeField] private bool showInspectorDebugInfo = true;

        #endregion

        #region Private Fields

        // Core time tracking
        private float celestialTime;
        private int currentDay;
        private int currentYear;
        private Season currentSeason;
        private TimeOfDay currentTimeOfDay;
        private WorldTimeData.SeasonRange currentSeasonRange;

        // Calendar tracking
        private Month currentMonth;
        private int currentDayOfMonth;
        private Month previousMonth;

        // Season transition tracking
        private bool isInSeasonTransition;
        private Season targetSeason;
        private float seasonTransitionProgress;

        // Cached game time object
        private GameTime cachedGameTime;
        private float lastCacheUpdateTime;
        private float cacheUpdateInterval; // Will be set from WorldTimeData.dayLengthInSeconds

        // Performance optimization
        private float lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.1f; // Update 10 times per second

        #endregion

        #region ITimeManager Properties - Core Time

        /// <summary>
        /// Current celestial time as a normalized value (0.0 = start of day, 1.0 = end of day)
        /// </summary>
        public float CelestialTime => celestialTime;

        /// <summary>
        /// Current time scale multiplier affecting time progression speed
        /// </summary>
        public float TimeScale 
        { 
            get => timeScale; 
            set => SetTimeScale(value); 
        }

        /// <summary>
        /// Currently active season based on day of year
        /// </summary>
        public Season CurrentSeason => currentSeason;

        /// <summary>
        /// Current day number within the year (1-based)
        /// </summary>
        public int CurrentDay => currentDay;

        /// <summary>
        /// Current year number (1-based)
        /// </summary>
        public int CurrentYear => currentYear;

        /// <summary>
        /// Total number of days in one complete year
        /// </summary>
        public int DaysPerYear => worldTimeData?.totalDaysInYear ?? 832;
        
        /// <summary>
        /// Cached game time updated once per in-game second for performance
        /// </summary>
        public GameTime CachedGameTime => cachedGameTime;

        #endregion

        #region ITimeManager Properties - Calendar System

        /// <summary>
        /// Current day of year (1-based, same as CurrentDay for compatibility)
        /// </summary>
        public int CurrentDayOfYear => currentDay;

        /// <summary>
        /// Current month information
        /// </summary>
        public Month CurrentMonth => currentMonth;

        /// <summary>
        /// Current day within the month (1-based)
        /// </summary>
        public int CurrentDayOfMonth => currentDayOfMonth;

        #endregion

        #region ITimeManager Properties - World Time Data Integration

        /// <summary>
        /// Reference to the world time configuration data
        /// </summary>
        public WorldTimeData WorldTimeData => worldTimeData;

        /// <summary>
        /// Current game time state with all temporal information
        /// </summary>
        public GameTime CurrentGameTime => GetCurrentGameTime();

        #endregion

        #region ITimeManager Properties - Season Information

        /// <summary>
        /// Number of days in the currently active season
        /// </summary>
        public int DaysInCurrentSeason => currentSeasonRange.duration;

        /// <summary>
        /// Number of days remaining in the current season
        /// </summary>
        public int DaysRemainingInSeason => currentSeasonRange.GetDaysRemainingForDay(currentDay);

        /// <summary>
        /// Progress through current season (0.0 = season start, 1.0 = season end)
        /// </summary>
        public float SeasonProgress => currentSeasonRange.GetProgressForDay(currentDay);

        /// <summary>
        /// Season range data for the currently active season
        /// </summary>
        public WorldTimeData.SeasonRange CurrentSeasonRange => currentSeasonRange;

        #endregion

        #region ITimeManager Properties - Season Transitions

        /// <summary>
        /// Whether the system is currently in a season transition period
        /// </summary>
        public bool IsInSeasonTransition => isInSeasonTransition;

        /// <summary>
        /// Target season when transitioning (same as CurrentSeason when not transitioning)
        /// </summary>
        public Season TargetSeason => targetSeason;

        /// <summary>
        /// Progress through current season transition (0.0 = transition start, 1.0 = transition complete)
        /// </summary>
        public float SeasonTransitionProgress => seasonTransitionProgress;

        /// <summary>
        /// Number of days over which season transitions occur
        /// </summary>
        public int SeasonTransitionDurationDays => worldTimeData?.seasonTransitionDays ?? 20;

        #endregion

        #region ITimeManager Properties - Time Display

        /// <summary>
        /// Current time in 24-hour format (HH:MM:SS)
        /// </summary>
        public string CurrentTimeDisplay => worldTimeData?.GetDisplayTime(celestialTime) ?? "00:00:00";

        /// <summary>
        /// Current time in 12-hour format with AM/PM (HH:MM:SS AM/PM)
        /// </summary>
        public string CurrentTimeDisplay12Hour => worldTimeData?.GetDisplayTime12Hour(celestialTime) ?? "12:00:00 AM";

        /// <summary>
        /// Current time of day category (Morning, Afternoon, Evening, Night)
        /// </summary>
        public TimeOfDay CurrentTimeOfDay => currentTimeOfDay;

        /// <summary>
        /// Formatted date display including day, year, and season
        /// </summary>
        public string CurrentDateDisplay => $"Day {currentDay}, Year {currentYear}, {currentSeason}";

        #endregion

        #region ITimeManager Properties - Progress

        /// <summary>
        /// Progress through current day (0.0 = day start, 1.0 = day end)
        /// </summary>
        public float DayProgress => celestialTime;

        /// <summary>
        /// Progress through current year (0.0 = year start, 1.0 = year end)
        /// </summary>
        public float YearProgress => worldTimeData != null ? (float)(currentDay - 1) / worldTimeData.totalDaysInYear : 0f;

        #endregion

        #region ITimeManager Events

        /// <summary>
        /// Event triggered when the active season changes
        /// </summary>
        public event System.Action<Season> OnSeasonChanged;

        /// <summary>
        /// Event triggered when the day number changes
        /// </summary>
        public event System.Action<int> OnDayChanged;

        /// <summary>
        /// Event triggered when the year number changes
        /// </summary>
        public event System.Action<int> OnYearChanged;

        /// <summary>
        /// Event triggered when the time of day category changes
        /// </summary>
        public event System.Action<TimeOfDay> OnTimeOfDayChanged;

        /// <summary>
        /// Event triggered when game time is updated (frequent updates)
        /// </summary>
        public event System.Action<GameTime> OnGameTimeUpdated;

        /// <summary>
        /// Event triggered when time scale changes
        /// </summary>
        public event System.Action OnTimeScaleChanged;

        /// <summary>
        /// Event triggered when the month changes
        /// </summary>
        public event System.Action<Month> OnMonthChanged;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Unity Awake callback. Initializes the time manager with starting values.
        /// </summary>
        private void Awake()
        {
            InitializeTimeManager();
        }

        /// <summary>
        /// Unity Start callback. Validates configuration and sets up initial state.
        /// </summary>
        private void Start()
        {
            ValidateConfiguration();
            UpdateAllTimeInfo();
            
            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Initialized - Day {currentDay}, Year {currentYear}, Season {currentSeason}");
                if (worldTimeData != null && worldTimeData.MonthsPerYear > 0)
                {
                    Debug.Log($"[TimeManager] Calendar - {currentMonth.name} {currentDayOfMonth}");
                }
            }
        }

        /// <summary>
        /// Unity Update callback. Handles time progression and periodic updates.
        /// </summary>
        private void Update()
        {
            if (autoProgressTime && timeScale > 0f)
            {
                ProgressTime();
            }

            // Throttled updates for performance
            if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
            {
                UpdateAllTimeInfo();
                lastUpdateTime = Time.time;
            }
        }

        #endregion
        
                #region Initialization

        /// <summary>
        /// Initializes the time manager with starting values and creates cached objects.
        /// </summary>
        private void InitializeTimeManager()
        {
            // Set starting values
            celestialTime = Mathf.Clamp01(startingCelestialTime);
            currentDay = Mathf.Max(1, startingDay);
            currentYear = Mathf.Max(1, startingYear);

            // Initialize cached objects
            cachedGameTime = new GameTime();

            // Initialize transition state
            isInSeasonTransition = false;
            targetSeason = Season.PolarSummer;
            seasonTransitionProgress = 0f;

            // Initialize calendar state
            InitializeCalendarState();

            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Initialized with starting values - Time: {celestialTime:F3}, Day: {currentDay}, Year: {currentYear}");
            }
        }

        /// <summary>
        /// Initialize calendar state from current day
        /// </summary>
        private void InitializeCalendarState()
        {
            if (worldTimeData == null || worldTimeData.MonthsPerYear == 0)
            {
                // No calendar system configured
                currentMonth = new Month("Unknown", Season.Spring, 0);
                currentDayOfMonth = 1;
                previousMonth = currentMonth;
                return;
            }

            try
            {
                currentMonth = worldTimeData.GetMonthForDay(currentDay);
                currentDayOfMonth = worldTimeData.GetDayOfMonth(currentDay);
                previousMonth = currentMonth;

                if (enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] Calendar initialized - {currentMonth.name} {currentDayOfMonth} ({currentMonth.season})");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TimeManager] Error initializing calendar: {ex.Message}");
                // Fallback to safe values
                currentMonth = new Month("Unknown", Season.Spring, 0);
                currentDayOfMonth = 1;
                previousMonth = currentMonth;
            }
        }

        /// <summary>
        /// Validates the time manager configuration and logs any issues.
        /// </summary>
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

            // Validate seasonal data references
            if (!worldTimeData.ValidateSeasonalDataReferences())
            {
                Debug.LogWarning("[TimeManager] Some SeasonalData references are missing. Celestial system may not work properly.");
            }

            // Validate starting values
            if (startingDay > worldTimeData.totalDaysInYear)
            {
                Debug.LogWarning($"[TimeManager] Starting day ({startingDay}) exceeds total days in year ({worldTimeData.totalDaysInYear}). Clamping to valid range.");
                currentDay = worldTimeData.totalDaysInYear;
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

        #endregion
        
        #region Time Progression

        /// <summary>
        /// Advances time based on real-time delta and current time scale.
        /// Handles day and year transitions with appropriate event triggering.
        /// </summary>
        private void ProgressTime()
        {
            if (worldTimeData == null) return;

            // Calculate time progression
            float timeIncrement = (Time.deltaTime * timeScale) / worldTimeData.dayLengthInSeconds;
            float previousCelestialTime = celestialTime;
            int previousDay = currentDay;
            int previousYear = currentYear;

            // Advance celestial time
            celestialTime += timeIncrement;

            // Handle day transitions
            if (celestialTime >= 1f)
            {
                HandleDayTransition();
            }

            // Trigger events if values changed
            if (previousDay != currentDay)
            {
                OnDayChanged?.Invoke(currentDay);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] Day changed: {previousDay} → {currentDay}");
                    if (worldTimeData.MonthsPerYear > 0)
                    {
                        Debug.Log($"[TimeManager] Calendar date: {GetFormattedDate()}");
                    }
                }
            }

            if (previousYear != currentYear)
            {
                OnYearChanged?.Invoke(currentYear);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] Year changed: {previousYear} → {currentYear}");
                }
            }
        }

        /// <summary>
        /// Handles the transition from one day to the next, including year wraparound.
        /// Updates day and year counters and normalizes celestial time.
        /// </summary>
        private void HandleDayTransition()
        {
            // Normalize celestial time and advance day
            celestialTime = celestialTime - 1f;
            currentDay++;

            // Handle year transition
            if (currentDay > worldTimeData.totalDaysInYear)
            {
                currentDay = 1;
                currentYear++;
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] New year started: Year {currentYear}");
                }
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] New day started: Day {currentDay}, Year {currentYear}");
            }
        }

        #endregion

        #region Calendar Management

        /// <summary>
        /// Updates calendar information based on current day
        /// </summary>
        private void UpdateCalendarInfo()
        {
            if (worldTimeData == null || worldTimeData.MonthsPerYear == 0) return;

            try
            {
                // Get current month and day of month
                Month newMonth = worldTimeData.GetMonthForDay(currentDay);
                int newDayOfMonth = worldTimeData.GetDayOfMonth(currentDay);

                // Check for month change
                if (previousMonth.monthIndex != newMonth.monthIndex)
                {
                    previousMonth = currentMonth;
                    currentMonth = newMonth;
                    OnMonthChanged?.Invoke(currentMonth);

                    if (enableDebugLogging)
                    {
                        Debug.Log($"[TimeManager] Month changed: {previousMonth.name} → {currentMonth.name} ({currentMonth.season})");
                    }
                }
                else
                {
                    currentMonth = newMonth;
                }

                currentDayOfMonth = newDayOfMonth;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TimeManager] Error updating calendar: {ex.Message}");
            }
        }

        /// <summary>
        /// Get formatted date string using calendar system
        /// </summary>
        public string GetFormattedDate()
        {
            if (worldTimeData == null) return $"Day {currentDay}";
            return worldTimeData.FormatDate(currentDay);
        }

        /// <summary>
        /// Get formatted time string
        /// </summary>
        public string GetFormattedTime()
        {
            if (worldTimeData == null) return "00:00:00";
            return worldTimeData.GetDisplayTime(celestialTime);
        }

        /// <summary>
        /// Get formatted date and time string
        /// </summary>
        public string GetFormattedDateTime()
        {
            return $"{GetFormattedDate()} - {GetFormattedTime()}";
        }

        #endregion

        #region Season Management

        /// <summary>
        /// Updates season information based on current day and handles season transitions.
        /// Calculates season progress and manages transition states.
        /// </summary>
        private void UpdateSeasonInfo()
        {
            if (worldTimeData == null) return;

            Season newSeason = worldTimeData.GetSeasonForDay(currentDay);
            if (newSeason != currentSeason)
            {
                HandleSeasonChange(newSeason);
            }
            else
            {
                // Update season range even if season hasn't changed (for progress calculations)
                currentSeasonRange = worldTimeData.GetSeasonRange(currentSeason);
            }

            // Update transition state
            UpdateSeasonTransition();
        }

        /// <summary>
        /// Handles the transition from one season to another.
        /// Triggers appropriate events and updates season-related state.
        /// </summary>
        /// <param name="newSeason">The season being transitioned to</param>
        private void HandleSeasonChange(Season newSeason)
        {
            Season previousSeason = currentSeason;
            currentSeason = newSeason;
            currentSeasonRange = worldTimeData.GetSeasonRange(currentSeason);

            // Trigger season change events
            OnSeasonChanged?.Invoke(currentSeason);
            
            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Season Changed: {previousSeason} → {currentSeason}");
            }
        }

        /// <summary>
        /// Calculates and updates season transition state based on proximity to season boundaries.
        /// Handles both regular season transitions and year-boundary transitions.
        /// </summary>
        private void UpdateSeasonTransition()
        {
            if (worldTimeData == null) return;

            int transitionDays = worldTimeData.seasonTransitionDays;
            int halfTransition = transitionDays / 2;

            // Reset transition state
            isInSeasonTransition = false;
            targetSeason = currentSeason;
            seasonTransitionProgress = 0f;

            // Check each season boundary for transitions
            foreach (var seasonRange in worldTimeData.SeasonRanges)
            {
                // Check transition at end of season (leading into next season)
                int daysFromSeasonEnd = seasonRange.endDay - currentDay;

                if (daysFromSeasonEnd >= 0 && daysFromSeasonEnd <= halfTransition)
                {
                    // We're in the second half of transition (approaching new season)
                    isInSeasonTransition = true;
                    targetSeason = GetNextSeason(seasonRange.season);
                    seasonTransitionProgress = 0.5f + (0.5f * (halfTransition - daysFromSeasonEnd) / halfTransition);
                    
                    LogTransitionState("End of season transition");
                    return;
                }

                // Check transition at start of season (coming from previous season)
                int daysFromSeasonStart = currentDay - seasonRange.startDay;

                if (daysFromSeasonStart >= 0 && daysFromSeasonStart < halfTransition)
                {
                    // We're in the first half of transition (coming from previous season)
                    isInSeasonTransition = true;
                    targetSeason = seasonRange.season; // Current season is the target
                    seasonTransitionProgress = 0.5f * (daysFromSeasonStart / halfTransition);
                    
                    LogTransitionState("Start of season transition");
                    return;
                }
            }

            // Handle year boundary transition (Spring -> PolarSummer)
            HandleYearBoundaryTransition(halfTransition);
        }

        /// <summary>
        /// Handles season transitions that occur across year boundaries (Spring to PolarSummer).
        /// </summary>
        /// <param name="halfTransition">Half of the total transition duration in days</param>
        private void HandleYearBoundaryTransition(int halfTransition)
        {
            if (currentSeason == Season.Spring)
            {
                int daysFromYearEnd = worldTimeData.totalDaysInYear - currentDay;
                if (daysFromYearEnd <= halfTransition)
                {
                    // Transitioning from Spring to PolarSummer
                    isInSeasonTransition = true;
                    targetSeason = Season.PolarSummer;
                    seasonTransitionProgress = 0.5f + (0.5f * (halfTransition - daysFromYearEnd) / halfTransition);
                    
                    LogTransitionState("Year boundary transition (Spring → PolarSummer)");
                    return;
                }
            }

            if (currentSeason == Season.PolarSummer && currentDay <= halfTransition)
            {
                // Coming from Spring into PolarSummer
                isInSeasonTransition = true;
                targetSeason = Season.PolarSummer;
                seasonTransitionProgress = 0.5f * (currentDay / halfTransition);
                
                LogTransitionState("Year boundary transition (Spring → PolarSummer completion)");
                return;
            }
        }

        /// <summary>
        /// Gets the next season in the seasonal cycle.
        /// </summary>
        /// <param name="season">Current season</param>
        /// <returns>Next season in the cycle</returns>
        private Season GetNextSeason(Season season)
        {
            return season switch
            {
                Season.PolarSummer => Season.Fall,
                Season.Fall => Season.LongNight,
                Season.LongNight => Season.Spring,
                Season.Spring => Season.PolarSummer,
                _ => Season.PolarSummer
            };
        }

        /// <summary>
        /// Logs transition state information if transition logging is enabled.
        /// </summary>
        /// <param name="transitionType">Description of the transition type</param>
        private void LogTransitionState(string transitionType)
        {
            if (enableTransitionLogging)
            {
                Debug.Log($"[TimeManager] {transitionType}: {currentSeason} → {targetSeason}, Progress: {seasonTransitionProgress:P1}");
            }
        }

        #endregion
        
                #region Time of Day Management

        /// <summary>
        /// Updates the current time of day category based on celestial time.
        /// Triggers events when time of day changes.
        /// </summary>
        private void UpdateTimeOfDay()
        {
            TimeOfDay newTimeOfDay = CalculateTimeOfDay(celestialTime);
            
            if (newTimeOfDay != currentTimeOfDay)
            {
                TimeOfDay previousTimeOfDay = currentTimeOfDay;
                currentTimeOfDay = newTimeOfDay;
                
                OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
                TimeEvents.OnTimeOfDayChanged?.Invoke(previousTimeOfDay, currentTimeOfDay);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] Time of Day changed: {previousTimeOfDay} → {currentTimeOfDay}");
                }
            }
        }

        /// <summary>
        /// Calculates time of day category based on celestial time value.
        /// </summary>
        /// <param name="celestialTime">Current celestial time (0-1)</param>
        /// <returns>Appropriate TimeOfDay category</returns>
        private TimeOfDay CalculateTimeOfDay(float celestialTime)
        {
            // Using the defined TimeOfDay ranges
            return celestialTime switch
            {
                >= 0.0f and < 0.2f => TimeOfDay.EarlyMorning,  // 0.0 - 0.2
                >= 0.2f and < 0.4f => TimeOfDay.Morning,       // 0.2 - 0.4
                >= 0.4f and < 0.6f => TimeOfDay.Midday,        // 0.4 - 0.6
                >= 0.6f and < 0.8f => TimeOfDay.Afternoon,     // 0.6 - 0.8
                _ => TimeOfDay.Evening                          // 0.8 - 1.0
            };
        }

        #endregion

        #region Comprehensive Updates

        /// <summary>
        /// Updates all time-related information including seasons, time of day, calendar, and cached game time.
        /// Called periodically to maintain system consistency.
        /// </summary>
        private void UpdateAllTimeInfo()
        {
            UpdateSeasonInfo();
            UpdateTimeOfDay();
            UpdateCalendarInfo();
            UpdateCachedGameTime();
            
            // Trigger game time update event
            OnGameTimeUpdated?.Invoke(cachedGameTime);
        }

        /// <summary>
        /// Updates the cached GameTime object with current temporal state.
        /// Provides a consistent snapshot of all time information.
        /// </summary>
        private void UpdateCachedGameTime()
        {
            if (worldTimeData == null) return;

            // Calculate cache update interval (once per in-game second)
            float cacheUpdateInterval = worldTimeData.dayLengthInSeconds / 86400f;

            // Check if enough time has passed to update cache
            if (Time.time - lastCacheUpdateTime >= cacheUpdateInterval)
            {
                // Initialize cachedGameTime if null
                if (cachedGameTime == null)
                {
                    cachedGameTime = new GameTime();
                }

                // Update GameTime using WorldTimeData's built-in method
                worldTimeData.UpdateGameTimeFromCelestialTime(cachedGameTime, celestialTime, currentDay);

                // Set additional fields that aren't handled by UpdateGameTimeFromCelestialTime
                cachedGameTime.seasonTransition = isInSeasonTransition ? seasonTransitionProgress : 0f;
                cachedGameTime.nextSeason = targetSeason;
                // cachedGameTime.currentYear = currentYear;
                // cachedGameTime.timeOfDay = currentTimeOfDay;

                // Calculate total game time from existing values if needed
                // (days * seconds per day) + (current day progress * seconds per day)
                cachedGameTime.totalGameTime = (currentDay - 1) * worldTimeData.dayLengthInSeconds + 
                                               (celestialTime * worldTimeData.dayLengthInSeconds);

                // Calculate and set the additional properties
                var currentSeasonRange = worldTimeData.GetSeasonRange(currentSeason);
                cachedGameTime.DaysRemainingInSeason = currentSeasonRange.GetDaysRemainingForDay(currentDay);
                cachedGameTime.TotalDaysInSeason = currentSeasonRange.duration;

                // Update last cache time
                lastCacheUpdateTime = Time.time;
            }
        }

        #endregion

        #region ITimeManager Implementation - SeasonalData Integration

        /// <summary>
        /// Gets the seasonal data configuration for the currently active season.
        /// </summary>
        /// <returns>SeasonalData for current season or null if not available</returns>
        public SeasonalData GetCurrentSeasonalData()
        {
            if (worldTimeData == null) return null;
            return worldTimeData.GetSeasonalData(currentSeason);
        }

        /// <summary>
        /// Gets the seasonal data configuration for a specific season.
        /// </summary>
        /// <param name="season">Season to get data for</param>
        /// <returns>SeasonalData for specified season or null if not available</returns>
        public SeasonalData GetSeasonalData(Season season)
        {
            if (worldTimeData == null) return null;
            return worldTimeData.GetSeasonalData(season);
        }

        #endregion

        #region ITimeManager Implementation - Time Queries

        /// <summary>
        /// Gets current game time state with all temporal information.
        /// </summary>
        /// <returns>Complete GameTime object with current state</returns>
        public GameTime GetCurrentGameTime()
        {
            return cachedGameTime;
        }

        /// <summary>
        /// Determines which season contains the specified day of year.
        /// </summary>
        /// <param name="dayOfYear">Day number to check (1-based)</param>
        /// <returns>Season containing the specified day</returns>
        public Season GetSeasonForDay(int dayOfYear)
        {
            if (worldTimeData == null) return Season.PolarSummer;
            return worldTimeData.GetSeasonForDay(dayOfYear);
        }

        /// <summary>
        /// Gets season range information for a specific season.
        /// </summary>
        /// <param name="season">Season to get range for</param>
        /// <returns>SeasonRange with temporal boundaries and duration</returns>
        public WorldTimeData.SeasonRange GetSeasonRange(Season season)
        {
            if (worldTimeData == null) return new WorldTimeData.SeasonRange();
            return worldTimeData.GetSeasonRange(season);
        }

        #endregion
        
        #region ITimeManager Implementation - Time Control

        /// <summary>
        /// Sets the time scale multiplier for time progression speed.
        /// Validates input and triggers appropriate events.
        /// </summary>
        /// <param name="newTimeScale">New time scale value (0 = paused, 1 = normal speed, >1 = accelerated)</param>
        public void SetTimeScale(float newTimeScale)
        {
            float previousTimeScale = timeScale;
            timeScale = Mathf.Max(0f, newTimeScale);
            
            if (Mathf.Abs(previousTimeScale - timeScale) > 0.001f)
            {
                OnTimeScaleChanged?.Invoke();
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] Time scale changed: {previousTimeScale:F2} → {timeScale:F2}");
                }
            }
        }

        /// <summary>
        /// Pauses time progression by setting time scale to zero.
        /// </summary>
        public void PauseTime()
        {
            SetTimeScale(0f);
        }

        /// <summary>
        /// Resumes time progression by setting time scale to normal speed.
        /// </summary>
        public void ResumeTime()
        {
            SetTimeScale(1f);
        }

        /// <summary>
        /// Sets time progression to accelerated speed.
        /// </summary>
        /// <param name="accelerationFactor">Acceleration multiplier (default 2x speed)</param>
        public void AccelerateTime(float accelerationFactor = 2f)
        {
            SetTimeScale(Mathf.Max(1f, accelerationFactor));
        }

        #endregion

        #region ITimeManager Implementation - Time Conversion Utilities

        /// <summary>
        /// Converts real-time seconds to equivalent game time duration.
        /// Accounts for current time scale and day length configuration.
        /// </summary>
        /// <param name="realTimeSeconds">Real-time duration in seconds</param>
        /// <returns>Equivalent game time duration in celestial time units</returns>
        public float ConvertRealTimeToGameTime(float realTimeSeconds)
        {
            if (worldTimeData == null || worldTimeData.dayLengthInSeconds <= 0f)
            {
                Debug.LogWarning("[TimeManager] Cannot convert time: WorldTimeData not configured properly");
                return 0f;
            }

            // Convert real seconds to celestial time units (0-1 range per day)
            return (realTimeSeconds * timeScale) / worldTimeData.dayLengthInSeconds;
        }

        /// <summary>
        /// Converts game time duration to equivalent real-time seconds.
        /// Accounts for current time scale and day length configuration.
        /// </summary>
        /// <param name="gameTimeSeconds">Game time duration in celestial time units</param>
        /// <returns>Equivalent real-time duration in seconds</returns>
        public float ConvertGameTimeToRealTime(float gameTimeSeconds)
        {
            if (worldTimeData == null || timeScale <= 0f)
            {
                Debug.LogWarning("[TimeManager] Cannot convert time: Invalid configuration or time scale");
                return 0f;
            }

            // Convert celestial time units to real seconds
            return (gameTimeSeconds * worldTimeData.dayLengthInSeconds) / timeScale;
        }

        /// <summary>
        /// Calculates how many real-world seconds are needed for a specific number of game days.
        /// </summary>
        /// <param name="gameDays">Number of game days</param>
        /// <returns>Real-world seconds required</returns>
        public float CalculateRealTimeForGameDays(int gameDays)
        {
            if (worldTimeData == null || timeScale <= 0f) return 0f;
            return (gameDays * worldTimeData.dayLengthInSeconds) / timeScale;
        }

        /// <summary>
        /// Calculates how many game days will pass in a specific real-world time duration.
        /// </summary>
        /// <param name="realTimeSeconds">Real-world time duration in seconds</param>
        /// <returns>Number of game days that will pass</returns>
        public float CalculateGameDaysForRealTime(float realTimeSeconds)
        {
            if (worldTimeData == null || worldTimeData.dayLengthInSeconds <= 0f) return 0f;
            return (realTimeSeconds * timeScale) / worldTimeData.dayLengthInSeconds;
        }

        #endregion

        #region Time Manipulation Methods

        /// <summary>
        /// Manually sets the current celestial time.
        /// Useful for testing or save/load functionality.
        /// </summary>
        /// <param name="newCelestialTime">New celestial time value (0-1)</param>
        public void SetCelestialTime(float newCelestialTime)
        {
            float previousTime = celestialTime;
            celestialTime = Mathf.Clamp01(newCelestialTime);
            
            UpdateAllTimeInfo();
            
            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Celestial time manually set: {previousTime:F3} → {celestialTime:F3}");
            }
        }

        /// <summary>
        /// Manually sets the current day of year.
        /// Automatically updates season, calendar, and other dependent values.
        /// </summary>
        /// <param name="newDay">New day number (1-based)</param>
        public void SetCurrentDay(int newDay)
        {
            if (worldTimeData == null) return;

            int previousDay = currentDay;
            currentDay = Mathf.Clamp(newDay, 1, worldTimeData.totalDaysInYear);
            
            UpdateAllTimeInfo();
            OnDayChanged?.Invoke(currentDay);
            
            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Day manually set: {previousDay} → {currentDay}");
                if (worldTimeData.MonthsPerYear > 0)
                {
                    Debug.Log($"[TimeManager] Calendar updated to: {GetFormattedDate()}");
                }
            }
        }

        /// <summary>
        /// Manually sets the current year.
        /// </summary>
        /// <param name="newYear">New year number (1-based)</param>
        public void SetCurrentYear(int newYear)
        {
            int previousYear = currentYear;
            currentYear = Mathf.Max(1, newYear);
            
            UpdateAllTimeInfo();
            OnYearChanged?.Invoke(currentYear);
            
            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Year manually set: {previousYear} → {currentYear}");
            }
        }

        /// <summary>
        /// Advances time by a specific number of game days.
        /// Handles day and year transitions automatically.
        /// </summary>
        /// <param name="daysToAdvance">Number of days to advance</param>
        public void AdvanceByDays(int daysToAdvance)
        {
            if (daysToAdvance <= 0 || worldTimeData == null) return;

            int startingDay = currentDay;
            int startingYear = currentYear;

            for (int i = 0; i < daysToAdvance; i++)
            {
                currentDay++;
                if (currentDay > worldTimeData.totalDaysInYear)
                {
                    currentDay = 1;
                    currentYear++;
                }
            }

            UpdateAllTimeInfo();
            
            // Trigger events
            if (currentDay != startingDay)
            {
                OnDayChanged?.Invoke(currentDay);
            }
            if (currentYear != startingYear)
            {
                OnYearChanged?.Invoke(currentYear);
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Advanced by {daysToAdvance} days: Day {startingDay} Year {startingYear} → Day {currentDay} Year {currentYear}");
                if (worldTimeData.MonthsPerYear > 0)
                {
                    Debug.Log($"[TimeManager] Calendar updated to: {GetFormattedDate()}");
                }
            }
        }

        #endregion
        
                #region Debug and Utility Methods

        /// <summary>
        /// Logs comprehensive time information to the console.
        /// Useful for debugging and monitoring system state.
        /// </summary>
        [ContextMenu("Log Current Time Info")]
        public void DebugLogTimeInfo()
        {
            string transitionInfo = isInSeasonTransition 
                ? $"\nTransition: {currentSeason} → {targetSeason} (Progress: {seasonTransitionProgress:P1})"
                : "\nTransition: None";

            string seasonInfo = worldTimeData != null 
                ? $"\nSeason Progress: {SeasonProgress:P1} ({DaysRemainingInSeason} days remaining)"
                : "\nSeason Progress: N/A";

            string calendarInfo = "";
            if (worldTimeData != null && worldTimeData.MonthsPerYear > 0)
            {
                calendarInfo = $"\nCalendar: {currentMonth.name} {currentDayOfMonth} ({currentMonth.season})";
            }
                
            Debug.Log($"[TimeManager] Current Time Info:\n" +
                $"Time: {CurrentTimeDisplay} ({CurrentTimeDisplay12Hour})\n" +
                $"Date: {CurrentDateDisplay}" + calendarInfo +
                $"\nSeason: {currentSeason}" + seasonInfo +
                $"\nTime of Day: {currentTimeOfDay}\n" +
                $"Celestial Time: {celestialTime:F3}\n" +
                $"Time Scale: {timeScale:F2}\n" +
                $"Year Progress: {YearProgress:P1}" +
                transitionInfo);
        }

        /// <summary>
        /// Validates the current time manager state and reports any inconsistencies.
        /// </summary>
        [ContextMenu("Validate Time Manager State")]
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
                if (currentDay < 1 || currentDay > worldTimeData.totalDaysInYear)
                {
                    issues.AppendLine($"- Current day ({currentDay}) is outside valid range (1-{worldTimeData.totalDaysInYear})");
                    isValid = false;
                }

                // Check celestial time bounds
                if (celestialTime < 0f || celestialTime > 1f)
                {
                    issues.AppendLine($"- Celestial time ({celestialTime:F3}) is outside valid range (0.0-1.0)");
                    isValid = false;
                }

                // Check season consistency
                Season expectedSeason = worldTimeData.GetSeasonForDay(currentDay);
                if (expectedSeason != currentSeason)
                {
                    issues.AppendLine($"- Season mismatch: Expected {expectedSeason} for day {currentDay}, but current is {currentSeason}");
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
                Debug.Log("[TimeManager] Validation passed: All systems are functioning correctly");
            }
            else
            {
                Debug.LogWarning($"[TimeManager] Validation failed:\n{issues.ToString()}");
            }
        }

        /// <summary>
        /// Gets a formatted string with detailed time information for UI display.
        /// </summary>
        /// <returns>Formatted time information string</returns>
        public string GetDetailedTimeInfo()
        {
            if (worldTimeData == null) return "Time Manager not configured";

            string calendarInfo = "";
            if (worldTimeData.MonthsPerYear > 0)
            {
                calendarInfo = $"\nDate: {GetFormattedDate()}";
            }

            return $"Time: {CurrentTimeDisplay}" + calendarInfo +
                   $"\nDay {currentDay} of {worldTimeData.totalDaysInYear}, Year {currentYear}\n" +
                   $"Season: {currentSeason} ({SeasonProgress:P0} complete)\n" +
                   $"Time of Day: {currentTimeOfDay}\n" +
                   $"Time Scale: {timeScale:F1}x" +
                   (isInSeasonTransition ? $"\nTransitioning to {targetSeason} ({seasonTransitionProgress:P0})" : "");
        }

        /// <summary>
        /// Resets the time manager to its initial state.
        /// Useful for testing or restarting scenarios.
        /// </summary>
        [ContextMenu("Reset to Initial State")]
        public void ResetToInitialState()
        {
            celestialTime = Mathf.Clamp01(startingCelestialTime);
            currentDay = Mathf.Max(1, startingDay);
            currentYear = Mathf.Max(1, startingYear);
            timeScale = 1f;
            
            // Reset calendar state
            InitializeCalendarState();
            
            UpdateAllTimeInfo();
            
            Debug.Log($"[TimeManager] Reset to initial state - Time: {celestialTime:F3}, Day: {currentDay}, Year: {currentYear}");
            if (worldTimeData != null && worldTimeData.MonthsPerYear > 0)
            {
                Debug.Log($"[TimeManager] Calendar reset to: {GetFormattedDate()}");
            }
        }

        /// <summary>
        /// Advance to a specific month and day for testing
        /// </summary>
        [ContextMenu("Test: Advance to Specific Month")]
        public void TestAdvanceToMonth()
        {
            if (worldTimeData == null || worldTimeData.MonthsPerYear == 0)
            {
                Debug.LogWarning("[TimeManager] Cannot advance to month: Calendar system not configured");
                return;
            }

            // Example: Advance to month 3 (index 2), day 15
            int targetMonthIndex = 2;
            int targetDayInMonth = 15;
            
            if (targetMonthIndex >= worldTimeData.MonthsPerYear)
            {
                Debug.LogWarning($"[TimeManager] Month index {targetMonthIndex} is out of range (0-{worldTimeData.MonthsPerYear - 1})");
                return;
            }

            // Calculate the day of year for this month and day
            int targetDayOfYear = (targetMonthIndex * worldTimeData.daysPerMonth) + targetDayInMonth;
            
            if (targetDayOfYear > worldTimeData.totalDaysInYear)
            {
                targetDayOfYear = worldTimeData.totalDaysInYear;
            }

            SetCurrentDay(targetDayOfYear);
            
            Debug.Log($"[TimeManager] Advanced to {GetFormattedDate()} (Day {currentDay} of year)");
        }

        /// <summary>
        /// Test method to cycle through all months quickly
        /// </summary>
        [ContextMenu("Test: Cycle Through All Months")]
        public void TestCycleMonths()
        {
            if (worldTimeData == null || worldTimeData.MonthsPerYear == 0)
            {
                Debug.LogWarning("[TimeManager] Cannot cycle months: Calendar system not configured");
                return;
            }

            Debug.Log("[TimeManager] Cycling through all months:");
            
            for (int monthIndex = 0; monthIndex < worldTimeData.MonthsPerYear; monthIndex++)
            {
                // Set to middle of each month
                int dayOfYear = (monthIndex * worldTimeData.daysPerMonth) + (worldTimeData.daysPerMonth / 2);
                SetCurrentDay(dayOfYear);
                
                Debug.Log($"Month {monthIndex}: {GetFormattedDate()} (Day {currentDay}, Season: {currentSeason})");
            }
            
            // Reset to starting day
            SetCurrentDay(startingDay);
            Debug.Log($"Reset to starting day: {GetFormattedDate()}");
        }

        #endregion

        #region Unity Inspector Debug Info

        #if UNITY_EDITOR
        [Header("Runtime Debug Info (Read Only)")]
        [SerializeField, HideInInspector] private string debugCurrentTime;
        [SerializeField, HideInInspector] private string debugCurrentDate;
        [SerializeField, HideInInspector] private string debugCalendarDate;
        [SerializeField, HideInInspector] private string debugSeasonInfo;
        [SerializeField, HideInInspector] private string debugTransitionInfo;

        /// <summary>
        /// Updates debug information displayed in the inspector during play mode.
        /// </summary>
        private void UpdateInspectorDebugInfo()
        {
            if (!showInspectorDebugInfo) return;

            debugCurrentTime = $"{CurrentTimeDisplay} ({CurrentTimeDisplay12Hour})";
            debugCurrentDate = CurrentDateDisplay;
            
            if (worldTimeData != null && worldTimeData.MonthsPerYear > 0)
            {
                debugCalendarDate = $"{currentMonth.name} {currentDayOfMonth} ({currentMonth.season})";
            }
            else
            {
                debugCalendarDate = "Calendar not configured";
            }
            
            debugSeasonInfo = $"{currentSeason} - {SeasonProgress:P1} complete ({DaysRemainingInSeason} days remaining)";
            debugTransitionInfo = isInSeasonTransition 
                ? $"Transitioning to {targetSeason} ({seasonTransitionProgress:P1})"
                : "No active transition";
        }

        /// <summary>
        /// Unity OnValidate callback for inspector updates.
        /// </summary>
        private void OnValidate()
        {
            // Clamp values to valid ranges
            startingCelestialTime = Mathf.Clamp01(startingCelestialTime);
            startingDay = Mathf.Max(1, startingDay);
            startingYear = Mathf.Max(1, startingYear);
            timeScale = Mathf.Max(0f, timeScale);

            // Update debug info if in play mode
            if (Application.isPlaying && showInspectorDebugInfo)
            {
                UpdateInspectorDebugInfo();
            }
        }
        #endif

        #endregion

        #region Cleanup

        /// <summary>
        /// Unity OnDestroy callback. Cleans up events and resources.
        /// </summary>
        private void OnDestroy()
        {
            // Clear all events to prevent memory leaks
            OnSeasonChanged = null;
            OnDayChanged = null;
            OnYearChanged = null;
            OnTimeOfDayChanged = null;
            OnGameTimeUpdated = null;
            OnTimeScaleChanged = null;
            OnMonthChanged = null;
        }

        #endregion
    }
}