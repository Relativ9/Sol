using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Interface for update frequency optimization
    /// Allows different optimization strategies while maintaining dependency inversion
    /// </summary>
    public interface IUpdateFrequencyOptimizer
    {
        /// <summary>
        /// Checks if it's time for a new calculation update
        /// </summary>
        /// <param name="currentTime">Current time (usually Time.time)</param>
        /// <returns>True if calculation should be performed</returns>
        bool ShouldUpdate(float currentTime);

        /// <summary>
        /// Changes the update frequency at runtime
        /// </summary>
        /// <param name="newFrequency">New updates per second</param>
        void SetUpdateFrequency(float newFrequency);

        /// <summary>
        /// Gets the current update frequency
        /// </summary>
        float CurrentFrequency { get; }

        /// <summary>
        /// Resets the optimizer state
        /// </summary>
        void Reset();
    }
}
