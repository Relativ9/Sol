using UnityEngine;

namespace Sol
{
        /// <summary>
        /// Runtime time state - changes frequently during gameplay
        /// </summary>
        [System.Serializable]
        public class GameTime
        {
            // public float totalGameTime;      // Total elapsed game time in seconds
            // public float dayTime;           // Current time within the day (0-1, where 0.5 = noon)
            // public int currentDay;          // Day number since game start
            // public Season currentSeason;    // Current dominant season
            // public float seasonProgress;    // Progress through current season (0-1)
            // public float seasonTransition;  // Blend factor between seasons (0-1)
            // public Season nextSeason;       // Season we're transitioning to
            
            // Your existing fields
            public float totalGameTime;
            public float dayTime;
            public int currentDay;
            public float seasonProgress;
            public float seasonTransition;
            public int hours;
            public int minutes;
            public int seconds;
            
            [Tooltip("Current season index (0-based)")]
            public int currentSeasonIndex;
    
            [Tooltip("Current season name")]
            public string currentSeasonName;
    
            [Tooltip("Next season index during transitions")]
            public int nextSeasonIndex = -1; // -1 means no transition
    
            [Tooltip("Next season name during transitions")]
            public string nextSeasonName;
    
            // Add these calculated properties (no new storage)
            public int DaysRemainingInSeason { get; set; }  // Set by TimeManager
            public int TotalDaysInSeason { get; set; }      // Set by TimeManager
            
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
            Svik,    // Fall equivalent
            Evinotr,       // Long Polar nights, barely any light from Sol for only a few hours every day.
            Gro    // Spring equivalent  
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
        }
        
}
