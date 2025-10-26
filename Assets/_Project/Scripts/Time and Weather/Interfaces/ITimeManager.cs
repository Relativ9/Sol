namespace Sol
{
    /// <summary>
    /// Interface for time management systems in Sol.
    /// Provides access to planetary time, seasons, calendar, and celestial cycles.
    /// Time-dependent systems should poll these properties as needed.
    /// Major state changes (day/season/year) are broadcast via GameEvent assets.
    /// </summary>
    public interface ITimeManager
    {
        #region Configuration

        /// <summary>
        /// World time configuration data containing day length, seasons, and calendar settings
        /// </summary>
        WorldTimeData WorldTimeData { get; }

        #endregion

        #region Core Time Properties
        
        float CelestialTime { get; }
        int CurrentDay { get; }
        int CurrentYear { get; }
        float TimeScale { get; }
        bool IsPaused { get; }
        
        #endregion

        #region Season Properties
        
        int CurrentSeasonIndex { get; }
        string CurrentSeasonName { get; }
        int TargetSeasonIndex { get; }
        string TargetSeasonName { get; }
        bool IsInSeasonTransition { get; }
        float SeasonTransitionProgress { get; }
        int DaysInCurrentSeason { get; }
        float SeasonProgress { get; }
        
        #endregion

        #region Time of Day Properties
        
        TimeOfDay CurrentTimeOfDay { get; }
        float YearProgress { get; }
        
        #endregion

        #region Calendar Properties
        
        Month CurrentMonth { get; }
        int CurrentDayOfMonth { get; }
        
        #endregion

        #region Display Properties
        
        string CurrentTimeDisplay { get; }
        string CurrentTimeDisplay12Hour { get; }
        string CurrentDateDisplay { get; }
        
        #endregion

        #region Game Time Access
        
        GameTime CurrentGameTime { get; }
        GameTime GetCurrentGameTime();
        
        #endregion

        #region Time Control Methods
        
        void SetTimeScale(float newTimeScale);
        void PauseTime();
        void ResumeTime();
        void TogglePause();
        void SetCelestialTime(float newCelestialTime);
        void SetCurrentDay(int newDay);
        void SetCurrentYear(int newYear);
        void AdvanceDays(int days);
        void AdvanceYears(int years);
        
        #endregion

        #region Seasonal Data Access
        
        SeasonalData GetCurrentSeasonalData();
        SeasonalData GetSeasonalData(int seasonIndex);
        int GetSeasonIndexForDay(int dayOfYear);
        WorldTimeData.SeasonRange GetSeasonRange(int seasonIndex);
        string GetSeasonName(int seasonIndex);
        
        #endregion

        #region Date Formatting
        
        string GetFormattedDate();
        string GetFormattedFullDate();
        
        #endregion

        #region Validation
        
        void ValidateTimeManagerState();
        
        #endregion
    }
}
