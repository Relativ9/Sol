using UnityEngine;
using UnityEngine.VFX;

namespace Sol
{
    /// <summary>
    /// Shows the equipped main-hand weapon. While drawn it is a child of the right-hand mount, while sheathed of the back mount.
    /// Its local position and rotation on each mount come from WeaponDataSO (sheathed and unsheathed fields). Those values are in
    /// the mount's local space, so the numbers tuned on the asset are the numbers used at runtime. Being a child, the weapon
    /// follows its mount every frame with no per-frame placement.
    /// A shown weapon is a visual only: kinematic, collider-free, and without a WorldItem.
    /// </summary>
    public class WeaponVisualManager : MonoBehaviour, IPlayerComponent, IAnimationEventReceiver
    {
        private const string TrailName = "WeaponBlur";

        [Header("Attack Events")]
        [Tooltip("Starts the trail. Fired at the beginning of the swing.")]
        [SerializeField] private string _attackStartEvent = "OnAttackStart";
        [Tooltip("Stops the trail. Fired when the blade begins returning to idle, so the trail covers the swing only.")]
        [SerializeField] private string _trailEndEvent = "AttackRecovery";

        private AnimationEventDispatcher _eventDispatcher;
        private VisualEffect _trail;
        private WeaponHitbox _hitbox;
        private IStatsService _stats;
        private IStatusController _status;
        
        private const string TrailRateProperty = "Rate";
        [SerializeField] private float _trailRate = 30f;
        
        [Header("Mounts")]
        [Tooltip("Where the weapon rests while sheathed. Parent this under a spine or hip bone.")]
        [SerializeField] private Transform _backMount;
        [Tooltip("Where the weapon is held while drawn. Parent this under the right hand bone.")]
        [SerializeField] private Transform _rightHandMount;

        [Header("Debug")]
        [SerializeField] private bool _debugPlacement = false;

        private ICombatController _combat;
        private IEquipmentService _equipment;
        private GameObject _instance;
        private WeaponDataSO _shownWeapon;
        private Vector3 _authoredScale = Vector3.one;
        private bool _shownDrawn;

        public void Initialize(IPlayerContext context)
        {
            _combat = context.GetService<ICombatController>();
            _equipment = ServiceLocator.TryGet<IEquipmentService>();
            _stats = ServiceLocator.TryGet<IStatsService>();
            _status = context.GetService<IStatusController>();

            if (_backMount == null || _rightHandMount == null)
            {
                Debug.LogError($"[{nameof(WeaponVisualManager)}] A mount is not assigned. The weapon will not be shown.", this);
                enabled = false;
                return;
            }

            if (_equipment != null) _equipment.Changed += Rebuild;

            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                _eventDispatcher = animator.gameObject.GetComponent<AnimationEventDispatcher>();
                if (_eventDispatcher == null)
                    _eventDispatcher = animator.gameObject.AddComponent<AnimationEventDispatcher>();
                _eventDispatcher.RegisterReceiver(this, _attackStartEvent);
                _eventDispatcher.RegisterReceiver(this, _trailEndEvent);
            }

            Rebuild();
        }

        private void OnDestroy()
        {
            if (_equipment != null) _equipment.Changed -= Rebuild;
            if (_eventDispatcher == null) return;
            _eventDispatcher.UnregisterReceiver(this, _attackStartEvent);
            _eventDispatcher.UnregisterReceiver(this, _trailEndEvent);
        }

        // The trail shows only during a swing. Enabled on the attack start, cleared on the attack end.
        public void OnAnimationEvent(string eventName)
        {
            if (_trail == null) return;

            if (eventName == _attackStartEvent)
            {
                if (_trail != null)
                {
                    _trail.Reinit();
                    _trail.SetFloat(TrailRateProperty, _trailRate);
                  
                }
                _hitbox?.BeginSwing();
            }
            else if (eventName == _trailEndEvent)
            {
                // Stopping emission keeps the existing segments, which then fade over the trail's Time (5 s on this prefab).
                // Clearing removes them now, so the trail disappears when the blade reaches its resting point.
                if (_trail != null)
                {
                    _trail.SetFloat(TrailRateProperty, 0f);
                    _trail.Reinit();
                }
                _hitbox?.EndSwing();
            }
        }

        // Only the mount changes between states. The weapon itself is a child, so it needs no per-frame work.
        private void LateUpdate()
        {
            bool drawn = _combat != null && _combat.IsWeaponDrawn();
            if (drawn != _shownDrawn) Place(drawn);
        }

