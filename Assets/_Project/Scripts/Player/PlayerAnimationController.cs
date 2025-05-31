using UnityEngine;

namespace Sol
{
    public class PlayerAnimationController : MonoBehaviour, IAnimationController
    {
                [Header("Animation Parameters")]
        [SerializeField] private string _horizontalParameter = "Horizontal";
        [SerializeField] private string _verticalParameter = "Vertical";
        [SerializeField] private string _speedParameter = "Speed";
        [SerializeField] private string _isGroundedParameter = "IsGrounded";
        
        [Header("Animation Settings")]
        [SerializeField] private float _animationDampTime = 0.1f;
        [SerializeField] private float _inputDeadZone = 0.05f;
        [SerializeField] private float _velocityThreshold = 0.1f;
        [SerializeField] private float _walkMagnitude = 1.0f; // Normalized magnitude for walking
        [SerializeField] private float _runMagnitude = 2.0f;  // Normalized magnitude for running
        [SerializeField] private bool _useDirectionalBlending = true;
        [SerializeField] private bool _useCameraRelativeDirection = true;
        
        [Header("Debug")]
        [SerializeField] private bool _debugAnimation = false;
        [SerializeField] private Vector2 _currentDirectionParams;
        [SerializeField] private float _currentSpeedParam;
        [SerializeField] private bool _isRunningDebug;
        
        private Animator _animator;
        private IPlayerContext _context;
        private ICameraController _cameraController;
        private Transform _cameraTransform;
        
        // Smoothed values
        private Vector2 _smoothedDirection = Vector2.zero;
        private float _smoothedSpeed = 0f;
        private float _directionMagnitude = 0f;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _animator = GetComponentInChildren<Animator>();
            
            // Try to get the camera controller from the context
            _cameraController = context.GetService<ICameraController>();
            
            if (_animator == null)
            {
                Debug.LogError("No Animator component found in children. Animation will not work!");
                return;
            }
            
            // Get camera reference for direction calculations
            if (_useCameraRelativeDirection)
            {
                if (_cameraController != null)
                {
                    _cameraTransform = _cameraController.GetCameraTransform();
                    Debug.Log("Using CameraController for animation direction calculations");
                }
                else
                {
                    _cameraTransform = Camera.main.transform;
                    if (_cameraTransform == null)
                    {
                        Debug.LogWarning("Main camera not found! Animations will use world space directions.");
                    }
                    else
                    {
                        Debug.Log("Using main camera for animation direction calculations");
                    }
                }
            }
            
            // Initialize animation parameters to zero
            ResetAnimationParameters();
            
            Debug.Log("PlayerAnimationController initialized");
        }
        
        private void ResetAnimationParameters()
        {
            if (_animator == null) return;
            
            _animator.SetFloat(_horizontalParameter, 0f);
            _animator.SetFloat(_verticalParameter, 0f);
            _animator.SetFloat(_speedParameter, 0f);
            
            _smoothedDirection = Vector2.zero;
            _smoothedSpeed = 0f;
            _directionMagnitude = 0f;
            
            _currentDirectionParams = Vector2.zero;
            _currentSpeedParam = 0f;
        }
        
        private void Update()
        {
            if (_animator == null || _context == null) return;
            
            // Get movement data from context
            Vector2 input = _context.GetMovementInput();
            Vector3 velocity = _context.GetCurrentVelocity();
            bool isRunning = _context.GetStateValue<bool>("IsRunning", false);
            _isRunningDebug = isRunning;
            
            // Update animation parameters
            UpdateAnimationParameters(input, velocity, isRunning);
            
            // Update grounded state
            bool isGrounded = _context.GetStateValue<bool>("IsGrounded", true);
            _animator.SetBool(_isGroundedParameter, isGrounded);
            
            // Debug info
            if (_debugAnimation)
            {
                Debug.Log($"Animation Params - Dir: {_currentDirectionParams}, Speed: {_currentSpeedParam}, Running: {isRunning}");
            }
        }
        
