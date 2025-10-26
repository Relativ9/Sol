using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Runtime time state - changes frequently during gameplay
    /// </summary>
    [System.Serializable]
    public class GameTime
    {
        [Header("Time Progression")]
        [Tooltip("Total elapsed game time in seconds")]
        public float totalGameTime;
        
        [Tooltip("Current time within the day (0-1, where 0.5 = midday)")]
        public float dayTime;
        
        [Tooltip("Current day number (0-based within year)")]
        public int currentDay;
        
        [Tooltip("Current year number (configurable start year)")]
        public int currentYear;

        [Header("Time Display")]
        [Tooltip("Hours (0-23 or custom range)")]
        public int hours;
        
        [Tooltip("Minutes (0-59)")]
        public int minutes;
        
        [Tooltip("Seconds (0-59)")]
        public int seconds;

        [Header("Season Information")]
        [Tooltip("Current season index (0-based)")]
        public int currentSeasonIndex;

        [Tooltip("Current season name")]
        public string currentSeasonName;

        [Tooltip("Progress through current season (0-1)")]
        public float seasonProgress;

        [Tooltip("Season transition blend factor (0-1)")]
        public float seasonTransition;

        [Tooltip("Next season index during transitions (-1 if not transitioning)")]
        public int nextSeasonIndex = -1;

        [Tooltip("Next season name during transitions")]
        public string nextSeasonName;

        [Header("Calculated Properties")]
        [Tooltip("Days remaining in current season")]
        public int DaysRemainingInSeason { get; set; }
        
        [Tooltip("Total days in current season")]
        public int TotalDaysInSeason { get; set; }

        /// <summary>
        /// Gets current season name for display
        /// </summary>
        public string GetCurrentSeasonDisplayName()
        {
            return !string.IsNullOrEmpty(currentSeasonName) ? currentSeasonName : "Unknown Season";
        }

        /// <summary>
        /// Gets next season name for display during transitions
        /// </summary>
        public string GetNextSeasonDisplayName()
        {
            return !string.IsNullOrEmpty(nextSeasonName) ? nextSeasonName : currentSeasonName;
        }

        /// <summary>
        /// Checks if currently in a season transition
        /// </summary>
        public bool IsInSeasonTransition()
        {
            return nextSeasonIndex >= 0 && seasonTransition > 0f;
        }
    }

    /// <summary>
    /// Season enumeration based on orbital position and axial tilt
    /// </summary>
    public enum Season
    {
        Lansomr,    // Perihelion - close to primary star, midnight sun
        Svik,       // Fall equivalent
        Evinotr,    // Long Polar nights, barely any light from Sol for only a few hours every day
        Gro         // Spring equivalent  
    }

    /// <summary>
    /// Time of day periods for gameplay systems
    /// </summary>
    public enum TimeOfDay
    {
        EarlyMorning,   // 0.0 - 0.2
        Morning,        // 0.2 - 0.4
        Midday,         // 0.4 - 0.6
        Afternoon,      // 0.6 - 0.8
        Evening         // 0.8 - 1.0
    }

    /// <summary>
    /// Decoupled event system for time notifications
    /// Follows Observer pattern for loose coupling
    /// </summary>
    public static class TimeEvents
    {
        public static System.Action<GameTime> OnTimeUpdated;
        public static System.Action<Season, Season> OnSeasonChanged;
        public static System.Action<TimeOfDay, TimeOfDay> OnTimeOfDayChanged;
        public static System.Action<int> OnNewDay;
        public static System.Action<int> OnNewYear;
    }
}
