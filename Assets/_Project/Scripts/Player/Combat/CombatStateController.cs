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

        [Header("Combo")]
        [Tooltip("Animator bool. True from the start of a swing until its hold ends. The idle exits need it false.")]
        [SerializeField] private string _canStringToComboParam = "CanStringToCombo";
        [Tooltip("Animator state names for the combo swings, in order. A press in the hold plays the next one by name.")]
        [SerializeField] private string[] _comboStates = { "Melee Combo Attack 1", "Melee Combo Attack 2", "Melee Combo Attack 3" };
        [Tooltip("Animator layer the combo states live on (the weapon layer).")]
        [SerializeField] private int _comboLayerIndex = 1;
        [Tooltip("Seconds of crossfade when a press chains into the next swing.")]
        [SerializeField] private float _comboCrossFade = 0.05f;
        [Tooltip("Seconds the swing eases down to the slow speed after it ends, waiting for the next press. A press in this time continues the combo.")]
        [SerializeField] private float _comboHoldSeconds = 1f;
        [Tooltip("Animator speed during the hold. 0.1 is 10% speed. The swing eases down to it rather than stopping.")]
        [Range(0.01f, 1f)] [SerializeField] private float _comboSlowSpeed = 0.1f;
        [Tooltip("Seconds the speed takes to ease between normal and the slow hold speed.")]
        [SerializeField] private float _comboEaseSeconds = 0.25f;

        private bool _comboHolding;
        private float _comboHoldEndsAt = -10f;
        private float _animatorSpeed = 1f;
        private float _animatorSpeedTarget = 1f;
        private bool _comboQueued;
        private int _comboStage = 0; // index into _comboStates of the swing that is running, or -1 when none
        
        [Header("Animation Events")]
        [SerializeField] private string _attackStartEvent = "OnAttackStart";
        [SerializeField] private string _attackEndEvent = "OnAttackEnd";
        [Tooltip("Starts the combo hold. Fired while the blade is returning, before the clip exits, so the hold begins before the idle exit can fire.")]
        [SerializeField] private string _attackRecoveryEvent = "AttackRecovery";
        [SerializeField] private string _beginSheatheEvent = "OnBeginSheathe";
        [SerializeField] private string _sheatheWeaponEvent = "OnSheatheWeapon";
        [SerializeField] private string _beginUnsheatheEvent = "OnBeginUnsheathe";
        [SerializeField] private string _drawWeaponEvent = "OnDrawWeapon";
        
        // Dependencies
        private IPlayerContext _context;
        private IAnimationController _animationController;
        private IStatusController _status;
        private IEquipmentService _equipment;
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

        // The swing window. The attack clip has no animation events, so the combat controller owns when a swing is in
        // progress. The weapon rig releases the hand for this window. Set this to the clip's length.
        [Header("Swing")]
        [Tooltip("Seconds one swing takes, from its start to its end. Match it to the combo clip length (1.0 s for the combo clips). " +
                 "After this window, a stale attack flag can no longer block input.")]
        [SerializeField] private float _swingDuration = 1f;
        private float _swingEndTime = -10f;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            
            _animationController = context.GetService<IAnimationController>();
            _status = context.GetService<IStatusController>();
            _equipment = ServiceLocator.TryGet<IEquipmentService>();
            
            if (_animationController == null)
            {
                Debug.LogError("Animation Controller not found! Combat animations won't work.");
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
            _eventDispatcher.RegisterReceiver(this, _attackRecoveryEvent);
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
        
        private void Update()
        {
            UpdateComboHold();
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
                // Every clip starts with the combo closed. Only a press sets it true again.
                SetCanStringToCombo(false);
            }
            else if (eventName == _attackEndEvent)
            {
                _isAttackAnimationPlaying = false;
                _swingEndTime = -10f; // The clip has finished, so the hand is held again at once, not when the timer runs out.
                _isAttacking = false;
                _context.SetStateValue("IsAttacking", false);
                
                if (_debugCombat)
                {
                    Debug.Log("Attack completed");
                }

                // A press during the swing continues the combo straight into the next clip, with no pause.
                if (_comboQueued)
                {
                    _comboQueued = false;
                    StartChainedSwing();
                }
                else
                {
                    // Nothing queued. The hold already started at AttackRecovery.
                }
            }
            else if (eventName == _attackRecoveryEvent)
            {
                // The strike is done and the blade is returning. Decide here: a queued press continues the combo,
                // otherwise hold now, before the clip exits to idle.
                if (_comboQueued)
                {
                    // The queued swing starts at OnAttackEnd, so nothing to do yet. Keep the queue for that event.
                    return;
                }

                BeginComboHold();
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
    // A combat lock (e.g. stagger) blocks new attacks until it ends.
    if (_status != null && _status.IsCombatLocked)
    {
        return;
    }

    // An attack whose end event never arrived (the Animator left the state early) must not block input forever.
    // Once its swing window has passed, the attack is over, whatever the animation did.
    if (_isAttackAnimationPlaying && Time.time >= _swingEndTime)
    {
        _isAttackAnimationPlaying = false;
        _isAttacking = false;
        _context.SetStateValue("IsAttacking", false);
    }

    // A press during the hold continues the combo straight away: the animator returns to normal speed and the next swing starts.
    if (_comboHolding)
    {
        // A press opens the combo, then the next swing plays by name.
        SetCanStringToCombo(true);
        EndComboHold(resumeNormalSpeed: true);
        StartChainedSwing();
        return;
    }

    // A press during a swing queues the next swing in the combo. The queued swing starts when this one ends.
    if (_isAttackAnimationPlaying && _currentState == CombatState.Unsheathed)
    {
        SetCanStringToCombo(true);
        _comboQueued = true;
        return;
    }

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

// Sets the Animator bool that gates the combo. True lets a press chain into the next swing; false lets the idle exit run.
private void SetCanStringToCombo(bool value)
{
    _animationController?.SetBool(_canStringToComboParam, value);
}

// Plays the next combo swing by its state name, so the chain does not depend on Animator transitions or triggers.
// The stage moves to the swing that is now playing. Chaining stops after the last swing in the list.
private void StartChainedSwing()
{
    int next = _comboStage + 1;
    if (next >= _comboStates.Length)
    {
        EndCombo();
        return;
    }

    _comboStage = next;
    _isAttacking = true;
    _lastAttackTime = Time.time;
    _swingEndTime = Time.time + _swingDuration;

    _animationController?.PlayState(_comboStates[_comboStage], _comboLayerIndex, _comboCrossFade);
    _context.SetStateValue("IsAttacking", true);

    if (_debugCombat)
    {
        Debug.Log($"[Combo] chained to {_comboStates[_comboStage]}");
    }
}

// Starts the hold after a swing ends with nothing queued. The animator eases to the slow speed, and a press in the window continues the combo.
private void BeginComboHold()
{
    _comboHolding = true;
    _comboHoldEndsAt = Time.time + _comboHoldSeconds;
    _animatorSpeedTarget = _comboSlowSpeed;
}

// Ends the hold. Returns the animator to normal speed, and optionally keeps it slow when a continuing press has already started.
private void EndComboHold(bool resumeNormalSpeed)
{
    if (!_comboHolding) return;
    _comboHolding = false;
    if (resumeNormalSpeed) _animatorSpeedTarget = 1f;
}

// Ends the combo: the hold is over and no press continued it. Clears the bool so the idle exit can run.
private void EndCombo()
{
    EndComboHold(resumeNormalSpeed: true);
    SetCanStringToCombo(false);
}

// Runs every frame. Eases the animator speed toward its target on unscaled time, and ends the combo when the hold runs out.
private void UpdateComboHold()
{
    if (!Mathf.Approximately(_animatorSpeed, _animatorSpeedTarget))
    {
        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, _comboEaseSeconds) * (1f - _comboSlowSpeed);
        _animatorSpeed = Mathf.MoveTowards(_animatorSpeed, _animatorSpeedTarget, step);
        _animationController?.SetAnimatorSpeed(_animatorSpeed);
    }

    if (!_comboHolding) return;
    if (Time.time < _comboHoldEndsAt) return;

    // The hold ran out with no press: the combo is over.
    EndCombo();
}

private void StartAttack()
{
    // Set attack state
    _isAttacking = true;
    _lastAttackTime = Time.time;
    _swingEndTime = Time.time + _swingDuration;
    
    // Clear any trigger left over from an attack the Animator never took, so it cannot fire later out of sequence.
    if (_animationController != null)
    {
        _animationController.ResetTrigger(_attackTrigger);
        _animationController.SetTrigger(_attackTrigger);
    }
    _comboStage = 0;
    
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

// A one-handed loadout is a one-handed weapon in the main hand and nothing in the off hand. Anything else is not one-handed,
// so the one-handed grip never plays for it.
private bool IsOneHandedLoadout()
{
    if (_equipment == null) return false;

    if (_equipment.GetEquipped(EquipmentSlot.MainHand) is not WeaponDataSO weapon) return false;
    if (weapon.weaponSize != WeaponSize.OneHanded) return false;

    return _equipment.GetEquipped(EquipmentSlot.Offhand) == null;
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
    
    // The one-handed grip layer only turns on for a real one-handed loadout. The animator is told the truth
    // about the equipment, so the grip never plays with nothing in the hand.
    bool oneHanded = IsOneHandedLoadout();
    if (_animationController != null)
    {
        _animationController.SetTrigger("Unsheathe");
        _animationController.SetBool("OneHanded", oneHanded);
        _animationController.SetBool("TwoHanded", false);
    }

    if (_debugCombat)
    {
        Debug.Log($"Unsheathing. One-handed loadout: {oneHanded}");
    }

    // Start coroutine to complete unsheathing after animation time
    if (_stateTransitionCoroutine != null)
    {
        StopCoroutine(_stateTransitionCoroutine);
    }
    
    _stateTransitionCoroutine = StartCoroutine(CompleteUnsheathe());
    
    // Update context state
    _context.SetStateValue("IsWeaponDrawn", true);
    
}

private IEnumerator CompleteUnsheathe()
{
    yield return new WaitForSeconds(_unsheatheTime);

    // Complete transition to unsheathed state
    _isUnsheatheAnimationPlaying = false;
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
    _isSheatheAnimationPlaying = false;
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
    EndCombo();

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

// Cancels an attack in progress (e.g. when staggered). Safe to call when not attacking.
public void CancelAttack()
{
    // A cancel ends any hold too, so the idle exit is never blocked by a stale bool.
    EndCombo();

    if (!_isAttacking && !_isAttackAnimationPlaying) return;

    _isAttacking = false;
    _isAttackAnimationPlaying = false;
    _context.SetStateValue("IsAttacking", false);

    if (_debugCombat)
    {
        Debug.Log("Attack cancelled");
    }
}

// True for the swing window that starts when an attack begins. Driven by time, not animation events, because the clip has none.
public bool IsSwinging()
{
    return Time.time < _swingEndTime;
}

// Helper method to check if player is currently in an attack
public bool IsAttacking()
{
    return _isAttackAnimationPlaying || IsSwinging() || _comboHolding;
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
}
}
