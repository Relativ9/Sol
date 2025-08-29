// using UnityEngine;
//
// namespace Sol
// {
//     /// <summary>
//     /// Interface for weather management systems
//     /// Provides access to weather state, transitions, and weather control
//     /// Integrates with calendar system for time-based weather patterns
//     /// </summary>
//     public interface IWeatherManager
//     {
//         // Weather State Properties
//         /// <summary>
//         /// Whether the weather system is currently enabled
//         /// </summary>
//         bool WeatherSystemEnabled { get; }
//
//         /// <summary>
//         /// Current active weather state
//         /// </summary>
//         WeatherState CurrentWeatherState { get; }
//
//         /// <summary>
//         /// Target weather state during transitions
//         /// </summary>
//         WeatherState TargetWeatherState { get; }
//
//         /// <summary>
//         /// Whether a weather transition is currently in progress
//         /// </summary>
//         bool IsTransitioning { get; }
//
//         /// <summary>
//         /// Progress of current weather transition (0-1, where 1 = complete)
//         /// </summary>
//         float WeatherTransitionProgress { get; }
//
//         /// <summary>
//         /// Whether it is currently snowing (includes transitioning to snow)
//         /// </summary>
//         bool IsSnowing { get; }
//
//         /// <summary>
//         /// How long the current weather state has been active (in seconds)
//         /// </summary>
//         float CurrentWeatherDuration { get; }
//
//         /// <summary>
//         /// How much time remains in the current weather period (in seconds)
//         /// </summary>
//         float RemainingWeatherDuration { get; }
//
//         // Events
//         /// <summary>
//         /// Event fired when weather state changes completely
//         /// </summary>
//         System.Action<WeatherState> OnWeatherChanged { get; set; }
//
//         /// <summary>
//         /// Event fired during weather transitions with progress updates
//         /// Parameters: fromState, toState, progress (0-1)
//         /// </summary>
//         System.Action<WeatherState, WeatherState, float> OnWeatherTransitionUpdate { get; set; }
//
//         /// <summary>
//         /// Event fired when snowing state changes (useful for particle systems, audio, etc.)
//         /// Parameters: isSnowing
//         /// </summary>
//         System.Action<bool> OnSnowingChanged { get; set; }
//
//         // Weather Control Methods
//         /// <summary>
//         /// Enables or disables the weather system
//         /// </summary>
//         /// <param name="enabled">Whether to enable the weather system</param>
//         void SetWeatherSystemEnabled(bool enabled);
//
//         /// <summary>
//         /// Sets the weather state manually
//         /// </summary>
//         /// <param name="state">Target weather state</param>
//         /// <param name="immediate">If true, changes immediately without transition</param>
//         void SetWeatherState(WeatherState state, bool immediate = false);
//
//         /// <summary>
//         /// Forces an immediate weather evaluation check
//         /// </summary>
//         void ForceWeatherCheck();
//         
//         WeatherState CurrentState { get; }
//         void ForceWeatherState(WeatherState state, float durationHours);
//     }
// }