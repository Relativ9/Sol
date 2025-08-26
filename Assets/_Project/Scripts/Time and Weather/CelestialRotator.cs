using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Component that applies celestial rotations to GameObjects
    /// Works with TimeManager's SeasonalData and supports seasonal transitions
    /// Identifies itself by celestialBodyName to get appropriate settings
    /// Integrates with CelestialCalculator for day synchronization
    /// </summary>
    public class CelestialRotator : MonoBehaviour
    {
        [Header("Celestial Configuration")]
        [SerializeField] private string celestialBodyName = "PrimaryStar";
        [SerializeField] private bool useSeasonalTransitions = true;
        
        [Header("Calculator Integration")]
        [SerializeField] private CelestialCalculator celestialCalculator;
        [SerializeField] private bool createCalculatorIfMissing = true;
        
        [Header("Performance")]
        [SerializeField] private bool useOptimization = true;
        [SerializeField] private float updateFrequency = 20f;
        
        [Header("Control")]
        [SerializeField] private bool rotationActive = true;
        [SerializeField] private bool overrideCelestialTime = false;
        [SerializeField] private float manualCelestialTime = 0f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool logSynchronizationStatus = false;

        // Dependencies
        private ITimeManager _timeManager;
        private ICelestialCalculator _celestialCalculatorInterface;
        private IUpdateFrequencyOptimizer _optimizer;

        // State
        private Vector3 _baseRotation;
        private Vector3 _currentRotation;
        private bool _isInitialized = false;

        // Performance tracking
        private float _lastUpdateTime;
        private float _lastSyncLogTime;
        private int _frameCount = 0;

        // Properties
        public string CelestialBodyName => celestialBodyName;
        public Vector3 CurrentRotation => _currentRotation;
        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            _baseRotation = transform.rotation.eulerAngles;
            _currentRotation = _baseRotation;
        }

        private void Start()
        {
            InitializeDependencies();
        }

        private void Update()
        {
            if (!_isInitialized || !rotationActive) return;

            if (useOptimization)
            {
                UpdateWithOptimization();
            }
            else
            {
                UpdateCelestialRotation();
                ApplyRotation();
            }

            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }

            if (logSynchronizationStatus)
            {
                LogSynchronizationStatus();
            }
        }

        private void InitializeDependencies()
        {
            // Find TimeManager
            _timeManager = FindObjectOfType<TimeManager>();
            if (_timeManager == null)
            {
                Debug.LogError($"CelestialRotator on {gameObject.name}: No TimeManager found in scene!");
                return;
            }

            // Setup CelestialCalculator
            if (celestialCalculator == null && createCalculatorIfMissing)
            {
                // Create a new calculator instance
                GameObject calculatorObject = new GameObject($"{celestialBodyName}_Calculator");
                calculatorObject.transform.SetParent(transform);
                celestialCalculator = calculatorObject.AddComponent<CelestialCalculator>();
                
                if (showDebugInfo)
                {
                    Debug.Log($"Created CelestialCalculator for {celestialBodyName}");
                }
            }

            if (celestialCalculator != null)
            {
                _celestialCalculatorInterface = celestialCalculator as ICelestialCalculator;
                
                // Initialize calculator with time manager
                _celestialCalculatorInterface?.Initialize(_timeManager);
            }
            else
            {
                Debug.LogError($"CelestialRotator on {gameObject.name}: No CelestialCalculator assigned and createCalculatorIfMissing is false!");
                return;
            }

            // Setup optimizer
            if (useOptimization)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }

            _isInitialized = true;

            if (showDebugInfo)
            {
                Debug.Log($"CelestialRotator initialized for {celestialBodyName} on {gameObject.name}");
                LogInitializationStatus();
            }
        }

        private void UpdateWithOptimization()
        {
            if (_optimizer == null) return;

            // ALWAYS calculate rotation every frame (for smooth time progression)
            UpdateCelestialRotation();

            // Only apply the rotation at optimized frequency
            if (_optimizer.ShouldUpdate(Time.time))
            {
                ApplyRotation();
                _lastUpdateTime = Time.time;
            }
        }

        private void UpdateCelestialRotation()
        {
            if (_celestialCalculatorInterface == null) return;

            float celestialTime = overrideCelestialTime ? manualCelestialTime : _timeManager.CelestialTime;

            if (useSeasonalTransitions && _timeManager.IsTransitioning)
            {
                SeasonalData fromSeason = _timeManager.GetSeasonalData(_timeManager.CurrentSeason);
                SeasonalData toSeason = _timeManager.GetSeasonalData(_timeManager.TargetSeason);

                if (fromSeason != null && toSeason != null)
                {
                    _currentRotation = _celestialCalculatorInterface.InterpolateCelestialRotation(
                        fromSeason,
                        toSeason,
                        celestialBodyName,
                        _baseRotation,
                        celestialTime,
                        _timeManager.SeasonTransitionProgress
                    );
                }
            }
            else
            {
                SeasonalData currentSeason = _timeManager.GetCurrentSeasonalData();
                if (currentSeason != null)
                {
                    _currentRotation = _celestialCalculatorInterface.CalculateCelestialRotation(
                        currentSeason,
                        celestialBodyName,
                        _baseRotation,
                        celestialTime
                    );
                }
            }

            _frameCount++;
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(_currentRotation);
        }

        private void DisplayDebugInfo()
        {
            if (Time.time - _lastUpdateTime > 1f)
            {
                Debug.Log($"[CelestialRotator - {celestialBodyName}] " +
                         $"Season: {_timeManager?.CurrentSeason}, " +
                         $"Rotation: {_currentRotation:F2}, " +
                         $"Transitioning: {_timeManager?.IsTransitioning}, " +
                         $"Updates/sec: {_frameCount}, " +
                         $"Day Sync: {_celestialCalculatorInterface?.IsDaySynchronizationEnabled()}");
                
                _frameCount = 0;
                _lastUpdateTime = Time.time;
            }
        }

        private void LogSynchronizationStatus()
        {
            if (Time.time - _lastSyncLogTime < 5f) return; // Log every 5 seconds
            
            if (_celestialCalculatorInterface == null || _timeManager == null) return;

            SeasonalData currentData = _timeManager.GetCurrentSeasonalData();
            if (currentData == null) return;

            float dayLength = _timeManager.DayLengthInSeconds;
            float requiredSpeed = _celestialCalculatorInterface.GetRequiredYAxisSpeedForDay(dayLength);

            // Check synchronization based on celestial body type
            bool isSync = false;
            float currentSpeed = 0f;

            if (celestialBodyName.ToLower().Contains("primary"))
            {
                currentSpeed = currentData.PrimaryYAxisSpeed;
                isSync = _celestialCalculatorInterface.IsYAxisSynchronizedWithDay(currentSpeed, dayLength);
            }
            else if (celestialBodyName.ToLower().Contains("dwarf"))
            {
                currentSpeed = currentData.RedDwarfYAxisSpeed;
                isSync = _celestialCalculatorInterface.IsYAxisSynchronizedWithDay(currentSpeed, dayLength);
            }

            Debug.Log($"[{celestialBodyName}] Sync Status - Current: {currentSpeed:F3} deg/sec, Required: {requiredSpeed:F3} deg/sec, Synced: {isSync}");
            
            _lastSyncLogTime = Time.time;
        }

        private void LogInitializationStatus()
        {
            Debug.Log($"=== {celestialBodyName} Initialization Status ===");
            Debug.Log($"TimeManager: {(_timeManager != null ? "Found" : "Missing")}");
            Debug.Log($"CelestialCalculator: {(_celestialCalculatorInterface != null ? "Ready" : "Missing")}");
            Debug.Log($"Day Synchronization: {(_celestialCalculatorInterface?.IsDaySynchronizationEnabled() ?? false)}");
            Debug.Log($"Seasonal Transitions: {useSeasonalTransitions}");
            Debug.Log($"Optimization: {useOptimization} ({updateFrequency} Hz)");
            Debug.Log($"============================================");
        }

        // Public API Methods
        public void SetCelestialBodyName(string newName)
        {
            celestialBodyName = newName;
            if (showDebugInfo)
            {
                Debug.Log($"CelestialRotator celestial body name changed to: {newName}");
            }
        }

        public void SetRotationActive(bool active)
        {
            rotationActive = active;
            
            if (showDebugInfo)
            {
                Debug.Log($"{celestialBodyName} rotation: {(active ? "Activated" : "Deactivated")}");
            }
        }

        public void SetUpdateFrequency(float frequency)
        {
            updateFrequency = Mathf.Max(1f, frequency);
            
            // Recreate optimizer with new frequency if using optimization
            if (useOptimization && _optimizer != null)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"{celestialBodyName} update frequency set to: {updateFrequency} Hz");
            }
        }

        public void SetUseOptimization(bool useOpt)
        {
            useOptimization = useOpt;
            
            if (useOpt && _optimizer == null)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }
            else if (!useOpt)
            {
                _optimizer = null;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"{celestialBodyName} optimization: {(useOpt ? "Enabled" : "Disabled")}");
            }
        }

        public void SetDaySynchronization(bool enabled)
        {
            if (_celestialCalculatorInterface != null)
            {
                _celestialCalculatorInterface.SetDaySynchronizationEnabled(enabled);
                
                if (showDebugInfo)
                {
                    Debug.Log($"{celestialBodyName} day synchronization: {(enabled ? "Enabled" : "Disabled")}");
                }
            }
        }

        public bool IsDaySynchronizationEnabled()
        {
            return _celestialCalculatorInterface?.IsDaySynchronizationEnabled() ?? false;
        }

        public void SetCelestialCalculator(CelestialCalculator newCalculator)
        {
            celestialCalculator = newCalculator;
            _celestialCalculatorInterface = newCalculator as ICelestialCalculator;
            
            if (_isInitialized && _timeManager != null)
            {
                _celestialCalculatorInterface?.Initialize(_timeManager);
            }
        }

        public void ResetToBaseRotation()
        {
            _currentRotation = _baseRotation;
            ApplyRotation();
            
            if (showDebugInfo)
            {
                Debug.Log($"{celestialBodyName} reset to base rotation: {_baseRotation:F2}");
            }
        }

        public void ForceRotationUpdate()
        {
            if (_isInitialized)
            {
                UpdateCelestialRotation();
                ApplyRotation();
            }
        }

        // Editor validation
        private void OnValidate()
        {
            updateFrequency = Mathf.Max(1f, updateFrequency);
            
            if (string.IsNullOrEmpty(celestialBodyName))
            {
                celestialBodyName = "PrimaryStar";
            }
        }

        // Context menu methods for easy testing
        [ContextMenu("Log Initialization Status")]
        private void ContextMenuLogInitStatus()
        {
            LogInitializationStatus();
        }

        [ContextMenu("Log Synchronization Status")]
        private void ContextMenuLogSyncStatus()
        {
            LogSynchronizationStatus();
        }

        [ContextMenu("Force Rotation Update")]
        private void ContextMenuForceUpdate()
        {
            ForceRotationUpdate();
        }

        [ContextMenu("Toggle Day Synchronization")]
        private void ContextMenuToggleDaySync()
        {
            if (_celestialCalculatorInterface != null)
            {
                bool currentState = _celestialCalculatorInterface.IsDaySynchronizationEnabled();
                SetDaySynchronization(!currentState);
            }
        }

        // Gizmos for visual debugging
        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo || !_isInitialized) return;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right * 2f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up * 2f);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }

        // Cleanup
        private void OnDestroy()
        {
            if (_celestialCalculatorInterface != null)
            {
                _celestialCalculatorInterface.Cleanup();
            }
        }
    }
}