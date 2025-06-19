using UnityEngine;

namespace Sol
{
    public class GravityController : MonoBehaviour, IPlayerComponent, IGravityController
    {
                [Header("Gravity Settings")]
        [SerializeField] private float _defaultGravityScale = 1.0f;
        [SerializeField] private Vector3 _defaultGravityDirection = Vector3.down;
        [SerializeField] private float _defaultGravityStrength = 9.81f;
        
        [Header("Fall Settings")]
        [SerializeField] private float _fallMultiplier = 2.5f; // Faster falling
        [SerializeField] private float _terminalVelocity = -20f; // Maximum fall speed
        
        [Header("Debug")]
        [SerializeField] private bool _debugGravity = false;
        [SerializeField] private float _currentGravityScale;
        [SerializeField] private Vector3 _currentGravity;
        [SerializeField] private bool _gravityEnabled = true;
        
        // Dependencies
        private IPlayerContext _context;
        private IStatsService _statsService;
        private IGroundChecker _groundChecker;
        private Rigidbody _rigidbody;
        
        // State
        private bool _isActive;
        private Vector3 _customGravity;
        private bool _useCustomGravity;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _statsService = context.GetService<IStatsService>();
            _groundChecker = context.GetService<IGroundChecker>();
            _rigidbody = GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
            {
                Debug.LogError("No Rigidbody found on the GameObject. Gravity won't work!");
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            
            // Initialize gravity
            _currentGravityScale = _defaultGravityScale;
            _currentGravity = _defaultGravityDirection.normalized * _defaultGravityStrength;
            
            // Make sure the rigidbody doesn't use Unity's built-in gravity
            _rigidbody.useGravity = false;
            
            // Activate immediately
            _isActive = true;
            
            Debug.Log("GravityController initialized");
        }
        
        public bool CanBeActivated()
        {
            // Gravity should always be active
            return true;
        }
        
        public void OnActivate()
        {
            _isActive = true;
            if (_debugGravity)
            {
                Debug.Log("GravityController activated");
            }
        }
        
        public void OnDeactivate()
        {
            _isActive = false;
            if (_debugGravity)
            {
                Debug.Log("GravityController deactivated");
            }
        }
        
        private void FixedUpdate()
        {
            // Always process gravity, regardless of _isActive
            // This ensures gravity is always applied
            if (_gravityEnabled)
            {
                ProcessGravity();
            }
        }
        
        public void ProcessGravity()
        {
            if (_rigidbody == null || !_gravityEnabled) return;
            
            // Get gravity multiplier from stats if available
            float gravityMultiplier = 1.0f;
            if (_statsService != null)
            {
                gravityMultiplier = _statsService.GetStat("GravityMultiplier");
            }
            
            // Don't apply gravity when grounded and not moving upward
            bool isGrounded = _groundChecker != null ? _groundChecker.IsGrounded : 
                            _context.GetStateValue<bool>("IsGrounded", false);
            
            if (isGrounded && _rigidbody.linearVelocity.y <= 0)
            {
                // Apply a small downward force to keep the player grounded
                _rigidbody.AddForce(Vector3.down * 5f, ForceMode.Acceleration);
                return;
            }
            
            // Calculate gravity force
            Vector3 gravityForce;
            
            if (_useCustomGravity)
            {
                gravityForce = _customGravity * _currentGravityScale;
            }
            else
            {
                // Apply different multiplier when falling
                float currentMultiplier = _rigidbody.linearVelocity.y < 0 ? gravityMultiplier : 1.0f;
                
                // Apply gravity with multiplier
                gravityForce = _currentGravity * _currentGravityScale * currentMultiplier;
            }
            
            // Apply gravity force
            _rigidbody.AddForce(gravityForce, ForceMode.Acceleration);
            
            // Update current gravity for debugging
            _currentGravity = _useCustomGravity ? _customGravity : _defaultGravityDirection.normalized * _defaultGravityStrength;
            
            // Clamp to terminal velocity
            float currentTerminalVelocity = _statsService != null ? 
                _statsService.GetStat("TerminalVelocity") : _terminalVelocity;
                
            // Only clamp vertical velocity (simplified from your original code)
            if (_rigidbody.linearVelocity.y < currentTerminalVelocity)
            {
                Vector3 clampedVelocity = _rigidbody.linearVelocity;
                clampedVelocity.y = currentTerminalVelocity;
                _rigidbody.linearVelocity = clampedVelocity;
            }
        }
        
        public void SetGravityScale(float scale)
        {
            _currentGravityScale = Mathf.Max(0.0f, scale);
            
            if (_debugGravity)
            {
                Debug.Log($"Gravity scale set to {_currentGravityScale}");
            }
        }
        
        public void SetCustomGravity(Vector3 gravity)
        {
            _customGravity = gravity;
            _useCustomGravity = true;
            
            if (_debugGravity)
            {
                Debug.Log($"Custom gravity set to {_customGravity}");
            }
        }
        
        public void ResetToDefaultGravity()
        {
            _useCustomGravity = false;
            _currentGravityScale = _defaultGravityScale;
            
            if (_debugGravity)
            {
                Debug.Log("Reset to default gravity");
            }
        }
        
        public float GetCurrentGravityScale()
        {
            return _currentGravityScale;
        }
        
        public Vector3 GetCurrentGravity()
        {
            return _useCustomGravity ? _customGravity : _currentGravity;
        }
        
        public void EnableGravity(bool enable)
        {
            _gravityEnabled = enable;
            
            if (_debugGravity)
            {
                Debug.Log($"Gravity {(_gravityEnabled ? "enabled" : "disabled")}");
            }
        }
    }
}
