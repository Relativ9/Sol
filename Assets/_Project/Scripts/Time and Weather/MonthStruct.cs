// using System;
//
// namespace Sol
// {
//     /// <summary>
//     /// Represents a month in the Sol calendar system
//     /// Each month contains 104 days and belongs to a specific season
//     /// </summary>
//     [Serializable]
//     public struct Month
//     {
//         public string name;
//         public Season season;
//         public int monthIndex; // 0-7 for the 8 months
//         
//         public Month(string name, Season season, int monthIndex)
//         {
//             this.name = name;
//             this.season = season;
//             this.monthIndex = monthIndex;
//         }
//         
//         /// <summary>
//         /// Gets the starting day of year for this month (0-based)
//         /// </summary>
//         public int StartDayOfYear => monthIndex * 104;
//         
//         /// <summary>
//         /// Gets the ending day of year for this month (0-based, inclusive)
//         /// </summary>
//         public int EndDayOfYear => (monthIndex + 1) * 104 - 1;
//         
//         /// <summary>
//         /// Checks if a given day of year falls within this month
//         /// </summary>
//         /// <param name="dayOfYear">Day of year (0-based)</param>
//         /// <returns>True if the day falls within this month</returns>
//         public bool ContainsDay(int dayOfYear)
//         {
//             return dayOfYear >= StartDayOfYear && dayOfYear <= EndDayOfYear;
//         }
//         
//         /// <summary>
//         /// Converts a day of year to day of month for this month
//         /// </summary>
//         /// <param name="dayOfYear">Day of year (0-based)</param>
//         /// <returns>Day of month (1-based, 1-104)</returns>
//         public int GetDayOfMonth(int dayOfYear)
//         {
//             if (!ContainsDay(dayOfYear))
//                 throw new ArgumentOutOfRangeException(nameof(dayOfYear), $"Day {dayOfYear} is not in month {name}");
//             
//             return (dayOfYear - StartDayOfYear) + 1; // Convert to 1-based
//         }
//     }
// }

using System;

namespace Sol
{
    /// <summary>
    /// Represents a month in the calendar system
    /// Each month contains a configurable number of days
    /// </summary>
    [Serializable]
    public struct Month
    {
        public string name;
        public int monthIndex; // 0-based index
        public int daysPerMonth; // Configurable days per month
        
        public Month(string name, int monthIndex, int daysPerMonth = 104)
        {
            this.name = name;
            this.monthIndex = monthIndex;
            this.daysPerMonth = daysPerMonth;
        }
        
        /// <summary>
        /// Gets the starting day of year for this month (0-based)
        /// </summary>
        public int StartDayOfYear => monthIndex * daysPerMonth;
        
        /// <summary>
        /// Gets the ending day of year for this month (0-based, inclusive)
        /// </summary>
        public int EndDayOfYear => (monthIndex + 1) * daysPerMonth - 1;
        
        /// <summary>
        /// Checks if a given day of year falls within this month
        /// </summary>
        /// <param name="dayOfYear">Day of year (0-based)</param>
        /// <returns>True if the day falls within this month</returns>
        public bool ContainsDay(int dayOfYear)
        {
            return dayOfYear >= StartDayOfYear && dayOfYear <= EndDayOfYear;
        }
        
        /// <summary>
        /// Converts a day of year to day of month for this month
        /// </summary>
        /// <param name="dayOfYear">Day of year (0-based)</param>
        /// <returns>Day of month (1-based)</returns>
        public int GetDayOfMonth(int dayOfYear)
        {
            if (!ContainsDay(dayOfYear))
                throw new ArgumentOutOfRangeException(nameof(dayOfYear), $"Day {dayOfYear} is not in month {name}");
            
            return (dayOfYear - StartDayOfYear) + 1; // Convert to 1-based
        }
    }
}