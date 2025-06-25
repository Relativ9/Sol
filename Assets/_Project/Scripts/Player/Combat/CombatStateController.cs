using System;
using System.Collections;
using UnityEngine;

namespace Sol
{
    public class CombatStateController : MonoBehaviour, ICombatController, IPlayerComponent, IAnimationEventReceiver
    {
        [Header("Combat Settings")]
        [SerializeField] private float _unsheatheTime = 0.5f;
        [SerializeField] private float _sheatheTime = 0.5f;
        [SerializeField] private bool _debugCombat = true;
        private bool _isFirstUnsheathe = true;
        
        [Header("Attack Settings")]
        [SerializeField] private float _attackCooldown = 0.8f;
        [SerializeField] private string _attackTrigger = "Attack";
        
        [Header("Animation Events")]
        [SerializeField] private string _attackStartEvent = "OnAttackStart";
        [SerializeField] private string _attackEndEvent = "OnAttackEnd";
        [SerializeField] private string _beginSheatheEvent = "OnBeginSheathe";
        [SerializeField] private string _sheatheWeaponEvent = "OnSheatheWeapon";
        [SerializeField] private string _beginUnsheatheEvent = "OnBeginUnsheathe";
        [SerializeField] private string _drawWeaponEvent = "OnDrawWeapon";
        
        // Dependencies
        private IPlayerContext _context;
        private IAnimationController _animationController;
        private IWeaponService _weaponService;
        private AnimationEventDispatcher _eventDispatcher;
        
        // State
        private CombatState _currentState = CombatState.Sheathed;
        private Coroutine _stateTransitionCoroutine;
        
        // Animation state tracking
        private bool _isAttackAnimationPlaying = false;
        private bool _isSheatheAnimationPlaying = false;
        private bool _isUnsheatheAnimationPlaying = false;
        
        // Attack state
        private bool _isAttacking = false;
        private float _lastAttackTime = -10f;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            
            _animationController = context.GetService<IAnimationController>();
            _weaponService = context.GetService<IWeaponService>();
            
            if (_animationController == null)
            {
                Debug.LogError("Animation Controller not found! Combat animations won't work.");
            }
            
            if (_weaponService == null)
            {
                Debug.LogWarning("Weapon Service not found! Using default weapon settings.");
            }
            
            // Find the animator component
            Animator animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("No Animator found in children! Animation events won't work.");
                return;
            }
            
            // Find or create the animation event dispatcher
            _eventDispatcher = animator.gameObject.GetComponent<AnimationEventDispatcher>();
            if (_eventDispatcher == null)
            {
                _eventDispatcher = animator.gameObject.AddComponent<AnimationEventDispatcher>();
                _eventDispatcher.Initialize(context);
            }
            
            // Register for animation events
            _eventDispatcher.RegisterReceiver(this, _attackStartEvent);
            _eventDispatcher.RegisterReceiver(this, _attackEndEvent);
            _eventDispatcher.RegisterReceiver(this, _beginSheatheEvent);
            _eventDispatcher.RegisterReceiver(this, _sheatheWeaponEvent);
            _eventDispatcher.RegisterReceiver(this, _beginUnsheatheEvent);
            _eventDispatcher.RegisterReceiver(this, _drawWeaponEvent);
            
            // Initialize in sheathed state
            SetCombatState(CombatState.Sheathed);
            
            if (_context != null)
            {
                _context.SetStateValue("IsWeaponDrawn", false);
                _context.SetStateValue("CombatState", (int)CombatState.Sheathed);
            }
            
            _isFirstUnsheathe = true;
        
