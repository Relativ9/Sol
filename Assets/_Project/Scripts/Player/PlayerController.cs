using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sol
{
    // public interface IMovementBehavior
    // {
    //     void Initialize(PlayerController controller);
    //     void ProcessMovement();
    //     bool CanBeActivated();
    //     void OnActivate();
    //     void OnDeactivate();
    // }
    
    public class PlayerController : MonoBehaviour, IPlayerContext
    {

        private Rigidbody _playerRb;
        private CapsuleCollider _playerCollider;
        private PlayerInput _playerInput;
        private Camera _mainCamera;

        [HideInInspector]public bool _moveInputDetected = false;
        private Vector2 _moveInput;
        
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private List<IPlayerComponent> _allComponents = new List<IPlayerComponent>();
        private List<IBaseMovement> _baseMovementState = new List<IBaseMovement>();
        private IBaseMovement _currentMovementState;
        

    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            if(_playerRb == null) _playerRb = GetComponent<Rigidbody>();
            
            RegisterService<IStatsService>(GetComponent<IStatsService>());
            
            
            InitializeComponents();
        }

        void Update()
        {

            DetermineActiveMovementState();
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
                        }
                    
                        // Activate new behavior
                        _currentMovementState = state;
                        _currentMovementState.OnActivate();
                    }
                    return;
                }
            }
        
            // If we get here, no behavior could be activated
            if (_currentMovementState != null)
            {
                _currentMovementState.OnDeactivate();
                _currentMovementState = null;
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
                
                    // Also check if it's a movement behavior
                    if (component is IBaseMovement movementBehavior)
                    {
                        _baseMovementState.Add(movementBehavior);
                    }
                }
            }
        
            // Sort movement behaviors by priority if they implement IComparable
            _baseMovementState.Sort((a, b) => {
                if (a is IComparable<IBaseMovement> comparable)
                    return comparable.CompareTo(b);
                return 0;
            });
        
            Debug.Log($"Initialized {_allComponents.Count} player components, including {_baseMovementState.Count} movement behaviors");
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

        public Vector3 GetCurrentVeolocity()
        {
            return _playerRb.linearVelocity;
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
            throw new System.NotImplementedException();
        }

        public T GetStateValue<T>(string key, T defaultValue)
        {
            throw new System.NotImplementedException();
        }
    }
}

