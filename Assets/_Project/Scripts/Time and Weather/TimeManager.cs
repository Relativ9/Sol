using UnityEngine;

namespace Sol
{
    public class TimeManager : MonoBehaviour, ITimeManager
    {
        [Header("Configuration")]
        [SerializeField] private WorldTimeData _worldTimeData;

        [Header("Starting Values")]
        [Tooltip("Starting time of day (0-1, where 0 = midnight, 0.5 = noon)")]
        [SerializeField] private float _startingTimeOfDay = 0.25f;
        
        [Tooltip("Starting day of year (0-based, must be less than totalDaysInYear)")]
        [SerializeField] private int _startingDay = 1;
        
        [Tooltip("Starting year (can be any value: 0 for creation myths, 2025 for modern, etc.)")]
        [SerializeField] private int _startingYear = 1;

        [Header("Time Control")]
        [Tooltip("Time scale multiplier (1 = normal, 2 = double speed, 0.5 = half speed)")]
        [SerializeField] private float _celestialTimeScale = 1f;

        private float _normalTimeScale = 1f;
        private float _cachedTimeScale;
        private float _cachedCelestialTimeScale;
        
        [Tooltip("Pause time progression")]
        [SerializeField] private bool _isPaused = false;

        [Header("Events")]
        [Tooltip("Raised when a new day begins")]
        [SerializeField] private GameEvent _onDayChanged;
        
        [Tooltip("Raised when season changes (after transition completes)")]
        [SerializeField] private GameEvent _onSeasonChanged;
        
        [Tooltip("Raised when a new year begins")]
        [SerializeField] private GameEvent _onYearChanged;

        [Header("Debug")]
        [Tooltip("Enable detailed logging for time system debugging")]
        [SerializeField] private bool _enableDebugLogging = false;

        // Current time state
        private float _celestialTime;
        private int _currentDay;
        private int _currentYear;
        
        // Season state
        private int _currentSeasonIndex;
        private int _targetSeasonIndex;
        private bool _isInSeasonTransition;
        private float _seasonTransitionProgress;

        // Event tracking (prevents duplicate event raises)
        private int _lastDayRaised = -1;
        private int _lastSeasonRaised = -1;
        private int _lastYearRaised = -1;
        
        #region Properties

        public WorldTimeData WorldTimeData => _worldTimeData;
        public float CelestialTime => _celestialTime;
        public int CurrentDay => _currentDay;
        public int CurrentYear => _currentYear;
        public float CelestialTimeScale => _celestialTimeScale;
        public bool IsPaused => _isPaused;
        public int CurrentSeasonIndex => _currentSeasonIndex;
        public string CurrentSeasonName => _worldTimeData?.GetSeasonName(_currentSeasonIndex) ?? "Unknown";
        public int TargetSeasonIndex => _targetSeasonIndex;
        public string TargetSeasonName => _worldTimeData?.GetSeasonName(_targetSeasonIndex) ?? "Unknown";
        public bool IsInSeasonTransition => _isInSeasonTransition;
        public float SeasonTransitionProgress => _seasonTransitionProgress;
        
        public int DaysInCurrentSeason
        {
            get
            {
                if (_worldTimeData == null) return 0;
                var range = _worldTimeData.GetSeasonRange(_currentSeasonIndex);
                return range.duration;
            }
        }

        public float SeasonProgress
        {
            get
            {
                if (_worldTimeData == null) return 0f;
                var range = _worldTimeData.GetSeasonRange(_currentSeasonIndex);
                return range.GetProgressForDay(_currentDay);
            }
        }

        public TimeOfDay CurrentTimeOfDay => DetermineTimeOfDay(_celestialTime);
        
        public float YearProgress => _worldTimeData != null ? (float)_currentDay / _worldTimeData.totalDaysInYear : 0f;
        
        public Month CurrentMonth => _worldTimeData?.GetMonthForDay(_currentDay) ?? default;
        
