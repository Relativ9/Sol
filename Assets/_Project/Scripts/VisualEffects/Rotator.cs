using UnityEngine;

namespace Sol
{
    public enum CelestialRotationMode
    {
        Oscillate,
        Continuous
    }

    public class Rotator : MonoBehaviour
    {
                [SerializeField] private float _rotationSpeed;
        [SerializeField] private bool _faceCamera;
        [SerializeField] private bool _xAxisRot;
        [SerializeField] private bool _yAxisRot;
        [SerializeField] private bool _zAxisRot;

        private Camera _playerCamera;
        private Quaternion _targetRot;
        private Vector3 _spinRotation;

        [Header("Wheel Settings")]
        private Vector3 lastPosition;
        public float wheelRadius = 1.0f;
        public bool rotationActive = true;
        public bool getSpeedExternally = false;

        [Header("Celestial Body Settings")]
        [SerializeField] private bool _celestialBodyMode = false;

        [SerializeField] private bool _xAxisCelestial = false;
        [SerializeField] private CelestialRotationMode _xRotationMode = CelestialRotationMode.Oscillate;
        [SerializeField] private bool _xSyncWithAxis = false;
        [SerializeField] private int _xSyncAxisIndex = 1;
        [SerializeField] private float _xOscillationSpeed = 1.0f;
        [SerializeField] private float _xMinRange = -30.0f;
        [SerializeField] private float _xMaxRange = 30.0f;
        [SerializeField] private float _xRotationSpeed = 0.0f;

        [SerializeField] private bool _yAxisCelestial = true;
        [SerializeField] private CelestialRotationMode _yRotationMode = CelestialRotationMode.Continuous;
        [SerializeField] private bool _ySyncWithAxis = false;
        [SerializeField] private int _ySyncAxisIndex = 0;
        [SerializeField] private float _yOscillationSpeed = 0.5f;
        [SerializeField] private float _yMinRange = -45.0f;
        [SerializeField] private float _yMaxRange = 45.0f;
        [SerializeField] private float _yRotationSpeed = 360.0f;

        [SerializeField] private bool _zAxisCelestial = false;
        [SerializeField] private CelestialRotationMode _zRotationMode = CelestialRotationMode.Oscillate;
        [SerializeField] private bool _zSyncWithAxis = false;
        [SerializeField] private int _zSyncAxisIndex = 1;
        [SerializeField] private float _zOscillationSpeed = 1.0f;
        [SerializeField] private float _zMinRange = -20.0f;
        [SerializeField] private float _zMaxRange = 20.0f;
        [SerializeField] private float _zRotationSpeed = 0.0f;

        // Internal celestial tracking variables
        private float _xOscillationTime = 0f;
        private float _yOscillationTime = 0f;
        private float _zOscillationTime = 0f;
        private Vector3 _celestialRotation = Vector3.zero;
        private Vector3 _baseWorldRotation = Vector3.zero;

        [SerializeField] private SeasonalData seasonalData;
        private bool useScriptableObject = false;
        
        [Header("Performance Settings")]
        [SerializeField] private float updateFrequency = 30f; // Updates per second
        private float lastUpdateTime = 0f;
        private Vector3 targetRotation;
        private Vector3 previousRotation;
        private bool hasTargetRotation = false;
        
        private float celestialTime = 0f;

        private void Start()
        {
            lastPosition = transform.position;
            _playerCamera = Camera.main;

            // Initialize celestial rotation to current world rotation if in celestial mode
            if (_celestialBodyMode)
            {
                _baseWorldRotation = transform.eulerAngles;
                _celestialRotation = _baseWorldRotation;
            }

            // Apply seasonal settings if using ScriptableObject
            if (seasonalData != null)
            {
                useScriptableObject = true;
                ApplySeasonalSettings();
            }
        }

        void Update()
        {
            if (getSpeedExternally) SetSpeedExternally();

            if (_celestialBodyMode)
            {
                UpdateCelestialBody();
            }
            else
            {
                UpdateRegularRotation();
            }
        }

