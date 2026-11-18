using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Melee attack system for enemies: weapon hitbox delivery, animation triggering, and animation cancellation on stagger.
    /// The weapon hitbox is a child Collider of the weapon mesh (not the body), providing clear visual reach feedback.
    /// Damage is delivered via OnTriggerStay contact with the player's IDamageReceiver.
    /// 
    /// Animation cancellation is handled by StatusController when stagger is applied: it calls SetAnimatorTrigger("Stagger"),
    /// which immediately interrupts any in-flight attack and plays recoil animation.
    /// 
    /// Layer Weight Management:
    /// - Matches player's CombatStateController architecture with 3-layer animator
    /// - During attack: Combat layer (1) active, Hands layer (2) hidden
    /// - During idle: Combat layer (0), Hands layer (1) for weapon pose
    /// 
    /// Single responsibility: attack delivery only. Does not decide when to attack (EnemyBehavior does).
    /// </summary>
    public class EnemyMeleeAttack : MonoBehaviour, IAnimationEventReceiver
    {
        [SerializeField]
        [Tooltip("Fallback damage profile when unarmed. Base amount should be 0 so damage comes from stats only.")]
        private DamageProfileSO _unarmedDamageProfile;

        private Collider _weaponHitbox;

        [SerializeField]
        [Tooltip("How long after an attack before another can be triggered. Seconds.")]
        private float _attackCooldown = 2f;

        [SerializeField]
        [Tooltip("Animator parameter for triggering an attack animation.")]
        private string _attackTrigger = "Attack";

        [Header("Layer Management (matches player architecture)")]
        [SerializeField]
        [Tooltip("Index of the Combat layer in the animator (attack animations). Usually 1.")]
        [Min(0)]
        private int _combatLayerIndex = 1;

        [SerializeField]
        [Tooltip("Index of the Hands layer in the animator (sheathe/unsheathe anims). Usually 2.")]
        [Min(0)]
        private int _handsLayerIndex = 2;

        [SerializeField]
        [Tooltip("Enable debug logging for attack events and layer transitions.")]
        private bool _debugAttack = false;

        private Animator _animator;
        private EnemyStats _stats;
        private EnemyWeaponVisualManager _weaponVisuals;
        private AnimationEventDispatcher _eventDispatcher;
        private bool _hitThisSwing;

        void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
                Debug.LogWarning($"[EnemyMeleeAttack] {name} has no Animator. Attacks will not play.", this);

            _stats = GetComponentInParent<EnemyStats>();
            _weaponVisuals = GetComponent<EnemyWeaponVisualManager>();
            _eventDispatcher = GetComponentInChildren<AnimationEventDispatcher>();

            if (_weaponHitbox == null)
                Debug.LogWarning($"[EnemyMeleeAttack] {name} has no weapon hitbox assigned. Damage will not be delivered.", this);
        }

        void Start()
        {
            // Register to receive animation events from the dispatcher
            if (_eventDispatcher != null)
            {
                _eventDispatcher.RegisterReceiver(this, "OnAttackEnd");
            }
        }

        void OnDestroy()
        {
            // Unregister from animation events
            if (_eventDispatcher != null)
            {
                _eventDispatcher.UnregisterReceiver(this, "OnAttackEnd");
            }
        }

        /// <summary>
        /// Trigger an attack animation. Called by EnemyBehavior when cooldown is ready.
        /// Activates Combat layer and hides Hands layer for smooth weapon draw during attack.
        /// Enables weapon collider for damage delivery.
        /// </summary>
        public void PlayAttack()
        {
            if (_animator == null) return;

            // Switch animator layers: show Combat, hide Hands
            // This matches player's CombatStateController unsheathe behavior
            SetLayerWeights(combatActive: true);

            // Draw weapon visually (weapon moves from hip to hand)
            if (_weaponVisuals != null)
                _weaponVisuals.SetDrawn(true);

            // Enable weapon collider for damage delivery
            if (_weaponHitbox != null)
                _weaponHitbox.enabled = true;

            // Trigger the attack animation
            _animator.SetTrigger(_attackTrigger);
            _hitThisSwing = false;

            if (_debugAttack)
                Debug.Log($"[EnemyMeleeAttack] {name} PlayAttack - Combat layer active, weapon drawn, collider enabled");
        }

        /// <summary>
        /// Called after an attack completes (via animation event or behavior).
        /// Restores animator layers to idle state and sheathes weapon.
        /// Disables weapon collider for safety.
        /// </summary>
        public void EndAttack()
        {
            if (_animator == null) return;

            // Switch animator layers: hide Combat, show Hands
            // This matches player's CombatStateController sheathe behavior
            SetLayerWeights(combatActive: false);

            // Sheathe weapon visually (weapon moves from hand to hip)
            if (_weaponVisuals != null)
                _weaponVisuals.SetDrawn(false);

            // Disable weapon collider (safety, in case animation event missed)
            if (_weaponHitbox != null)
                _weaponHitbox.enabled = false;

            _hitThisSwing = false;

            if (_debugAttack)
                Debug.Log($"[EnemyMeleeAttack] {name} EndAttack - Combat layer inactive, weapon sheathed, collider disabled");
        }

        /// <summary>
        /// Controls animator layer weights to match player's 3-layer architecture:
        /// Combat layer (1) for attack animations, Hands layer (2) for poses.
        /// </summary>
        private void SetLayerWeights(bool combatActive)
        {
            if (_animator == null) return;

            // Combat layer: 1 during attack, 0 during idle
            float combatWeight = combatActive ? 1f : 0f;
            _animator.SetLayerWeight(_combatLayerIndex, combatWeight);

            // Hands layer: 0 during attack (weapon in hand), 1 during idle (weapon sheathed)
            float handsWeight = combatActive ? 0f : 1f;
            _animator.SetLayerWeight(_handsLayerIndex, handsWeight);
        }

        void Update()
        {
            // Reset hit flag when attack animation ends
            if (_animator != null)
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsTag("Attack") && _hitThisSwing)
                    _hitThisSwing = false;
            }
        }

        /// <summary>Called by EnemyWeaponVisualManager to link the weapon's hitbox. Can be null if unarmed.</summary>
        public void SetWeaponHitbox(Collider hitbox)
        {
            _weaponHitbox = hitbox;
        }

        void OnTriggerStay(Collider other)
        {
            if (_hitThisSwing) return;
            
            // Check if Combat layer (where attacks play) is active and in an Attack state
            if (_animator == null)
                return;
            
            AnimatorStateInfo combatStateInfo = _animator.GetCurrentAnimatorStateInfo(_combatLayerIndex);
            if (!combatStateInfo.IsTag("Attack"))
                return;

            // If we have a weapon hitbox, only damage if the collision is with the weapon
            if (_weaponHitbox != null && other != _weaponHitbox)
                return;

            // Find the damage receiver on the hit object (player or another target)
            IDamageReceiver receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver == null) return;

            // Only hit once per swing
            _hitThisSwing = true;

            // Determine damage profile and bonus damage from equipped weapon
            DamageProfileSO damageProfile = _unarmedDamageProfile;
            float weaponBonus = 0f;

            if (damageProfile == null)
            {
                Debug.LogError($"[EnemyMeleeAttack] {name} has no unarmed damage profile. Cannot deliver damage.", this);
                return;
            }

            if (_stats != null && _stats.Stats != null)
            {
                EquipmentDataSO equipped = _stats.Stats.ActorStatSO.GetEquipped(EquipmentSlot.MainHand);
                if (equipped is WeaponDataSO weapon)
                {
                    // Add weapon damage bonus (damage profile remains unarmed with weapon bonus applied)
                    weaponBonus = weapon.weaponDamage;
                }
            }

            // Build and deliver damage
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            DamageInfo hit = damageProfile.Build(
                _stats != null ? _stats.name : "Enemy",
                weaponBonus, false, // Add weapon bonus to the damage
                transform.position,
                hitPoint,
                _stats != null ? _stats.Stats.GetStat(StatTypeEnum.StaggerChance) : 0f,
                _stats != null ? _stats.Stats.GetStat(StatTypeEnum.BleedChance) : 0f,
                _stats != null ? _stats.Stats.GetStat(StatTypeEnum.Penetration) : 0f
            );

            receiver.ReceiveHit(hit);
        }

        /// <summary>Debug visualization of weapon reach.</summary>
        void OnDrawGizmosSelected()
        {
            if (_weaponHitbox == null) return;

            Gizmos.color = Color.red;
            if (_weaponHitbox is BoxCollider box)
                Gizmos.DrawCube(_weaponHitbox.transform.position, box.size * 2f);
            else if (_weaponHitbox is SphereCollider sphere)
                Gizmos.DrawWireSphere(_weaponHitbox.transform.position, sphere.radius);
        }

        /// <summary>
        /// IAnimationEventReceiver implementation: Called when animation events fire.
        /// Primarily handles OnAttackEnd to restore idle state and sheathe weapon.
        /// </summary>
        public void OnAnimationEvent(string eventName)
        {
            if (eventName == "OnAttackEnd")
            {
                EndAttack();
            }
        }
    }
}
