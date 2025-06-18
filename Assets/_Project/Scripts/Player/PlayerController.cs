using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sol
{
    public class PlayerController : MonoBehaviour, IPlayerContext
    {

        private Rigidbody _playerRb;
        private CapsuleCollider _playerCollider;
        private PlayerInput _playerInput;
        private Camera _mainCamera;

        [HideInInspector] public bool _moveInputDetected = false;
        [HideInInspector] public bool _runInputDetected = false;
        [HideInInspector] public bool _jumpInputDetected = false;
        private Dictionary<string, object> _stateValues = new Dictionary<string, object>();
        private Vector2 _moveInput;
        private Vector2 _lookInput; 
        
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private List<IPlayerComponent> _allComponents = new List<IPlayerComponent>();
        private List<IBaseMovement> _baseMovementState = new List<IBaseMovement>();
        // private List<IBaseJumping> _jumpingComponents = new List<IBaseJumping>();
        private IBaseMovement _currentMovementState;
        private IBaseJumping _jumpingComponent;
        private IGravityController _gravityController;

        [SerializeField] private Vector3 _debugVelDisplay;
        [SerializeField] private Vector2 _debugLookDisplay;

        void Awake()
        {
            if(_playerRb == null) _playerRb = GetComponent<Rigidbody>();
            
            RegisterService<IStatsService>(GetComponent<IStatsService>());
            RegisterService<IGroundChecker>(GetComponent<GroundChecker>());
            RegisterService<IAnimationController>(GetComponent<PlayerAnimationController>());
            RegisterService<ICameraController>(GetComponent<CameraController>());
            
            InitializeComponents();
            
            if (_gravityController != null)
            {
                RegisterService<IGravityController>(_gravityController);
            }
            
            Debug.Log($"Movement states: {_baseMovementState.Count}");
            foreach (var state in _baseMovementState)
            {
                Debug.Log($"- {state.GetType().Name}");
            }
            
        }

        void Update()
        {

            DetermineActiveMovementState();
            _debugVelDisplay = _playerRb.linearVelocity;
        }

        private void DetermineActiveMovementState()
        {
            // Check behaviors in priority order (sorted in InitializeComponents)
            foreach (var state in _baseMovementState)
            {
                if (state.CanBeActivated())
                {
                    if (_currentMovementState != state)
                    {
                        // Deactivate current behavior
                        if (_currentMovementState != null)
                        {
                            _currentMovementState.OnDeactivate();
                            Debug.Log($"Deactivated movement: {_currentMovementState.GetType().Name}");
                        }
                
                        // Activate new behavior
                        _currentMovementState = state;
                        _currentMovementState.OnActivate();
                        Debug.Log($"Activated movement: {_currentMovementState.GetType().Name}");
                    }
                    return;
                }
            }

            // If we get here, no behavior could be activated
            if (_currentMovementState != null)
            {
                _currentMovementState.OnDeactivate();
                Debug.Log($"Deactivated movement: {_currentMovementState.GetType().Name}");
                _currentMovementState = null;
            }
    
            // Also check if jumping component should be activated/deactivated
            if (_jumpingComponent != null)
            {
                // For jumping, we don't need to activate/deactivate here
                // It will be triggered by input events in OnJump
        
                // However, if your jumping component needs to be active to process updates,
                // you could add logic here to activate it based on conditions
        
                // For example:
                // bool canJumpBeActivated = /* your condition */;
                // if (canJumpBeActivated)
                // {
                //     if (_jumpingComponent is MonoBehaviour jumpBehavior)
                //     {
                //         Debug.Log($"Ensuring jump component is active: {jumpBehavior.GetType().Name}");
                //     }
                // }
            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (_currentMovementState != null)
            {
                _currentMovementState.ProcessMovement();
            }
        }

        void InitializeComponents()
        {
            // Find all components implementing our interfaces
            foreach (var component in GetComponentsInChildren<MonoBehaviour>())
            {
                // Check if component implements IPlayerComponent
                if (component is IPlayerComponent playerComponent)
                {
                    _allComponents.Add(playerComponent);
                    playerComponent.Initialize(this);
                
                    // Check if it's a movement behavior
                    if (component is IBaseMovement movementBehavior)
                    {
                        _baseMovementState.Add(movementBehavior);
                        Debug.Log($"Found movement component: {component.GetType().Name}");
                    }
                    
                    // Check if it's a jumping component
                    if (component is IBaseJumping jumpingComponent)
                    {
                        _jumpingComponent = jumpingComponent;
                        Debug.Log($"Found jumping component: {component.GetType().Name}");
                    }
                    
                    // Check if it's a gravity controller
                    if (component is IGravityController gravityController)
                    {
                        _gravityController = gravityController;
                        Debug.Log($"Found gravity controller: {component.GetType().Name}");
                    }
                }
            }

            // Sort movement behaviors by priority if they implement IComparable
            _baseMovementState.Sort((a, b) => {
                if (a is IComparable<IBaseMovement> comparable)
                    return comparable.CompareTo(b);
                return 0;
            });

            // If no jumping component was found, log a warning
            if (_jumpingComponent == null)
            {
                Debug.LogWarning("No jumping component found during initialization!");
            }

            Debug.Log($"Initialized {_allComponents.Count} player components, including {_baseMovementState.Count} movement behaviors");
            
            // Let the DetermineActiveMovementState method handle movement activation
            DetermineActiveMovementState();
            
            // For jumping, we can activate it directly if needed
            if (_jumpingComponent != null && _jumpingComponent is IBaseJumping)
            {
                // The jumping component will be activated when needed through input
                Debug.Log($"Jumping component ready: {(_jumpingComponent as MonoBehaviour).GetType().Name}");
            }
        }
        

        void CheckInputState()
        {
            _moveInputDetected = _moveInput.sqrMagnitude > 0.001f;
        }
        
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
            CheckInputState();
        }
        
        public void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        public void OnRun(InputAction.CallbackContext context)
        {
            // For a button action, we check if it's pressed or released
            _runInputDetected = context.ReadValueAsButton();
            
            // Update the state value so other components can access it
            SetStateValue("IsRunning", _runInputDetected);
            
            if (_debugVelDisplay != null)
            {
                Debug.Log($"Run input: {_runInputDetected}");
            }
        }
        
        public void OnJump(InputAction.CallbackContext context)
        {
            // Only handle the button press event
            if (context.started)
            {
                _jumpInputDetected = true;
                SetStateValue("JumpPressed", true);
        
                // Directly notify the jumping component
                if (_jumpingComponent != null)
                {
                    _jumpingComponent.HandleJumpInput();
                    Debug.Log("Jump input handled by jumping component");
                }
                else
                {
                    Debug.LogWarning("Jump input detected but no jumping component found!");
                }
            }
            else if (context.canceled)
            {
                _jumpInputDetected = false;
                SetStateValue("JumpPressed", false);
            }
        }
        
        private IEnumerator ClearJumpPriority(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetStateValue("JumpPriority", false);
        }

        public bool IsRunning()
        {
            return _runInputDetected && _moveInputDetected;
        }
        
        private void RegisterService<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"Attempted to register null service for type {typeof(T).Name}");
                return;
            }
        
            _services[typeof(T)] = service;
            Debug.Log($"Registered service: {typeof(T).Name}");
        }

        //PlayerContext implementation
        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return service as T;
            }
        
            Debug.LogWarning($"Service not found: {typeof(T).Name}");
            return null;
        }

        public Vector2 GetMovementInput()
        {
            return _moveInput;
        }

        public Vector3 GetCurrentVelocity()
        {
            return _playerRb.linearVelocity;
        }

        public Vector2 GetLookInput()
        {
            return _lookInput;
        }

        public void ApplyMovement(Vector3 movement)
        {
            if (_moveInputDetected)
            {
                _playerRb.linearVelocity = movement;
            }
        }

        public void SetStateValue<T>(string key, T value)
        {
            _stateValues[key] = value;
        }

        public T GetStateValue<T>(string key, T defaultValue)
        {
            if (_stateValues.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public bool GetRunInput()
        {
            return _runInputDetected;
        }

        public bool GetJumpInput()
        {   
            return _jumpInputDetected;
        }
    }
}