        public void SetSeasonalData(SeasonalData data)
        {
            seasonalData = data;
            useScriptableObject = (data != null);
            ApplySeasonalSettings();
        }
        
        private void ApplySeasonalSettings()
        {
            if (seasonalData == null)
                return;

            _celestialBodyMode = true;

            // X-Axis Settings
            _xAxisCelestial = seasonalData.xAxis.enabled;
            _xRotationMode = seasonalData.xAxis.rotationMode;
            _xOscillationSpeed = seasonalData.xAxis.oscillationSpeed;
            _xSyncWithAxis = seasonalData.xAxis.syncWithAxis;
            _xSyncAxisIndex = seasonalData.xAxis.syncAxisIndex;
            _xRotationSpeed = seasonalData.xAxis.rotationSpeed;
            _xMinRange = seasonalData.xAxis.minRange;
            _xMaxRange = seasonalData.xAxis.maxRange;

            // Y-Axis Settings
            _yAxisCelestial = seasonalData.yAxis.enabled;
            _yRotationMode = seasonalData.yAxis.rotationMode;
            _yOscillationSpeed = seasonalData.yAxis.oscillationSpeed;
            _ySyncWithAxis = seasonalData.yAxis.syncWithAxis;
            _ySyncAxisIndex = seasonalData.yAxis.syncAxisIndex;
            _yRotationSpeed = seasonalData.yAxis.rotationSpeed;
            _yMinRange = seasonalData.yAxis.minRange;
            _yMaxRange = seasonalData.yAxis.maxRange;

            // Disable Z-axis for celestial lights
            _zAxisCelestial = false;

            // Reset oscillation times
            _xOscillationTime = 0f;
            _yOscillationTime = 0f;
            _zOscillationTime = 0f;
            
            // Reset celestial time when applying new settings
            celestialTime = 0f;

            // Update base rotation
            if (_celestialBodyMode)
            {
                _baseWorldRotation = transform.eulerAngles;
                _celestialRotation = _baseWorldRotation;
            }
        }

        private void UpdateCelestialBody()
        {
            // Always update celestial time at consistent rate
            celestialTime += Time.deltaTime;
    
            // Calculate the target rotation using consistent time
            CalculateTargetRotation();
    
            // Only apply rotation at specified frequency
            if (Time.time - lastUpdateTime >= (1f / updateFrequency))
            {
                ApplyRotation();
                lastUpdateTime = Time.time;
            }
            else if (hasTargetRotation)
            {
                // Smooth interpolation between updates for visual smoothness
                float timeSinceLastUpdate = Time.time - lastUpdateTime;
                float updateInterval = 1f / updateFrequency;
                float t = timeSinceLastUpdate / updateInterval;
        
                Vector3 smoothRotation = Vector3.Lerp(previousRotation, targetRotation, t);
        
                if (_faceCamera && rotationActive) 
                {
                    Vector3 celestialEuler = smoothRotation;
                    Vector3 targetEuler = _targetRot.eulerAngles;

                    Vector3 finalEuler = celestialEuler;

                    if (!_xAxisCelestial)
                        finalEuler.x = targetEuler.x;
                    if (!_yAxisCelestial)
                        finalEuler.y = targetEuler.y;
                    if (!_zAxisCelestial)
                        finalEuler.z = targetEuler.z;

                    transform.eulerAngles = finalEuler;
                }
                else if (rotationActive)
                {
                    transform.eulerAngles = smoothRotation;
                }
            }
        }
        
