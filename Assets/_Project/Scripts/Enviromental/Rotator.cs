using UnityEngine;

namespace Sol
{

    /// <summary>
    /// Simple rotator component for environmental objects
    /// Can run at full frame rate or use update frequency optimization
    /// Optimization trades visual smoothness for performance
    /// </summary>
    public class Rotator : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 90f; // Degrees per second
        [SerializeField] private bool xAxisRotation = false;
        [SerializeField] private bool yAxisRotation = true;
        [SerializeField] private bool zAxisRotation = false;
        
        [Header("Camera Facing")]
        [SerializeField] private bool faceCamera = false;
        [SerializeField] private bool lockX = false;
        [SerializeField] private bool lockY = false;
        [SerializeField] private bool lockZ = false;
        [SerializeField] private float facingSpeed = 5f;
        
        [Header("Performance")]
        [SerializeField] private bool useOptimization = false; // Toggle optimization on/off
        [SerializeField] private float updateFrequency = 30f; // Only used when optimization is enabled
        
        [Header("Control")]
        [SerializeField] private bool rotationActive = true;
        [SerializeField] private bool getSpeedExternally = false;
        [SerializeField] private float wheelRadius = 1f;

        // Components
        private IUpdateFrequencyOptimizer _updateOptimizer;
        private Camera _targetCamera;
        private Quaternion _targetCameraRotation;
        
        // Rotation state
        private Vector3 _currentRotation;
        private Vector3 _rotationAxis;

        // Public properties for external control
        public bool RotationActive 
        { 
            get => rotationActive; 
            set => rotationActive = value; 
        }

        public float RotationSpeed 
        { 
            get => rotationSpeed; 
            set => rotationSpeed = value; 
        }

        public float WheelRadius => wheelRadius;

        public bool UseOptimization
        {
            get => useOptimization;
            set 
            { 
                useOptimization = value;
                if (useOptimization && _updateOptimizer == null)
                {
                    _updateOptimizer = new UpdateFrequencyOptimizer(updateFrequency);
                }
            }
        }

        private void Awake()
        {
            // Initialize rotation state
            _currentRotation = transform.eulerAngles;
            UpdateRotationAxis();
            
            // Initialize optimizer only if needed
            if (useOptimization)
            {
                _updateOptimizer = new UpdateFrequencyOptimizer(updateFrequency);
            }
            
            // Setup camera facing if enabled
            if (faceCamera)
            {
                _targetCamera = Camera.main;
                if (_targetCamera != null)
                {
                    _targetCameraRotation = _targetCamera.transform.rotation;
                }
            }
        }

        private void Update()
        {
            if (!rotationActive) return;

            // ALWAYS calculate rotation every frame (this maintains correct speed)
            UpdateRotationAxis();
            Vector3 rotationDelta = _rotationAxis * rotationSpeed * Time.deltaTime;
            _currentRotation += rotationDelta;

            // Handle camera facing every frame for smoothness
            if (faceCamera && _targetCamera != null)
            {
                UpdateCameraFacing();
            }

            // Only the TRANSFORM UPDATE is optimized, not the calculation
            if (useOptimization && _updateOptimizer != null)
            {
                // Only update transform when optimizer says so
                if (_updateOptimizer.ShouldUpdate(Time.time))
                {
                    ApplyRotationToTransform();
                }
            }
            else
            {
                // Update transform every frame
                ApplyRotationToTransform();
            }
        }

        private void ApplyRotationToTransform()
        {
            // Apply camera facing if enabled
            Vector3 finalRotation = _currentRotation;
            if (faceCamera)
            {
                ApplyCameraFacingToRotation(ref finalRotation);
            }
            
            // Apply rotation to transform
            transform.eulerAngles = finalRotation;
        }

        private void UpdateRotationAxis()
        {
            _rotationAxis = new Vector3(
                xAxisRotation ? 1f : 0f,
                yAxisRotation ? 1f : 0f,
                zAxisRotation ? 1f : 0f
            );
        }

        private void UpdateCameraFacing()
        {
            if (_targetCamera == null) return;

            Vector3 directionToCamera = (_targetCamera.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
            
            _targetCameraRotation = Quaternion.Slerp(_targetCameraRotation, lookRotation, 
                facingSpeed * Time.deltaTime);
        }

        private void ApplyCameraFacingToRotation(ref Vector3 rotation)
        {
            Vector3 cameraEuler = _targetCameraRotation.eulerAngles;
            
            if (!lockX) rotation.x = cameraEuler.x;
            if (!lockY) rotation.y = cameraEuler.y;
            if (!lockZ) rotation.z = cameraEuler.z;
        }

        // Public API methods
        public void SetUpdateFrequency(float frequency)
        {
            updateFrequency = frequency;
            if (_updateOptimizer != null)
            {
                _updateOptimizer.SetUpdateFrequency(frequency);
            }
        }

        public void SetRotationAxis(bool x, bool y, bool z)
        {
            xAxisRotation = x;
            yAxisRotation = y;
            zAxisRotation = z;
            UpdateRotationAxis();
        }

        public void SetCameraFacing(bool enabled, Camera targetCamera = null)
        {
            faceCamera = enabled;
            if (targetCamera != null)
                _targetCamera = targetCamera;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateRotationAxis();
                if (useOptimization && _updateOptimizer != null)
                {
                    _updateOptimizer.SetUpdateFrequency(updateFrequency);
                }
            }
        }
    }
}