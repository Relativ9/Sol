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

        [Header("Weapon Layer")]
        [Tooltip("Animator layer index of the weapon (Combat) layer. Its weight is 1 only while a weapon is in hand (OneHanded or TwoHanded), otherwise 0.")]
        [Min(0)] [SerializeField] private int _weaponLayerIndex = 1;

        [Header("Combo")]
        [Tooltip("Animator bool. True while a queued press is waiting for the chain. The idle exits need it false, so a queued press holds the exit closed until the chain takes it.")]
        [SerializeField] private string _canStringToComboParam = "CanStringToCombo";
        [Tooltip("Animator int. The swing number the combo is on (1..n). Set to the next number on a press, and to 0 when no melee combo state is playing.")]
        [SerializeField] private string _comboIndexParam = "ComboIndex";
        [Tooltip("Number of swings in the combo. Presses are ignored once the last swing has been queued.")]
        [Min(1)] [SerializeField] private int _comboLength = 3;
        [Tooltip("Real seconds the combo hold lasts. It opens at AttackRecovery, and the rest of the swing (up to Hold End Normalized Time) is played at whatever speed fills exactly this time. A queued press is taken by the chain only during the hold.")]
        [Min(0.05f)] [SerializeField] private float _comboHoldSeconds = 0.5f;
        [Tooltip("Normalised clip time where the hold ends. Match it to the Exit Time of the idle exit on the chained attack states, so the hold ends exactly when the swing would exit to idle.")]
        [Range(0f, 1f)] [SerializeField] private float _holdEndNormalizedTime = 0.8f;
        [Tooltip("Animator float used as the Speed Multiplier of the melee combo states. Only those states are slowed during the hold, not locomotion or other layers.")]
        [SerializeField] private string _comboSpeedParam = "ComboSpeed";
        [Tooltip("Animator tag on every melee combo state. A swing is live while the combo layer is in a state with this tag. When none is playing, the combo ends.")]
        [SerializeField] private string _comboTag = "MeleeCombo";
        [Tooltip("Index of the Combat (weapon) layer in the Animator. Same as _weaponLayerIndex.")]
        [Min(0)] [SerializeField] private int _comboLayerIndex = 1;

        // Combo state.
        // _currentSwing: the swing the Animator is playing (1..n), 0 when no melee combo state is playing.
        // _comboIndex: the Animator's ComboIndex. Equals _currentSwing, or _currentSwing + 1 once a press is queued.
        // _attackQueued: one press waiting for the next swing. Further presses in the same swing are ignored.
        // _holdOpen: from AttackRecovery until _comboHoldSeconds later. The chain can only take the queued press then.
        // _comboActive: true from the idle press until the combo ends (it covers the frames before swing 1 has started).
        private bool _comboActive = false;
        private float _comboStartedAt = -10f;
        // Longest the Animator may take to start swing 1 after an idle press before the press is dropped.
        private const float MaxSwingStartWait = 0.5f;
        private int _currentSwing = 0;
        private int _currentSwingHash = 0;
        private int _comboIndex = 0;
        private bool _attackQueued = false;
        private bool _holdOpen = false;
        private float _holdEndsAt = -10f;
        private bool _hasComboSpeedParam = false;
        private Animator _animator;
        private int _comboTagHash;

        [Header("Debug Time")]
        [Tooltip("Slows the game clock so the swing can be watched. Affects everything that uses Time.deltaTime, and the cooldown and swing timers, which run in game time.")]
        [SerializeField] private bool _useDebugTimeScale = false;
        [Tooltip("Time.timeScale while the debug slow-motion is on. 1 is normal speed.")]
        [Range(0.05f, 1f)] [SerializeField] private float _debugTimeScale = 0.25f;
        private bool _ownsTimeScale = false;

        [Header("Animation Events")]
        [SerializeField] private string _attackStartEvent = "OnAttackStart";
        [SerializeField] private string _attackEndEvent = "OnAttackEnd";
        [Tooltip("Fired while the blade is returning. The combo does not use it (presses are queued). Kept for other listeners, such as the weapon trail.")]
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
            _animator = animator;
            _comboTagHash = Animator.StringToHash(_comboTag);
            if (animator == null)
            {
                Debug.LogError("No Animator found in children! Animation events won't work.");
                return;
            }

            _hasComboSpeedParam = HasFloatParameter(animator, _comboSpeedParam);
            if (!_hasComboSpeedParam)
            {
                Debug.LogWarning($"Animator has no float parameter '{_comboSpeedParam}'. The combo hold will not change the swing speed.");
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
            SetWeaponLayer(false);

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
            TrackComboFromAnimator();

            if (_holdOpen && Time.time >= _holdEndsAt)
            {
                CloseHold();
            }
        }

        // The Animator decides which swing is playing. Each frame, read the combo layer: the state it is in, or the state
        // it is blending into. A new melee state is a new swing. Leaving every melee state, or a melee state blending out
        // to something else, ends the combo. No animation event is needed for either, so a cut or faded-out
        // OnAttackStart or OnAttackEnd cannot leave the counter wrong.
        private void TrackComboFromAnimator()
        {
            if (_animator == null || !_comboActive) return;

            bool inTransition = _animator.IsInTransition(_comboLayerIndex);
            AnimatorStateInfo state = inTransition
                ? _animator.GetNextAnimatorStateInfo(_comboLayerIndex)
                : _animator.GetCurrentAnimatorStateInfo(_comboLayerIndex);

            if (state.tagHash == _comboTagHash)
            {
                // The first melee state seen after an idle press is swing 1. Any later new one is the chained swing.
                if (state.fullPathHash != _currentSwingHash)
                {
                    BeginSwing(state.fullPathHash);
                }
                return;
            }

            // Not in, or not heading into, a melee state. Before the first swing has started the idle press is still on
            // its way (the Attack trigger is set but not taken yet). Only a trigger that is no longer pending means
            // the Animator did not take it.
            if (_currentSwing == 0)
            {
                // A trigger the Animator never takes (for example no one-handed weapon) must not keep the combo live.
                bool waiting = _animator.GetBool(_attackTrigger) && Time.time < _comboStartedAt + MaxSwingStartWait;
                if (waiting) return;

                _animationController?.ResetTrigger(_attackTrigger);
                if (_debugCombat) Debug.Log("[Combo] The idle press did not start a swing. Combo ended.");
                EndCombo();
                return;
            }

            if (_debugCombat) Debug.Log($"[Combo] Swing {_currentSwing} left the combo. ComboIndex {_comboIndex} -> 0.");
            EndCombo();
        }

        // A new melee state has started (or is blending in). Its number is one up from the last.
        private void BeginSwing(int stateHash)
        {
            _currentSwing = Mathf.Min(_currentSwing + 1, _comboLength);
            _currentSwingHash = stateHash;
            _attackQueued = false;
            _isAttackAnimationPlaying = true;
            CloseHold();
            SetComboIndex(_currentSwing);

            if (_debugCombat) Debug.Log($"[Combo] Swing {_currentSwing} started.");
        }

        // AttackRecovery: the strike is done and the blade is returning. The hold opens, and the rest of the swing up to
        // _holdEndNormalizedTime is played at the speed that makes it last exactly _comboHoldSeconds.
        private void OpenHold()
        {
            if (_currentSwing == 0 || _holdOpen) return;

            _holdOpen = true;
            _holdEndsAt = Time.time + _comboHoldSeconds;

            // The swing that is being chained into may already be blending in. Only slow the swing that owns the hold.
            if (_animator != null && !_animator.IsInTransition(_comboLayerIndex))
            {
                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_comboLayerIndex);
                float clipLength = CurrentClipLength();
                float remainingNorm = _holdEndNormalizedTime - state.normalizedTime;
                // Never above 1: the hold only ever slows the swing. If the hold is shorter than the rest of the swing
                // at normal speed, the swing plays at 1 and the hold simply ends before the idle exit.
                float speed = clipLength > 0f && remainingNorm > 0f
                    ? Mathf.Min(1f, clipLength * remainingNorm / _comboHoldSeconds)
                    : 1f;
                SetComboSpeed(speed);

                if (_debugCombat)
                {
                    Debug.Log($"[Combo] Hold open on swing {_currentSwing} at {state.normalizedTime:F2}. " +
                              $"Clip {clipLength:F2}s, playing to {_holdEndNormalizedTime:F2} at speed {speed:F2} over {_comboHoldSeconds:F2}s.");
                }
            }

            // A press made earlier in the swing is taken now.
            if (_attackQueued)
            {
                SetCanStringToCombo(true);
            }
        }

        // Ends the hold: the swing plays at normal speed again, and the chain gate closes.
        private void CloseHold()
        {
            _holdOpen = false;
            SetComboSpeed(1f);
            SetCanStringToCombo(false);
        }

        private void SetComboSpeed(float speed)
        {
            if (!_hasComboSpeedParam) return;
            _animationController?.SetFloat(_comboSpeedParam, speed);
        }

        private float CurrentClipLength()
        {
            AnimatorClipInfo[] clips = _animator.GetCurrentAnimatorClipInfo(_comboLayerIndex);
            return clips.Length > 0 ? clips[0].clip.length : 0f;
        }

        private static bool HasFloatParameter(Animator animator, string name)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == name) return true;
            }
            return false;
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
            SetComboSpeed(1f);
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
                // Swing starts are read from the Animator state in TrackComboFromAnimator, not from this event, so a
                // start that is blended over cannot be missed. Other listeners (hitbox, trail) still use the event.
            }
            else if (eventName == _attackEndEvent)
            {
                // Combo end is read from the Animator state in TrackComboFromAnimator. This event can be cut or blended
                // out, so nothing depends on it.
                if (_debugCombat)
                {
                    Debug.Log("Attack completed");
                }
            }
            else if (eventName == _attackRecoveryEvent)
            {
                OpenHold();
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

            // A press during a live combo queues the next swing. It is counted at once: ComboIndex becomes the next
            // swing's number. Only one press is queued per swing, so spamming cannot skip ahead. The chain only takes
            // the queued press while the hold is open. A press before swing 1 has started, or during the last swing,
            // does nothing.
            if (_comboActive)
            {
                bool canQueue = !_attackQueued
                                && _currentSwing > 0
                                && _currentSwing < _comboLength
                                && _currentState == CombatState.Unsheathed;
                if (canQueue)
                {
                    _attackQueued = true;
                    SetComboIndex(_currentSwing + 1);
                    if (_holdOpen)
                    {
                        SetCanStringToCombo(true);
                    }
                    if (_debugCombat) Debug.Log($"[Combo] Press queued swing {_currentSwing + 1} (hold open: {_holdOpen}).");
                }
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

        // Ends the combo: drops any queued press, closes the hold and the chain gate, and sets the counter to 0.
        // Called when the Animator leaves the melee states, and on cancel and force-sheathe.
        private void EndCombo()
        {
            _comboActive = false;
            _currentSwing = 0;
            _currentSwingHash = 0;
            _attackQueued = false;
            CloseHold();
            SetComboIndex(0);

            _isAttackAnimationPlaying = false;
            _isAttacking = false;
            _context?.SetStateValue("IsAttacking", false);
        }

        private void SetComboIndex(int value)
        {
            _comboIndex = value;
            _animationController?.SetInteger(_comboIndexParam, _comboIndex);
        }

        private void StartAttack()
        {
            // Set attack state
            _isAttacking = true;
            _lastAttackTime = Time.time;

            // The combo is live from this press. ComboIndex stays 0 here, because the entry transition into Attack 1
            // requires ComboIndex == 0. It becomes 1 when the Animator actually enters swing 1 (BeginSwing).
            _comboActive = true;
            _comboStartedAt = Time.time;
            _currentSwing = 0;
            _currentSwingHash = 0;
            _attackQueued = false;
            CloseHold();
            SetComboIndex(0);

            // Clear any trigger left over from an attack the Animator never took, so it cannot fire later out of sequence.
            if (_animationController != null)
            {
                _animationController.ResetTrigger(_attackTrigger);
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
            SetWeaponLayer(oneHanded);

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
            SetWeaponLayer(false);

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
            SetWeaponLayer(false);

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

        // The weapon layer only contributes while a weapon is in hand. Weight 0 hides it, so an idle hand never
        // shows the weapon pose. Set it in the same place the OneHanded / TwoHanded bools are set.
        private void SetWeaponLayer(bool weaponInHand)
        {
            _animationController?.SetLayerWeight(_weaponLayerIndex, weaponInHand ? 1f : 0f);
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
            bool wasAttacking = _comboActive;

            // A cancel ends the combo, so the idle exit is never blocked by a stale bool and the counter is 0.
            EndCombo();

            if (wasAttacking && _debugCombat)
            {
                Debug.Log("Attack cancelled");
            }
        }

        // True for the swing window that starts when an attack begins. Driven by time, not animation events, because the clip has none.
        // True while a melee combo swing is playing, read from the Animator state (see TrackComboFromAnimator).
        public bool IsSwinging()
        {
            return _currentSwing > 0;
        }

        // Helper method to check if player is currently in an attack
        public bool IsAttacking()
        {
            return _comboActive;
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
            GUILayout.Label($"Combo: active {_comboActive}, swing {_currentSwing}, ComboIndex {_comboIndex}, queued {_attackQueued}, hold {_holdOpen}");

            GUILayout.EndArea();
        }
    }
}