        // Rebuilds the instance from the main-hand weapon. Only swaps when the weapon really changed.
        private void Rebuild()
        {
            WeaponDataSO weapon = _equipment != null
                ? _equipment.GetEquipped(EquipmentSlot.MainHand) as WeaponDataSO
                : null;

            if (weapon == _shownWeapon && _instance != null) return;

            if (_instance != null) Destroy(_instance);
            _instance = null;
            _shownWeapon = weapon;

            if (weapon == null) return;
            if (weapon.weaponPrefab == null)
            {
                Debug.LogWarning($"[{nameof(WeaponVisualManager)}] '{weapon.name}' has no weaponPrefab, so it cannot be shown.", weapon);
                return;
            }

            _instance = Instantiate(weapon.weaponPrefab);
            // Taken before the weapon is parented, so it is the scale the prefab was authored at.
            _authoredScale = _instance.transform.localScale;
            PrepareAsHeld(_instance);
            _trail = FindTrail(_instance.transform);
            _hitbox = _instance.GetComponent<WeaponHitbox>();
            if (_hitbox == null)
                Debug.LogWarning($"[{nameof(WeaponVisualManager)}] '{_instance.name}' has no WeaponHitbox, so swings cannot hit anything. " +
                                 "Add a WeaponHitbox to the blade's trigger collider on the held prefab.", _instance);
            _hitbox?.Configure(weapon, _stats, _status);
            Place(_combat != null && _combat.IsWeaponDrawn());
        }

        // Parents the weapon to the mount for its state, then sets its local pose from the asset. The scale is divided by the
        // mount's world scale, so the weapon keeps its authored size on a scaled rig (Mixamo rigs often use 0.01).
        private void Place(bool drawn)
        {
            _shownDrawn = drawn;
            if (_instance == null || _shownWeapon == null) return;

            Transform mount = drawn ? _rightHandMount : _backMount;
            Vector3 position = drawn ? _shownWeapon.unsheathedPosition : _shownWeapon.sheathedPosition;
            Vector3 rotation = drawn ? _shownWeapon.unsheathedRotation : _shownWeapon.sheathedRotation;

            Transform root = _instance.transform;
            root.SetParent(mount, worldPositionStays: false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(rotation);
            root.localScale = DivideByParentScale(_authoredScale, mount.lossyScale);

            if (_debugPlacement)
                Debug.Log($"[{nameof(WeaponVisualManager)}] '{root.name}' on {mount.name}: local position {position}, rotation {rotation}.", root);
        }

        // Finds the swing trail on the weapon. A weapon without one is allowed, and simply shows no trail.
        private static VisualEffect FindTrail(Transform root)
        {
            Transform trail = root.Find(TrailName);
            if (trail == null) trail = FindDeep(root, TrailName);
            if (trail == null) return null;

            VisualEffect effect = trail.GetComponent<VisualEffect>();
            if (effect != null) effect.SetFloat(TrailRateProperty, 0f);

            return effect;
        }

        private static Transform FindDeep(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                Transform found = FindDeep(child, childName);
                if (found != null) return found;
            }
            return null;
        }

        // A held weapon follows its mount, so physics must not move it. It has no colliders and no world identity.
        private static void PrepareAsHeld(GameObject instance)
        {
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            // The blade's hitbox owns its own enabled state, so it is left alone here. Every other collider is switched off.
            WeaponHitbox hitbox = instance.GetComponent<WeaponHitbox>();
            foreach (Collider col in instance.GetComponentsInChildren<Collider>())
            {
                if (hitbox != null && col.gameObject == hitbox.gameObject) continue;
                col.enabled = false;
            }

            // A held weapon must never be a world item. The held prefab is authored without WorldItem, and the world copy is a
            // separate prefab (WeaponDataSO.WorldPrefab). Reported here instead of being rewritten, so the setup is fixed in the asset.
            if (instance.GetComponent<WorldItem>() != null)
                Debug.LogError($"[{nameof(WeaponVisualManager)}] '{instance.name}' is the held weapon prefab but has a WorldItem. " +
                               "Remove it from the held prefab; the world copy belongs in WorldPrefab.", instance);
        }

        private static Vector3 DivideByParentScale(Vector3 value, Vector3 parent)
        {
            return new Vector3(SafeDivide(value.x, parent.x), SafeDivide(value.y, parent.y), SafeDivide(value.z, parent.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
        }
    }
}
