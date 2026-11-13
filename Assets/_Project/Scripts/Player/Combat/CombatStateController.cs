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
        [Tooltip("Turns the post-swing hold window on. Off: a press only counts during the swing itself, and CanStringToCombo is cleared when the swing ends.")]
        [SerializeField] private bool _comboHoldEnabled = false;
        [Tooltip("Seconds after a swing's recovery that a press still continues the combo. Past this, CanStringToCombo is set false and the swing exits to idle.")]
        [Min(0f)] [SerializeField] private float _comboHoldSeconds = 1f;
        [Tooltip("Dead time in seconds after the swing's recovery before the hold window opens. A press during this time does not continue the combo.")]
        [Min(0f)] [SerializeField] private float _comboHoldStartDelay = 0f;

        [Header("Debug Time")]
        [Tooltip("Slows the game clock so the swing can be watched. Affects everything that uses Time.deltaTime, and the hold and cooldown timers, which run in game time.")]
        [SerializeField] private bool _useDebugTimeScale = false;
        [Tooltip("Time.timeScale while the debug slow-motion is on. 1 is normal speed.")]
        [Range(0.05f, 1f)] [SerializeField] private float _debugTimeScale = 0.25f;
        private bool _ownsTimeScale = false;

        // Owns the combo hold window. The Animator plays the swings and decides transitions from the parameters set here.
        private CombatStateMachine _combatStateMachine;

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

            _combatStateMachine = new CombatStateMachine(_comboHoldSeconds, _comboHoldStartDelay);
            _combatStateMachine.OnHoldExpired += () => SetCanStringToCombo(false);

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
            _combatStateMachine?.UpdateComboHoldTimer();
        }

        private void Start()
        {
            ApplyDebugTimeScale();
        }

        // Applies the debug slow-motion live when the Inspector value changes during Play Mode.
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            ApplyDebugTimeScale();
        }

        private void OnDisable()
        {
            RestoreTimeScale();
        }

        private void ApplyDebugTimeScale()
        {
            if (_useDebugTimeScale)
            {
                Time.timeScale = _debugTimeScale;
                _ownsTimeScale = true;
            }
            else
            {
                RestoreTimeScale();
            }
        }

        // Only restores the clock if this controller changed it, so it never undoes a pause set elsewhere.
        private void RestoreTimeScale()
        {
            if (!_ownsTimeScale) return;
            Time.timeScale = 1f;
            _ownsTimeScale = false;
        }

        // IAnimationEventReceiver implementation
        public void OnAnimationEvent(string eventName)
        {
            if (_debugCombat)
            {
                Debug.Log($"Combat controller received animation event: {eventName}");
            }

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

                _combatStateMachine?.EndSwing();
            }
            else if (eventName == _attackRecoveryEvent)
            {
                // The strike is done and the blade is returning. The hold window opens here, unless a press already arrived.
                if (_comboHoldEnabled)
                {
                    _combatStateMachine?.BeginComboHold();
                }
            }
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

            // A press during a swing or the hold only opens the gate. The Animator decides whether the next swing plays.
            // A press that lands outside the hold leaves the gate closed, so the swing exits to idle.
            if (_isAttackAnimationPlaying && _currentState == CombatState.Unsheathed)
            {
                // With the hold off, a press during the swing must not open the gate. Nothing would close it afterwards.
                if (!_comboHoldEnabled) return;

                SetCanStringToCombo(true);
                _combatStateMachine?.RegisterPress();
                return;
            }

            // A press in the hold counts only once the start delay has passed.
            if (_combatStateMachine != null && _combatStateMachine.IsPressWindowOpen)
            {
                SetCanStringToCombo(true);
                _combatStateMachine.RegisterPress();
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

        // Ends the combo: the hold is over and no press continued it. Clears the bool so the idle exit can run.
        private void EndCombo()
        {
            _combatStateMachine?.EndCombo();
            SetCanStringToCombo(false);
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

            _combatStateMachine?.StartNewCombo();

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
            return _isAttackAnimationPlaying || IsSwinging() || (_combatStateMachine?.IsComboHolding ?? false);
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