        private void CalculateTargetRotation()
        {
            if (_faceCamera) 
            {
                FaceCameraSpin();
            }

            // Calculate synchronized speeds using celestial time
            float xEffectiveOscillationSpeed = GetEffectiveOscillationSpeed(0, _xSyncWithAxis, _xSyncAxisIndex, _xOscillationSpeed);
            float yEffectiveOscillationSpeed = GetEffectiveOscillationSpeed(1, _ySyncWithAxis, _ySyncAxisIndex, _yOscillationSpeed);

            // Use celestial time for consistent calculations
            float xOscillationTime = celestialTime * xEffectiveOscillationSpeed;
            float yOscillationTime = celestialTime * yEffectiveOscillationSpeed;

            Vector3 newWorldRotation = _baseWorldRotation;

            // Handle X-axis celestial movement (Elevation in world space)
            if (_xAxisCelestial)
            {
                if (_xRotationMode == CelestialRotationMode.Oscillate)
                {
                    float oscillationValue = Mathf.Sin(xOscillationTime) * 0.5f + 0.5f;
                    float targetX = Mathf.Lerp(_xMinRange, _xMaxRange, oscillationValue);
                    
                    // Apply additional continuous rotation if specified
                    if (Mathf.Abs(_xRotationSpeed) > 0.001f)
                    {
                        float degreesPerSecond = (_xRotationSpeed * 360f) / 3600f;
                        float additionalRotation = degreesPerSecond * celestialTime;
                        newWorldRotation.x = targetX + additionalRotation;
                    }
                    else
                    {
                        newWorldRotation.x = targetX;
                    }
                }
                else // Continuous rotation
                {
                    // Convert rotations per hour to degrees per second
                    float degreesPerSecond = (_xRotationSpeed * 360f) / 3600f;
                    float totalRotation = degreesPerSecond * celestialTime;
                    totalRotation = totalRotation % 360f;
                    if (totalRotation < 0) totalRotation += 360f;
                    newWorldRotation.x = _baseWorldRotation.x + totalRotation;
                }
            }

            // Handle Y-axis celestial movement (Azimuth in world space)
            if (_yAxisCelestial)
            {
                if (_yRotationMode == CelestialRotationMode.Oscillate)
                {
                    float oscillationValue = Mathf.Sin(yOscillationTime) * 0.5f + 0.5f;
                    float targetY = Mathf.Lerp(_yMinRange, _yMaxRange, oscillationValue);
                    
                    // Apply additional continuous rotation if specified
                    if (Mathf.Abs(_yRotationSpeed) > 0.001f)
                    {
                        float degreesPerSecond = (_yRotationSpeed * 360f) / 3600f;
                        float additionalRotation = degreesPerSecond * celestialTime;
                        newWorldRotation.y = targetY + additionalRotation;
                    }
                    else
                    {
                        newWorldRotation.y = targetY;
                    }
                }
                else // Continuous rotation
                {
                    // Convert rotations per hour to degrees per second
                    float degreesPerSecond = (_yRotationSpeed * 360f) / 3600f;
                    float totalRotation = degreesPerSecond * celestialTime;
                    totalRotation = totalRotation % 360f;
                    if (totalRotation < 0) totalRotation += 360f;
                    newWorldRotation.y = _baseWorldRotation.y + totalRotation;
                }
            }

            // Handle Z-axis celestial movement (Roll in world space) - kept for compatibility
            if (_zAxisCelestial)
            {
                float zEffectiveOscillationSpeed = GetEffectiveOscillationSpeed(2, _zSyncWithAxis, _zSyncAxisIndex, _zOscillationSpeed);
                float zOscillationTime = celestialTime * zEffectiveOscillationSpeed;
                
                if (_zRotationMode == CelestialRotationMode.Oscillate)
                {
                    float oscillationValue = Mathf.Sin(zOscillationTime) * 0.5f + 0.5f;
                    float targetZ = Mathf.Lerp(_zMinRange, _zMaxRange, oscillationValue);
                    
                    if (Mathf.Abs(_zRotationSpeed) > 0.001f)
                    {
                        float degreesPerSecond = (_zRotationSpeed * 360f) / 3600f;
                        float additionalRotation = degreesPerSecond * celestialTime;
                        newWorldRotation.z = targetZ + additionalRotation;
                    }
                    else
                    {
                        newWorldRotation.z = targetZ;
                    }
                }
                else // Continuous rotation
                {
                    float degreesPerSecond = (_zRotationSpeed * 360f) / 3600f;
                    float totalRotation = degreesPerSecond * celestialTime;
                    totalRotation = totalRotation % 360f;
                    if (totalRotation < 0) totalRotation += 360f;
                    newWorldRotation.z = _baseWorldRotation.z + totalRotation;
                }
            }

            targetRotation = newWorldRotation;
            hasTargetRotation = true;
        }
        
