using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Simplified interface for clock display components
    /// Focuses on basic time and date display from TimeManager
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Updates the clock display with current time information
        /// </summary>
        /// <param name="hours">Current hour (0-23)</param>
        /// <param name="minutes">Current minute (0-59)</param>
        /// <param name="seconds">Current second (0-59)</param>
        /// <param name="monthName">Name of current month</param>
        /// <param name="dayOfMonth">Day within the month (1-104)</param>
        void UpdateDisplay(int hours, int minutes, int seconds, string monthName, int dayOfMonth);

        /// <summary>
        /// Sets whether the clock should automatically update
        /// </summary>
        /// <param name="autoUpdate">True to enable automatic updates</param>
        void SetAutoUpdate(bool autoUpdate);

        /// <summary>
        /// Gets the current auto-update state
        /// </summary>
        bool IsAutoUpdating { get; }
    }
}