            Debug.Log("Combat State Controller initialized in Sheathed state");
        }
        
        // IAnimationEventReceiver implementation
        public void OnAnimationEvent(string eventName)
        {
            if (_debugCombat)
            {
                Debug.Log($"Combat controller received animation event: {eventName}");
            }
            
            // Handle attack events
            if (eventName == _attackStartEvent)
            {
                _isAttackAnimationPlaying = true;
            }
            else if (eventName == _attackEndEvent)
            {
                _isAttackAnimationPlaying = false;
                _isAttacking = false;
                _context.SetStateValue("IsAttacking", false);
                
                if (_debugCombat)
                {
                    Debug.Log("Attack completed");
                }
            }
            // Handle sheathe/unsheathe events
            else if (eventName == _beginSheatheEvent)
            {
                _isSheatheAnimationPlaying = true;
            }
            else if (eventName == _sheatheWeaponEvent)
            {
                _isSheatheAnimationPlaying = false;
            }
            else if (eventName == _beginUnsheatheEvent)
            {
                _isUnsheatheAnimationPlaying = true;
            }
            else if (eventName == _drawWeaponEvent)
            {
                _isUnsheatheAnimationPlaying = false;
            }
        }
        
        public bool CanBeActivated()
        {
            // Combat controller can always be active
            return true;
        }
        
        public void OnActivate()
        {
            // Nothing special needed when activated
            Debug.Log("Combat controller activated");
        }
        
        public void OnDeactivate()
        {
            // Force sheathe weapon when deactivated
            if (_currentState != CombatState.Sheathed)
            {
                ForceSheatheWeapon();
            }
            
            // Unregister from animation events
            if (_eventDispatcher != null)
            {
                _eventDispatcher.UnregisterAllEvents(this);
            }
            
            Debug.Log("Combat controller deactivated");
        }
        
        public void ToggleWeaponState()
{
    // Check if any animation is playing that would prevent toggling
    if (_isAttackAnimationPlaying || _isSheatheAnimationPlaying || _isUnsheatheAnimationPlaying)
    {
        if (_debugCombat)
        {
            Debug.Log("Cannot toggle weapon during animation");
        }
        return;
    }
    
    // Only allow toggling if we're in a stable state
    if (_currentState == CombatState.Sheathed || _currentState == CombatState.Unsheathed)
    {
        if (_currentState == CombatState.Sheathed)
        {
            UnsheatheWeapon();
        }
        else
        {
            SheatheWeapon();
        }
    }
    else
    {
        Debug.Log($"Cannot toggle weapon during state: {_currentState}");
    }
}

public bool IsWeaponDrawn()
{
    return _currentState != CombatState.Sheathed && _currentState != CombatState.Sheathing;
}

public void HandleAttackInput()
{
    // Check if any animation is playing that would prevent attacking
    if (_isAttackAnimationPlaying || _isSheatheAnimationPlaying || _isUnsheatheAnimationPlaying)
    {
        if (_debugCombat)
        {
            Debug.Log("Cannot attack during animation");
        }
        return;
    }
    
    // Only allow attacks when weapon is drawn and in the unsheathed state
    if (_currentState != CombatState.Unsheathed)
    {
        if (_debugCombat)
        {
            Debug.Log($"Cannot attack in state: {_currentState}");
        }
        return;
    }
    
    // Check cooldown
    float timeSinceLastAttack = Time.time - _lastAttackTime;
    if (timeSinceLastAttack < _attackCooldown)
    {
        if (_debugCombat)
        {
            Debug.Log($"Attack on cooldown for {_attackCooldown - timeSinceLastAttack:F2} more seconds");
        }
        return;
    }
    
    // Start attack
    StartAttack();
}

private void StartAttack()
{
    // Set attack state
    _isAttacking = true;
    _lastAttackTime = Time.time;
    
    // Set animation trigger
    if (_animationController != null)
    {
        _animationController.SetTrigger(_attackTrigger);
    }
    
    // Set context state for other components
    _context.SetStateValue("IsAttacking", true);
    
    if (_debugCombat)
    {
        Debug.Log("Starting attack");
    }
}

public CombatState GetCurrentCombatState()
{
    return _currentState;
}

