using UnityEngine;

namespace Sol
{
    public class Movement : MonoBehaviour, IPlayerComponent, IBaseMovement
    {
                [Header("Movement Settings")]
        [SerializeField] private float _defaultSpeed = 3f;
        [SerializeField] private float _defaultDeceleration = 20f;
        [SerializeField] private bool _debugMovement = true;
        
        [Header("Running Settings")]
        [SerializeField] private string _runModifierSourceId = "PlayerRunning"; // Unique ID for run modifier
        [SerializeField] private float _speedTransitionRate = 5f; // How quickly to transition between speeds
        
        [Header("Direction Modifiers")]
        [SerializeField] private float _backwardSpeedMultiplier = 0.5f; // Multiplier for backward movement
        
        [Header("Rotation Settings")]
        [SerializeField] private bool _rotateWithCamera = true;
        [SerializeField] private float _rotationSpeed = 10f;
        
        [Header("Air Movement")]
        [SerializeField] private float _airDeceleration = 0.5f; // Much lower deceleration in air
        
        // Dependencies
        private IPlayerContext _context;
        private IStatsService _statsService;
        private IGroundChecker _groundChecker;
        private ICameraController _cameraController;
        private Rigidbody _rigidbody;
        
        // State
        private Vector3 _moveDirection = Vector3.zero;
        private Vector2 _rawInput = Vector2.zero;
        private bool _isActive;
        private bool _hasMoveInput;
        private bool _isRunning;
        private bool _isJumping;
        private float _currentSpeed; // Current interpolated speed
        private bool _isMovingBackward;
        private float _lastCameraYaw; // Store the last camera yaw to detect changes
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _statsService = context.GetService<IStatsService>();
            _groundChecker = context.GetService<IGroundChecker>();
            _cameraController = context.GetService<ICameraController>();
            _rigidbody = GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
            {
                Debug.LogError("No Rigidbody found on the GameObject. Deceleration won't work!");
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            
            // Use CameraController if available, otherwise fall back to main camera
            if (_cameraController == null)
            {
                Debug.LogWarning("CameraController not found! Falling back to main camera.");
            }
            else
            {
                Debug.Log("Using CameraController for movement direction");
                // Initialize last camera yaw
                _lastCameraYaw = _cameraController.GetCameraYaw();
            }
            
            _currentSpeed = _defaultSpeed;
            
            Debug.Log("Movement behavior initialized");
        }
        
        public bool CanBeActivated()
        {
            // Only activate when grounded
            bool isGrounded = _groundChecker != null ? _groundChecker.IsGrounded :
                            _context.GetStateValue<bool>("IsGrounded", false);
            bool canMove = !_context.GetStateValue<bool>("IsStunned", false);
            bool isInWater = _context.GetStateValue<bool>("IsInWater", false);
            
            return isGrounded && canMove && !isInWater;
        }
        
        private void Update()
        {
            // Always update rotation with camera, regardless of movement input or active state
            if (_rotateWithCamera && _cameraController != null)
            {
                UpdateRotation();
            }
            
            if (!_isActive) return;
            
            // Get input and calculate direction in Update for responsive controls
            _rawInput = _context.GetMovementInput();
            _hasMoveInput = _rawInput.magnitude > 0.1f;
            
            // Check if moving backward
            _isMovingBackward = _rawInput.y < -0.1f;
            
            // Check if running
            bool wasRunning = _isRunning;
            _isRunning = _context.GetRunInput() && _hasMoveInput;
            
            // Update the context state
            _context.SetStateValue("IsRunning", _isRunning);
            _context.SetStateValue("IsMoving", _hasMoveInput);
            _context.SetStateValue("IsJumping", _isJumping);
            
            // Store the raw input in the context for animation
            _context.SetStateValue("MoveInputX", _rawInput.x);
            _context.SetStateValue("MoveInputZ", _rawInput.y);
            
            // Calculate movement speed for animation blending (0 = idle, 0.5 = walk, 1.0 = run)
            float animSpeed = _hasMoveInput ? (_isRunning ? 1.0f : 0.5f) : 0.0f;
            _context.SetStateValue("MovementSpeed", animSpeed);
            
            // Apply or remove running modifier when state changes
            if (_isRunning != wasRunning && _statsService != null)
            {
                UpdateRunningState();
            }
            
            if (_hasMoveInput)
            {
                _moveDirection = CalculateStrafingDirection(_rawInput);
            }
        }
        
