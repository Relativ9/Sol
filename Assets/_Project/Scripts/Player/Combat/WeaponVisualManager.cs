using UnityEngine;

namespace Sol
{
    public class WeaponVisualManager : MonoBehaviour, IPlayerComponent, IAnimationEventReceiver
    {
        [Header("Weapon Transforms")]
        [SerializeField] private Transform _rightHandMount;
        [SerializeField] private Transform _leftHandMount;
        [SerializeField] private Transform _backMount;
        
        [Header("Weapon Prefabs")]
        [SerializeField] private GameObject _oneHandedWeaponPrefab;
        [SerializeField] private GameObject _twoHandedWeaponPrefab;
        
        [Header("Animation Events")]
        [SerializeField] private string _drawWeaponEvent = "OnDrawWeapon";
        [SerializeField] private string _sheatheWeaponEvent = "OnSheatheWeapon";
        [SerializeField] private bool _debugEvents = true;
        
        // Dependencies
        private IPlayerContext _context;
        private IWeaponService _weaponService;
        private ICombatController _combatController;
        private AnimationEventDispatcher _eventDispatcher;
        
        // State
        private GameObject _currentWeaponInstance;
        private WeaponType _currentWeaponType = WeaponType.None;
        private bool _isWeaponDrawn = false;
        private bool _isTransitioning = false;
        private bool _weaponCreated = false;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _weaponService = context.GetService<IWeaponService>();
            _combatController = context.GetService<ICombatController>();
    
            // Find the animator component
            Animator animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("No Animator found in children! Animation events won't work.");
                return;
            }
    
            // Find or create the animation event dispatcher on the animator's GameObject
            _eventDispatcher = animator.gameObject.GetComponent<AnimationEventDispatcher>();
            if (_eventDispatcher == null)
            {
                _eventDispatcher = animator.gameObject.AddComponent<AnimationEventDispatcher>();
                _eventDispatcher.Initialize(context);
                Debug.Log($"Added AnimationEventDispatcher to {animator.gameObject.name}");
            }
    
            // Register for animation events
            _eventDispatcher.RegisterReceiver(this, _drawWeaponEvent);
            _eventDispatcher.RegisterReceiver(this, _sheatheWeaponEvent);
            
            // Create default mounts if not assigned
            if (_rightHandMount == null)
            {
                GameObject rightHand = new GameObject("RightHandMount");
                _rightHandMount = rightHand.transform;
                _rightHandMount.SetParent(transform);
                _rightHandMount.localPosition = new Vector3(0.2f, 0, 0.3f);
            }
            
            if (_backMount == null)
            {
                GameObject back = new GameObject("BackMount");
                _backMount = back.transform;
                _backMount.SetParent(transform);
                _backMount.localPosition = new Vector3(0, 0, -0.2f);
            }
            
            _isWeaponDrawn = _combatController != null && _combatController.IsWeaponDrawn();
        
            // Create the weapon in the correct initial position (should be on back)
            PreCreateWeapon();
        