        private void ApplyRotation()
        {
            if (!hasTargetRotation) return;

            // Store previous rotation for interpolation
            previousRotation = transform.eulerAngles;

            if (_faceCamera && rotationActive) 
            {
                Vector3 celestialEuler = targetRotation;
                Vector3 targetEuler = _targetRot.eulerAngles;

                Vector3 finalEuler = celestialEuler;

                if (!_xAxisCelestial)
                    finalEuler.x = targetEuler.x;
                if (!_yAxisCelestial)
                    finalEuler.y = targetEuler.y;
                if (!_zAxisCelestial)
                    finalEuler.z = targetEuler.z;

                transform.eulerAngles = finalEuler;
                previousRotation = finalEuler; // Update previous rotation to actual applied rotation
            }
            else if (rotationActive)
            {
                transform.eulerAngles = targetRotation;
                previousRotation = targetRotation; // Update previous rotation to actual applied rotation
            }
        }

        private float GetEffectiveOscillationSpeed(int currentAxis, bool syncEnabled, int syncAxisIndex, float manualSpeed)
        {
            if (!syncEnabled)
                return manualSpeed;

            float syncAxisRotationSpeed = 0f;
            CelestialRotationMode syncAxisMode = CelestialRotationMode.Oscillate;

            switch (syncAxisIndex)
            {
                case 0: // X-axis
                    syncAxisRotationSpeed = _xRotationSpeed;
                    syncAxisMode = _xRotationMode;
                    break;
                case 1: // Y-axis
                    syncAxisRotationSpeed = _yRotationSpeed;
                    syncAxisMode = _yRotationMode;
                    break;
                case 2: // Z-axis (kept for compatibility)
                    syncAxisRotationSpeed = _zRotationSpeed;
                    syncAxisMode = _zRotationMode;
                    break;
            }

            // Only sync with continuous rotation axes
            if (syncAxisMode != CelestialRotationMode.Continuous || Mathf.Abs(syncAxisRotationSpeed) < 0.001f)
                return manualSpeed;

            // Convert rotations per hour to oscillations per second
            // If Y-axis does 24 rotations per hour, we want 1 complete oscillation per Y rotation
            // So oscillation frequency = rotations per hour / 3600 seconds per hour * 2π (for sine wave)
            float rotationsPerSecond = syncAxisRotationSpeed / 3600f; // Convert rotations/hour to rotations/second
            float oscillationsPerSecond = rotationsPerSecond * Mathf.PI * 2f; // One complete sine cycle per rotation
    
            return oscillationsPerSecond;
        }
        
        private void UpdateRegularRotation()
        {
            if (_faceCamera) 
            {
                FaceCameraSpin();
            }

            if (_xAxisRot)
            {
                if (!getSpeedExternally)
                {
                    _spinRotation = new Vector3(_rotationSpeed * Time.deltaTime, 0, 0);
                }
                else
                {
                    _spinRotation = new Vector3(_rotationSpeed, 0, 0);
                }
            }
            else if (_yAxisRot)
            {
                if (!getSpeedExternally)
                {
                    _spinRotation = new Vector3(0, _rotationSpeed * Time.deltaTime, 0);
                }
                else
                {
                    _spinRotation = new Vector3(0, _rotationSpeed, 0);
                }
            }
            else if (_zAxisRot)
            {
                if (!getSpeedExternally)
                {
                    _spinRotation = new Vector3(0, 0, _rotationSpeed * Time.deltaTime);
                }
                else
                {
                    _spinRotation = new Vector3(0, 0, _rotationSpeed);
                }
            }
            else
            {
                _spinRotation = Vector3.zero;
            }

            if (_faceCamera && rotationActive) 
            {
                transform.rotation = _targetRot;
            }
            else if(rotationActive)
            {
                transform.Rotate(_spinRotation, Space.Self);
            }
        }