        private void UpdateRotation()
        {
            // Get the camera's yaw rotation
            float currentYaw = _cameraController.GetCameraYaw();
            
            // Create rotation based on camera yaw
            Quaternion targetRotation = Quaternion.Euler(0, currentYaw, 0);
            
            // Apply a small amount of smoothing to character rotation
            // This makes the character rotation look natural without affecting camera control
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );
        }
        
        private void UpdateRunningState()
        {
            if (_statsService == null) return;
            
            if (_isRunning)
            {
                // Get the run multiplier from stats
                float runMultiplier = _statsService.GetStat("runMultiplier");
                
                // Create a running modifier
                StatModifier runModifier = new StatModifier(
                    ModifierType.Multiplicative,
                    ModifierCatagory.Temporary,
                    runMultiplier,
                    this,
                    -1f // No duration (will be removed manually)
                );
                
                // Apply the modifier
                _statsService.ApplyOrReplaceModifier(
                    "moveSpeed",
                    runModifier,
                    ModifierCatagory.Temporary,
                    _runModifierSourceId
                );
            }
            else
            {
                // Remove the running modifier
                _statsService.RemoveModifiersFromSource("moveSpeed", _runModifierSourceId);
            }
        }
        
        private void FixedUpdate()
        {
            // Always process movement to handle deceleration in air
            ProcessMovement();
        }
        
        public void ProcessMovement()
        {
            if (_rigidbody == null) return;
            
            // Check if jump has priority
            bool jumpHasPriority = _context.GetStateValue<bool>("JumpPriority", false);
            if (jumpHasPriority) return; // Don't modify velocity during jump priority period
            
            // Check if grounded
            bool isGrounded = _groundChecker != null ? _groundChecker.IsGrounded :
                            _context.GetStateValue<bool>("IsGrounded", false);
            
            // Get current velocity
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            
            // Only process movement input if grounded and active
            if (isGrounded && _isActive)
            {
                // Get target speed from stats service
                float targetSpeed = _statsService != null
                    ? _statsService.GetStat("moveSpeed")
                    : _defaultSpeed;
                
                float deceleration = _statsService != null
                    ? _statsService.GetStat("deceleration")
                    : _defaultDeceleration;
                
                // Smoothly transition to the target speed
                _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.fixedDeltaTime * _speedTransitionRate);
                
                if (_hasMoveInput)
                {
                    // Apply backward movement multiplier if moving backward
                    float speedMultiplier = _isMovingBackward ? _backwardSpeedMultiplier : 1.0f;
                    
                    // Calculate target velocity based on input direction and interpolated speed
                    Vector3 targetVelocity = _moveDirection * (_currentSpeed * speedMultiplier);
                    
                    // Apply the new velocity directly to the rigidbody
                    _rigidbody.linearVelocity = new Vector3(
                        targetVelocity.x,
                        currentVelocity.y,
                        targetVelocity.z
                    );
                }
                else if (horizontalVelocity.magnitude > 0.1f)
                {
                    // Calculate how much to reduce velocity this frame
                    float reductionAmount = deceleration * Time.fixedDeltaTime;
                    float newMagnitude = Mathf.Max(0, horizontalVelocity.magnitude - reductionAmount);
                    
                    if (newMagnitude > 0.1f)
                    {
                        // Apply reduced velocity in the same direction
                        Vector3 deceleratedVelocity = horizontalVelocity.normalized * newMagnitude;
                        
                        _rigidbody.linearVelocity = new Vector3(
                            deceleratedVelocity.x,
                            currentVelocity.y,
                            deceleratedVelocity.z
                        );
                    }
                    else
                    {
                        // Below threshold, stop horizontal movement completely
                        _rigidbody.linearVelocity = new Vector3(0, currentVelocity.y, 0);
                    }
                }
            }
            else if (!isGrounded)
            {
                // In air - apply very minimal deceleration to preserve momentum
                // but still have some air resistance
                if (horizontalVelocity.magnitude > 0.1f)
                {
                    float airReductionAmount = _airDeceleration * Time.fixedDeltaTime;
                    float newMagnitude = Mathf.Max(0, horizontalVelocity.magnitude - airReductionAmount);
                    
                    if (newMagnitude > 0.1f)
                    {
                        // Apply very slight deceleration in air
                        Vector3 deceleratedVelocity = horizontalVelocity.normalized * newMagnitude;
                        
                        _rigidbody.linearVelocity = new Vector3(
                            deceleratedVelocity.x,
                            currentVelocity.y,
                            deceleratedVelocity.z
                        );
                    }
                }
                
                // Update the context with our current state
                _context.SetStateValue("IsInAir", true);
            }
            
