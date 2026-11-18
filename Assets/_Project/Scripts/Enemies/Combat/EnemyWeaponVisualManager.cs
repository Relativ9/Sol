using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Manages enemy weapon visuals: spawning, positioning (sheathed/unsheathed), and hitbox linkage.
    /// Similar to player's WeaponVisualManager but adapted for enemies.
    /// 
    /// - Spawns weapon prefab at initialization (Awake)
    /// - Maintains weapon position based on equipment state (sheathed/unsheathed)
    /// - Updates EnemyMeleeAttack's hitbox reference when weapon changes
    /// - Weapon is kinematic and follows its mount transform
    /// </summary>
    public class EnemyWeaponVisualManager : MonoBehaviour
    {
        [Header("Mounts")]
        [SerializeField]
        [Tooltip("Where the weapon is held while drawn. Parent this under the right hand bone.")]
        private Transform _rightHandMount;

        [SerializeField]
        [Tooltip("Where the weapon rests while sheathed. Parent this under a hip or spine bone.")]
        private Transform _sheathedMount;

        [Header("State")]
        [SerializeField]
        [Tooltip("Is the weapon currently drawn? If false, weapon is sheathed.")]
        private bool _drawn = false;

        private EnemyStats _enemyStats;
        private EnemyMeleeAttack _meleeAttack;
        private GameObject _weaponInstance;
        private WeaponDataSO _currentWeapon;
        private Vector3 _authoredScale = Vector3.one;

        void Awake()
        {
            _enemyStats = GetComponentInParent<EnemyStats>();
            _meleeAttack = GetComponent<EnemyMeleeAttack>();

            // Validate mounts
            if (_rightHandMount == null || _sheathedMount == null)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemyWeaponVisualManager)}] {name} is missing a weapon mount. " +
                    "Weapon will not be displayed.",
                    this
                );
                enabled = false;
                return;
            }

            // Subscribe to equipment changes
            if (_enemyStats != null)
                _enemyStats.EquipmentChanged += OnEquipmentChanged;

            // Initial weapon spawn if equipped
            SpawnOrUpdateWeapon();
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (_enemyStats != null)
                _enemyStats.EquipmentChanged -= OnEquipmentChanged;

            if (_weaponInstance != null)
                Destroy(_weaponInstance);
        }

        /// <summary>Called by EnemyStats when equipment changes. Updates weapon visuals and hitbox.</summary>
        public void OnEquipmentChanged(EquipmentSlot slot, EquipmentDataSO newItem)
        {
            // Only care about MainHand changes
            if (slot != EquipmentSlot.MainHand) return;

            SpawnOrUpdateWeapon();
        }

        /// <summary>Set the weapon draw state (drawn or sheathed).</summary>
        public void SetDrawn(bool drawn)
        {
            if (_drawn == drawn) return;

            _drawn = drawn;
            RepositionWeapon();
        }

        /// <summary>Spawns or updates the weapon based on current MainHand equipment.</summary>
        private void SpawnOrUpdateWeapon()
        {
            if (_enemyStats == null || _enemyStats.Stats == null)
            {
                DestroyWeapon();
                return;
            }

            EquipmentDataSO equipped = _enemyStats.Stats.ActorStatSO.GetEquipped(EquipmentSlot.MainHand);

            // If nothing equipped or not a weapon, destroy current weapon
            if (equipped == null || !(equipped is WeaponDataSO weapon))
            {
                DestroyWeapon();
                return;
            }

            // If same weapon, don't respawn
            if (_currentWeapon == weapon && _weaponInstance != null) return;

            // Destroy old weapon
            DestroyWeapon();

            // Spawn new weapon
            _currentWeapon = weapon;
            SpawnWeapon(weapon);
        }

        /// <summary>Instantiates the weapon prefab and sets it up for the enemy.</summary>
        private void SpawnWeapon(WeaponDataSO weapon)
        {
            if (weapon.weaponPrefab == null)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemyWeaponVisualManager)}] {weapon.name} has no weaponPrefab. " +
                    "Weapon will not be displayed.",
                    this
                );
                return;
            }

            _weaponInstance = Instantiate(weapon.weaponPrefab);
            _weaponInstance.name = weapon.name;
            _authoredScale = _weaponInstance.transform.localScale;

            // Prepare as held (kinematic, colliders off except hitbox)
            PrepareAsHeld(_weaponInstance);

            // Position on the appropriate mount
            RepositionWeapon();

            // Link hitbox to melee attack if this is a melee weapon
            LinkHitbox();
        }

        /// <summary>Destroys the current weapon instance.</summary>
        private void DestroyWeapon()
        {
            if (_weaponInstance != null)
                Destroy(_weaponInstance);

            _weaponInstance = null;
            _currentWeapon = null;

            // Clear hitbox reference
            if (_meleeAttack != null)
                _meleeAttack.SetWeaponHitbox(null);
        }

        /// <summary>Positions the weapon on the correct mount (drawn or sheathed).</summary>
        private void RepositionWeapon()
        {
            if (_weaponInstance == null || _currentWeapon == null) return;

            Transform mount = _drawn ? _rightHandMount : _sheathedMount;
            Vector3 position = _drawn ? _currentWeapon.unsheathedPosition : _currentWeapon.sheathedPosition;
            Vector3 rotation = _drawn ? _currentWeapon.unsheathedRotation : _currentWeapon.sheathedRotation;

            Transform root = _weaponInstance.transform;
            root.SetParent(mount, worldPositionStays: false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(rotation);
            root.localScale = DivideByParentScale(_authoredScale, mount.lossyScale);
        }

        /// <summary>Links the weapon's hitbox to EnemyMeleeAttack for damage delivery.</summary>
        private void LinkHitbox()
        {
            if (_meleeAttack == null || _weaponInstance == null) return;

            // Find the first collider on the weapon (the hitbox)
            Collider hitbox = _weaponInstance.GetComponent<Collider>();
            if (hitbox == null)
                hitbox = _weaponInstance.GetComponentInChildren<Collider>();

            _meleeAttack.SetWeaponHitbox(hitbox);
        }

        /// <summary>Prepares the weapon instance for being held: kinematic physics and disabled colliders.</summary>
        private static void PrepareAsHeld(GameObject instance)
        {
            // Make all rigidbodies kinematic
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            // Disable all colliders (they'll be enabled by EnemyMeleeAttack when attacking)
            foreach (Collider col in instance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
        }

        /// <summary>Scales a vector by dividing by parent's scale (inverse of lossy scale).</summary>
        private static Vector3 DivideByParentScale(Vector3 value, Vector3 parent)
        {
            return new Vector3(SafeDivide(value.x, parent.x), SafeDivide(value.y, parent.y), SafeDivide(value.z, parent.z));
        }

        /// <summary>Divides safely, returning the original value if divisor is ~0.</summary>
        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
        }
    }
}