private void UnsheatheWeapon()
{
    if (_currentState != CombatState.Sheathed)
    {
        Debug.LogWarning($"Cannot unsheathe from state: {_currentState}");
        return;
    }
    
    // Start transition to unsheathed state
    SetCombatState(CombatState.Unsheathing);
    
    // Get weapon type from service or use default
    WeaponType weaponType = _weaponService != null ? 
        _weaponService.GetEquippedWeaponType() : WeaponType.OneHanded;
    
    // Set animation parameters
    if (_animationController != null)
    {
        _animationController.SetTrigger("Unsheathe");
        
        // Set weapon type in animator
        switch (weaponType)
        {
            case WeaponType.OneHanded:
                _animationController.SetBool("OneHanded", true);
                break;
            case WeaponType.TwoHanded:
                _animationController.SetBool("TwoHanded", true);
                break;
            // Add other weapon types as needed
        }
    }
    
    // Start coroutine to complete unsheathing after animation time
    if (_stateTransitionCoroutine != null)
    {
        StopCoroutine(_stateTransitionCoroutine);
    }
    
    _stateTransitionCoroutine = StartCoroutine(CompleteUnsheathe());
    
    // Update context state
    _context.SetStateValue("IsWeaponDrawn", true);
    
    if (_debugCombat)
    {
        Debug.Log($"Unsheathing weapon of type: {weaponType}");
    }
}

private IEnumerator CompleteUnsheathe()
{
    yield return new WaitForSeconds(_unsheatheTime);

    // Complete transition to unsheathed state
    SetCombatState(CombatState.Unsheathed);

    if (_debugCombat)
    {
        Debug.Log("Weapon fully unsheathed");
    }

    _stateTransitionCoroutine = null;
}

private void SheatheWeapon()
{
    if (_currentState != CombatState.Unsheathed)
    {
        Debug.LogWarning($"Cannot sheathe from state: {_currentState}");
        return;
    }
    
    // Start transition to sheathed state
    SetCombatState(CombatState.Sheathing);
    
    // Set animation parameters
    if (_animationController != null)
    {
        _animationController.SetTrigger("Sheathe");
    }
    
    // Start coroutine to complete sheathing after animation time
    if (_stateTransitionCoroutine != null)
    {
        StopCoroutine(_stateTransitionCoroutine);
    }
    
    _stateTransitionCoroutine = StartCoroutine(CompleteSheathe());
    
    // Update context state
    _context.SetStateValue("IsWeaponDrawn", false);
    
    if (_debugCombat)
    {
        Debug.Log("Sheathing weapon");
    }
}

private IEnumerator CompleteSheathe()
{
    // Wait for sheathe animation to complete
    yield return new WaitForSeconds(_sheatheTime);
    
    // Complete transition to sheathed state
    SetCombatState(CombatState.Sheathed);
    
    // Reset weapon type booleans in animator
    if (_animationController != null)
    {
        _animationController.SetBool("OneHanded", false);
        _animationController.SetBool("TwoHanded", false);
        // Reset other weapon types as needed
    }
    
    if (_debugCombat)
    {
        Debug.Log("Weapon fully sheathed");
    }
    
    _stateTransitionCoroutine = null;
}

private void ForceSheatheWeapon()
{
    // Reset all animation states
    _isAttackAnimationPlaying = false;
    _isSheatheAnimationPlaying = false;
    _isUnsheatheAnimationPlaying = false;
    _isAttacking = false;
    
    // Immediately set to sheathed state without animation
    SetCombatState(CombatState.Sheathed);
    
    // Reset all weapon type booleans
    if (_animationController != null)
    {
        _animationController.SetBool("OneHanded", false);
        _animationController.SetBool("TwoHanded", false);
        // Reset other weapon types as needed
    }
    
    // Update context state
    _context.SetStateValue("IsWeaponDrawn", false);
    _context.SetStateValue("IsAttacking", false);
    
    if (_debugCombat)
    {
        Debug.Log("Weapon forcibly sheathed");
    }
    
    // Stop any ongoing transitions
    if (_stateTransitionCoroutine != null)
    {
        StopCoroutine(_stateTransitionCoroutine);
        _stateTransitionCoroutine = null;
    }
}

private void SetCombatState(CombatState newState)
{
    _currentState = newState;
    _context.SetStateValue("CombatState", (int)newState);
    
    if (_debugCombat)
    {
        Debug.Log($"Combat state changed to: {newState}");
    }
}

// Helper method to check if player is currently in an attack
public bool IsAttacking()
{
    return _isAttackAnimationPlaying;
}

