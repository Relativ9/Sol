using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Interface for time management systems
    /// Provides abstraction for celestial time control and seasonal transitions
    /// Allows different time management strategies while maintaining dependency inversion
    /// </summary>
    public interface ITimeManager
    {
        // Time Properties
        /// <summary>
        /// Current celestial time in seconds since system start
        /// </summary>
        float CelestialTime { get; }

        /// <summary>
        /// Current time scale multiplier affecting all time calculations
        /// </summary>
        float TimeScale { get; }

        // Calendar Properties
        /// <summary>
        /// Length of one game day in real seconds
        /// </summary>
        float DayLengthInSeconds { get; }

        /// <summary>
        /// Number of days in one game year
        /// </summary>
        int DaysPerYear { get; }

        /// <summary>
        /// Current day of the year (1 to DaysPerYear)
        /// </summary>
        int CurrentDay { get; }

        /// <summary>
        /// Current year
        /// </summary>
        int CurrentYear { get; }

        /// <summary>
        /// Number of days per season (calculated from DaysPerYear / number of seasons)
        /// </summary>
        int DaysPerSeason { get; }

        /// <summary>
        /// Duration of one season in seconds
        /// </summary>
        float SeasonDurationInSeconds { get; }

        /// <summary>
        /// Progress through current day (0-1, where 1 = day complete)
        /// </summary>
        float DayProgress { get; }

        /// <summary>
        /// Whether Primary Star Y-axis synchronization with day length is enforced
        /// </summary>
        bool EnforceCelestialDaySync { get; }

        // Season Properties
        /// <summary>
        /// Currently active season
        /// </summary>
        Season CurrentSeason { get; }

        /// <summary>
        /// Target season during transitions (same as CurrentSeason when not transitioning)
        /// </summary>
        Season TargetSeason { get; }

        /// <summary>
        /// Progress of current seasonal transition (0-1, where 1 = complete)
        /// </summary>
        float SeasonTransitionProgress { get; }

        /// <summary>
        /// Whether a seasonal transition is currently in progress
        /// </summary>
        bool IsTransitioning { get; }

        // Events
        /// <summary>
        /// Event fired when a season change completes
        /// </summary>
        System.Action<Season> OnSeasonChanged { get; set; }

        /// <summary>
        /// Event fired during seasonal transitions with progress updates
        /// Parameters: fromSeason, toSeason, progress (0-1)
        /// </summary>
        System.Action<Season, Season, float> OnSeasonTransitionUpdate { get; set; }

        /// <summary>
        /// Event fired when time scale changes
        /// </summary>
        System.Action OnTimeScaleChanged { get; set; }

        /// <summary>
        /// Event fired when a new day begins
        /// Parameters: newDay, currentYear
        /// </summary>
        System.Action<int, int> OnDayChanged { get; set; }

        /// <summary>
        /// Event fired when a new year begins
        /// Parameters: newYear
        /// </summary>
        System.Action<int> OnYearChanged { get; set; }

        // Time Control Methods
        /// <summary>
        /// Sets the time scale multiplier
        /// </summary>
        /// <param name="newTimeScale">New time scale (0 or positive)</param>
        void SetTimeScale(float newTimeScale);

        /// <summary>
        /// Sets the celestial time multiplier for faster/slower celestial calculations
        /// </summary>
        /// <param name="multiplier">Celestial time multiplier (0 or positive)</param>
        void SetCelestialTimeMultiplier(float multiplier);

        /// <summary>
        /// Pauses all time progression
        /// </summary>
        void PauseTime();

        /// <summary>
        /// Resumes time progression
        /// </summary>
        void ResumeTime();

        /// <summary>
        /// Manually sets the celestial time
        /// </summary>
        /// <param name="time">New celestial time in seconds</param>
        void SetCelestialTime(float time);

        // Calendar Control Methods
        /// <summary>
        /// Sets the current day and year
        /// </summary>
        /// <param name="day">Day of year (1 to DaysPerYear)</param>
        /// <param name="year">Year (1 or greater)</param>
        void SetDay(int day, int year);

        /// <summary>
        /// Sets the length of one game day in real seconds
        /// </summary>
        /// <param name="lengthInSeconds">Day length in seconds</param>
        void SetDayLength(float lengthInSeconds);

        // Season Control Methods
        /// <summary>
        /// Starts a transition to a new season
        /// </summary>
        /// <param name="newSeason">Target season</param>
        void StartSeasonTransition(Season newSeason);

        /// <summary>
        /// Sets the current season
        /// </summary>
        /// <param name="season">New season</param>
        /// <param name="immediate">If true, changes immediately without transition</param>
        void SetSeason(Season season, bool immediate = false);

        // Seasonal Data Access Methods
        /// <summary>
        /// Gets the seasonal data for the current season
        /// </summary>
        /// <returns>Current season's data or null if not found</returns>
        SeasonalData GetCurrentSeasonalData();

        /// <summary>
        /// Gets the seasonal data for the target season (during transitions)
        /// </summary>
        /// <returns>Target season's data or null if not found</returns>
        SeasonalData GetTargetSeasonalData();

        /// <summary>
        /// Gets seasonal data for a specific season
        /// </summary>
        /// <param name="season">Season to get data for</param>
        /// <returns>Seasonal data or null if not found</returns>
        SeasonalData GetSeasonalData(Season season);
    }
}
