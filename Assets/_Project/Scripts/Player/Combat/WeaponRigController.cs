using System.Collections;
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
        [SerializeField] private float _transitionDuration = 0.5f;
        [SerializeField] private float _quickTransitionDuration = 0.2f; // For begin events
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Weapon State Events")]
        [SerializeField] private string _drawWeaponEvent = "OnDrawWeapon";
        [SerializeField] private string _sheatheWeaponEvent = "OnSheatheWeapon";
        [SerializeField] private string _beginSheatheEvent = "OnBeginSheathe";
        [SerializeField] private string _beginUnsheatheEvent = "OnBeginUnsheathe";
        
        [Header("Attack Events")]
        [SerializeField] private string _attackStartEvent = "OnAttackStart";
        [SerializeField] private string _attackEndEvent = "OnAttackEnd";
        [SerializeField] private float _attackTransitionDuration = 0.1f; // Fast transition for attacks
        
        [SerializeField] private bool _debugRig = true;
        
        // Dependencies
        private IPlayerContext _context;
        private ICombatController _combatController;
        private AnimationEventDispatcher _eventDispatcher;
        
        // State
        private bool _isWeaponDrawn = false;
        private bool _isTransitioning = false;
        private bool _isAttacking = false;
        private float _preAttackDrawnRigWeight = 1f; // Store weight before attack
        
        // Coroutine references
        private Coroutine _drawnRigTransition;
        private Coroutine _sheathedRigTransition;
        
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
            _eventDispatcher.RegisterReceiver(this, _beginSheatheEvent);
            _eventDispatcher.RegisterReceiver(this, _beginUnsheatheEvent);
            
            // Register for attack events
            _eventDispatcher.RegisterReceiver(this, _attackStartEvent);
            _eventDispatcher.RegisterReceiver(this, _attackEndEvent);
            
            // Initialize rig weights based on initial weapon state
            bool initialWeaponDrawn = _combatController != null && _combatController.IsWeaponDrawn();
            _isWeaponDrawn = initialWeaponDrawn;
            
            // Set initial weights directly
            if (_drawnWeaponRig != null)
            {
                _drawnWeaponRig.weight = initialWeaponDrawn ? 1f : 0f;
                
                if (_debugRig)
                {
                    Debug.Log($"Initial drawn rig weight set to: {_drawnWeaponRig.weight}");
                }
            }
            
            if (_sheathedWeaponRig != null)
            {
                _sheathedWeaponRig.weight = initialWeaponDrawn ? 0f : 1f;
                
                if (_debugRig)
                {
                    Debug.Log($"Initial sheathed rig weight set to: {_sheathedWeaponRig.weight}");
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
        }
        
        // IAnimationEventReceiver implementation
        public void OnAnimationEvent(string eventName)
        {
            if (_debugRig)
            {
                Debug.Log($"Weapon Rig Controller received animation event: {eventName}");
            }
            
            // Handle weapon state events
            if (eventName == _beginSheatheEvent)
            {
                // Smoothly fade out the drawn rig at the start of sheathe animation
                if (_drawnWeaponRig != null)
                {
                    StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, 0f, _quickTransitionDuration, ref _drawnRigTransition);
                    
                    if (_debugRig)
                    {
                        Debug.Log($"Beginning smooth fade-out of drawn rig for sheathe animation (from {_drawnWeaponRig.weight} to 0)");
                    }
                }
            }
            else if (eventName == _beginUnsheatheEvent)
            {
                // Smoothly fade out the sheathed rig at the start of unsheathe animation
                if (_sheathedWeaponRig != null)
                {
                    StartRigTransition(_sheathedWeaponRig, _sheathedWeaponRig.weight, 0f, _quickTransitionDuration, ref _sheathedRigTransition);
                    
                    if (_debugRig)
                    {
                        Debug.Log($"Beginning smooth fade-out of sheathed rig for unsheathe animation (from {_sheathedWeaponRig.weight} to 0)");
                    }
                }
                
                // Start fading in the drawn rig to allow IK to gradually take effect
                if (_drawnWeaponRig != null)
                {
                    // Use a partial weight initially to blend with the animation
                    float partialWeight = 0.5f; // Start at half strength
                    StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, partialWeight, _quickTransitionDuration, ref _drawnRigTransition);
                    
                    if (_debugRig)
                    {
                        Debug.Log($"Beginning partial fade-in of drawn rig for unsheathe animation (from {_drawnWeaponRig.weight} to {partialWeight})");
                    }
                }
            }
            else if (eventName == _drawWeaponEvent)
            {
                // Complete transition to drawn state
                TransitionToDrawn();
                _isTransitioning = false;
                
                if (_debugRig)
                {
                    Debug.Log("Completing transition to drawn state");
                }
            }
            else if (eventName == _sheatheWeaponEvent)
            {
                // Complete transition to sheathed state
                TransitionToSheathed();
                _isTransitioning = false;
                
                if (_debugRig)
                {
                    Debug.Log("Completing transition to sheathed state");
                }
            }
            // Handle attack events
            else if (eventName == _attackStartEvent)
            {
                // Store current drawn rig weight before disabling for attack
                if (_drawnWeaponRig != null)
                {
                    _preAttackDrawnRigWeight = _drawnWeaponRig.weight;
                    
                    // Quickly disable the drawn rig for the attack animation
                    StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, 0f, _attackTransitionDuration, ref _drawnRigTransition);
                    _isAttacking = true;
                    
                    if (_debugRig)
                    {
                        Debug.Log($"Attack started - Disabling drawn weapon rig (from {_preAttackDrawnRigWeight} to 0)");
                    }
                }
            }
            else if (eventName == _attackEndEvent)
            {
                // Restore the drawn rig weight after attack completes
                if (_drawnWeaponRig != null && _isAttacking)
                {
                    // Restore to pre-attack weight
                    StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, _preAttackDrawnRigWeight, _attackTransitionDuration, ref _drawnRigTransition);
                    _isAttacking = false;
                    
                    if (_debugRig)
                    {
                        Debug.Log($"Attack ended - Restoring drawn weapon rig (from {_drawnWeaponRig.weight} to {_preAttackDrawnRigWeight})");
                    }
                }
            }
        }
        
        // Transition to drawn state with coroutines
        private void TransitionToDrawn()
        {
            // Don't override attack state
            if (_isAttacking)
            {
                if (_debugRig)
                {
                    Debug.Log("Skipping drawn transition during attack");
                }
                return;
            }
            
            // Start transitions for each rig - always use 0 and 1 as target values
            StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, 1f, _transitionDuration, ref _drawnRigTransition);
            StartRigTransition(_sheathedWeaponRig, _sheathedWeaponRig.weight, 0f, _transitionDuration, ref _sheathedRigTransition);
            
            if (_debugRig)
            {
                Debug.Log($"Transitioning to drawn state: drawn rig from {_drawnWeaponRig.weight} to 1, sheathed rig from {_sheathedWeaponRig.weight} to 0");
            }
        }
        
        // Transition to sheathed state with coroutines
        private void TransitionToSheathed()
        {
            // Reset attack state when sheathing
            _isAttacking = false;
            
            // Start transitions for each rig - always use 0 and 1 as target values
            StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, 0f, _transitionDuration, ref _drawnRigTransition);
            StartRigTransition(_sheathedWeaponRig, _sheathedWeaponRig.weight, 1f, _transitionDuration, ref _sheathedRigTransition);
            
            if (_debugRig)
            {
                Debug.Log($"Transitioning to sheathed state: drawn rig from {_drawnWeaponRig.weight} to 0, sheathed rig from {_sheathedWeaponRig.weight} to 1");
            }
        }
        
        // Start a rig weight transition coroutine with custom duration
        private void StartRigTransition(Rig rig, float fromWeight, float toWeight, float duration, ref Coroutine coroutineRef)
        {
            if (rig == null) return;
            
            // Stop existing coroutine if running
            if (coroutineRef != null)
            {
                StopCoroutine(coroutineRef);
                coroutineRef = null;
            }
            
            // Skip transition if already at target weight
            if (Mathf.Approximately(fromWeight, toWeight))
            {
                rig.weight = toWeight;
                return;
            }
            
            // Start new transition
            coroutineRef = StartCoroutine(TransitionWeight(rig, fromWeight, toWeight, duration));
        }
        
        // Coroutine to transition a rig's weight with custom duration
        private IEnumerator TransitionWeight(Rig rig, float fromWeight, float toWeight, float duration)
        {
            float startTime = Time.time;
            float endTime = startTime + duration;
            
            while (Time.time < endTime)
            {
                float normalizedTime = (Time.time - startTime) / duration;
                float curveValue = _transitionCurve.Evaluate(normalizedTime);
                
                rig.weight = Mathf.Lerp(fromWeight, toWeight, curveValue);
                
                yield return null;
            }
            
            // Ensure final weight is exact
            rig.weight = toWeight;
            
            if (_debugRig)
            {
                Debug.Log($"Completed weight transition for {rig.name} to {toWeight}");
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
            // Stop all coroutines
            if (_drawnRigTransition != null) StopCoroutine(_drawnRigTransition);
            if (_sheathedRigTransition != null) StopCoroutine(_sheathedRigTransition);
            
            // Unregister from animation events
            if (_eventDispatcher != null)
            {
                _eventDispatcher.UnregisterAllEvents(this);
            }
        }
        
        // Optional: Debug visualization
        private void OnGUI()
        {
            if (!_debugRig) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            
            if (_drawnWeaponRig != null)
            {
                GUILayout.Label($"Drawn Rig: {_drawnWeaponRig.weight:F3}");
            }
            
            if (_sheathedWeaponRig != null)
            {
                GUILayout.Label($"Sheathed Rig: {_sheathedWeaponRig.weight:F3}");
            }
            
            GUILayout.Label($"Weapon State: {(_isWeaponDrawn ? "Drawn" : "Sheathed")}, Transitioning: {_isTransitioning}");
            GUILayout.Label($"Attacking: {_isAttacking}, Pre-Attack Weight: {_preAttackDrawnRigWeight:F3}");
            
            GUILayout.EndArea();
        }
        //         [Header("Rig References")]
        // [SerializeField] private RigBuilder _rigBuilder;
        // [SerializeField] private Rig _drawnWeaponRig;
        // [SerializeField] private Rig _sheathedWeaponRig;
        //
        // [Header("Transition Settings")]
        // [SerializeField] private float _transitionDuration = 0.5f;
        // [SerializeField] private float _quickTransitionDuration = 0.2f; // For begin events
        // [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        // [SerializeField] private string _drawWeaponEvent = "OnDrawWeapon";
        // [SerializeField] private string _sheatheWeaponEvent = "OnSheatheWeapon";
        // [SerializeField] private string _beginSheatheEvent = "OnBeginSheathe";
        // [SerializeField] private string _beginUnsheatheEvent = "OnBeginUnsheathe";
        // [SerializeField] private bool _debugRig = true;
        //
        // // Dependencies
        // private IPlayerContext _context;
        // private ICombatController _combatController;
        // private AnimationEventDispatcher _eventDispatcher;
        //
        // // State
        // private bool _isWeaponDrawn = false;
        // private bool _isTransitioning = false;
        //
        // // Coroutine references
        // private Coroutine _drawnRigTransition;
        // private Coroutine _sheathedRigTransition;
        //
        // public void Initialize(IPlayerContext context)
        // {
        //     _context = context;
        //     _combatController = context.GetService<ICombatController>();
        //     
        //     // Find the animator component
        //     Animator animator = GetComponentInChildren<Animator>();
        //     if (animator == null)
        //     {
        //         Debug.LogError("No Animator found in children! Animation events won't work.");
        //         return;
        //     }
        //     
        //     // Find or create the animation event dispatcher on the animator's GameObject
        //     _eventDispatcher = animator.gameObject.GetComponent<AnimationEventDispatcher>();
        //     if (_eventDispatcher == null)
        //     {
        //         _eventDispatcher = animator.gameObject.AddComponent<AnimationEventDispatcher>();
        //         _eventDispatcher.Initialize(context);
        //         Debug.Log($"Added AnimationEventDispatcher to {animator.gameObject.name}");
        //     }
        //     
        //     // Register for animation events
        //     _eventDispatcher.RegisterReceiver(this, _drawWeaponEvent);
        //     _eventDispatcher.RegisterReceiver(this, _sheatheWeaponEvent);
        //     _eventDispatcher.RegisterReceiver(this, _beginSheatheEvent);
        //     _eventDispatcher.RegisterReceiver(this, _beginUnsheatheEvent);
        //     
        //     // Initialize rig weights based on initial weapon state
        //     bool initialWeaponDrawn = _combatController != null && _combatController.IsWeaponDrawn();
        //     _isWeaponDrawn = initialWeaponDrawn;
        //     
        //     // Set initial weights directly
        //     if (_drawnWeaponRig != null)
        //     {
        //         _drawnWeaponRig.weight = initialWeaponDrawn ? 1f : 0f;
        //         
        //         if (_debugRig)
        //         {
        //             Debug.Log($"Initial drawn rig weight set to: {_drawnWeaponRig.weight}");
        //         }
        //     }
        //     
        //     if (_sheathedWeaponRig != null)
        //     {
        //         _sheathedWeaponRig.weight = initialWeaponDrawn ? 0f : 1f;
        //         
        //         if (_debugRig)
        //         {
        //             Debug.Log($"Initial sheathed rig weight set to: {_sheathedWeaponRig.weight}");
        //         }
        //     }
        //     
        //     Debug.Log("Weapon Rig Controller initialized");
        // }
        //
        // private void Update()
        // {
        //     // Check if weapon state has changed (only if not already transitioning)
        //     if (!_isTransitioning && _combatController != null)
        //     {
        //         bool weaponDrawn = _combatController.IsWeaponDrawn();
        //         if (weaponDrawn != _isWeaponDrawn)
        //         {
        //             // State has changed, but wait for animation event to start transition
        //             _isTransitioning = true;
        //             _isWeaponDrawn = weaponDrawn;
        //             
        //             if (_debugRig)
        //             {
        //                 Debug.Log($"Weapon state changed to {(weaponDrawn ? "drawn" : "sheathed")}, waiting for animation event");
        //             }
        //         }
        //     }
        // }
        //
        // // IAnimationEventReceiver implementation
        // public void OnAnimationEvent(string eventName)
        // {
        //     if (_debugRig)
        //     {
        //         Debug.Log($"Weapon Rig Controller received animation event: {eventName}");
        //     }
        //     
        //     if (eventName == _beginSheatheEvent)
        //     {
        //         // Smoothly fade out the drawn rig at the start of sheathe animation
        //         if (_drawnWeaponRig != null)
        //         {
        //             StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, 0f, _quickTransitionDuration, ref _drawnRigTransition);
        //             
        //             if (_debugRig)
        //             {
        //                 Debug.Log($"Beginning smooth fade-out of drawn rig for sheathe animation (from {_drawnWeaponRig.weight} to 0)");
        //             }
        //         }
        //     }
        //     else if (eventName == _beginUnsheatheEvent)
        //     {
        //         // Smoothly fade out the sheathed rig at the start of unsheathe animation
        //         if (_sheathedWeaponRig != null)
        //         {
        //             StartRigTransition(_sheathedWeaponRig, _sheathedWeaponRig.weight, 0f, _quickTransitionDuration, ref _sheathedRigTransition);
        //             
        //             if (_debugRig)
        //             {
        //                 Debug.Log($"Beginning smooth fade-out of sheathed rig for unsheathe animation (from {_sheathedWeaponRig.weight} to 0)");
        //             }
        //         }
        //         
        //         // Start fading in the drawn rig to allow IK to gradually take effect
        //         if (_drawnWeaponRig != null)
        //         {
        //             // Use a partial weight initially to blend with the animation
        //             float partialWeight = 0.5f; // Start at half strength
        //             StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, partialWeight, _quickTransitionDuration, ref _drawnRigTransition);
        //             
        //             if (_debugRig)
        //             {
        //                 Debug.Log($"Beginning partial fade-in of drawn rig for unsheathe animation (from {_drawnWeaponRig.weight} to {partialWeight})");
        //             }
        //         }
        //     }
        //     else if (eventName == _drawWeaponEvent)
        //     {
        //         // Complete transition to drawn state
        //         TransitionToDrawn();
        //         _isTransitioning = false;
        //         
        //         if (_debugRig)
        //         {
        //             Debug.Log("Completing transition to drawn state");
        //         }
        //     }
        //     else if (eventName == _sheatheWeaponEvent)
        //     {
        //         // Complete transition to sheathed state
        //         TransitionToSheathed();
        //         _isTransitioning = false;
        //         
        //         if (_debugRig)
        //         {
        //             Debug.Log("Completing transition to sheathed state");
        //         }
        //     }
        // }
        //
        // // Transition to drawn state with coroutines
        // private void TransitionToDrawn()
        // {
        //     // Start transitions for each rig - always use 0 and 1 as target values
        //     StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, 1f, _transitionDuration, ref _drawnRigTransition);
        //     StartRigTransition(_sheathedWeaponRig, _sheathedWeaponRig.weight, 0f, _transitionDuration, ref _sheathedRigTransition);
        //     
        //     if (_debugRig)
        //     {
        //         Debug.Log($"Transitioning to drawn state: drawn rig from {_drawnWeaponRig.weight} to 1, sheathed rig from {_sheathedWeaponRig.weight} to 0");
        //     }
        // }
        //
        // // Transition to sheathed state with coroutines
        // private void TransitionToSheathed()
        // {
        //     // Start transitions for each rig - always use 0 and 1 as target values
        //     StartRigTransition(_drawnWeaponRig, _drawnWeaponRig.weight, 0f, _transitionDuration, ref _drawnRigTransition);
        //     StartRigTransition(_sheathedWeaponRig, _sheathedWeaponRig.weight, 1f, _transitionDuration, ref _sheathedRigTransition);
        //     
        //     if (_debugRig)
        //     {
        //         Debug.Log($"Transitioning to sheathed state: drawn rig from {_drawnWeaponRig.weight} to 0, sheathed rig from {_sheathedWeaponRig.weight} to 1");
        //     }
        // }
        //
        // // Start a rig weight transition coroutine with custom duration
        // private void StartRigTransition(Rig rig, float fromWeight, float toWeight, float duration, ref Coroutine coroutineRef)
        // {
        //     if (rig == null) return;
        //     
        //     // Stop existing coroutine if running
        //     if (coroutineRef != null)
        //     {
        //         StopCoroutine(coroutineRef);
        //         coroutineRef = null;
        //     }
        //     
        //     // Skip transition if already at target weight
        //     if (Mathf.Approximately(fromWeight, toWeight))
        //     {
        //         rig.weight = toWeight;
        //         return;
        //     }
        //     
        //     // Start new transition
        //     coroutineRef = StartCoroutine(TransitionWeight(rig, fromWeight, toWeight, duration));
        // }
        //
        // // Coroutine to transition a rig's weight with custom duration
        // private IEnumerator TransitionWeight(Rig rig, float fromWeight, float toWeight, float duration)
        // {
        //     float startTime = Time.time;
        //     float endTime = startTime + duration;
        //     
        //     while (Time.time < endTime)
        //     {
        //         float normalizedTime = (Time.time - startTime) / duration;
        //         float curveValue = _transitionCurve.Evaluate(normalizedTime);
        //         
        //         rig.weight = Mathf.Lerp(fromWeight, toWeight, curveValue);
        //         
        //         yield return null;
        //     }
        //     
        //     // Ensure final weight is exact
        //     rig.weight = toWeight;
        //     
        //     if (_debugRig)
        //     {
        //         Debug.Log($"Completed weight transition for {rig.name} to {toWeight}");
        //     }
        // }
        //
        // public bool CanBeActivated()
        // {
        //     return true;
        // }
        //
        // public void OnActivate()
        // {
        //     // Nothing special needed
        // }
        //
        // public void OnDeactivate()
        // {
        //     // Stop all coroutines
        //     if (_drawnRigTransition != null) StopCoroutine(_drawnRigTransition);
        //     if (_sheathedRigTransition != null) StopCoroutine(_sheathedRigTransition);
        //     
        //     // Unregister from animation events
        //     if (_eventDispatcher != null)
        //     {
        //         _eventDispatcher.UnregisterAllEvents(this);
        //     }
        // }
        //
        // // Optional: Debug visualization
        // private void OnGUI()
        // {
        //     if (!_debugRig) return;
        //     
        //     GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        //     
        //     if (_drawnWeaponRig != null)
        //     {
        //         GUILayout.Label($"Drawn Rig: {_drawnWeaponRig.weight:F3}");
        //     }
        //     
        //     if (_sheathedWeaponRig != null)
        //     {
        //         GUILayout.Label($"Sheathed Rig: {_sheathedWeaponRig.weight:F3}");
        //     }
        //     
        //     GUILayout.Label($"Weapon State: {(_isWeaponDrawn ? "Drawn" : "Sheathed")}, Transitioning: {_isTransitioning}");
        //     
        //     GUILayout.EndArea();
        // }
    }
}
