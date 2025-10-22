using System;

namespace Sol
{
    /// <summary>
    /// Interface for time management systems in Sol.
    /// Provides access to planetary time, seasons, calendar, and celestial cycles.
    /// </summary>
    public interface ITimeManager
    {
        #region Core Time Properties
        
        /// <summary>
        /// Current celestial time (0.0 = midnight, 0.5 = noon, 1.0 = midnight next day)
        /// </summary>
        float CelestialTime { get; }
        
        /// <summary>
        /// Current day of the year (1-based)
        /// </summary>
        int CurrentDay { get; }
        
        /// <summary>
        /// Current year (1-based)
        /// </summary>
        int CurrentYear { get; }
        
        /// <summary>
        /// Current time scale multiplier
        /// </summary>
        float TimeScale { get; }
        
        /// <summary>
        /// Whether time progression is paused
        /// </summary>
        bool IsPaused { get; }
        
        #endregion

        #region Season Properties
        
        /// <summary>
        /// Index of the currently active season (0-based)
        /// </summary>
        int CurrentSeasonIndex { get; }
        
        /// <summary>
        /// Name of the currently active season
        /// </summary>
        string CurrentSeasonName { get; }
        
        /// <summary>
        /// Target season index during transitions (-1 if no transition)
        /// </summary>
        int TargetSeasonIndex { get; }
        
        /// <summary>
        /// Target season name during transitions
        /// </summary>
        string TargetSeasonName { get; }
        
        /// <summary>
        /// Whether currently in a season transition
        /// </summary>
        bool IsInSeasonTransition { get; }
        
        /// <summary>
        /// Progress through current season transition (0.0 to 1.0)
        /// </summary>
        float SeasonTransitionProgress { get; }
        
        /// <summary>
        /// Number of days in the currently active season
        /// </summary>
        int DaysInCurrentSeason { get; }
        
        /// <summary>
        /// Progress through current season (0.0 = season start, 1.0 = season end)
        /// </summary>
        float SeasonProgress { get; }
        
        #endregion

        #region Time of Day Properties
        
        /// <summary>
        /// Current time of day classification
        /// </summary>
        TimeOfDay CurrentTimeOfDay { get; }
        
        /// <summary>
        /// Progress through the current year (0.0 to 1.0)
        /// </summary>
        float YearProgress { get; }
        
        #endregion

        #region Calendar Properties
        
        /// <summary>
        /// Current month information
        /// </summary>
        Month CurrentMonth { get; }
        
        /// <summary>
        /// Current day within the month (1-based)
        /// </summary>
        int CurrentDayOfMonth { get; }
        
        #endregion

        #region Display Properties
        
        /// <summary>
        /// Current time formatted as HH:MM:SS (24-hour format)
        /// </summary>
        string CurrentTimeDisplay { get; }
        
        /// <summary>
        /// Current time formatted as HH:MM:SS AM/PM (12-hour format)
        /// </summary>
        string CurrentTimeDisplay12Hour { get; }
        
        /// <summary>
        /// Current date display including day, year, and season
        /// </summary>
        string CurrentDateDisplay { get; }
        
        #endregion

        #region Game Time Access
        
        /// <summary>
        /// Complete game time information object
        /// </summary>
        GameTime CurrentGameTime { get; }
        
        /// <summary>
        /// Gets the current game time with all calculated values
        /// </summary>
        /// <returns>GameTime object with current time information</returns>
        GameTime GetCurrentGameTime();
        
        #endregion

        #region Time Control Methods
        
        /// <summary>
        /// Sets the time scale multiplier
        /// </summary>
        /// <param name="newTimeScale">New time scale (0 or positive)</param>
        void SetTimeScale(float newTimeScale);
        
        /// <summary>
        /// Pauses time progression
        /// </summary>
        void PauseTime();
        
        /// <summary>
        /// Resumes time progression
        /// </summary>
        void ResumeTime();
        
        /// <summary>
        /// Toggles pause state
        /// </summary>
        void TogglePause();
        
        /// <summary>
        /// Sets the celestial time directly
        /// </summary>
        /// <param name="newCelestialTime">New celestial time (0.0 to 1.0)</param>
        void SetCelestialTime(float newCelestialTime);
        
        /// <summary>
        /// Sets the current day of year
        /// </summary>
        /// <param name="newDay">New day (1-based)</param>
        void SetCurrentDay(int newDay);
        
        /// <summary>
        /// Sets the current year
        /// </summary>
        /// <param name="newYear">New year (1-based)</param>
        void SetCurrentYear(int newYear);
        
        /// <summary>
        /// Advances time by the specified number of days
        /// </summary>
        /// <param name="days">Number of days to advance</param>
        void AdvanceDays(int days);
        
        #endregion

        #region Seasonal Data Access
        
        /// <summary>
        /// Gets seasonal data for the currently active season
        /// </summary>
        /// <returns>SeasonalData for current season or null if not available</returns>
        SeasonalData GetCurrentSeasonalData();
        
        /// <summary>
        /// Gets seasonal data for a specific season index
        /// </summary>
        /// <param name="seasonIndex">Season index to get data for</param>
        /// <returns>SeasonalData for specified season or null if not available</returns>
        SeasonalData GetSeasonalData(int seasonIndex);
        
        /// <summary>
        /// Gets the season index for a specific day of year
        /// </summary>
        /// <param name="dayOfYear">Day of year to check</param>
        /// <returns>Season index containing the specified day</returns>
        int GetSeasonIndexForDay(int dayOfYear);
        
        /// <summary>
        /// Gets season range information for a specific season index
        /// </summary>
        /// <param name="seasonIndex">Season index to get range for</param>
        /// <returns>SeasonRange with temporal boundaries and duration</returns>
        WorldTimeData.SeasonRange GetSeasonRange(int seasonIndex);
        
        /// <summary>
        /// Gets the name of a season by its index
        /// </summary>
        /// <param name="seasonIndex">Season index</param>
        /// <returns>Season name or "Unknown Season" if invalid</returns>
        string GetSeasonName(int seasonIndex);
        
        #endregion

        #region Date Formatting
        
        /// <summary>
        /// Gets formatted date string (e.g., "Glavyr 15")
        /// </summary>
        /// <returns>Formatted date string</returns>
        string GetFormattedDate();
        
        /// <summary>
        /// Gets formatted full date string with season (e.g., "Glavyr 15 (Spring), Year 3")
        /// </summary>
        /// <returns>Formatted full date string</returns>
        string GetFormattedFullDate();
        
        #endregion

        #region Validation
        
        /// <summary>
        /// Validates the current time manager state and logs any issues
        /// </summary>
        void ValidateTimeManagerState();
        
        #endregion

        #region Events
        
        /// <summary>
        /// Triggered when celestial time changes
        /// </summary>
        event Action<float> OnCelestialTimeChanged;
        
        /// <summary>
        /// Triggered when the day changes
        /// </summary>
        event Action<int> OnDayChanged;
        
        /// <summary>
        /// Triggered when the year changes
        /// </summary>
        event Action<int> OnYearChanged;
        
        /// <summary>
        /// Triggered when the active season changes (seasonIndex, seasonName)
        /// </summary>
        event Action<int, string> OnSeasonChanged;
        
        /// <summary>
        /// Triggered when time of day classification changes
        /// </summary>
        event Action<TimeOfDay> OnTimeOfDayChanged;
        
        /// <summary>
        /// Triggered when the month changes
        /// </summary>
        event Action<Month> OnMonthChanged;
        
        #endregion
    }
}