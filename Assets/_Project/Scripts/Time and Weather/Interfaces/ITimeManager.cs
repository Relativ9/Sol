// namespace Sol
// {
//     /// <summary>
//     /// Core interface for time management system providing access to game time, seasons, and temporal calculations.
//     /// Follows Interface Segregation Principle by grouping related functionality while keeping the interface focused.
//     /// Supports dependency inversion by allowing different time management implementations.
//     /// </summary>
//     public interface ITimeManager
//     {
//         #region Core Time Properties
//         
//         /// <summary>
//         /// Current celestial time as a normalized value (0.0 = start of day, 1.0 = end of day)
//         /// </summary>
//         float CelestialTime { get; }
//         
//         /// <summary>
//         /// Current time scale multiplier affecting time progression speed
//         /// </summary>
//         float TimeScale { get; }
//         
//         /// <summary>
//         /// Currently active season based on day of year
//         /// </summary>
//         Season CurrentSeason { get; }
//         
//         /// <summary>
//         /// Current day number within the year (1-based)
//         /// </summary>
//         int CurrentDay { get; }
//         
//         /// <summary>
//         /// Current year number (1-based)
//         /// </summary>
//         int CurrentYear { get; }
//         
//         /// <summary>
//         /// Total number of days in one complete year
//         /// </summary>
//         int DaysPerYear { get; }
//
//         #endregion
//
//         #region World Time Data Integration
//         
//         /// <summary>
//         /// Reference to the world time configuration data
//         /// </summary>
//         WorldTimeData WorldTimeData { get; }
//         
//         /// <summary>
//         /// Current game time state with all temporal information
//         /// </summary>
//         GameTime CurrentGameTime { get; }
//
//         #endregion
//
//         #region Season Information
//         
//         /// <summary>
//         /// Number of days in the currently active season
//         /// </summary>
//         int DaysInCurrentSeason { get; }
//         
//         /// <summary>
//         /// Number of days remaining in the current season
//         /// </summary>
//         int DaysRemainingInSeason { get; }
//         
//         /// <summary>
//         /// Progress through current season (0.0 = season start, 1.0 = season end)
//         /// </summary>
//         float SeasonProgress { get; }
//         
//         /// <summary>
//         /// Season range data for the currently active season
//         /// </summary>
//         WorldTimeData.SeasonRange CurrentSeasonRange { get; }
//
//         #endregion
//
//         #region Season Transition Properties
//         
//         /// <summary>
//         /// Whether the system is currently in a season transition period
//         /// </summary>
//         bool IsInSeasonTransition { get; }
//         
//         /// <summary>
//         /// Target season when transitioning (same as CurrentSeason when not transitioning)
//         /// </summary>
//         Season TargetSeason { get; }
//         
//         /// <summary>
//         /// Progress through current season transition (0.0 = transition start, 1.0 = transition complete)
//         /// </summary>
//         float SeasonTransitionProgress { get; }
//         
//         /// <summary>
//         /// Number of days over which season transitions occur
//         /// </summary>
//         int SeasonTransitionDurationDays { get; }
//
//         #endregion
//
//         #region Time Display Properties
//         
//         /// <summary>
//         /// Current time in 24-hour format (HH:MM:SS)
//         /// </summary>
//         string CurrentTimeDisplay { get; }
//         
//         /// <summary>
//         /// Current time in 12-hour format with AM/PM (HH:MM:SS AM/PM)
//         /// </summary>
//         string CurrentTimeDisplay12Hour { get; }
//         
//         /// <summary>
//         /// Current time of day category (Morning, Afternoon, Evening, Night)
//         /// </summary>
//         TimeOfDay CurrentTimeOfDay { get; }
//         
//         /// <summary>
//         /// Formatted date display including day, year, and season
//         /// </summary>
//         string CurrentDateDisplay { get; }
//
//         #endregion
//
//         #region Progress Properties
//         
//         /// <summary>
//         /// Progress through current day (0.0 = day start, 1.0 = day end)
//         /// </summary>
//         float DayProgress { get; }
//         
//         /// <summary>
//         /// Progress through current year (0.0 = year start, 1.0 = year end)
//         /// </summary>
//         float YearProgress { get; }
//
//         #endregion
//
//         #region SeasonalData Integration
//         
//         /// <summary>
//         /// Gets the seasonal data configuration for the currently active season
//         /// </summary>
//         /// <returns>SeasonalData for current season or null if not available</returns>
//         SeasonalData GetCurrentSeasonalData();
//         
//         /// <summary>
//         /// Gets the seasonal data configuration for a specific season
//         /// </summary>
//         /// <param name="season">Season to get data for</param>
//         /// <returns>SeasonalData for specified season or null if not available</returns>
//         SeasonalData GetSeasonalData(Season season);
//
//         #endregion
//
//         #region Time Queries
//         
//         /// <summary>
//         /// Gets current game time state with all temporal information
//         /// </summary>
//         /// <returns>Complete GameTime object with current state</returns>
//         GameTime GetCurrentGameTime();
//         
//         /// <summary>
//         /// Determines which season contains the specified day of year
//         /// </summary>
//         /// <param name="dayOfYear">Day number to check (1-based)</param>
//         /// <returns>Season containing the specified day</returns>
//         Season GetSeasonForDay(int dayOfYear);
//         
//         /// <summary>
//         /// Gets season range information for a specific season
//         /// </summary>
//         /// <param name="season">Season to get range for</param>
//         /// <returns>SeasonRange with temporal boundaries and duration</returns>
//         WorldTimeData.SeasonRange GetSeasonRange(Season season);
//
//         #endregion
//
//         #region Time Control
//         
//         /// <summary>
//         /// Sets the time scale multiplier for time progression speed
//         /// </summary>
//         /// <param name="newTimeScale">New time scale value (0 = paused, 1 = normal speed, >1 = accelerated)</param>
//         void SetTimeScale(float newTimeScale);
//
//         #endregion
//
//         #region Time Conversion Utilities
//         
//         /// <summary>
//         /// Converts real-time seconds to equivalent game time duration
//         /// </summary>
//         /// <param name="realTimeSeconds">Real-time duration in seconds</param>
//         /// <returns>Equivalent game time duration</returns>
//         float ConvertRealTimeToGameTime(float realTimeSeconds);
//         
//         /// <summary>
//         /// Converts game time duration to equivalent real-time seconds
//         /// </summary>
//         /// <param name="gameTimeSeconds">Game time duration</param>
//         /// <returns>Equivalent real-time duration in seconds</returns>
//         float ConvertGameTimeToRealTime(float gameTimeSeconds);
//
//         #endregion
//
//         #region Events
//         
//         /// <summary>
//         /// Event triggered when the active season changes
//         /// </summary>
//         event System.Action<Season> OnSeasonChanged;
//         
//         /// <summary>
//         /// Event triggered when the day number changes
//         /// </summary>
//         event System.Action<int> OnDayChanged;
//         
//         /// <summary>
//         /// Event triggered when the year number changes
//         /// </summary>
//         event System.Action<int> OnYearChanged;
//         
//         /// <summary>
//         /// Event triggered when the time of day category changes
//         /// </summary>
//         event System.Action<TimeOfDay> OnTimeOfDayChanged;
//         
//         /// <summary>
//         /// Event triggered when game time is updated (frequent updates)
//         /// </summary>
//         event System.Action<GameTime> OnGameTimeUpdated;
//         
//         /// <summary>
//         /// Event triggered when time scale changes
//         /// </summary>
//         event System.Action OnTimeScaleChanged;
//
//         #endregion
//     }
// }

using System;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Interface for time management system with calendar support
    /// Provides access to time, seasons, and calendar information
    /// </summary>
    public interface ITimeManager
    {
        // Existing properties
        WorldTimeData WorldTimeData { get; }
        float CelestialTime { get; }
        Season CurrentSeason { get; }
        float TimeScale { get; set; }
        
        // New calendar properties
        int CurrentDayOfYear { get; }
        Month CurrentMonth { get; }
        int CurrentDayOfMonth { get; }
        
        // Existing events
        event Action OnTimeScaleChanged;
        event Action<Season> OnSeasonChanged;
        
        // New calendar events
        event Action<Month> OnMonthChanged;
        event Action<int> OnDayChanged;
        
        // Existing methods
        void SetTimeScale(float newTimeScale);
        SeasonalData GetCurrentSeasonalData();
        
        // New calendar methods
        string GetFormattedTime();
        string GetFormattedDate();
        string GetFormattedDateTime();
    }
}