using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Manages celestial body rotation based on seasonal data and time progression.
    /// Handles smooth transitions between seasons with performance optimization.
    /// Supports both stars and moons with orbital period calculations.
    /// </summary>
    [System.Serializable]
    public class CelestialRotator : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Core Configuration")]
        [Tooltip("Time manager reference for accessing temporal data")]
        [SerializeField] private TimeManager timeManager;

        [Tooltip("Transform to rotate (if null, uses this GameObject's transform)")]
        [SerializeField] private Transform celestialBodyTransform;

        [Tooltip("Name identifier for this celestial body (Sol, Notr, Iskarn, Rynth, Veldris)")]
        [SerializeField] private string celestialBodyName = "Sol";

        [Header("Celestial Body Type")]
        [Tooltip("Is this celestial body a moon? (enables orbital period drift calculations)")]
        [SerializeField] private bool isMoon = false;

        [Header("Rotation Settings")]
        [Tooltip("Base rotation offset applied before calculations")]
        [SerializeField] private Vector3 baseRotationOffset = Vector3.zero;

        [Header("Performance")]
        [Tooltip("Enable performance optimization for rotation updates")]
        [SerializeField] private bool useOptimization = true;

        [Tooltip("Update frequency in Hz (updates per second)")]
        [SerializeField] private float updateFrequency = 20f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        [SerializeField] private bool enableDebugLogging = false;

        #endregion

        #region Private Fields

        private ICelestialCalculator _calculator;
        private IUpdateFrequencyOptimizer _optimizer;
        private Transform _targetTransform;
        private Vector3 _currentRotation;
        private Vector3 _targetRotation;
        private Season _lastSeason;
        private bool _isInitialized;
        private float _lastUpdateTime;
        
        private Vector3 _originalBaseRotation;
        private bool _hasStoredOriginal = false;
        private int _lastDayCalculated = -1;

        #endregion

        #region Properties

        /// <summary>
        /// Current rotation of the celestial body
        /// </summary>
        public Vector3 CurrentRotation => _currentRotation;

        /// <summary>
        /// Target rotation the celestial body is moving towards
        /// </summary>
        public Vector3 TargetRotation => _targetRotation;

        /// <summary>
        /// Name of this celestial body
        /// </summary>
        public string CelestialBodyName => celestialBodyName;

        /// <summary>
        /// Whether this celestial body is a moon
        /// </summary>
        public bool IsMoon => isMoon;

        /// <summary>
        /// Whether the rotator is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeDependencies();
        }

        private void Start()
        {
            InitializeRotator();
        }

        private void Update()
        {
            if (!_isInitialized || timeManager == null)
                return;

            if (useOptimization)
            {
                UpdateWithOptimization();
            }
            else
            {
                UpdateWithoutOptimization();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void InitializeDependencies()
        {
            // Initialize calculator
            _calculator = new CelestialCalculator();

            // Setup optimizer
            if (useOptimization)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }

            // Cache transform reference
            _targetTransform = celestialBodyTransform != null ? celestialBodyTransform : transform;
        }

        private void InitializeRotator()
        {
            if (timeManager == null)
            {
                Debug.LogError($"[CelestialRotator - {celestialBodyName}] TimeManager not assigned!");
                return;
            }

            // Initialize calculator with time manager
            _calculator.Initialize(timeManager);

            // Set initial rotation state
            _currentRotation = _targetTransform.eulerAngles;
            _targetRotation = _currentRotation;
            _lastSeason = timeManager.CurrentSeason;
            _lastUpdateTime = Time.time;
            _isInitialized = true;

            if (enableDebugLogging)
            {
                Debug.Log($"[CelestialRotator - {celestialBodyName}] Initialized successfully as {(isMoon ? "Moon" : "Star")}");
            }
        }

        #endregion

        #region Update Methods

        private void UpdateWithOptimization()
        {
            if (_optimizer == null) return;
            
            // ALWAYS calculate rotation every frame (for smooth time progression)
            UpdateCelestialRotation();

            // Only apply the rotation at optimized frequency
            if (_optimizer.ShouldUpdate(Time.time))
            {
                ApplyRotation();
                UpdateMoonOrbitalDrift();
                _lastUpdateTime = Time.time;
            }
        }

        private void UpdateWithoutOptimization()
        {
            // Update moon orbital drift first
            UpdateMoonOrbitalDrift();
    
            // Calculate and apply rotation every frame
            UpdateCelestialRotation();
            ApplyRotation();
        }

        private void UpdateCelestialRotation()
        {
            // Check for season changes
            if (timeManager.CurrentSeason != _lastSeason)
            {
                _lastSeason = timeManager.CurrentSeason;
                if (enableDebugLogging)
                {
                    Debug.Log($"[CelestialRotator - {celestialBodyName}] Season changed to {_lastSeason}");
                }
            }

            // Get current seasonal data
            
            SeasonalData seasonalData = timeManager.GetSeasonalData(timeManager.CurrentSeason);
            if (seasonalData == null)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[CelestialRotator - {celestialBodyName}] No seasonal data for {timeManager.CurrentSeason}");
                }
                return;
            }

            // Validate that the celestial body exists in the seasonal data
            var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
            if (celestialBody == null)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[CelestialRotator - {celestialBodyName}] Celestial body not found in seasonal data for {timeManager.CurrentSeason}");
                }
                return;
            }

            // Get effective base rotation (includes moon orbital drift)
            Vector3 effectiveBaseRotation = GetEffectiveBaseRotation();

            // Calculate new target rotation
            if (timeManager.IsInSeasonTransition)
            {
                _targetRotation = CalculateTransitionRotation(effectiveBaseRotation);
            }
            else
            {
                _targetRotation = _calculator.CalculateCelestialRotation(
                    seasonalData,
                    celestialBodyName,
                    effectiveBaseRotation,  // Use the effective base rotation
                    timeManager.CelestialTime,
                    isMoon
                );
            }
            // SeasonalData seasonalData = timeManager.GetSeasonalData(timeManager.CurrentSeason);
            // if (seasonalData == null)
            // {
            //     if (enableDebugLogging)
            //     {
            //         Debug.LogWarning($"[CelestialRotator - {celestialBodyName}] No seasonal data for {timeManager.CurrentSeason}");
            //     }
            //     return;
            // }
            //
            // // Validate that the celestial body exists in the seasonal data
            // var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
            // if (celestialBody == null)
            // {
            //     if (enableDebugLogging)
            //     {
            //         Debug.LogWarning($"[CelestialRotator - {celestialBodyName}] Celestial body not found in seasonal data for {timeManager.CurrentSeason}");
            //     }
            //     return;
            // }
            //
            // // Calculate new target rotation
            // if (timeManager.IsInSeasonTransition)
            // {
            //     _targetRotation = CalculateTransitionRotation();
            // }
            // else
            // {
            //     _targetRotation = _calculator.CalculateCelestialRotation(
            //         seasonalData,
            //         celestialBodyName,
            //         baseRotationOffset,
            //         timeManager.CelestialTime,
            //         isMoon
            //     );
            // }
        }
        
        // private void UpdateMoonOrbitalDrift()
        // {
        //     if (!isMoon || timeManager?.WorldTimeData == null) return;
        //
        //     // Store original base rotation on first run
        //     if (!_hasStoredOriginal)
        //     {
        //         _originalBaseRotation = baseRotationOffset;
        //         _hasStoredOriginal = true;
        //     }
        //
        //     int currentDay = timeManager.CurrentDayOfYear;
        //     
        //     // Only recalculate if the day has changed
        //     if (currentDay == _lastDayCalculated) return;
        //     
        //     _lastDayCalculated = currentDay;
        //
        //     // Get the celestial body data to access orbital period
        //     SeasonalData seasonalData = timeManager.GetSeasonalData(timeManager.CurrentSeason);
        //     if (seasonalData != null)
        //     {
        //         var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
        //         if (celestialBody != null)
        //         {
        //             // Calculate orbital progress (0-1) based on current day and orbital period
        //             float orbitalProgress = (currentDay % celestialBody.orbitalPeriod) / celestialBody.orbitalPeriod;
        //             
        //             // Convert to degrees (0-360)
        //             float orbitalDrift = orbitalProgress * 360f;
        //             
        //             // Update the actual baseRotationOffset field
        //             baseRotationOffset = _originalBaseRotation;
        //             baseRotationOffset.y += orbitalDrift;
        //             
        //             // Clamp all rotation values to 0-360 range for performance and readability
        //             baseRotationOffset.x = ClampAngle(baseRotationOffset.x);
        //             baseRotationOffset.y = ClampAngle(baseRotationOffset.y);
        //             baseRotationOffset.z = ClampAngle(baseRotationOffset.z);
        //             
        //             if (enableDebugLogging)
        //             {
        //                 Debug.Log($"[CelestialRotator - {celestialBodyName}] Updated base rotation offset: Day {currentDay}, Progress: {orbitalProgress:F3}, Drift: {orbitalDrift:F1}°, Clamped Y: {baseRotationOffset.y:F1}°");
        //             }
        //         }
        //     }
        // }
        
        private void UpdateMoonOrbitalDrift()
        {
            if (!isMoon || timeManager?.WorldTimeData == null) return;

            // Store original base rotation on first run
            if (!_hasStoredOriginal)
            {
                _originalBaseRotation = baseRotationOffset;
                _hasStoredOriginal = true;
            }

            // Get the celestial body data to access orbital period
            SeasonalData seasonalData = timeManager.GetSeasonalData(timeManager.CurrentSeason);
            if (seasonalData != null)
            {
                var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
                if (celestialBody != null)
                {
                    // Calculate smooth orbital progress using current day + time within day
                    int currentDay = timeManager.CurrentDayOfYear;
                    float timeWithinDay = timeManager.CelestialTime; // This should be 0-1 within the current day
                    
                    // Total elapsed time in days (including fractional part for smooth movement)
                    float totalElapsedDays = currentDay + timeWithinDay;
                    
                    // Calculate orbital progress (0-1) based on total elapsed time and orbital period
                    float orbitalProgress = (totalElapsedDays % celestialBody.orbitalPeriod) / celestialBody.orbitalPeriod;
                    
                    // Convert to degrees (0-360)
                    float orbitalDrift = orbitalProgress * 360f;
                    
                    // Update the actual baseRotationOffset field
                    baseRotationOffset = _originalBaseRotation;
                    baseRotationOffset.y += orbitalDrift;
                    
                    // Normalize all angles to 0-360 range
                    baseRotationOffset.x = Mathf.Repeat(baseRotationOffset.x, 360f);
                    baseRotationOffset.y = Mathf.Repeat(baseRotationOffset.y, 360f);
                    baseRotationOffset.z = Mathf.Repeat(baseRotationOffset.z, 360f);
                    
                    if (enableDebugLogging && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps to avoid spam
                    {
                        Debug.Log($"[CelestialRotator - {celestialBodyName}] Smooth orbital drift: Day {currentDay}, Time: {timeWithinDay:F3}, Total: {totalElapsedDays:F3}, Progress: {orbitalProgress:F4}, Drift: {orbitalDrift:F2}°");
                    }
                }
            }
        }

        /// <summary>
        /// Clamps an angle to the 0-360 degree range
        /// </summary>
        private float ClampAngle(float angle)
        {
            angle = angle % 360f;
            if (angle < 0f)
                angle += 360f;
            return angle;
        }
        
        // private void UpdateMoonOrbitalDrift()
        // {
        //     if (!isMoon || timeManager?.WorldTimeData == null) return;
        //
        //     // Store original base rotation on first run
        //     if (!_hasStoredOriginal)
        //     {
        //         _originalBaseRotation = baseRotationOffset;
        //         _hasStoredOriginal = true;
        //     }
        //
        //     int currentDay = timeManager.CurrentDayOfYear;
        //
        //     // Only recalculate if the day has changed
        //     if (currentDay == _lastDayCalculated) return;
        //
        //     _lastDayCalculated = currentDay;
        //
        //     // Get the celestial body data to access orbital period
        //     SeasonalData seasonalData = timeManager.GetSeasonalData(timeManager.CurrentSeason);
        //     if (seasonalData != null)
        //     {
        //         var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
        //         if (celestialBody != null)
        //         {
        //             // Calculate orbital progress (0-1) based on current day and orbital period
        //             float orbitalProgress = (currentDay % celestialBody.orbitalPeriod) / celestialBody.orbitalPeriod;
        //     
        //             // Convert to degrees (0-360)
        //             float orbitalDrift = orbitalProgress * 360f;
        //     
        //             // Update the actual baseRotationOffset field
        //             baseRotationOffset = _originalBaseRotation;
        //             baseRotationOffset.y += orbitalDrift;
        //     
        //             if (enableDebugLogging)
        //             {
        //                 Debug.Log($"[CelestialRotator - {celestialBodyName}] Updated base rotation offset: Day {currentDay}, Drift: {orbitalDrift:F1}°, New Y: {baseRotationOffset.y:F1}°");
        //             }
        //         }
        //     }
        // }
        
        private Vector3 GetEffectiveBaseRotation()
        {
            Vector3 effectiveBase = baseRotationOffset;
    
            // Apply moon orbital drift if this is a moon
            if (isMoon && timeManager?.WorldTimeData != null)
            {
                // Get current day of year from time manager
                int currentDay = timeManager.CurrentDayOfYear;
        
                // Get the celestial body data to access orbital period
                SeasonalData seasonalData = timeManager.GetSeasonalData(timeManager.CurrentSeason);
                if (seasonalData != null)
                {
                    var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
                    if (celestialBody != null)
                    {
                        // Calculate orbital progress (0-1) based on current day and orbital period
                        float orbitalProgress = (currentDay % celestialBody.orbitalPeriod) / celestialBody.orbitalPeriod;
                
                        // Convert to degrees (0-360)
                        float orbitalDrift = orbitalProgress * 360f;
                
                        // Apply drift to base rotation Y-axis
                        effectiveBase.y += orbitalDrift;
                
                        if (enableDebugLogging)
                        {
                            Debug.Log($"[CelestialRotator - {celestialBodyName}] Moon orbital drift: Day {currentDay}, Progress: {orbitalProgress:F3}, Drift: {orbitalDrift:F1}°");
                        }
                    }
                }
            }
    
            return effectiveBase;
        }

        private void ApplyRotation()
        {
            if (_targetTransform == null) return;

            _currentRotation = _targetRotation;
            _targetTransform.eulerAngles = _currentRotation;
        }
        
        private Vector3 CalculateTransitionRotation(Vector3 effectiveBaseRotation)
        {
            SeasonalData fromSeason = timeManager.GetSeasonalData(timeManager.CurrentSeason);
            SeasonalData toSeason = timeManager.GetSeasonalData(timeManager.TargetSeason);

            if (fromSeason == null || toSeason == null)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[CelestialRotator - {celestialBodyName}] Missing seasonal data for transition");
                }
                return effectiveBaseRotation;
            }

            // Validate celestial body exists in both seasons
            if (fromSeason.GetCelestialBodyByName(celestialBodyName) == null || 
                toSeason.GetCelestialBodyByName(celestialBodyName) == null)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[CelestialRotator - {celestialBodyName}] Celestial body missing in transition seasons");
                }
                return effectiveBaseRotation;
            }

            return _calculator.InterpolateCelestialRotation(
                fromSeason,
                toSeason,
                celestialBodyName,
                effectiveBaseRotation,  // Use the effective base rotation
                timeManager.CelestialTime,
                timeManager.SeasonTransitionProgress,
                isMoon
            );
        }

        // private Vector3 CalculateTransitionRotation()
        // {
        //     SeasonalData fromSeason = timeManager.GetSeasonalData(timeManager.CurrentSeason);
        //     SeasonalData toSeason = timeManager.GetSeasonalData(timeManager.TargetSeason);
        //
        //     if (fromSeason == null || toSeason == null)
        //     {
        //         if (enableDebugLogging)
        //         {
        //             Debug.LogWarning($"[CelestialRotator - {celestialBodyName}] Missing seasonal data for transition");
        //         }
        //         return baseRotationOffset;
        //     }
        //
        //     // Validate celestial body exists in both seasons
        //     if (fromSeason.GetCelestialBodyByName(celestialBodyName) == null || 
        //         toSeason.GetCelestialBodyByName(celestialBodyName) == null)
        //     {
        //         if (enableDebugLogging)
        //         {
        //             Debug.LogWarning($"[CelestialRotator - {celestialBodyName}] Celestial body missing in transition seasons");
        //         }
        //         return baseRotationOffset;
        //     }
        //
        //     return _calculator.InterpolateCelestialRotation(
        //         fromSeason,
        //         toSeason,
        //         celestialBodyName,
        //         baseRotationOffset,
        //         timeManager.CelestialTime,
        //         timeManager.SeasonTransitionProgress,
        //         isMoon
        //     );
        // }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets whether to use performance optimization
        /// </summary>
        /// <param name="enabled">Whether optimization should be enabled</param>
        public void SetOptimization(bool enabled)
        {
            useOptimization = enabled;
            
            if (enabled && _optimizer == null)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }
        }

        /// <summary>
        /// Sets the update frequency for optimized updates
        /// </summary>
        /// <param name="frequency">Update frequency in Hz</param>
        public void SetUpdateFrequency(float frequency)
        {
            updateFrequency = Mathf.Max(0.1f, frequency);
            
            if (_optimizer != null)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }
        }

        /// <summary>
        /// Sets whether this celestial body is a moon
        /// </summary>
        /// <param name="moonStatus">True if this is a moon, false if it's a star</param>
        public void SetMoonStatus(bool moonStatus)
        {
            isMoon = moonStatus;
            
            if (enableDebugLogging)
            {
                Debug.Log($"[CelestialRotator - {celestialBodyName}] Set as {(isMoon ? "Moon" : "Star")}");
            }
        }

        /// <summary>
        /// Forces immediate recalculation and application of rotation
        /// </summary>
        public void ForceUpdate()
        {
            if (_isInitialized)
            {
                UpdateCelestialRotation();
                ApplyRotation();
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            _calculator?.Cleanup();
            _calculator = null;
            _optimizer = null;
            _isInitialized = false;

            if (enableDebugLogging)
            {
                Debug.Log($"[CelestialRotator - {celestialBodyName}] Cleanup completed");
            }
        }

        #endregion

        #region Editor Support

        #if UNITY_EDITOR
        [ContextMenu("Force Update")]
        private void ForceUpdateMenu()
        {
            ForceUpdate();
        }

        [ContextMenu("Log Status")]
        private void LogStatus()
        {
            Debug.Log($"[CelestialRotator - {celestialBodyName}] " +
                     $"Type: {(isMoon ? "Moon" : "Star")}, " +
                     $"Initialized: {_isInitialized}, " +
                     $"Current Rotation: {_currentRotation}, " +
                     $"Season: {(timeManager != null ? timeManager.CurrentSeason.ToString() : "No TimeManager")}");
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(celestialBodyName))
            {
                celestialBodyName = "Sol";
            }

            updateFrequency = Mathf.Max(0.1f, updateFrequency);

            // Validate celestial body name against common names
            string lowerName = celestialBodyName.ToLower();
            if (lowerName.Contains("primary") || lowerName.Contains("sun"))
            {
                celestialBodyName = "Sol";
            }
            else if (lowerName.Contains("dwarf") || lowerName.Contains("red"))
            {
                celestialBodyName = "Notr";
            }
        }
        #endif

        #endregion
    }
}