using UnityEngine;

namespace Sol
{
        /// <summary>
    /// Component that applies celestial rotations to GameObjects
    /// Works with TimeManager's SeasonalData and supports seasonal transitions
    /// Identifies itself by celestialBodyName to get appropriate settings
    /// </summary>
    public class CelestialRotator : MonoBehaviour
    {
        [Header("Celestial Configuration")]
        [SerializeField] private string celestialBodyName = "PrimaryStar";
        [SerializeField] private bool useSeasonalTransitions = true;
        
        [Header("Performance")]
        [SerializeField] private bool useOptimization = true;
        [SerializeField] private float updateFrequency = 20f;
        
        [Header("Control")]
        [SerializeField] private bool rotationActive = true;
        [SerializeField] private bool overrideCelestialTime = false;
        [SerializeField] private float manualCelestialTime = 0f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Dependencies
        private ITimeManager _timeManager;
        private ICelestialCalculator _celestialCalculator;
        private IUpdateFrequencyOptimizer _optimizer;

        // State
        private Vector3 _baseRotation;
        private Vector3 _currentRotation;
        private bool _isInitialized = false;

        // Performance tracking
        private float _lastUpdateTime;
        private int _frameCount = 0;

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
        }

        private void InitializeDependencies()
        {
            _timeManager = FindObjectOfType<TimeManager>();
            if (_timeManager == null)
            {
                Debug.LogError($"CelestialRotator on {gameObject.name}: No TimeManager found in scene!");
                return;
            }

            _celestialCalculator = new CelestialCalculator();

            if (useOptimization)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }

            _isInitialized = true;

            if (showDebugInfo)
            {
                Debug.Log($"CelestialRotator initialized for {celestialBodyName} on {gameObject.name}");
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
            float celestialTime = overrideCelestialTime ? manualCelestialTime : _timeManager.CelestialTime;

            if (useSeasonalTransitions && _timeManager.IsTransitioning)
            {
                SeasonalData fromSeason = _timeManager.GetSeasonalData(_timeManager.CurrentSeason);
                SeasonalData toSeason = _timeManager.GetSeasonalData(_timeManager.TargetSeason);

                if (fromSeason != null && toSeason != null)
                {
                    _currentRotation = _celestialCalculator.InterpolateCelestialRotation(
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
                    _currentRotation = _celestialCalculator.CalculateCelestialRotation(
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
                         $"Rotation: {_currentRotation}, " +
                         $"Transitioning: {_timeManager?.IsTransitioning}, " +
                         $"Updates/sec: {_frameCount}");
                
                _frameCount = 0;
                _lastUpdateTime = Time.time;
            }
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
        }

        public void SetUpdateFrequency(float frequency)
        {
            updateFrequency = Mathf.Max(1f, frequency);
            
            // Recreate optimizer with new frequency if using optimization
            if (useOptimization && _optimizer != null)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
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
        }

        public void ResetToBaseRotation()
        {
            _currentRotation = _baseRotation;
            ApplyRotation();
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
    }
}
