using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Sol
{
    /// <summary>
    /// Sets the hand IK weight on the drawn weapon rig. The weight is 1 only when the weapon is drawn and resting. It is 0 while
    /// an attack plays (between OnAttackStart and OnAttackEnd), and while the weapon is being drawn or sheathed, so the
    /// animation clip controls the arm for every weapon movement.
    /// </summary>
    public class WeaponRigController : MonoBehaviour, IPlayerComponent, IAnimationEventReceiver
    {
        [Header("Rig")]
        [Tooltip("The drawn weapon rig. It holds the Right Hand IK constraint.")]
        [SerializeField] private Rig _handRig;

        [Header("Attack Events")]
        [SerializeField] private string _attackStartEvent = "OnAttackStart";
        [SerializeField] private string _attackEndEvent = "OnAttackEnd";

        private ICombatController _combat;
        private AnimationEventDispatcher _eventDispatcher;
        private Animator _animator;

        // Animator layer indices. Base is 0, WeaponLayer 1, Hands 2 (see Player.controller).
        private const int WeaponLayerIndex = 1;
        private const int HandsLayerIndex = 2;

        [Header("Layer Blend")]
        [Tooltip("Seconds for the Hands and WeaponLayer weights to fade in or out, so the arm does not snap between poses.")]
        [SerializeField] private float _layerBlendSeconds = 0.15f;
        private float _handsWeight;
        private float _weaponLayerWeight;
        private bool _attacking;

        public void Initialize(IPlayerContext context)
        {
            _combat = context.GetService<ICombatController>();

            if (_handRig == null)
            {
                Debug.LogError($"[{nameof(WeaponRigController)}] No hand rig assigned. The weapon hand will not be held or released.", this);
                enabled = false;
                return;
            }

            Animator animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError($"[{nameof(WeaponRigController)}] No Animator in children. Attack events will not reach the rig.", this);
                enabled = false;
                return;
            }

            _animator = animator;
            _eventDispatcher = animator.gameObject.GetComponent<AnimationEventDispatcher>();
            if (_eventDispatcher == null)
                _eventDispatcher = animator.gameObject.AddComponent<AnimationEventDispatcher>();

            _eventDispatcher.RegisterReceiver(this, _attackStartEvent);
            _eventDispatcher.RegisterReceiver(this, _attackEndEvent);

            _handRig.weight = Holds() ? 1f : 0f;
        }

        private void OnDestroy()
        {
            if (_eventDispatcher == null) return;
            _eventDispatcher.UnregisterReceiver(this, _attackStartEvent);
            _eventDispatcher.UnregisterReceiver(this, _attackEndEvent);
        }

        // The events only record that a swing is playing. The weight is set in one place, Update.
        public void OnAnimationEvent(string eventName)
        {
            if (eventName == _attackStartEvent) _attacking = true;
            else if (eventName == _attackEndEvent) _attacking = false;
        }

        private void Update()
        {
            if (_handRig == null || _combat == null) return;

            // If an attack is cancelled, its end event may never arrive. The combat controller clears its own attack state,
            // so the rig follows it and cannot stay released.
            if (_attacking && !_combat.IsAttacking()) _attacking = false;

            // An attack counts from the moment it is requested, not from its event. The event fires only once the clip is
            // playing, and the clip cannot play until the WeaponLayer is on, so waiting for the event never starts the swing.
            bool swinging = _attacking || _combat.IsAttacking();
            bool hold = !swinging && Holds();
            _handRig.weight = hold ? 1f : 0f;

            // The same state drives the animator layers. The grip layer is on only while holding. The weapon layer plays
            // the swing and the draw/sheathe clips, so it is on whenever the weapon is in a swing or a transition.
            if (_animator != null)
            {
                _handsWeight = Mathf.MoveTowards(_handsWeight, hold ? 1f : 0f, Time.deltaTime / _layerBlendSeconds);
                _weaponLayerWeight = Mathf.MoveTowards(_weaponLayerWeight, IsWeaponLayerActive() ? 1f : 0f, Time.deltaTime / _layerBlendSeconds);
                _animator.SetLayerWeight(HandsLayerIndex, _handsWeight);
                _animator.SetLayerWeight(WeaponLayerIndex, _weaponLayerWeight);
            }
        }

        private bool IsWeaponLayerActive()
        {
            if (_attacking || _combat.IsAttacking()) return true;
            CombatState state = _combat.GetCurrentCombatState();
            return state == CombatState.Unsheathing || state == CombatState.Sheathing;
        }

        // Held only when the weapon is drawn and the combat state is resting in Unsheathed. Sheathing, unsheathing and the
        // sheathed state all make this false, so the weight is 0 through every weapon transition.
        private bool Holds()
        {
            return _combat.IsWeaponDrawn() && _combat.GetCurrentCombatState() == CombatState.Unsheathed;
        }
    }
}