        public void UpdateAnimationParameters(Vector2 movementInput, Vector3 velocity, bool isRunning)
        {
            if (_animator == null) return;
            
            // Calculate horizontal velocity magnitude
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            float currentSpeed = horizontalVelocity.magnitude;
            
            // Apply dead zone to input
            Vector2 inputWithDeadZone = movementInput.magnitude < _inputDeadZone ? Vector2.zero : movementInput;
            
            // Determine if we're actually moving
            bool isMoving = currentSpeed > _velocityThreshold && inputWithDeadZone.magnitude > 0;
            
            if (isMoving)
            {
                // Calculate movement direction
                Vector2 movementDirection;
                
                if (_useDirectionalBlending)
                {
                    // IMPORTANT: Always use the raw input direction for animations
                    // This is the key change to fix the animation direction issue
                    movementDirection = inputWithDeadZone.normalized;
                    
                    // Determine the target magnitude based on whether we're running or walking
                    float targetMagnitude = isRunning ? _runMagnitude : _walkMagnitude;
                    
                    // Smooth the direction magnitude
                    _directionMagnitude = Mathf.Lerp(_directionMagnitude, targetMagnitude, Time.deltaTime / _animationDampTime);
                    
                    // Scale the direction by the magnitude to represent walking vs running
                    Vector2 scaledDirection = movementDirection * _directionMagnitude;
                    
                    // Smooth direction values
                    _smoothedDirection = Vector2.Lerp(_smoothedDirection, scaledDirection, Time.deltaTime / _animationDampTime);
                }
                else
                {
                    _smoothedDirection = Vector2.zero;
                }
                
                // Smooth speed value (this is now just for the Speed parameter, not for direction scaling)
                _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, currentSpeed, Time.deltaTime / _animationDampTime);
            }
            else
            {
                // When not moving, gradually reset to zero
                _smoothedDirection = Vector2.Lerp(_smoothedDirection, Vector2.zero, Time.deltaTime / (_animationDampTime * 0.5f));
                _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, 0f, Time.deltaTime / (_animationDampTime * 0.5f));
                _directionMagnitude = Mathf.Lerp(_directionMagnitude, 0f, Time.deltaTime / (_animationDampTime * 0.5f));
                
                // Force to zero if very small
                if (_smoothedDirection.magnitude < 0.01f) _smoothedDirection = Vector2.zero;
                if (_smoothedSpeed < 0.01f) _smoothedSpeed = 0f;
            }
            
            // Apply to animator parameters
            _animator.SetFloat(_horizontalParameter, _smoothedDirection.x);
            _animator.SetFloat(_verticalParameter, _smoothedDirection.y);
            _animator.SetFloat(_speedParameter, _smoothedSpeed);
            
            // Store for debugging
            _currentDirectionParams = _smoothedDirection;
            _currentSpeedParam = _smoothedSpeed;
        }
        
        private Vector2 ConvertToCameraSpace(Vector2 input)
        {
            // If no input or no camera, return zero
            if (input.magnitude < 0.1f || _cameraTransform == null)
                return Vector2.zero;
            
            // Get camera forward and right vectors (ignore Y)
            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;
            
            camForward.y = 0;
            camRight.y = 0;
            
            if (camForward.magnitude < 0.01f)
                return Vector2.zero;
                
            camForward.Normalize();
            camRight.Normalize();
            
            // Project input onto camera space
            Vector3 moveDir = camRight * input.x + camForward * input.y;
            
            // Convert to a normalized 2D vector for the blend tree
            if (moveDir.magnitude > 0.01f)
            {
                moveDir.Normalize();
                return new Vector2(moveDir.x, moveDir.z);
            }
            
            return Vector2.zero;
        }
        
        public void SetTrigger(string triggerName)
        {
            if (_animator != null)
            {
                _animator.SetTrigger(triggerName);
            }
        }
        
        public void SetBool(string paramName, bool value)
        {
            if (_animator != null)
            {
                _animator.SetBool(paramName, value);
            }
        }
        
        public void SetFloat(string paramName, float value)
        {
            if (_animator != null)
            {
                _animator.SetFloat(paramName, value);
            }
        }
        
        public void ResetTrigger(string paramName)
        {
            if (_animator != null)
            {
                _animator.ResetTrigger(paramName);
            }
        }
        
        public void SetInteger(string paramName, int value)
        {
            if (_animator != null)
            {
                _animator.SetInteger(paramName, value);
            }
        }
        
        // Called when the component is disabled
        private void OnDisable()
        {
            if (_animator != null)
            {
                ResetAnimationParameters();
            }
        }
        
        public void UpdateAnimationParameters(Vector2 movementInput, Vector3 velocity)
        {
            // Get the running state from context
            bool isRunning = _context.GetStateValue<bool>("IsRunning", false);
            UpdateAnimationParameters(movementInput, velocity, isRunning);
        }
        
        // IPlayerComponent implementation
        public void OnActivate()
        {
            // Nothing to do here
        }
        
        public void OnDeactivate()
        {
            ResetAnimationParameters();
        }
        
        public bool CanBeActivated()
        {
            return true;
        }
    }
    
}