// Debug visualization
private void OnGUI()
{
    if (!_debugCombat) return;
    
    GUILayout.BeginArea(new Rect(10, 60, 300, 200));
    
    GUILayout.Label($"Combat State: {_currentState}");
    GUILayout.Label($"Attack Animation: {_isAttackAnimationPlaying}");
    GUILayout.Label($"Sheathe Animation: {_isSheatheAnimationPlaying}");
    GUILayout.Label($"Unsheathe Animation: {_isUnsheatheAnimationPlaying}");
    GUILayout.Label($"Last Attack Time: {_lastAttackTime:F2}, Cooldown: {_attackCooldown:F2}");
    
    GUILayout.EndArea();
}
        
        // [Header("Combat Settings")]
        // [SerializeField] private float _unsheatheTime = 0.5f;
        // [SerializeField] private float _sheatheTime = 0.5f;
        // [SerializeField] private bool _debugCombat = true;
        // private bool _isFirstUnsheathe = true;
        //
        // [Header("Attack Settings")]
        // [SerializeField] private float _attackCooldown = 0.8f;
        // [SerializeField] private string _attackTrigger = "Attack";
        //
        // // Dependencies
        // private IPlayerContext _context;
        // private IAnimationController _animationController;
        // private IWeaponService _weaponService;
        //
        // // State
        // private CombatState _currentState = CombatState.Sheathed;
        // private Coroutine _stateTransitionCoroutine;
        //
        // // Attack state
        // private bool _isAttacking = false;
        // private float _lastAttackTime = -10f;
        // private Coroutine _attackCoroutine;
        //
        // public void Initialize(IPlayerContext context)
        // {
        //     _context = context;
        //     
        //     _animationController = context.GetService<IAnimationController>();
        //     _weaponService = context.GetService<IWeaponService>();
        //     
        //     if (_animationController == null)
        //     {
        //         Debug.LogError("Animation Controller not found! Combat animations won't work.");
        //     }
        //     
        //     if (_weaponService == null)
        //     {
        //         Debug.LogWarning("Weapon Service not found! Using default weapon settings.");
        //     }
        //     
        //     // Initialize in sheathed state
        //     SetCombatState(CombatState.Sheathed);
        //     
        //     if (_context != null)
        //     {
        //         _context.SetStateValue("IsWeaponDrawn", false);
        //         _context.SetStateValue("CombatState", (int)CombatState.Sheathed);
        //     }
        //     
        //     _isFirstUnsheathe = true;
        //
        //     Debug.Log("Combat State Controller initialized in Sheathed state");
        // }
        //
        // public bool CanBeActivated()
        // {
        //     // Combat controller can always be active
        //     return true;
        // }
        //
        // public void OnActivate()
        // {
        //     // Nothing special needed when activated
        //     Debug.Log("Combat controller activated");
        // }
        //
        // public void OnDeactivate()
        // {
        //     // Force sheathe weapon when deactivated
        //     if (_currentState != CombatState.Sheathed)
        //     {
        //         ForceSheatheWeapon();
        //     }
        //     
        //     // Stop any attack coroutines
        //     if (_attackCoroutine != null)
        //     {
        //         StopCoroutine(_attackCoroutine);
        //         _attackCoroutine = null;
        //     }
        //     
        //     Debug.Log("Combat controller deactivated");
        // }
        //
        // public void ToggleWeaponState()
        // {
        //     // Only allow toggling if we're in a stable state and not attacking
        //     if ((_currentState == CombatState.Sheathed || _currentState == CombatState.Unsheathed) && !_isAttacking)
        //     {
        //         if (_currentState == CombatState.Sheathed)
        //         {
        //             UnsheatheWeapon();
        //         }
        //         else
        //         {
        //             SheatheWeapon();
        //         }
        //     }
        //     else
        //     {
        //         Debug.Log($"Cannot toggle weapon during state: {_currentState} or while attacking");
        //     }
        // }
        //
        // public bool IsWeaponDrawn()
        // {
        //     return _currentState != CombatState.Sheathed && _currentState != CombatState.Sheathing;
        // }
        //
        // public void HandleAttackInput()
        // {
        //     // Only allow attacks when weapon is drawn and in the unsheathed state
        //     if (_currentState != CombatState.Unsheathed)
        //     {
        //         if (_debugCombat)
        //         {
        //             Debug.Log($"Cannot attack in state: {_currentState}");
        //         }
        //         return;
        //     }
        //     
        //     // Check if we're already attacking or on cooldown
        //     if (_isAttacking)
        //     {
        //         if (_debugCombat)
        //         {
        //             Debug.Log("Already attacking");
        //         }
        //         return;
        //     }
        //     
        //     // Check cooldown
        //     float timeSinceLastAttack = Time.time - _lastAttackTime;
        //     if (timeSinceLastAttack < _attackCooldown)
        //     {
        //         if (_debugCombat)
        //         {
        //             Debug.Log($"Attack on cooldown for {_attackCooldown - timeSinceLastAttack:F2} more seconds");
        //         }
        //         return;
        //     }
        //     
        //     // Start attack
        //     StartAttack();
        // }
        //
        // private void StartAttack()
        // {
        //     // Set attack state
        //     _isAttacking = true;
        //     _lastAttackTime = Time.time;
        //     
        //     // Set animation trigger
        //     if (_animationController != null)
        //     {
        //         _animationController.SetTrigger(_attackTrigger);
        //     }
        //     
        //     // Set context state for other components
        //     _context.SetStateValue("IsAttacking", true);
        //     
        //     // Start cooldown coroutine
        //     if (_attackCoroutine != null)
        //     {
        //         StopCoroutine(_attackCoroutine);
        //     }
        //     _attackCoroutine = StartCoroutine(AttackCooldown());
        //     
        //     if (_debugCombat)
        //     {
        //         Debug.Log("Starting attack");
        //     }
        // }
        //
        // private IEnumerator AttackCooldown()
        // {
        //     // Wait for attack animation to complete
        //     yield return new WaitForSeconds(_attackCooldown);
        //     
        //     // Reset attack state
        //     _isAttacking = false;
        //     _context.SetStateValue("IsAttacking", false);
        //     
        //     if (_debugCombat)
        //     {
        //         Debug.Log("Attack completed and cooldown finished");
        //     }
        //     
        //     _attackCoroutine = null;
        // }
        //
        // public CombatState GetCurrentCombatState()
        // {
        //     return _currentState;
        // }
        //
        // private void UnsheatheWeapon()
        // {
        //     if (_currentState != CombatState.Sheathed)
        //     {
        //         Debug.LogWarning($"Cannot unsheathe from state: {_currentState}");
        //         return;
        //     }
        //     
        //     // Start transition to unsheathed state
        //     SetCombatState(CombatState.Unsheathing);
        //     
        //     // Get weapon type from service or use default
        //     WeaponType weaponType = _weaponService != null ? 
        //         _weaponService.GetEquippedWeaponType() : WeaponType.OneHanded;
        //     
        //     // Set animation parameters
        //     if (_animationController != null)
        //     {
        //         _animationController.SetTrigger("Unsheathe");
        //         
        //         // Set weapon type in animator
        //         switch (weaponType)
        //         {
        //             case WeaponType.OneHanded:
        //                 _animationController.SetBool("OneHanded", true);
        //                 break;
        //             case WeaponType.TwoHanded:
        //                 _animationController.SetBool("TwoHanded", true);
        //                 break;
        //             // Add other weapon types as needed
        //         }
        //     }
        //     
        //     // Start coroutine to complete unsheathing after animation time
        //     if (_stateTransitionCoroutine != null)
        //     {
        //         StopCoroutine(_stateTransitionCoroutine);
        //     }
        //     
        //     _stateTransitionCoroutine = StartCoroutine(CompleteUnsheathe());
        //     
        //     // Update context state
        //     _context.SetStateValue("IsWeaponDrawn", true);
        //     
        //     if (_debugCombat)
        //     {
        //         Debug.Log($"Unsheathing weapon of type: {weaponType}");
        //     }
        // }
        //
        // private IEnumerator CompleteUnsheathe()
        // {
        //     yield return new WaitForSeconds(_unsheatheTime);
        //
        //     // Complete transition to unsheathed state
        //     SetCombatState(CombatState.Unsheathed);
        //
        //     if (_debugCombat)
        //     {
        //         Debug.Log("Weapon fully unsheathed");
        //     }
        //
        //     _stateTransitionCoroutine = null;
        // }
        //
        // private void SheatheWeapon()
        // {
        //     if (_currentState != CombatState.Unsheathed)
        //     {
        //         Debug.LogWarning($"Cannot sheathe from state: {_currentState}");
        //         return;
        //     }
        //     
        //     // If attacking, cancel the attack
        //     if (_isAttacking)
        //     {
        //         CancelAttack();
        //     }
        //     
        //     // Start transition to sheathed state
        //     SetCombatState(CombatState.Sheathing);
        //     
        //     // Set animation parameters
        //     if (_animationController != null)
        //     {
        //         _animationController.SetTrigger("Sheathe");
        //     }
        //     
        //     // Start coroutine to complete sheathing after animation time
        //     if (_stateTransitionCoroutine != null)
        //     {
        //         StopCoroutine(_stateTransitionCoroutine);
        //     }
        //     
        //     _stateTransitionCoroutine = StartCoroutine(CompleteSheathe());
        //     
        //     // Update context state
        //     _context.SetStateValue("IsWeaponDrawn", false);
        //     
        //     if (_debugCombat)
        //     {
        //         Debug.Log("Sheathing weapon");
        //     }
        // }
        //
        // private void CancelAttack()
        // {
        //     if (_attackCoroutine != null)
        //     {
        //         StopCoroutine(_attackCoroutine);
        //         _attackCoroutine = null;
        //     }
        //     
        //     _isAttacking = false;
        //     _context.SetStateValue("IsAttacking", false);
        //     
        //     if (_debugCombat)
        //     {
        //         Debug.Log("Attack canceled due to weapon sheathing");
        //     }
        // }
        //
        // private IEnumerator CompleteSheathe()
        // {
        //     // Wait for sheathe animation to complete
        //     yield return new WaitForSeconds(_sheatheTime);
        //     
        //     // Complete transition to sheathed state
        //     SetCombatState(CombatState.Sheathed);
        //     
        //     // Reset weapon type booleans in animator
        //     if (_animationController != null)
        //     {
        //         _animationController.SetBool("OneHanded", false);
        //         _animationController.SetBool("TwoHanded", false);
        //         // Reset other weapon types as needed
        //     }
        //     
        //     if (_debugCombat)
        //     {
        //         Debug.Log("Weapon fully sheathed");
        //     }
        //     
        //     _stateTransitionCoroutine = null;
        // }
        //
        // private void ForceSheatheWeapon()
        // {
        //     // Cancel any attacks
        //     if (_isAttacking)
        //     {
        //         CancelAttack();
        //     }
        //     
        //     // Immediately set to sheathed state without animation
        //     SetCombatState(CombatState.Sheathed);
        //     
        //     // Reset all weapon type booleans
        //     if (_animationController != null)
        //     {
        //         _animationController.SetBool("OneHanded", false);
        //         _animationController.SetBool("TwoHanded", false);
        //         // Reset other weapon types as needed
        //     }
        //     
        //     // Update context state
        //     _context.SetStateValue("IsWeaponDrawn", false);
        //     
        //     if (_debugCombat)
        //     {
        //         Debug.Log("Weapon forcibly sheathed");
        //     }
        //     
        //     // Stop any ongoing transitions
        //     if (_stateTransitionCoroutine != null)
        //     {
        //         StopCoroutine(_stateTransitionCoroutine);
        //         _stateTransitionCoroutine = null;
        //     }
        // }
        //
        // private void SetCombatState(CombatState newState)
        // {
        //     _currentState = newState;
        //     _context.SetStateValue("CombatState", (int)newState);
        //     
        //     if (_debugCombat)
        //     {
        //         Debug.Log($"Combat state changed to: {newState}");
        //     }
        // }
        //
        // // Helper method to check if player is currently in an attack
        // public bool IsAttacking()
        // {
        //     return _isAttacking;
        // }
    }
}
