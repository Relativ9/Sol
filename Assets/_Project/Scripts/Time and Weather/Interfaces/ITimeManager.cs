using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Interface for time management systems
    /// Provides time tracking, seasonal transitions, and calendar functionality
    /// </summary>
    public interface ITimeManager
    {
        // Time Properties
        float CelestialTime { get; }
        float TimeScale { get; }
        float DayLengthInSeconds { get; }
        float DayProgress { get; }
        
        // Calendar Properties
        int CurrentDay { get; }
        int CurrentYear { get; }
        int DaysPerYear { get; }
        int DaysPerSeason { get; }
        float SeasonDurationInSeconds { get; }
        
        // Season Properties
        Season CurrentSeason { get; }
        Season TargetSeason { get; }
        float SeasonTransitionProgress { get; }
        bool IsTransitioning { get; }
        
        // Events
        System.Action<Season> OnSeasonChanged { get; set; }
        System.Action<Season, Season, float> OnSeasonTransitionUpdate { get; set; }
        System.Action OnTimeScaleChanged { get; set; }
        System.Action<int, int> OnDayChanged { get; set; } // day, year
        System.Action<int> OnYearChanged { get; set; }
        
        // Time Control Methods
        void SetTimeScale(float newTimeScale);
        void SetCelestialTimeMultiplier(float multiplier);
        void PauseTime();
        void ResumeTime();
        void SetCelestialTime(float time);
        
        // Season Control Methods
        void StartSeasonTransition(Season newSeason);
        void SetSeason(Season season, bool immediate = false);
        
        // Data Access Methods
        SeasonalData GetCurrentSeasonalData();
        SeasonalData GetTargetSeasonalData();
        SeasonalData GetSeasonalData(Season season);
        
        // Calendar Control Methods
        void SetDay(int day, int year);
        void SetDayLength(float lengthInSeconds);
    }
}