        void SetSpeedExternally()
        {
            Vector3 currentPosition = transform.position;
            float distanceThisFrame = Vector3.Distance(currentPosition, lastPosition);

            float wheelCircumference = Mathf.PI * 2.0f * wheelRadius;
            float rotationAngle = (distanceThisFrame / wheelCircumference) * 360.0f;

            _rotationSpeed = rotationAngle;
            lastPosition = currentPosition;
        }

        void FaceCameraSpin()
        {
            Vector3 camDirection = _playerCamera.transform.position - transform.position;

            Quaternion fullTargetRotation = Quaternion.LookRotation(camDirection, Vector3.up);

            Vector3 currentEulerAngles = transform.rotation.eulerAngles;
            Vector3 targetEulerAngles = fullTargetRotation.eulerAngles;

            Vector3 newEulerAngles = currentEulerAngles;

            if (_celestialBodyMode)
            {
                if (!_xAxisCelestial)
                    newEulerAngles.x = targetEulerAngles.x;
                if (!_yAxisCelestial)
                    newEulerAngles.y = targetEulerAngles.y;
                if (!_zAxisCelestial)
                    newEulerAngles.z = targetEulerAngles.z;
            }
            else
            {
                if (_xAxisRot)
                    newEulerAngles.x = targetEulerAngles.x;
                if (_yAxisRot)
                    newEulerAngles.y = targetEulerAngles.y;
                if (_zAxisRot)
                    newEulerAngles.z = targetEulerAngles.z;
            }

            _targetRot = Quaternion.Euler(newEulerAngles);
        }

        // Public methods for runtime control
        public void SetCelestialMode(bool enabled)
        {
            _celestialBodyMode = enabled;
            if (enabled)
            {
                _baseWorldRotation = transform.eulerAngles;
                _celestialRotation = _baseWorldRotation;
            }
        }

        public void SetRotationMode(int axis, CelestialRotationMode mode)
        {
            switch (axis)
            {
                case 0: _xRotationMode = mode; break;
                case 1: _yRotationMode = mode; break;
                case 2: _zRotationMode = mode; break;
            }
        }
        
        public void SetSyncMode(int axis, bool enabled, int syncWithAxis)
        {
            switch (axis)
            {
                case 0: 
                    _xSyncWithAxis = enabled; 
                    _xSyncAxisIndex = syncWithAxis; 
                    break;
                case 1: 
                    _ySyncWithAxis = enabled; 
                    _ySyncAxisIndex = syncWithAxis; 
                    break;
                case 2: 
                    _zSyncWithAxis = enabled; 
                    _zSyncAxisIndex = syncWithAxis; 
                    break;
            }
        }

        public void SetOscillationSpeed(int axis, float speed)
        {
            switch (axis)
            {
                case 0: _xOscillationSpeed = speed; break;
                case 1: _yOscillationSpeed = speed; break;
                case 2: _zOscillationSpeed = speed; break;
            }
        }

        public void SetOscillationRange(int axis, float min, float max)
        {
            switch (axis)
            {
                case 0: 
                    _xMinRange = min; 
                    _xMaxRange = max; 
                    break;
                case 1: 
                    _yMinRange = min; 
                    _yMaxRange = max; 
                    break;
                case 2: 
                    _zMinRange = min; 
                    _zMaxRange = max; 
                    break;
            }
        }
    }
}
