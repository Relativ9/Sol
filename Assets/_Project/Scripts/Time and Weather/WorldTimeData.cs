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

        [Header("Season Durations")]
        [Tooltip("Number of days in Polar Summer season (~37.5% of year)")]
        public int polarSummerDays = 312;
        
        [Tooltip("Number of days in Fall season (~12.5% of year)")]
        public int fallDays = 104;
        
        [Tooltip("Number of days in Long Night season (~37.5% of year)")]
        public int longNightDays = 312;
        
        [Tooltip("Number of days in Spring season (~12.5% of year)")]
        public int springDays = 104;

        [Header("Season Transition Settings")]
        [Tooltip("Number of days over which celestial bodies smoothly transition between seasons (centered on season boundary)")]
        [Range(1, 50)]
        public int seasonTransitionDays = 20;

        [Header("Seasonal Data References")]
        [Tooltip("SeasonalData asset containing celestial configuration for Polar Summer")]
        public SeasonalData polarSummerData;
        
        [Tooltip("SeasonalData asset containing celestial configuration for Fall")]
        public SeasonalData fallData;
        
        [Tooltip("SeasonalData asset containing celestial configuration for Long Night")]
        public SeasonalData longNightData;
        
        [Tooltip("SeasonalData asset containing celestial configuration for Spring")]
        public SeasonalData springData;

        [Header("Calendar System")]
        [Tooltip("Number of days in each month (should divide evenly into season lengths)")]
        public int daysPerMonth = 104;
        
        [Tooltip("Month definitions for the Sol calendar")]
        [SerializeField] private List<MonthDefinition> months = new List<MonthDefinition>();

        [Header("Calculated Season Ranges (Read Only)")]
        [SerializeField] private SeasonRange[] seasonRanges = new SeasonRange[4];

        /// <summary>
        /// Serializable month definition for inspector
        /// </summary>
        [Serializable]
        public class MonthDefinition
        {
            [Tooltip("Name of the month")]
            public string name;
            
            [Tooltip("Season this month belongs to")]
            public Season season;
            
            [Tooltip("Month index (0-7)")]
            [Range(0, 7)]
            public int index;
        }

        /// <summary>
        /// Represents a season's time range within the year with utility methods for progress calculation.
        /// Encapsulates season-specific data and provides methods for temporal calculations.
        /// </summary>
        [System.Serializable]
        public struct SeasonRange
        {
            public Season season;
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

        // Cached month objects for performance
        private Month[] _monthCache;

        // Calculated properties for time conversion
        public float SecondsPerGameHour => dayLengthInSeconds / hoursPerDay;
        public float SecondsPerGameMinute => SecondsPerGameHour / minutesPerHour;
        public float SecondsPerGameSecond => SecondsPerGameMinute / secondsPerMinute;
        public SeasonRange[] SeasonRanges => seasonRanges;

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
        /// Gets the SeasonalData asset for the specified season.
        /// Provides abstraction over the internal seasonal data storage.
        /// </summary>
        /// <param name="season">Season to get data for</param>
        /// <returns>SeasonalData asset or null if not assigned</returns>
        public SeasonalData GetSeasonalData(Season season)
        {
            return season switch
            {
                Season.PolarSummer => polarSummerData,
                Season.Fall => fallData,
                Season.LongNight => longNightData,
                Season.Spring => springData,
                _ => null
            };
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
        /// <param name="dayOfYear">Day of year (1-based to match existing system)</param>
        /// <returns>Formatted date string with season (e.g., "Glavyr 15 (Spring)")</returns>
        public string FormatFullDate(int dayOfYear)
        {
            if (MonthsPerYear == 0) 
            {
                Season season = GetSeasonForDay(dayOfYear);
                return $"Day {dayOfYear} ({season})";
            }
            
            Month month = GetMonthForDay(dayOfYear);
            int dayOfMonth = GetDayOfMonth(dayOfYear);
            return $"{month.name} {dayOfMonth} ({month.season})";
        }

        /// <summary>
        /// Validates that all seasonal data references are properly assigned.
        /// Supports configuration validation and error prevention.
        /// </summary>
        /// <returns>True if all seasonal data assets are assigned</returns>
        public bool ValidateSeasonalDataReferences()
        {
            bool isValid = true;
            
            if (polarSummerData == null)
            {
                Debug.LogWarning($"[WorldTimeData] Polar Summer SeasonalData is not assigned!");
                isValid = false;
            }
            
            if (fallData == null)
            {
                Debug.LogWarning($"[WorldTimeData] Fall SeasonalData is not assigned!");
                isValid = false;
            }
            
            if (longNightData == null)
            {
                Debug.LogWarning($"[WorldTimeData] Long Night SeasonalData is not assigned!");
                isValid = false;
            }
            
            if (springData == null)
            {
                Debug.LogWarning($"[WorldTimeData] Spring SeasonalData is not assigned!");
                isValid = false;
            }
            
            return isValid;
        }

        /// <summary>
        /// Determines which season contains the specified day of year.
        /// Handles year wraparound for calculations beyond year boundaries.
        /// </summary>
        /// <param name="dayOfYear">Day number (1-based, wraps around for values beyond year length)</param>
        /// <returns>Season that contains the specified day</returns>
        public Season GetSeasonForDay(int dayOfYear)
        {
            // Handle wrap-around for days beyond year length
            int normalizedDay = ((dayOfYear - 1) % totalDaysInYear) + 1;
            foreach (var range in seasonRanges)
            {
                if (range.ContainsDay(normalizedDay))
                {
                    return range.season;
                }
            }
            return Season.PolarSummer; // Fallback
        }

        /// <summary>
        /// Gets the season range data for the specified season.
        /// Provides access to temporal boundaries and duration information.
        /// </summary>
        /// <param name="season">Season to get range for</param>
        /// <returns>SeasonRange struct with timing information</returns>
        public SeasonRange GetSeasonRange(Season season)
        {
            foreach (var range in seasonRanges)
            {
                if (range.season == season)
                {
                    return range;
                }
            }
            return seasonRanges[0]; // Fallback to polar summer
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
            // Existing assignments
            gameTime.dayTime = celestialTime;
            gameTime.currentDay = currentDay;
            gameTime.currentSeason = GetSeasonForDay(currentDay);
            var seasonRange = GetSeasonRange(gameTime.currentSeason);
            gameTime.seasonProgress = seasonRange.GetProgressForDay(currentDay);

            // Calculate hours, minutes, seconds from celestial time
            float totalSecondsInDay = hoursPerDay * minutesPerHour * secondsPerMinute; // 86400 for 24:60:60
            float currentSecondOfDay = celestialTime * totalSecondsInDay;

            gameTime.hours = Mathf.FloorToInt(currentSecondOfDay / (minutesPerHour * secondsPerMinute)); // seconds per hour
            int remainingSeconds = Mathf.FloorToInt(currentSecondOfDay % (minutesPerHour * secondsPerMinute));
            gameTime.minutes = Mathf.FloorToInt(remainingSeconds / secondsPerMinute);
            gameTime.seconds = remainingSeconds % secondsPerMinute;

            // Clamp values to valid ranges (safety check)
            gameTime.hours = Mathf.Clamp(gameTime.hours, 0, hoursPerDay - 1);
            gameTime.minutes = Mathf.Clamp(gameTime.minutes, 0, minutesPerHour - 1);
            gameTime.seconds = Mathf.Clamp(gameTime.seconds, 0, secondsPerMinute - 1);
            
            // gameTime.dayTime = celestialTime;
            // gameTime.currentDay = currentDay;
            // gameTime.currentSeason = GetSeasonForDay(currentDay);
            // var seasonRange = GetSeasonRange(gameTime.currentSeason);
            // gameTime.seasonProgress = seasonRange.GetProgressForDay(currentDay);
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
            if (months.Count == 0) return;
            
            _monthCache = new Month[MonthsPerYear];
            
            for (int i = 0; i < months.Count; i++)
            {
                _monthCache[i] = new Month(months[i].name, months[i].season, months[i].index);
            }
        }

        #region Unity Lifecycle and Validation

        /// <summary>
        /// Unity callback for inspector value changes. Validates configuration integrity.
        /// </summary>
        private void OnValidate()
        {
            CalculateSeasonRanges();
            ValidateTotal();
            ValidateTimeSettings();
            ValidateSeasonalDataReferences();
            ValidateCalendarSettings();
            
            // Reset month cache when data changes
            _monthCache = null;
            
            // Auto-populate months if empty and we have the expected number of days
            if (months.Count == 0 && totalDaysInYear == 832)
            {
                PopulateDefaultMonths();
            }
        }

        /// <summary>
        /// Unity Awake callback. Ensures season ranges are calculated on load.
        /// </summary>
        private void Awake()
        {
            CalculateSeasonRanges();
            InitializeMonthCache();
        }

        /// <summary>
        /// Calculates season ranges based on configured season durations.
        /// Maintains data consistency and provides computed temporal boundaries.
        /// </summary>
        private void CalculateSeasonRanges()
        {
            seasonRanges = new SeasonRange[4];
            int currentDay = 1;

            // Spring (starts at day 1)
            seasonRanges[3] = new SeasonRange
            {
                season = Season.Spring,
                startDay = currentDay,
                endDay = currentDay + springDays - 1,
                duration = springDays
            };
            currentDay += springDays;

            // Polar Summer
            seasonRanges[0] = new SeasonRange
            {
                season = Season.PolarSummer,
                startDay = currentDay,
                endDay = currentDay + polarSummerDays - 1,
                duration = polarSummerDays
            };
            currentDay += polarSummerDays;

            // Fall
            seasonRanges[1] = new SeasonRange
            {
                season = Season.Fall,
                startDay = currentDay,
                endDay = currentDay + fallDays - 1,
                duration = fallDays
            };
            currentDay += fallDays;

            // Long Night
            seasonRanges[2] = new SeasonRange
            {
                season = Season.LongNight,
                startDay = currentDay,
                endDay = currentDay + longNightDays - 1,
                duration = longNightDays
            };
        }

        /// <summary>
        /// Validates that season durations add up to total year length.
        /// Prevents configuration errors that could break time calculations.
        /// </summary>
        private void ValidateTotal()
        {
            int total = polarSummerDays + fallDays + longNightDays + springDays;
            if (total != totalDaysInYear)
            {
                Debug.LogWarning($"[WorldTimeData] Season days ({total}) don't add up to total year days ({totalDaysInYear})!");
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

        /// <summary>
        /// Populate with default Sol calendar months
        /// </summary>
        private void PopulateDefaultMonths()
        {
            months.Clear();
            
            // Spring months (days 1-208)
            months.Add(new MonthDefinition { name = "Glavyr", season = Season.Spring, index = 0 });
            months.Add(new MonthDefinition { name = "Levorn", season = Season.Spring, index = 1 });
            
            // PolarSummer months (days 209-416)
            months.Add(new MonthDefinition { name = "Skjorn", season = Season.PolarSummer, index = 2 });
            months.Add(new MonthDefinition { name = "Glausk", season = Season.PolarSummer, index = 3 });
            
            // Fall months (days 417-624)
            months.Add(new MonthDefinition { name = "Farnok", season = Season.Fall, index = 4 });
            months.Add(new MonthDefinition { name = "Tvarn", season = Season.Fall, index = 5 });
            
            // LongNight months (days 625-832)
            months.Add(new MonthDefinition { name = "Nurlith", season = Season.LongNight, index = 6 });
            months.Add(new MonthDefinition { name = "Thrukn", season = Season.LongNight, index = 7 });
        }

        #endregion
    }
}

