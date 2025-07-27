using UnityEngine;

namespace Sol
{
        /// <summary>
        /// Runtime time state - changes frequently during gameplay
        /// </summary>
        [System.Serializable]
        public class GameTime
        {
            public float totalGameTime;      // Total elapsed game time in seconds
            public float dayTime;           // Current time within the day (0-1, where 0.5 = noon)
            public int currentDay;          // Day number since game start
            public Season currentSeason;    // Current dominant season
            public float seasonProgress;    // Progress through current season (0-1)
            public float seasonTransition;  // Blend factor between seasons (0-1)
            public Season nextSeason;       // Season we're transitioning to
        }

        /// <summary>
        /// Season enumeration based on orbital position and axial tilt
        /// </summary>
        public enum Season
        {
            PolarSummer,    // Perihelion - close to primary star, midnight sun
            Transition1,    // Spring/Fall equivalent
            Equinox,        // Balanced distance
            Transition2,    // Fall/Spring equivalent  
            LongNight       // Aphelion - far from primary star, long nights
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
