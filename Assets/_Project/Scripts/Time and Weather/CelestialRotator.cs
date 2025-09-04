using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Controls the rotation of celestial bodies (suns, moons) based on time progression and seasonal data.
    /// Uses pure quaternion rotation to eliminate gimbal lock and provide smooth movement at all angles.
    /// </summary>
    public class CelestialRotator : MonoBehaviour
    {
        // [Header("Celestial Body Configuration")]
        [Tooltip("Name of the celestial body in SeasonalData. Must match exactly (case-sensitive).")]
        [SerializeField] private string celestialBodyName = "Sol";
        
        [Tooltip("Whether this is a moon (affects orbital calculations and phase offsets) or a star/sun. Moons will drift over multiple days creating realistic lunar cycles, while suns follow consistent daily patterns.")]
        [SerializeField] private bool isMoon = false;
        
        [Tooltip("Additional rotation offset applied to the calculated celestial position. Use for fine-tuning alignment or correcting directional light orientation.")]
        [SerializeField] private Vector3 baseRotationOffset = Vector3.zero;

        // [Header("Rotation Settings")]
        [Tooltip("Enable smooth interpolation between rotation updates. Provides smoother visual movement but uses more CPU. Recommended for visible celestial bodies.")]
        [SerializeField] private bool smoothRotation = true;
        
        [Tooltip("Speed of rotation interpolation when smooth rotation is enabled. Higher values = faster convergence to target rotation. Range: 0.1-10.0")]
        [SerializeField] private float rotationSmoothSpeed = 2f;

        // [Header("Performance")]
        [Tooltip("Reduce update frequency for better performance. Rotation calculations are still done every frame for smooth time progression, but transform updates are optimized.")]
        [SerializeField] private bool enablePerformanceOptimization = true;
        
        [Tooltip("How often to apply rotation updates per second when performance optimization is enabled. Higher values = smoother movement but more CPU usage. Recommended: 10-30 Hz")]
        [SerializeField] private float updateFrequency = 10f;

        // [Header("Debug")]
        [Tooltip("Log rotation calculations and celestial body information to the console. Useful for debugging celestial movement but may impact performance in builds.")]
        [SerializeField] private bool enableDebugLogging = false;

        // Private fields
        private ICelestialCalculator _calculator;
        private TimeManager timeManager;
        private UpdateFrequencyOptimizer _optimizer;
        private Transform _targetTransform;
        private Quaternion _currentQuaternion;
        private Quaternion _targetQuaternion;

        #region Unity Lifecycle

        /// <summary>
        /// Initialize component references and set initial rotation state
        /// </summary>
        private void Awake()
        {
            _targetTransform = transform;
            _currentQuaternion = _targetTransform.rotation;
            _targetQuaternion = _currentQuaternion;
        }

        /// <summary>
        /// Initialize calculator and performance optimizer
        /// </summary>
        private void Start()
        {
            InitializeComponents();
        }

        /// <summary>
        /// Update celestial rotation based on current time and apply to transform
        /// </summary>
        private void Update()
        {
            if (timeManager == null || _calculator == null) return;

            if (enablePerformanceOptimization)
            {
                UpdateWithOptimization();
            }
            else
            {
                UpdateWithoutOptimization();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize TimeManager reference, celestial calculator, and performance optimizer
        /// </summary>
        private void InitializeComponents()
        {
            // Find TimeManager
            timeManager = FindObjectOfType<TimeManager>();
            if (timeManager == null)
            {
                Debug.LogError($"[CelestialRotator] TimeManager not found in scene. {gameObject.name} will not function properly.");
                enabled = false;
                return;
            }

            // Initialize calculator with pure quaternion system
            _calculator = new CelestialCalculator(timeManager);
            _calculator.enableDebugLogging = enableDebugLogging;

            // Initialize performance optimizer if enabled
            if (enablePerformanceOptimization)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }

            // Set initial rotation based on base offset
            _currentQuaternion = Quaternion.Euler(baseRotationOffset);
            _targetQuaternion = _currentQuaternion;
            _targetTransform.rotation = _currentQuaternion;

            if (enableDebugLogging)
            {
                Debug.Log($"[CelestialRotator] {gameObject.name} initialized. Body: {celestialBodyName}, IsMoon: {isMoon}");
            }
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Update with performance optimization - applies rotation at reduced frequency
        /// </summary>
        private void UpdateWithOptimization()
        {
            if (_optimizer == null) return;

            // Always calculate rotation for smooth time progression
            UpdateCelestialRotation();

            // Apply rotation at optimized frequency or every frame if smooth rotation is enabled
            if (_optimizer.ShouldUpdate(Time.time) || smoothRotation)
            {
                ApplyRotation();
            }
        }

        /// <summary>
        /// Update without optimization - calculates and applies rotation every frame
        /// </summary>
        private void UpdateWithoutOptimization()
        {
            UpdateCelestialRotation();
            ApplyRotation();
        }

        /// <summary>
        /// Calculate the target celestial rotation based on current time and seasonal data
        /// </summary>
        private void UpdateCelestialRotation()
        {
            var seasonalData = timeManager.GetCurrentSeasonalData();
            if (seasonalData == null) return;

            // Calculate pure quaternion rotation - no Euler conversions, no gimbal lock
            _targetQuaternion = _calculator.CalculateCelestialRotation(
                seasonalData,
                celestialBodyName,
                baseRotationOffset,
                timeManager.CelestialTime,
                isMoon
            );
        }

        /// <summary>
        /// Apply the calculated rotation to the transform, with optional smooth interpolation
        /// </summary>
        private void ApplyRotation()
        {
            if (smoothRotation)
            {
                // Smooth quaternion interpolation for fluid movement
                _currentQuaternion = Quaternion.Slerp(_currentQuaternion, _targetQuaternion, Time.deltaTime * rotationSmoothSpeed);
                _targetTransform.rotation = _currentQuaternion;
            }
            else
            {
                // Direct quaternion assignment for immediate updates
                _currentQuaternion = _targetQuaternion;
                _targetTransform.rotation = _currentQuaternion;
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate inspector values and provide sensible defaults
        /// </summary>
        private void OnValidate()
        {
            // Validate celestial body name
            if (string.IsNullOrEmpty(celestialBodyName))
            {
                celestialBodyName = gameObject.name;
            }

            // Validate update frequency
            if (updateFrequency <= 0f)
            {
                updateFrequency = 1f;
            }

            // Validate smooth speed
            if (rotationSmoothSpeed <= 0f)
            {
                rotationSmoothSpeed = 2f;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Name of the celestial body this rotator controls
        /// </summary>
        public string CelestialBodyName => celestialBodyName;

        /// <summary>
        /// Whether this celestial body is a moon (affects orbital drift calculations)
        /// </summary>
        public bool IsMoon => isMoon;

        /// <summary>
        /// Current rotation as Euler angles (for display/debugging purposes)
        /// </summary>
        public Vector3 CurrentRotation => _currentQuaternion.eulerAngles;

        /// <summary>
        /// Current rotation as quaternion (actual rotation used by transform)
        /// </summary>
        public Quaternion CurrentQuaternion => _currentQuaternion;

        /// <summary>
        /// Base rotation offset applied to calculated celestial position
        /// </summary>
        public Vector3 BaseRotationOffset => baseRotationOffset;

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the celestial body name at runtime
        /// </summary>
        /// <param name="newName">New celestial body name (must exist in SeasonalData)</param>
        public void SetCelestialBodyName(string newName)
        {
            if (!string.IsNullOrEmpty(newName))
            {
                celestialBodyName = newName;
                if (enableDebugLogging)
                {
                    Debug.Log($"[CelestialRotator] Celestial body name changed to: {newName}");
                }
            }
        }

        /// <summary>
        /// Update the moon flag at runtime
        /// </summary>
        /// <param name="isLunar">True if this is a moon, false if it's a sun/star</param>
        public void SetIsMoon(bool isLunar)
        {
            isMoon = isLunar;
            if (enableDebugLogging)
            {
                Debug.Log($"[CelestialRotator] Moon flag changed to: {isLunar}");
            }
        }

        /// <summary>
        /// Update the base rotation offset at runtime
        /// </summary>
        /// <param name="newOffset">New rotation offset to apply</param>
        public void SetBaseRotationOffset(Vector3 newOffset)
        {
            baseRotationOffset = newOffset;
            if (enableDebugLogging)
            {
                Debug.Log($"[CelestialRotator] Base rotation offset changed to: {newOffset}");
            }
        }

        /// <summary>
        /// Enable or disable smooth rotation at runtime
        /// </summary>
        /// <param name="enable">True to enable smooth interpolation</param>
        public void SetSmoothRotation(bool enable)
        {
            smoothRotation = enable;
            if (enableDebugLogging)
            {
                Debug.Log($"[CelestialRotator] Smooth rotation changed to: {enable}");
            }
        }

        /// <summary>
        /// Update the smooth rotation speed at runtime
        /// </summary>
        /// <param name="speed">New interpolation speed (must be > 0)</param>
        public void SetSmoothRotationSpeed(float speed)
        {
            if (speed > 0f)
            {
                rotationSmoothSpeed = speed;
                if (enableDebugLogging)
                {
                    Debug.Log($"[CelestialRotator] Smooth rotation speed changed to: {speed}");
                }
            }
        }

        /// <summary>
        /// Enable or disable performance optimization at runtime
        /// </summary>
        /// <param name="enable">True to enable optimization</param>
        public void SetPerformanceOptimization(bool enable)
        {
            enablePerformanceOptimization = enable;
            
            if (enable && _optimizer == null)
            {
                _optimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }
            
            if (enableDebugLogging)
            {
                Debug.Log($"[CelestialRotator] Performance optimization changed to: {enable}");
            }
        }

        /// <summary>
        /// Update the performance optimization frequency at runtime
        /// </summary>
        /// <param name="frequency">New update frequency in Hz (must be > 0)</param>
        public void SetUpdateFrequency(float frequency)
        {
            if (frequency > 0f)
            {
                updateFrequency = frequency;
                if (_optimizer != null)
                {
                    _optimizer = new UpdateFrequencyOptimizer(frequency);
                }
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[CelestialRotator] Update frequency changed to: {frequency} Hz");
                }
            }
        }

        /// <summary>
        /// Enable or disable debug logging at runtime
        /// </summary>
        /// <param name="enable">True to enable debug logging</param>
        public void SetDebugLogging(bool enable)
        {
            enableDebugLogging = enable;
            if (_calculator != null)
            {
                _calculator.enableDebugLogging = enable;
            }
            
            if (enable)
            {
                Debug.Log($"[CelestialRotator] Debug logging enabled for {gameObject.name}");
            }
        }

        #endregion
    }
}