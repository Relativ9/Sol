using Unity.Cinemachine;
using UnityEngine;

namespace Sol
{
    public class SpeedVisualizer : MonoBehaviour
    {
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _speedBoostParticles;
        
        [Header("Speed Thresholds")]
        [SerializeField] private float _speedBoostThreshold = 1.5f; // Show effects when speed multiplier exceeds this value
        [SerializeField] private bool _showEffectWhenRunning = true; // Option to always show effect when running
        
        [Header("Camera Effects")]
        [SerializeField] private bool _adjustCameraFOV = true;
        [SerializeField] private float _baseFOV = 60f;
        [SerializeField] private float _maxFOVIncrease = 15f;
        [SerializeField] private float _fovChangeSpeed = 5f;
        [SerializeField] private float _maxSpeedMultiplierForFOV = 2.5f; // At what speed multiplier should we reach max FOV
        
        [Header("Debug")]
        [SerializeField] private float _currentSpeedMultiplier;
        [SerializeField] private bool _isEmissionActive;
    
        private IPlayerContext _playerContext;
        private IStatsService _statsService;
        [SerializeField] private CinemachineCamera _normalCamera;
        private ParticleSystem.EmissionModule _emissionModule;
    
        private void Start()
        {
            // Get the player context
            _playerContext = GetComponent<IPlayerContext>();
            if (_playerContext != null)
            {
                _statsService = _playerContext.GetService<IStatsService>();
            }
            
            // Get main camera
            if (_adjustCameraFOV)
            {
                if (_normalCamera == null)
                {
                    Debug.LogWarning("Main camera not found! FOV adjustment will not work.");
                    _adjustCameraFOV = false;
                }
            }
        
            // Initialize particle system
            if (_speedBoostParticles != null)
            {
                _emissionModule = _speedBoostParticles.emission;
                _emissionModule.enabled = false;
                _isEmissionActive = false;
            }
            else
            {
                Debug.LogWarning("Speed boost particle system not assigned!");
            }
        }
    
        private void Update()
        {
            if (_statsService == null) return;

            UpdateSpeedEffects();
        }

        private void UpdateSpeedEffects()
        {
            // Get current speed multiplier
            _currentSpeedMultiplier = _statsService.GetSpeedMultiplier();
            
            // Check if running (for optional always-on effect when running)
            bool isRunning = _playerContext.GetStateValue<bool>("IsRunning", false);
            
            // Determine if effect should be active
            bool shouldShowEffect = _currentSpeedMultiplier >= _speedBoostThreshold || 
                                   (_showEffectWhenRunning && isRunning);
            
            // Update particle emission state
            if (_speedBoostParticles != null && _isEmissionActive != shouldShowEffect)
            {
                _emissionModule.enabled = shouldShowEffect;
                _isEmissionActive = shouldShowEffect;
                
                // If we're turning on emission and the system isn't playing, play it
                if (shouldShowEffect && !_speedBoostParticles.isPlaying)
                {
                    _speedBoostParticles.Play();
                }
            }
            
            // Update camera FOV if enabled
            if (_adjustCameraFOV && _normalCamera != null)
            {
                // Calculate target FOV based on speed
                float speedRatio = Mathf.Clamp01((_currentSpeedMultiplier - 1f) / (_maxSpeedMultiplierForFOV - 1f));
                float targetFOV = _baseFOV + (_maxFOVIncrease * speedRatio);

                _normalCamera.Lens.FieldOfView = Mathf.Lerp(_normalCamera.Lens.FieldOfView, targetFOV, _fovChangeSpeed * Time.deltaTime);
            }
        }
    }
}