            Debug.Log($"Weapon visual manager initialized. Weapon drawn: {_isWeaponDrawn}");
            // Debug.Log("Weapon visual manager initialized");
        }

        private void PreCreateWeapon()
        {
            // Only do this if we haven't created a weapon yet
            if (!_weaponCreated && _weaponService != null)
            {
                WeaponType weaponType = _weaponService.GetEquippedWeaponType();

                // If we have a valid weapon type, pre-create it
                if (weaponType != WeaponType.None)
                {
                    // Store the weapon type
                    _currentWeaponType = weaponType;

                    // Determine which prefab to use
                    GameObject prefab = null;
                    switch (weaponType)
                    {
                        case WeaponType.OneHanded:
                            prefab = _oneHandedWeaponPrefab;
                            break;
                        case WeaponType.TwoHanded:
                            prefab = _twoHandedWeaponPrefab;
                            break;
                        // Add other weapon types as needed
                    }

                    // Create the weapon on the appropriate mount
                    if (prefab != null)
                    {
                        // Always start on back mount since we're starting sheathed
                        Transform mountPoint = _backMount;

                        _currentWeaponInstance = Instantiate(prefab, mountPoint);
                        _currentWeaponInstance.transform.localPosition = Vector3.zero;
                        _currentWeaponInstance.transform.localRotation = Quaternion.identity;

                        _weaponCreated = true;

                        Debug.Log($"Pre-created weapon of type {weaponType} on back mount");
                    }
                }
            }
        }

        private void Update()
        {
            // Only check for state changes if we're not in a transition
            if (!_isTransitioning)
            {
                // Check if weapon state has changed
                bool weaponDrawn = _combatController != null && _combatController.IsWeaponDrawn();
                WeaponType weaponType = _weaponService != null ? _weaponService.GetEquippedWeaponType() : WeaponType.None;
                
                // If weapon type changed, update immediately
                if (weaponType != _currentWeaponType)
                {
                    UpdateWeaponType(weaponType);
                }
                
                // If draw state changed, mark as transitioning (weapon will be moved by animation events)
                if (weaponDrawn != _isWeaponDrawn)
                {
                    _isTransitioning = true;
                    _isWeaponDrawn = weaponDrawn;
                    
                    if (_debugEvents)
                    {
                        Debug.Log($"Weapon transitioning to {(weaponDrawn ? "drawn" : "sheathed")} state");
                    }
                }
            }
        }
        
        private void UpdateWeaponType(WeaponType weaponType)
        {
            // Store new weapon type
            _currentWeaponType = weaponType;
            
            // Destroy current weapon instance if it exists
            if (_currentWeaponInstance != null)
            {
                Destroy(_currentWeaponInstance);
                _currentWeaponInstance = null;
            }
            
            // If no weapon or no weapon service, nothing to show
            if (weaponType == WeaponType.None || _weaponService == null)
            {
                return;
            }
            
            // Create new weapon instance based on type
            GameObject prefab = null;
            
            switch (weaponType)
            {
                case WeaponType.OneHanded:
                    prefab = _oneHandedWeaponPrefab;
                    break;
                case WeaponType.TwoHanded:
                    prefab = _twoHandedWeaponPrefab;
                    break;
                // Add other weapon types as needed
            }
            
            // Instantiate the weapon if we have a prefab
            if (prefab != null)
            {
                // Determine initial mount point based on current state
                Transform mountPoint = _isWeaponDrawn ? _rightHandMount : _backMount;
                
                _currentWeaponInstance = Instantiate(prefab, mountPoint);
                _currentWeaponInstance.transform.localPosition = Vector3.zero;
                _currentWeaponInstance.transform.localRotation = Quaternion.identity;
                
                Debug.Log($"Created weapon visual: {weaponType} at {(_isWeaponDrawn ? "hand" : "back")}");
            }
        }
        
        // IAnimationEventReceiver implementation
        public void OnAnimationEvent(string eventName)
        {
            if (_debugEvents)
            {
                Debug.Log($"Weapon visual manager received animation event: {eventName}");
            }
            
            // Make sure the weapon exists before trying to move it
            if (_currentWeaponInstance == null && eventName == _drawWeaponEvent)
            {
                Debug.Log("Weapon doesn't exist yet, creating it now");
                PreCreateWeapon();
            }
            
            if (eventName == _drawWeaponEvent)
            {
                // Move weapon to hand
                if (_currentWeaponInstance != null)
                {
                    _currentWeaponInstance.transform.SetParent(_rightHandMount);
                    _currentWeaponInstance.transform.localPosition = Vector3.zero;
                    _currentWeaponInstance.transform.localRotation = Quaternion.identity;
                    
                    if (_debugEvents)
                    {
                        Debug.Log("Weapon moved to hand");
                    }
                }
                else
                {
                    Debug.LogWarning("Draw weapon event received but no weapon instance exists!");
                }
                _isTransitioning = false;
            }
            else if (eventName == _sheatheWeaponEvent)
            {
                // Move weapon to back
                if (_currentWeaponInstance != null)
                {
                    _currentWeaponInstance.transform.SetParent(_backMount);
                    _currentWeaponInstance.transform.localPosition = Vector3.zero;
                    _currentWeaponInstance.transform.localRotation = Quaternion.identity;
                    
                    if (_debugEvents)
                    {
                        Debug.Log("Weapon moved to back");
                    }
                }
                _isTransitioning = false;
            }
        
        }
        
        public bool CanBeActivated()
        {
            return true; // Visual manager is always active
        }
        
        public void OnActivate()
        {
            // Nothing special needed
        }
        
        public void OnDeactivate()
        {
            // Clean up weapon instance
            if (_currentWeaponInstance != null)
            {
                Destroy(_currentWeaponInstance);
                _currentWeaponInstance = null;
            }
            
            // Unregister from animation events
            if (_eventDispatcher != null)
            {
                _eventDispatcher.UnregisterAllEvents(this);
            }
        }
        
        // [Header("Weapon Transforms")]
        // [SerializeField] private Transform _rightHandMount;
        // [SerializeField] private Transform _leftHandMount;
        // [SerializeField] private Transform _backMount;
        //
        // [Header("Weapon Prefabs")]
        // [SerializeField] private GameObject _oneHandedWeaponPrefab;
        // [SerializeField] private GameObject _twoHandedWeaponPrefab;
        //
        // // Dependencies
        // private IPlayerContext _context;
        // private IWeaponService _weaponService;
        // private ICombatController _combatController;
        // private AnimationEventDispatcher _eventDispatcher;
        //
        // [Header("Animation Events")]
        // [SerializeField] private string _drawWeaponEvent = "OnDrawWeapon";
        // [SerializeField] private string _sheatheWeaponEvent = "OnSheatheWeapon";
        // [SerializeField] private bool _debugEvents = true;
        //
        // // State
        // private GameObject _currentWeaponInstance;
        // private WeaponType _currentWeaponType = WeaponType.None;
        // private bool _isWeaponDrawn = false;
        // private bool _isTransitioning = false;
        //
        // public void Initialize(IPlayerContext context)
        // {
        //     _context = context;
        //     _weaponService = context.GetService<IWeaponService>();
        //     _combatController = context.GetService<ICombatController>();
        //     
        //     // Find or create the animation event dispatcher
        //     _eventDispatcher = GetComponent<AnimationEventDispatcher>();
        //     if (_eventDispatcher == null)
        //     {
        //         _eventDispatcher = gameObject.AddComponent<AnimationEventDispatcher>();
        //         _eventDispatcher.Initialize(context);
        //     }
        //     
        //     // Register for animation events
        //     _eventDispatcher.RegisterReceiver(this, _drawWeaponEvent);
        //     _eventDispatcher.RegisterReceiver(this, _sheatheWeaponEvent);
        //     
        //     // Create default mounts if not assigned
        //     if (_rightHandMount == null)
        //     {
        //         GameObject rightHand = new GameObject("RightHandMount");
        //         _rightHandMount = rightHand.transform;
        //         _rightHandMount.SetParent(transform);
        //         _rightHandMount.localPosition = new Vector3(0.2f, 0, 0.3f);
        //     }
        //     
        //     if (_backMount == null)
        //     {
        //         GameObject back = new GameObject("BackMount");
        //         _backMount = back.transform;
        //         _backMount.SetParent(transform);
        //         _backMount.localPosition = new Vector3(0, 0, -0.2f);
        //     }
        //     
        //     Debug.Log("Weapon visual manager initialized");
        // }
        //
        // private void Update()
        // {
        //     if (!_isTransitioning)
        //     {
        //         // Check if weapon state has changed
        //         bool weaponDrawn = _combatController != null && _combatController.IsWeaponDrawn();
        //         WeaponType weaponType = _weaponService != null ? _weaponService.GetEquippedWeaponType() : WeaponType.None;
        //         
        //         // If weapon type changed, update immediately
        //         if (weaponType != _currentWeaponType)
        //         {
        //             UpdateWeaponType(weaponType);
        //         }
        //         
        //         // If draw state changed, mark as transitioning (weapon will be moved by animation events)
        //         if (weaponDrawn != _isWeaponDrawn)
        //         {
        //             _isTransitioning = true;
        //             _isWeaponDrawn = weaponDrawn;
        //             
        //             if (_debugEvents)
        //             {
        //                 Debug.Log($"Weapon transitioning to {(weaponDrawn ? "drawn" : "sheathed")} state");
        //             }
        //         }
        //     }
        //     
        //     // // Check if weapon state has changed
        //     // bool weaponDrawn = _combatController != null && _combatController.IsWeaponDrawn();
        //     // WeaponType weaponType = _weaponService != null ? _weaponService.GetEquippedWeaponType() : WeaponType.None;
        //     //
        //     // // Update weapon visuals if state changed
        //     // if (weaponDrawn != _isWeaponDrawn || weaponType != _currentWeaponType)
        //     // {
        //     //     UpdateWeaponVisuals(weaponType, weaponDrawn);
        //     // }
        // }
        //
        // private void UpdateWeaponType(WeaponType weaponType)
        // {
        //     // Store new weapon type
        //     _currentWeaponType = weaponType;
        //     
        //     // Destroy current weapon instance if it exists
        //     if (_currentWeaponInstance != null)
        //     {
        //         Destroy(_currentWeaponInstance);
        //         _currentWeaponInstance = null;
        //     }
        //     
        //     // If no weapon or no weapon service, nothing to show
        //     if (weaponType == WeaponType.None || _weaponService == null)
        //     {
        //         return;
        //     }
        //     
        //     // Create new weapon instance based on type
        //     GameObject prefab = null;
        //     
        //     switch (weaponType)
        //     {
        //         case WeaponType.OneHanded:
        //             prefab = _oneHandedWeaponPrefab;
        //             break;
        //         case WeaponType.TwoHanded:
        //             prefab = _twoHandedWeaponPrefab;
        //             break;
        //         // Add other weapon types as needed
        //     }
        //     
        //     // Instantiate the weapon if we have a prefab
        //     if (prefab != null)
        //     {
        //         // Determine initial mount point based on current state
        //         Transform mountPoint = _isWeaponDrawn ? _rightHandMount : _backMount;
        //         
        //         _currentWeaponInstance = Instantiate(prefab, mountPoint);
        //         _currentWeaponInstance.transform.localPosition = Vector3.zero;
        //         _currentWeaponInstance.transform.localRotation = Quaternion.identity;
        //         
        //         Debug.Log($"Created weapon visual: {weaponType} at {(_isWeaponDrawn ? "hand" : "back")}");
        //     }
        // }
        //
        // private void UpdateWeaponVisuals(WeaponType weaponType, bool isDrawn)
        // {
        //     // Store new state
        //     _isWeaponDrawn = isDrawn;
        //     _currentWeaponType = weaponType;
        //     
        //     // Destroy current weapon instance if it exists
        //     if (_currentWeaponInstance != null)
        //     {
        //         Destroy(_currentWeaponInstance);
        //         _currentWeaponInstance = null;
        //     }
        //     
        //     // If no weapon or no weapon service, nothing to show
        //     if (weaponType == WeaponType.None || _weaponService == null)
        //     {
        //         return;
        //     }
        //     
        //     // Create new weapon instance based on type
        //     GameObject prefab = null;
        //     Transform mountPoint = isDrawn ? _rightHandMount : _backMount;
        //     
        //     switch (weaponType)
        //     {
        //         case WeaponType.OneHanded:
        //             prefab = _oneHandedWeaponPrefab;
        //             break;
        //         case WeaponType.TwoHanded:
        //             prefab = _twoHandedWeaponPrefab;
        //             break;
        //         // Add other weapon types as needed
        //     }
        //     
        //     // Instantiate the weapon if we have a prefab
        //     if (prefab != null)
        //     {
        //         _currentWeaponInstance = Instantiate(prefab, mountPoint);
        //         _currentWeaponInstance.transform.localPosition = Vector3.zero;
        //         _currentWeaponInstance.transform.localRotation = Quaternion.identity;
        //         
        //         Debug.Log($"Created weapon visual: {weaponType} at {(isDrawn ? "hand" : "back")}");
        //     }
        // }
        //
        // public bool CanBeActivated()
        // {
        //     return true; // Visual manager is always active
        // }
        //
        // public void OnActivate()
        // {
        //     // Nothing special needed
        // }
        //
        // public void OnDeactivate()
        // {
        //     // Clean up weapon instance
        //     if (_currentWeaponInstance != null)
        //     {
        //         Destroy(_currentWeaponInstance);
        //         _currentWeaponInstance = null;
        //     }
        // }
        //
        // public void OnAnimationEvent(string eventName)
        // {
        //     if (_debugEvents)
        //     {
        //         Debug.Log($"Weapon visual manager received animation event: {eventName}");
        //     }
        //     
        //     if (eventName == _drawWeaponEvent)
        //     {
        //         // Move weapon to hand
        //         if (_currentWeaponInstance != null)
        //         {
        //             _currentWeaponInstance.transform.SetParent(_rightHandMount);
        //             _currentWeaponInstance.transform.localPosition = Vector3.zero;
        //             _currentWeaponInstance.transform.localRotation = Quaternion.identity;
        //             
        //             if (_debugEvents)
        //             {
        //                 Debug.Log("Weapon moved to hand");
        //             }
        //         }
        //         _isTransitioning = false;
        //     }
        //     else if (eventName == _sheatheWeaponEvent)
        //     {
        //         // Move weapon to back
        //         if (_currentWeaponInstance != null)
        //         {
        //             _currentWeaponInstance.transform.SetParent(_backMount);
        //             _currentWeaponInstance.transform.localPosition = Vector3.zero;
        //             _currentWeaponInstance.transform.localRotation = Quaternion.identity;
        //             
        //             if (_debugEvents)
        //             {
        //                 Debug.Log("Weapon moved to back");
        //             }
        //         }
        //         _isTransitioning = false;
        //     }
        // }
    }
}
