using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Sol
{
    public class WeaponRigController : MonoBehaviour, IPlayerComponent, IAnimationEventReceiver
    {
        [Header("Rig References")]
        [SerializeField] private RigBuilder _rigBuilder;
        [SerializeField] private Rig _drawnWeaponRig;
        [SerializeField] private Rig _sheathedWeaponRig;
        
        [Header("Transition Settings")]
        [SerializeField] private float _transitionSpeed = 5f;
        [SerializeField] private string _drawWeaponEvent = "OnDrawWeapon";
        [SerializeField] private string _sheatheWeaponEvent = "OnSheatheWeapon";
        [SerializeField] private bool _debugRig = true;
        
        // Dependencies
        private IPlayerContext _context;
        private ICombatController _combatController;
        private AnimationEventDispatcher _eventDispatcher;
        
        // State
        private bool _isWeaponDrawn = false;
        private bool _isTransitioning = false;
        private float _targetDrawnWeight = 0f;
        private float _targetSheathedWeight = 1f;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
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
            
            // Initialize rig weights based on initial weapon state
            bool initialWeaponDrawn = _combatController != null && _combatController.IsWeaponDrawn();
            _isWeaponDrawn = initialWeaponDrawn;
            
            if (_drawnWeaponRig != null)
            {
                _drawnWeaponRig.weight = initialWeaponDrawn ? 1f : 0f;
                _targetDrawnWeight = _drawnWeaponRig.weight;
                
                if (_debugRig)
                {
                    Debug.Log($"Initialized drawn weapon rig weight to {_drawnWeaponRig.weight}");
                }
            }
            
            if (_sheathedWeaponRig != null)
            {
                _sheathedWeaponRig.weight = initialWeaponDrawn ? 0f : 1f;
                _targetSheathedWeight = _sheathedWeaponRig.weight;
                
                if (_debugRig)
                {
                    Debug.Log($"Initialized sheathed weapon rig weight to {_sheathedWeaponRig.weight}");
                }
            }
            
            Debug.Log("Weapon Rig Controller initialized");
        }
        
        private void Update()
        {
            // Check if weapon state has changed (only if not already transitioning)
            if (!_isTransitioning && _combatController != null)
            {
                bool weaponDrawn = _combatController.IsWeaponDrawn();
                if (weaponDrawn != _isWeaponDrawn)
                {
                    // State has changed, but wait for animation event to start transition
                    _isTransitioning = true;
                    _isWeaponDrawn = weaponDrawn;
                    
                    if (_debugRig)
                    {
                        Debug.Log($"Weapon state changed to {(weaponDrawn ? "drawn" : "sheathed")}, waiting for animation event");
                    }
                }
            }
            
            // Smoothly transition rig weights
            if (_drawnWeaponRig != null)
            {
                _drawnWeaponRig.weight = Mathf.Lerp(_drawnWeaponRig.weight, _targetDrawnWeight, Time.deltaTime * _transitionSpeed);
            }
            
            if (_sheathedWeaponRig != null)
            {
                _sheathedWeaponRig.weight = Mathf.Lerp(_sheathedWeaponRig.weight, _targetSheathedWeight, Time.deltaTime * _transitionSpeed);
            }
        }
        
        // IAnimationEventReceiver implementation
        public void OnAnimationEvent(string eventName)
        {
            if (_debugRig)
            {
                Debug.Log($"Weapon Rig Controller received animation event: {eventName}");
            }
            
            if (eventName == _drawWeaponEvent)
            {
                // Start transition to drawn state
                _targetDrawnWeight = 1f;
                _targetSheathedWeight = 0f;
                _isTransitioning = false;
                
                if (_debugRig)
                {
                    Debug.Log("Transitioning rig to drawn state");
                }
            }
            else if (eventName == _sheatheWeaponEvent)
            {
                // Start transition to sheathed state
                _targetDrawnWeight = 0f;
                _targetSheathedWeight = 1f;
                _isTransitioning = false;
                
                if (_debugRig)
                {
                    Debug.Log("Transitioning rig to sheathed state");
                }
            }
        }
        
        public bool CanBeActivated()
        {
            return true;
        }
        
        public void OnActivate()
        {
            // Nothing special needed
        }
        
        public void OnDeactivate()
        {
            // Unregister from animation events
            if (_eventDispatcher != null)
            {
                _eventDispatcher.UnregisterAllEvents(this);
            }
        }
    }
}
