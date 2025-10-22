using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Configuration data for planetary time system including seasons, celestial bodies, transitions, and calendar.
    /// Serves as the single source of truth for all time-related calculations and seasonal data references.
    /// Follows Single Responsibility Principle by focusing solely on data configuration and basic calculations.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldTimeData", menuName = "Sol/World Time Data")]
    public class WorldTimeData : ScriptableObject
    {
        [Header("Planet Time Settings")]
        [Tooltip("Length of one game day in real-time seconds (e.g., 1200 = 20 minutes real time)")]
        public float dayLengthInSeconds = 7200;
        
        [Tooltip("Total number of days in one complete planetary year")]
        public int totalDaysInYear = 832;

        [Header("Day Time Display")]
        [Tooltip("Number of hours displayed per day for time formatting (typically 24)")]
        public int hoursPerDay = 20;
        
        [Tooltip("Number of minutes displayed per hour for time formatting (typically 60)")]
        public int minutesPerHour = 60;
        
        [Tooltip("Number of seconds displayed per minute for time formatting (typically 60)")]
        public int secondsPerMinute = 60;

        [Header("Season Configuration")]
        [Tooltip("All seasons in chronological order. System will cycle through these based on day count.")]
        public List<SeasonConfiguration> seasons = new List<SeasonConfiguration>();

        [Header("Season Transition Settings")]
        [Tooltip("Number of days over which celestial bodies smoothly transition between seasons (centered on season boundary)")]
        [Range(1, 50)]
        public int seasonTransitionDays = 20;

        [Header("Calendar System")]
        [Tooltip("Number of days in each month (should divide evenly into season lengths)")]
        public int daysPerMonth = 104;
        
        [Tooltip("Month definitions for the Sol calendar")]
        [SerializeField] private List<MonthDefinition> months = new List<MonthDefinition>();

        /// <summary>
        /// Serializable month definition for inspector
        /// </summary>
        [Serializable]
        public class MonthDefinition
        {
            [Tooltip("Name of the month")]
            public string name;
    
            [Tooltip("Month index (0-based)")]
            [Range(0, 15)]
            public int index;
    
            [Tooltip("Optional color for UI representation")]
            public Color monthColor = Color.white;
        }
        
        [System.Serializable]
        public class SeasonConfiguration
        {
            [Tooltip("Name of this season")]
            public string seasonName = "New Season";

            [Tooltip("Length of this season in days")]
            [Min(1)]
            public int lengthInDays = 30;

            [Tooltip("Seasonal data containing celestial body configurations for this season")]
            public SeasonalData seasonalData;

            [Header("Season-Specific Settings (Optional)")]
            [Tooltip("Override ambient colors for this season")]
            public bool overrideAmbientColors = false;

            [Tooltip("Day ambient color for this season")]
            public Color seasonDayAmbient = Color.white;

            [Tooltip("Night ambient color for this season")]
            public Color seasonNightAmbient = Color.blue;
    
            [Tooltip("Optional color for UI representation")]
            public Color seasonColor = Color.white;
        }

        /// <summary>
        /// Represents a season's time range within the year with utility methods for progress calculation.
        /// Encapsulates season-specific data and provides methods for temporal calculations.
        /// </summary>
        [System.Serializable]
        public struct SeasonRange
        {
            public int seasonIndex;
            public string seasonName;
            public int startDay;
            public int endDay;
            public int duration;

            /// <summary>
            /// Checks if the specified day falls within this season's range
            /// </summary>
            /// <param name="dayOfYear">Day number to check (1-based)</param>
            /// <returns>True if day is within this season</returns>
            public bool ContainsDay(int dayOfYear)
            {
                return dayOfYear >= startDay && dayOfYear <= endDay;
            }

            /// <summary>
            /// Calculates progress through this season for the given day
            /// </summary>
            /// <param name="dayOfYear">Current day of year</param>
            /// <returns>Progress from 0.0 (season start) to 1.0 (season end)</returns>
            public float GetProgressForDay(int dayOfYear)
            {
                if (!ContainsDay(dayOfYear)) return 0f;
                return (float)(dayOfYear - startDay) / (float)duration;
            }

            /// <summary>
            /// Calculates days remaining in this season from the given day
            /// </summary>
            /// <param name="dayOfYear">Current day of year</param>
            /// <returns>Number of days until season ends</returns>
            public int GetDaysRemainingForDay(int dayOfYear)
            {
                if (!ContainsDay(dayOfYear)) return 0;
                return endDay - dayOfYear;
            }
        }

        // Cached data for performance
        private Month[] _monthCache;
        private SeasonRange[] _seasonRangeCache;

        // Calculated properties for time conversion
        public float SecondsPerGameHour => dayLengthInSeconds / hoursPerDay;
        public float SecondsPerGameMinute => SecondsPerGameHour / minutesPerHour;
        public float SecondsPerGameSecond => SecondsPerGameMinute / secondsPerMinute;
        
        /// <summary>
        /// Gets calculated season ranges (cached for performance)
        /// </summary>
        public SeasonRange[] SeasonRanges
        {
            get
            {
                if (_seasonRangeCache == null)
                    CalculateSeasonRanges();
                return _seasonRangeCache;
            }
        }

        // Calendar properties
        /// <summary>
        /// Gets all months in the calendar
        /// </summary>
        public Month[] Months
        {
            get
            {
                if (_monthCache == null)
                    InitializeMonthCache();
                return _monthCache;
            }
        }
        
        /// <summary>
        /// Number of months in a year
        /// </summary>
        public int MonthsPerYear => months.Count;

        /// <summary>
        /// Gets the total number of seasons
        /// </summary>
        public int GetSeasonCount()
        {
            return seasons?.Count ?? 0;
        }

        
        /// <summary>
        /// Gets the SeasonalData asset for the specified season index
        /// </summary>
        /// <param name="seasonIndex">Season index (0-based)</param>
        /// <returns>SeasonalData asset or null if not assigned</returns>
        public SeasonalData GetSeasonalData(int seasonIndex)
        {
            if (seasons == null || seasonIndex < 0 || seasonIndex >= seasons.Count)
                return null;
        
            return seasons[seasonIndex].seasonalData;
        }
        

        /// <summary>
        /// Gets the season configuration for a given index
        /// </summary>
        public SeasonConfiguration GetSeasonConfiguration(int seasonIndex)
        {
            if (seasons == null || seasonIndex < 0 || seasonIndex >= seasons.Count)
                return null;
                
            return seasons[seasonIndex];
        }

        /// <summary>
        /// Gets season name by index
        /// </summary>
        public string GetSeasonName(int seasonIndex)
        {
            var seasonConfig = GetSeasonConfiguration(seasonIndex);
            return seasonConfig?.seasonName ?? "Unknown Season";
        }

        /// <summary>
        /// Determines which season contains the specified day
        /// </summary>
        /// <param name="dayOfYear">Day number (1-based)</param>
        /// <returns>Season index (0-based)</returns>
        public int GetSeasonIndexForDay(int dayOfYear)
        {
            if (seasons == null || seasons.Count == 0)
                return 0;
            
            int totalYearLength = GetTotalSeasonDays();
            if (totalYearLength == 0) return 0;
            
            // Handle wrap-around
            int normalizedDay = ((dayOfYear - 1) % totalYearLength) + 1;
            
            var ranges = SeasonRanges;
            foreach (var range in ranges)
            {
                if (range.ContainsDay(normalizedDay))
                {
                    return range.seasonIndex;
                }
            }
            
            return 0; // Fallback
        }


        /// <summary>
        /// Gets the day within the current season
        /// </summary>
        /// <param name="dayOfYear">Day number (1-based)</param>
        /// <returns>Day within season (0-based)</returns>
        public int GetDayInSeason(int dayOfYear)
        {
            if (seasons == null || seasons.Count == 0)
                return 0;
            
            int seasonIndex = GetSeasonIndexForDay(dayOfYear);
            var ranges = SeasonRanges;
            
            if (seasonIndex >= 0 && seasonIndex < ranges.Length)
            {
                int totalYearLength = GetTotalSeasonDays();
                int normalizedDay = ((dayOfYear - 1) % totalYearLength) + 1;
                return normalizedDay - ranges[seasonIndex].startDay;
            }
            
            return 0;
        }

        /// <summary>
        /// Gets total year length from season configurations
        /// </summary>
        public int GetTotalSeasonDays()
        {
            if (seasons == null) return totalDaysInYear;
            
            int total = 0;
            foreach (var season in seasons)
            {
                total += season.lengthInDays;
            }
            return total > 0 ? total : totalDaysInYear;
        }
        
        /// <summary>
        /// Gets the season range data for the specified season index
        /// </summary>
        /// <param name="seasonIndex">Season index to get range for</param>
        /// <returns>SeasonRange struct with timing information</returns>
        public SeasonRange GetSeasonRange(int seasonIndex)
        {
            var ranges = SeasonRanges;
            if (seasonIndex >= 0 && seasonIndex < ranges.Length)
                return ranges[seasonIndex];
            
            return ranges.Length > 0 ? ranges[0] : new SeasonRange();
        }
        

        /// <summary>
        /// Gets a month by its index (0-7)
        /// </summary>
        public Month GetMonth(int monthIndex)
        {
            if (monthIndex < 0 || monthIndex >= Months.Length)
                throw new ArgumentOutOfRangeException(nameof(monthIndex), $"Month index {monthIndex} is out of range (0-{Months.Length - 1})");
            
            return Months[monthIndex];
        }
        
        /// <summary>
        /// Gets the month that contains the specified day of year
        /// </summary>
        /// <param name="dayOfYear">Day of year (1-based to match existing system)</param>
        /// <returns>Month containing the specified day</returns>
        public Month GetMonthForDay(int dayOfYear)
        {
            if (MonthsPerYear == 0) 
                return new Month($"Day {dayOfYear}", 0); // Fallback to day-based naming
            
            // Convert to 0-based for calculation, then back to 1-based
            int zeroBased = dayOfYear - 1;
            zeroBased = Mathf.Clamp(zeroBased, 0, totalDaysInYear - 1);
            
            // Calculate month index
            int monthIndex = zeroBased / daysPerMonth;
            monthIndex = Mathf.Clamp(monthIndex, 0, MonthsPerYear - 1);
            
            return GetMonth(monthIndex);
        }
        
        /// <summary>
        /// Gets the day of month (1-104) for a given day of year
        /// </summary>
        /// <param name="dayOfYear">Day of year (1-based to match existing system)</param>
        /// <returns>Day of month (1-based)</returns>
        public int GetDayOfMonth(int dayOfYear)
        {
            // Convert to 0-based for calculation
            int zeroBased = dayOfYear - 1;
            zeroBased = Mathf.Clamp(zeroBased, 0, totalDaysInYear - 1);
            
            // Calculate day within month (0-based), then convert back to 1-based
            int dayInMonth = zeroBased % daysPerMonth;
            return dayInMonth + 1;
        }
        
        /// <summary>
        /// Formats a date string for display
        /// </summary>
        /// <param name="dayOfYear">Day of year (1-based to match existing system)</param>
        /// <returns>Formatted date string (e.g., "Glavyr 15")</returns>
        public string FormatDate(int dayOfYear)
        {
            if (MonthsPerYear == 0) return $"Day {dayOfYear}"; // Fallback if no months defined
            
            Month month = GetMonthForDay(dayOfYear);
            int dayOfMonth = GetDayOfMonth(dayOfYear);
            return $"{month.name} {dayOfMonth}";
        }
        
        /// <summary>
        /// Formats a full date string with season information
        /// </summary>
        /// <param name="dayOfYear">Day of year (1-based)</param>
        /// <returns>Formatted date string with season</returns>
        public string FormatFullDate(int dayOfYear)
        {
            string dateStr = FormatDate(dayOfYear);
            int seasonIndex = GetSeasonIndexForDay(dayOfYear);
            string seasonName = GetSeasonName(seasonIndex);
            return $"{dateStr} ({seasonName})";
        }

        /// <summary>
        /// Updates a GameTime object with calculated values based on celestial time and current day.
        /// Centralizes game time calculation logic for consistency across the system.
        /// </summary>
        /// <param name="gameTime">GameTime object to update</param>
        /// <param name="celestialTime">Current celestial time (0-1)</param>
        /// <param name="currentDay">Current day of year</param>
        public void UpdateGameTimeFromCelestialTime(GameTime gameTime, float celestialTime, int currentDay)
        {
            // Basic time assignments
            gameTime.dayTime = celestialTime;
            gameTime.currentDay = currentDay;
    
            // Season calculations using flexible system
            int seasonIndex = GetSeasonIndexForDay(currentDay);
            gameTime.currentSeasonIndex = seasonIndex;
            gameTime.currentSeasonName = GetSeasonName(seasonIndex);
    
            var seasonRange = GetSeasonRange(seasonIndex);
            gameTime.seasonProgress = seasonRange.GetProgressForDay(currentDay);

            // Calculate hours, minutes, seconds from celestial time
            float totalSecondsInDay = hoursPerDay * minutesPerHour * secondsPerMinute;
            float currentSecondOfDay = celestialTime * totalSecondsInDay;

            gameTime.hours = Mathf.FloorToInt(currentSecondOfDay / (minutesPerHour * secondsPerMinute));
            int remainingSeconds = Mathf.FloorToInt(currentSecondOfDay % (minutesPerHour * secondsPerMinute));
            gameTime.minutes = Mathf.FloorToInt(remainingSeconds / secondsPerMinute);
            gameTime.seconds = remainingSeconds % secondsPerMinute;

            // Clamp values to valid ranges
            gameTime.hours = Mathf.Clamp(gameTime.hours, 0, hoursPerDay - 1);
            gameTime.minutes = Mathf.Clamp(gameTime.minutes, 0, minutesPerHour - 1);
            gameTime.seconds = Mathf.Clamp(gameTime.seconds, 0, secondsPerMinute - 1);

            // Calculate additional season properties
            gameTime.DaysRemainingInSeason = seasonRange.GetDaysRemainingForDay(currentDay);
            gameTime.TotalDaysInSeason = seasonRange.duration;
        }

        /// <summary>
        /// Converts celestial time to 24-hour display format.
        /// Provides consistent time formatting across the application.
        /// </summary>
        /// <param name="celestialTime">Celestial time value (0-1)</param>
        /// <returns>Time string in HH:MM:SS format</returns>
        public string GetDisplayTime(float celestialTime)
        {
            float totalSecondsInDay = hoursPerDay * minutesPerHour * secondsPerMinute;
            float currentSecondOfDay = celestialTime * totalSecondsInDay;
            int hours = Mathf.FloorToInt(currentSecondOfDay / (minutesPerHour * secondsPerMinute));
            int remainingSeconds = Mathf.FloorToInt(currentSecondOfDay % (minutesPerHour * secondsPerMinute));
            int minutes = Mathf.FloorToInt(remainingSeconds / secondsPerMinute);
            int seconds = remainingSeconds % secondsPerMinute;
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
        
        /// <summary>
        /// Converts celestial time to 12-hour display format with AM/PM.
        /// Alternative time formatting for different UI preferences.
        /// </summary>
        /// <param name="celestialTime">Celestial time value (0-1)</param>
        /// <returns>Time string in HH:MM:SS AM/PM format</returns>
        public string GetDisplayTime12Hour(float celestialTime)
        {
            float totalSecondsInDay = hoursPerDay * minutesPerHour * secondsPerMinute;
            float currentSecondOfDay = celestialTime * totalSecondsInDay;
            int hours = Mathf.FloorToInt(currentSecondOfDay / (minutesPerHour * secondsPerMinute));
            int remainingSeconds = Mathf.FloorToInt(currentSecondOfDay % (minutesPerHour * secondsPerMinute));
            int minutes = Mathf.FloorToInt(remainingSeconds / secondsPerMinute);
            int secs = remainingSeconds % secondsPerMinute;
            int displayHour = hours == 0 ? 12 : (hours > 12 ? hours - 12 : hours);
            string ampm = hours < 12 ? "AM" : "PM";
            return $"{displayHour:D2}:{minutes:D2}:{secs:D2} {ampm}";
        }

        /// <summary>
        /// Initialize the month cache from serialized data
        /// </summary>
        private void InitializeMonthCache()
        {
            if (months.Count == 0) 
            {
                _monthCache = new Month[0]; // Empty array if no months configured
                return;
            }
    
            _monthCache = new Month[MonthsPerYear];
    
            for (int i = 0; i < months.Count; i++)
            {
                // Updated constructor call - remove Season parameter
                _monthCache[i] = new Month(months[i].name, months[i].index);
            }
        }

        #region Unity Lifecycle and Validation

        /// <summary>
        /// Unity callback for inspector value changes. Validates configuration integrity.
        /// </summary>
        private void OnValidate()
        {
            ValidateSeasonConfiguration();
            ValidateTimeSettings();
            ValidateCalendarSettings();
            
            // Reset caches when data changes
            _monthCache = null;
            _seasonRangeCache = null;
        }

        /// <summary>
        /// Unity Awake callback. Ensures season ranges are calculated on load.
        /// </summary>
        private void Awake()
        {
            InitializeMonthCache();
            CalculateSeasonRanges();
        }

        /// <summary>
        /// Calculates season ranges based on configured season durations.
        /// Maintains data consistency and provides computed temporal boundaries.
        /// </summary>
        private void CalculateSeasonRanges()
        {
            if (seasons == null || seasons.Count == 0)
            {
                _seasonRangeCache = new SeasonRange[0];
                return;
            }

            _seasonRangeCache = new SeasonRange[seasons.Count];
            int currentDay = 1;

            for (int i = 0; i < seasons.Count; i++)
            {
                var season = seasons[i];
                _seasonRangeCache[i] = new SeasonRange
                {
                    seasonIndex = i,
                    seasonName = season.seasonName,
                    startDay = currentDay,
                    endDay = currentDay + season.lengthInDays - 1,
                    duration = season.lengthInDays
                    // Remove solSeasonType assignment
                };
                currentDay += season.lengthInDays;
            }
        }

        /// <summary>
        /// Validates season configuration for consistency
        /// </summary>
        private void ValidateSeasonConfiguration()
        {
            if (seasons == null || seasons.Count == 0)
            {
                Debug.LogWarning("[WorldTimeData] No seasons configured! Use 'Create Default Sol Seasons' context menu to set up standard seasons.");
                return;
            }
            
            int totalSeasonDays = GetTotalSeasonDays();
            if (totalSeasonDays != totalDaysInYear)
            {
                Debug.LogWarning($"[WorldTimeData] Season total ({totalSeasonDays}) doesn't match totalDaysInYear ({totalDaysInYear})!");
            }
            
            for (int i = 0; i < seasons.Count; i++)
            {
                var season = seasons[i];
                if (season.lengthInDays <= 0)
                {
                    Debug.LogError($"[WorldTimeData] Season '{season.seasonName}' has invalid length: {season.lengthInDays}");
                }
                
                if (season.seasonalData == null)
                {
                    Debug.LogWarning($"[WorldTimeData] Season '{season.seasonName}' has no SeasonalData assigned!");
                }
                
                if (string.IsNullOrEmpty(season.seasonName))
                {
                    Debug.LogWarning($"[WorldTimeData] Season at index {i} has no name!");
                }
            }
        }

        /// <summary>
        /// Validates that time display settings are reasonable.
        /// Ensures time formatting will work correctly.
        /// </summary>
        private void ValidateTimeSettings()
        {
            if (dayLengthInSeconds <= 0)
            {
                Debug.LogWarning("[WorldTimeData] Day length must be greater than 0!");
            }
            if (hoursPerDay <= 0 || minutesPerHour <= 0 || secondsPerMinute <= 0)
            {
                Debug.LogWarning("[WorldTimeData] Time display settings must be greater than 0!");
            }
        }

        /// <summary>
        /// Validates calendar settings for consistency
        /// </summary>
        private void ValidateCalendarSettings()
        {
            if (months.Count > 0)
            {
                // Check if total days matches months * days per month
                if (totalDaysInYear != MonthsPerYear * daysPerMonth)
                {
                    Debug.LogWarning($"[WorldTimeData] Total days in year ({totalDaysInYear}) doesn't match months * days per month ({MonthsPerYear * daysPerMonth})!");
                }
                
                // Validate month indices
                for (int i = 0; i < months.Count; i++)
                {
                    if (months[i].index != i)
                    {
                        Debug.LogError($"[WorldTimeData] Month {months[i].name} has incorrect index {months[i].index}, expected {i}");
                    }
                    
                    if (string.IsNullOrEmpty(months[i].name))
                    {
                        Debug.LogError($"[WorldTimeData] Month at index {i} has no name");
                    }
                }
            }
        }
        #endregion
    }
}