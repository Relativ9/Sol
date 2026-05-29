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
        
        [Header("Slope Settings")]
        [SerializeField] private float _slopeAlignmentStrength = 1f;   // 0 = flat, 1 = fully slope-aligned
        [SerializeField] private float _maxWalkableSlopeAngle = 45f;   // Steeper than this = slide, not walk
        
        [Header("Ground Stickiness")]
        [SerializeField] private float _groundStickyForce = 1f;        // Downward force while grounded
        [SerializeField] private float _groundStickyMaxUpVelocity = 0.5f; // Only suppress upward vel below this
        [SerializeField] private float _ungroundedDelay = 0.02f;        // Seconds before stickiness turns off
        private float _timeSinceGrounded = 0f;
        
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
        private bool _isGrounded;
        private bool _isRunning;
        private bool _isJumping;
        private float _currentSpeed; // Current interpolated speed
        private bool _isMovingBackward;
        private float _lastCameraYaw; // Store the last camera yaw to detect changes
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _statsService = ServiceLocator.Get<IStatsService>();
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
            _context.SetStateValue("moveSpeed", animSpeed);
            
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
                float runMultiplier = _statsService.GetStat(StatTypeEnum.RunMultiplier);
                
                // Create a running modifier
                StatModifier runMod = new StatModifier(
                    type: ModifierType.PercentAdditive,
                    category: ModifierCategory.Temporary,
                    statType: StatTypeEnum.MoveSpeed,
                    value: runMultiplier,
                    sourceId: _runModifierSourceId,
                    duration: -1f
                );
                
                // Apply the modifier
                _statsService.ApplyOrReplaceModifier(runMod);
            }
            else
            {
                // Remove the running modifier
                _statsService.RemoveModifiersFromSource(_runModifierSourceId);
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
            if (_context.GetStateValue<bool>("JumpPriority", false)) return;
            UpdateGroundedTimeTracking();
            if (_isGrounded && _isActive)
            {
                ProcessGroundedMovement();
                ApplyGroundStickiness();
                UpdateLookAheadProbe();
            }
            else if (!_isGrounded)
            {
                ProcessAirMovement();
            }
            _context.SetStateValue("IsInAir", !_isGrounded);
        }
        
        //State Tracking
        private void UpdateGroundedTimeTracking()
        {
            _isGrounded = _groundChecker != null ? _groundChecker.IsGrounded :
                _context.GetStateValue<bool>("IsGrounded", false);
            bool isGroundedStrict = _groundChecker != null ? _groundChecker.IsGroundedStrict : _isGrounded;
            if (isGroundedStrict)
                _timeSinceGrounded = 0f;
            else
                _timeSinceGrounded += Time.fixedDeltaTime;
        }
        
        //Grounded movement
        private void ProcessGroundedMovement()
        {
            UpdateCurrentSpeed();
            Vector3 groundNormal = GetMovementGroundNormal();
            float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
            if (_hasMoveInput && slopeAngle <= _maxWalkableSlopeAngle)
                ApplyGroundedVelocity(groundNormal);
            else if (!_hasMoveInput)
                ApplyDeceleration();
        }
        
        private void UpdateCurrentSpeed()
        {
            float targetSpeed = _statsService != null ? _statsService.GetStat(StatTypeEnum.MoveSpeed) : _defaultSpeed;
            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.fixedDeltaTime * _speedTransitionRate);
        }
        
        private Vector3 GetMovementGroundNormal()
        {
            if (_groundChecker == null)
                return Vector3.up;
            // Look-ahead sees nothing (void, ledge, too far down). 
            // Trust only what is physically under the player's feet right now.
            if (!_groundChecker.HasLookAheadHit)
                return _groundChecker.GroundHit.normal;
            // Look-ahead sees geometry, but it's a cliff face or un-walkable slide.
            // Don't let Movement blend the player onto it.
            float lookAheadSlope = Vector3.Angle(Vector3.up, _groundChecker.LookAheadHit.normal);
            if (lookAheadSlope > _maxWalkableSlopeAngle)
                return _groundChecker.GroundHit.normal;
            // Look-ahead confirms a continuous walkable ramp ahead.
            // Safe to smooth the transition between current hit and upcoming hit.
            return _groundChecker.GetSmoothedGroundNormal();
            
            // return _groundChecker != null
            //     ? _groundChecker.GetSmoothedGroundNormal()
            //     : Vector3.up;
        }
        
        private void ApplyGroundedVelocity(Vector3 groundNormal)
        {
            float speedMultiplier = _isMovingBackward ? _backwardSpeedMultiplier : 1.0f;
            Vector3 slopeDirection = Vector3.ProjectOnPlane(_moveDirection, groundNormal).normalized;
            Vector3 finalDirection = Vector3.Slerp(_moveDirection, slopeDirection, _slopeAlignmentStrength);
            Vector3 targetVelocity = finalDirection * (_currentSpeed * speedMultiplier);
            float newY = ClampUpwardVelocity(_rigidbody.linearVelocity.y);
            _rigidbody.linearVelocity = new Vector3(
                targetVelocity.x,
                targetVelocity.y != 0 ? targetVelocity.y : newY,
                targetVelocity.z
            );
        }
        
        private float ClampUpwardVelocity(float currentY)
        {
            bool isGroundedStrict = _groundChecker != null ? _groundChecker.IsGroundedStrict : _isGrounded;
            if (currentY > _groundStickyMaxUpVelocity && isGroundedStrict)
                return Mathf.Lerp(currentY, 0f, Time.fixedDeltaTime * 20f);
            return currentY;
        }
        
        private void ApplyDeceleration()
        {
            float deceleration = _statsService != null ? _statsService.GetStat(StatTypeEnum.Deceleration) : _defaultDeceleration;
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            if (horizontalVelocity.magnitude <= 0.1f)
            {
                _rigidbody.linearVelocity = new Vector3(0, currentVelocity.y, 0);
                return;
            }
            float reductionAmount = deceleration * Time.fixedDeltaTime;
            float newMagnitude = Mathf.Max(0, horizontalVelocity.magnitude - reductionAmount);
            Vector3 deceleratedVelocity = newMagnitude > 0.1f
                ? horizontalVelocity.normalized * newMagnitude
                : Vector3.zero;
            _rigidbody.linearVelocity = new Vector3(
                deceleratedVelocity.x,
                currentVelocity.y,
                deceleratedVelocity.z
            );
        }
        
        // --- Stickiness ---
        private void ApplyGroundStickiness()
        {
            if (_timeSinceGrounded <= _ungroundedDelay)
            {
                float slopeAngle = _groundChecker != null 
                    ? _groundChecker.GroundNormalSlope 
                    : 0f;
                if (slopeAngle <= _maxWalkableSlopeAngle)
                {
                    _rigidbody.AddForce(Vector3.down * _groundStickyForce, ForceMode.Acceleration);
                }
            }
        }
        
        //Look ahead, used to calculate movement direction normal.
        private void UpdateLookAheadProbe()
        {
            if (_groundChecker != null && _hasMoveInput)
                _groundChecker.SetLookAheadDirection(_moveDirection);
        }
        
        private void ProcessAirMovement()
        {
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            if (horizontalVelocity.magnitude > 0.1f)
            {
                float newMagnitude = Mathf.Max(0, horizontalVelocity.magnitude - (_airDeceleration * Time.fixedDeltaTime));
                if (newMagnitude > 0.1f)
                {
                    Vector3 deceleratedVelocity = horizontalVelocity.normalized * newMagnitude;
                    _rigidbody.linearVelocity = new Vector3(
                        deceleratedVelocity.x,
                        currentVelocity.y,
                        deceleratedVelocity.z
                    );
                }
            }
            _context.SetStateValue("IsInAir", true);
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