            // Update the context with our current state
            _context.SetStateValue("IsInAir", !isGrounded);
        }
        
        private Vector3 CalculateStrafingDirection(Vector2 input)
        {
            // If we have a camera controller, use it
            if (_cameraController != null)
            {
                // Get camera forward and right vectors from the camera controller
                Vector3 forward = _cameraController.GetCameraForward();
                Vector3 right = _cameraController.GetCameraRight();
                
                // Project vectors onto the horizontal plane (ignore Y component)
                forward.y = 0f;
                right.y = 0f;
                
                // Normalize to ensure consistent movement speed
                if (forward.magnitude > 0.01f) forward.Normalize();
                if (right.magnitude > 0.01f) right.Normalize();
                
                // Calculate direction based on input
                Vector3 direction = (forward * input.y + right * input.x);
                
                // Normalize final direction
                if (direction.magnitude > 0.01f)
                {
                    return direction.normalized;
                }
                
                return Vector3.zero;
            }
            
            // Fallback to using the main camera if no camera controller
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Get camera forward and right vectors
                Vector3 forward = mainCamera.transform.forward;
                Vector3 right = mainCamera.transform.right;
                
                // Project vectors onto the horizontal plane (ignore Y component)
                forward.y = 0f;
                right.y = 0f;
                
                // Normalize to ensure consistent movement speed
                if (forward.magnitude > 0.01f) forward.Normalize();
                if (right.magnitude > 0.01f) right.Normalize();
                
                // Calculate direction based on input
                Vector3 direction = (forward * input.y + right * input.x);
                
                // Normalize final direction
                if (direction.magnitude > 0.01f)
                {
                    return direction.normalized;
                }
            }
            
            // Fallback to world coordinates if no camera
            return new Vector3(input.x, 0f, input.y).normalized;
        }
        
        public void OnActivate()
        {
            _isActive = true;
            UpdateRunningState();
            // Reset running state based on current input when reactivated
            Debug.Log("Movement behavior activated");
        }
        
        public void OnDeactivate()
        {
            _isActive = false;
            _moveDirection = Vector3.zero;
            _hasMoveInput = false;
            _isRunning = false;
            
            // Reset state values
            _context.SetStateValue("IsMoving", false);
            _context.SetStateValue("IsRunning", false);
            _context.SetStateValue("MoveInputX", 0f);
            _context.SetStateValue("MoveInputZ", 0f);
            _context.SetStateValue("MovementSpeed", 0f);
            
            // Note: We don't stop the rigidbody when deactivated anymore
            // This allows momentum to be preserved when going airborne
            
            Debug.Log("Movement behavior deactivated");
        }
    }
}