        public int CurrentDayOfMonth => _worldTimeData?.GetDayOfMonth(_currentDay) ?? 1;
        
        public string CurrentTimeDisplay => _worldTimeData?.GetDisplayTime(_celestialTime) ?? "00:00:00";
        
        public string CurrentTimeDisplay12Hour => _worldTimeData?.GetDisplayTime12Hour(_celestialTime) ?? "12:00:00 AM";
        
        public string CurrentDateDisplay => GetFormattedDate();
        
        public GameTime CurrentGameTime => GetCurrentGameTime();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.RegisterService<ITimeManager>(this); //Registers itself with the service locator.
            
            var calculator = new CelestialCalculator(this);
            ServiceLocator.RegisterService<ICelestialCalculator>(calculator);
        }

        private void Start()
        {
            ValidateConfiguration();
            InitializeTime();
        }

        private void Update()
        {
            if (_isPaused || _worldTimeData == null) return;

            UpdateCelestialTime();
            UpdateCalendar();
            UpdateSeasons();
            RaiseTimeEvents();
        }

        #endregion

        #region Time Updates

        private void UpdateCelestialTime()
        {
            float dayLengthInSeconds = _worldTimeData.TotalGameSecondsPerDay;
            if (dayLengthInSeconds <= 0)
            {
                Debug.LogError("[TimeManager] Day length must be greater than 0");
                return;
            }

            // Calculate time increment (scaled by time scale)
            float timeIncrement = (Time.deltaTime * _celestialTimeScale) / dayLengthInSeconds;
            _celestialTime += timeIncrement;

            // Handle day transition
            if (_celestialTime >= 1f)
            {
                _celestialTime -= 1f;
                _currentDay++;

                if (_enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] New day: {_currentDay}, Year: {_currentYear}");
                }
            }
        }

        private void UpdateCalendar()
        {
            int totalDaysInYear = _worldTimeData.totalDaysInYear;
            if (_currentDay >= totalDaysInYear)
            {
                _currentDay = 0; // Reset to day 0 of new year
                _currentYear++;

                if (_enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] New year: {_currentYear}");
                }
            }
        }

        private void UpdateSeasons()
        {
            int newSeasonIndex = _worldTimeData.GetSeasonIndexForDay(_currentDay);

            // Check for season change
            if (newSeasonIndex != _currentSeasonIndex && !_isInSeasonTransition)
            {
                _targetSeasonIndex = newSeasonIndex;
                _isInSeasonTransition = true;
                _seasonTransitionProgress = 0f;

                if (_enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] Season transition started: {CurrentSeasonName} -> {TargetSeasonName}");
                }
            }

            // Update transition progress
            if (_isInSeasonTransition)
            {
                UpdateSeasonTransition();
            }
        }

        private void UpdateSeasonTransition()
        {
            if (_worldTimeData.seasonTransitionDays <= 0)
            {
                CompleteSeasonTransition();
                return;
            }

            float transitionDaysElapsed = _seasonTransitionProgress * _worldTimeData.seasonTransitionDays;
            float dayIncrement = Time.deltaTime * _celestialTimeScale / _worldTimeData.TotalGameSecondsPerDay;
            transitionDaysElapsed += dayIncrement;
            _seasonTransitionProgress = transitionDaysElapsed / _worldTimeData.seasonTransitionDays;

            if (_seasonTransitionProgress >= 1f)
            {
                CompleteSeasonTransition();
            }
        }

        private void CompleteSeasonTransition()
        {
            _currentSeasonIndex = _targetSeasonIndex;
            _isInSeasonTransition = false;
            _seasonTransitionProgress = 1f;

            if (_enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Season transition completed: {CurrentSeasonName}");
            }
        }

        #endregion

        #region Event Raising

        private void RaiseTimeEvents()
        {
            // Raise day changed event
            if (_currentDay != _lastDayRaised)
            {
                _onDayChanged?.Raise();
                _lastDayRaised = _currentDay;

                if (_enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] Raised OnDayChanged: Day {_currentDay}, Year {_currentYear}");
                }
            }

            // Raise season changed event
            if (_currentSeasonIndex != _lastSeasonRaised && !_isInSeasonTransition)
            {
                _onSeasonChanged?.Raise();
                _lastSeasonRaised = _currentSeasonIndex;

                if (_enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] Raised OnSeasonChanged: {CurrentSeasonName}");
                }
            }

            // Raise year changed event
            if (_currentYear != _lastYearRaised)
            {
                _onYearChanged?.Raise();
                _lastYearRaised = _currentYear;

                if (_enableDebugLogging)
                {
                    Debug.Log($"[TimeManager] Raised OnYearChanged: Year {_currentYear}");
                }
            }
        }

        #endregion

        #region Initialization & Validation

        private void InitializeTime()
        {
            if (_worldTimeData == null)
            {
                Debug.LogError("[TimeManager] WorldTimeData not assigned!");
                return;
            }

            _celestialTime = _startingTimeOfDay;
            _currentDay = Mathf.Clamp(_startingDay, 0, _worldTimeData.totalDaysInYear - 1);
            _currentYear = _startingYear;
            _currentSeasonIndex = _worldTimeData.GetSeasonIndexForDay(_currentDay);
            _targetSeasonIndex = _currentSeasonIndex;

            _lastDayRaised = _currentDay;
            _lastSeasonRaised = _currentSeasonIndex;
            _lastYearRaised = _currentYear;

            if (_enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Initialized - Day: {_currentDay}, Year: {_currentYear}, Season: {CurrentSeasonName}");
            }
        }

        private void ValidateConfiguration()
        {
            if (_worldTimeData == null)
            {
                Debug.LogError("[TimeManager] WorldTimeData is not assigned!");
                return;
            }

            if (_worldTimeData.TotalGameSecondsPerDay <= 0)
            {
                Debug.LogError("[TimeManager] Day length must be greater than 0!");
            }

            if (_worldTimeData.totalDaysInYear <= 0)
            {
                Debug.LogError("[TimeManager] Total days in year must be greater than 0!");
            }

            if (_worldTimeData.GetSeasonCount() == 0)
            {
                Debug.LogWarning("[TimeManager] No seasons configured in WorldTimeData!");
            }

            if (_startingDay < 0 || _startingDay >= _worldTimeData.totalDaysInYear)
            {
                Debug.LogWarning($"[TimeManager] Starting day {_startingDay} is out of range (0-{_worldTimeData.totalDaysInYear - 1}). Clamping to valid range.");
            }
        }

        #endregion

        #region Time Control Methods

        public void SetCelestialTimeScale(float newTimeScale)
        {
            _celestialTimeScale = Mathf.Max(0f, newTimeScale);
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Time scale set to {_celestialTimeScale}x");
            }
        }

        public void PauseTime()
        {
            if (_isPaused) return;
            _isPaused = true;
            
            _cachedTimeScale = Time.timeScale;
            _cachedCelestialTimeScale = _celestialTimeScale;

            Time.timeScale = 0f;
            SetCelestialTimeScale(0f);
            if (_enableDebugLogging)
            {
                Debug.Log("[TimeManager] Time paused");
            }
        }

        public void ResumeTime()
        {
            _isPaused = false;

            Time.timeScale = _cachedTimeScale;
            SetCelestialTimeScale(_cachedCelestialTimeScale);
            
            if (_enableDebugLogging)
            {
                Debug.Log("[TimeManager] Time resumed");
            }
        }

        public void TogglePause()
        {
            if (_isPaused)
                ResumeTime();
            else
                PauseTime();
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Time {(_isPaused ? "paused" : "resumed")}");
            }
        }

        public void SetCelestialTime(float newCelestialTime)
        {
            _celestialTime = Mathf.Clamp01(newCelestialTime);
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Celestial time set to {_celestialTime:F3}");
            }
        }

        public void SetCurrentDay(int newDay)
        {
            if (_worldTimeData == null) return;
            
            _currentDay = Mathf.Clamp(newDay, 0, _worldTimeData.totalDaysInYear - 1);
            _currentSeasonIndex = _worldTimeData.GetSeasonIndexForDay(_currentDay);
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Day set to {_currentDay}, Season: {CurrentSeasonName}");
            }
        }

        public void SetCurrentYear(int newYear)
        {
            _currentYear = newYear;
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Year set to {_currentYear}");
            }
        }

        public void AdvanceDays(int days)
        {
            _currentDay += days;
            UpdateCalendar();
            UpdateSeasons();
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Advanced {days} days. Now: Day {_currentDay}, Year {_currentYear}");
            }
        }

        public void AdvanceYears(int years)
        {
            _currentYear += years;
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[TimeManager] Advanced {years} years. Now: Year {_currentYear}");
            }
        }

        #endregion

        #region Seasonal Data Access

        public SeasonalData GetCurrentSeasonalData()
        {
            return GetSeasonalData(_currentSeasonIndex);
        }

        public SeasonalData GetSeasonalData(int seasonIndex)
        {
            return _worldTimeData?.GetSeasonalData(seasonIndex);
        }

        public int GetSeasonIndexForDay(int dayOfYear)
        {
            return _worldTimeData?.GetSeasonIndexForDay(dayOfYear) ?? 0;
        }

        public WorldTimeData.SeasonRange GetSeasonRange(int seasonIndex)
        {
            return _worldTimeData?.GetSeasonRange(seasonIndex) ?? default;
        }

        public string GetSeasonName(int seasonIndex)
        {
            return _worldTimeData?.GetSeasonName(seasonIndex) ?? "Unknown";
        }

        #endregion

        #region Helper Methods

        private TimeOfDay DetermineTimeOfDay(float celestialTime)
        {
            if (celestialTime >= 0.0f && celestialTime < 0.2f) return TimeOfDay.EarlyMorning;
            if (celestialTime >= 0.2f && celestialTime < 0.4f) return TimeOfDay.Morning;
            if (celestialTime >= 0.4f && celestialTime < 0.6f) return TimeOfDay.Midday;
            if (celestialTime >= 0.6f && celestialTime < 0.8f) return TimeOfDay.Afternoon;
            return TimeOfDay.Evening; // 0.8 - 1.0
        }

        public string GetFormattedDate()
        {
            return _worldTimeData?.FormatDate(_currentDay) ?? $"Day {_currentDay}";
        }

        public string GetFormattedFullDate()
        {
            if (_worldTimeData == null)
                return $"Day {_currentDay}, Year {_currentYear}";
            
            string dateStr = _worldTimeData.FormatDate(_currentDay);
            string seasonName = CurrentSeasonName;
            return $"{dateStr}, Year {_currentYear} ({seasonName})";
        }

        public GameTime GetCurrentGameTime()
        {
            var gameTime = new GameTime();
            
            if (_worldTimeData != null)
            {
                _worldTimeData.UpdateGameTimeFromCelestialTime(gameTime, _celestialTime, _currentDay);
            }
            
            // Add year information
            gameTime.currentYear = _currentYear;
            
            // Add transition information if applicable
            if (_isInSeasonTransition)
            {
                gameTime.nextSeasonIndex = _targetSeasonIndex;
                gameTime.nextSeasonName = TargetSeasonName;
                gameTime.seasonTransition = _seasonTransitionProgress;
            }
            else
            {
                gameTime.nextSeasonIndex = -1;
                gameTime.nextSeasonName = string.Empty;
                gameTime.seasonTransition = 0f;
            }
            
            return gameTime;
        }

        public void ValidateTimeManagerState()
        {
            ValidateConfiguration();
        }

        #endregion
    }
}
