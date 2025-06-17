using UnityEngine;

namespace Sol
{
    public class Jumping : MonoBehaviour, IPlayerComponent
    {
        [Header("Jump Settings")]
        [SerializeField] private float _jumpForce = 5f;
        [SerializeField] private float _jumpCooldown = 0.1f;
        [SerializeField] private int _maxAirJumps = 0; // Double jump, etc.
        [SerializeField] private float _coyoteTime = 0.2f; // Time after leaving ground where you can still jump
        [SerializeField] private float _jumpBufferTime = 0.2f; // Time to buffer a jump input before hitting ground
        
        [Header("Gravity Settings")]
        [SerializeField] private float _fallMultiplier = 2.5f; // Faster falling
        [SerializeField] private float _lowJumpMultiplier = 2f; // For short hops
        [SerializeField] private float _terminalVelocity = -20f; // Maximum fall speed
        
        // Dependencies
        private IPlayerContext _context;
        private IGroundChecker _groundChecker;
        private Rigidbody _rigidbody;
        
        // State
        private bool _isActive;
        private bool _jumpRequested;
        private bool _isJumping;
        private int _airJumpsUsed;
        private float _lastJumpTime;
        private float _lastGroundedTime;
        private float _jumpBufferCounter;
        
        // Special ability states
        private bool _isFloating;
        private float _gravityScale = 1f;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _groundChecker = context.GetService<IGroundChecker>();
            _rigidbody = GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
            {
                Debug.LogError("No Rigidbody found on the GameObject. Jumping won't work!");
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            
            Debug.Log("Jumping behavior initialized");
        }
        
        public bool CanBeActivated()
        {
            bool canJump = !_context.GetStateValue<bool>("IsStunned", false);
            bool isInWater = _context.GetStateValue<bool>("IsInWater", false);
            
            return canJump && !isInWater;
        }
        
        private void Update()
        {
            if (!_isActive) return;
            
            // Track grounded state for coyote time
            bool isGrounded = _groundChecker != null ? _groundChecker.IsGrounded : 
                            _context.GetStateValue<bool>("IsGrounded", false);
            
            // Update timers
            if (isGrounded)
            {
                _lastGroundedTime = Time.time;
                _airJumpsUsed = 0;
            }
            
            // Check for jump input
            bool jumpInput = _context.GetJumpInput();
            
            // Handle jump buffer
            if (jumpInput)
            {
                _jumpBufferCounter = _jumpBufferTime;
            }
            else
            {
                _jumpBufferCounter -= Time.deltaTime;
            }
            
            // Check if we can jump (either grounded or within coyote time)
            bool canCoyoteJump = Time.time - _lastGroundedTime <= _coyoteTime;
            bool canAirJump = !isGrounded && _airJumpsUsed < _maxAirJumps;
            
            // Process jump request
            if (_jumpBufferCounter > 0 && (canCoyoteJump || canAirJump) && Time.time - _lastJumpTime >= _jumpCooldown)
            {
                _jumpRequested = true;
                _jumpBufferCounter = 0;
                
                if (!canCoyoteJump)
                {
                    _airJumpsUsed++;
                }
            }
            
            // Update context state
            _context.SetStateValue("IsJumping", _isJumping);
            _context.SetStateValue("IsInAir", !isGrounded);
            _context.SetStateValue("IsFloating", _isFloating);
        }
        
        private void FixedUpdate()
        {
            if (!_isActive || _rigidbody == null) return;
            
            // Apply custom gravity
            ApplyGravity();
            
            // Process jump if requested
            if (_jumpRequested)
            {
                PerformJump();
                _jumpRequested = false;
            }
        }
        
        private void ApplyGravity()
        {
            // Don't apply gravity when grounded
            bool isGrounded = _groundChecker != null ? _groundChecker.IsGrounded : 
                            _context.GetStateValue<bool>("IsGrounded", false);
            
            if (isGrounded && _rigidbody.linearVelocity.y <= 0)
            {
                // Apply a small downward force to keep the player grounded
                _rigidbody.AddForce(Vector3.down * 5f, ForceMode.Acceleration);
                _isJumping = false;
                return;
            }
            
            // Calculate gravity multiplier
            float gravityMultiplier = 1f;
            
            if (_isFloating)
            {
                // Reduced gravity when floating
                gravityMultiplier = 0.3f;
            }
            else if (_rigidbody.linearVelocity.y < 0)
            {
                // Falling - apply stronger gravity for better feel
                gravityMultiplier = _fallMultiplier;
            }
            else if (_rigidbody.linearVelocity.y > 0 && !_context.GetJumpInput())
            {
                // Rising but jump button released - apply stronger gravity for short hops
                gravityMultiplier = _lowJumpMultiplier;
            }
            
            // Apply gravity with multiplier
            float gravity = Physics.gravity.y * gravityMultiplier * _gravityScale;
            
            // Apply gravity force
            _rigidbody.AddForce(new Vector3(0, gravity, 0), ForceMode.Acceleration);
            
            // Clamp to terminal velocity
            if (_rigidbody.linearVelocity.y < _terminalVelocity)
            {
                _rigidbody.linearVelocity = new Vector3(
                    _rigidbody.linearVelocity.x,
                    _terminalVelocity,
                    _rigidbody.linearVelocity.z
                );
            }
        }
        
        private void PerformJump()
        {
            // Calculate jump velocity
            float jumpVelocity = Mathf.Sqrt(-2f * Physics.gravity.y * _jumpForce);
            
            // Set the Y velocity directly for consistent jump height
            _rigidbody.linearVelocity = new Vector3(
                _rigidbody.linearVelocity.x,
                jumpVelocity,
                _rigidbody.linearVelocity.z
            );
            
            _isJumping = true;
            _lastJumpTime = Time.time;
            
            // Trigger jump event for animations, etc.
            _context.SetStateValue("JumpTriggered", true);
            
            Debug.Log("Jump performed");
        }
        
        // Public methods for special abilities
        
        public void SetFloating(bool isFloating)
        {
            _isFloating = isFloating;
        }
        
        public void SetGravityScale(float scale)
        {
            _gravityScale = Mathf.Max(0.1f, scale);
        }
        
        public void ForceJump(float force = -1)
        {
            float jumpForce = force > 0 ? force : _jumpForce;
            float jumpVelocity = Mathf.Sqrt(-2f * Physics.gravity.y * jumpForce);
            
            _rigidbody.linearVelocity = new Vector3(
                _rigidbody.linearVelocity.x,
                jumpVelocity,
                _rigidbody.linearVelocity.z
            );
            
            _isJumping = true;
            _lastJumpTime = Time.time;
        }
        
        public void OnActivate()
        {
            _isActive = true;
            Debug.Log("Jumping behavior activated");
        }
        
        public void OnDeactivate()
        {
            _isActive = false;
            _jumpRequested = false;
            _isJumping = false;
            _isFloating = false;
            _gravityScale = 1f;
            
            // Reset context states
            _context.SetStateValue("IsJumping", false);
            _context.SetStateValue("IsFloating", false);
            
            Debug.Log("Jumping behavior deactivated");
        }
    }
}
