using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Hurts the player while they stay inside this trigger, once per cooldown.
    /// Builds each hit from a DamageSO, so this component never knows what the damage does.
    /// Used for the training dummy now, and reusable for traps and enemies later.
    /// Requires a Collider set to Is Trigger, and the player tagged "Player".
    /// </summary>
    public class EnemyDamageSource : MonoBehaviour
    {
        [Tooltip("The trigger collider to check. Can be on this object or a child. Leave empty to use one on this object.")]
        [SerializeField] private Collider _trigger;

        [SerializeField] private DamageSO _damage;

        [Tooltip("Seconds between hits while the player stays inside.")]
        [SerializeField] private float _cooldown = 1f;

        [Tooltip("Name shown in the combat log. Leave empty to use this object's name.")]
        [SerializeField] private string _sourceName = "";

        private float _nextHitTime;

        private string SourceId => string.IsNullOrEmpty(_sourceName) ? name : _sourceName;

        void Awake()
        {
            if (_damage == null)
                Debug.LogWarning($"[{nameof(EnemyDamageSource)}] {name} has no DamageSO assigned. It will do nothing.", this);

            if (_trigger == null) _trigger = GetComponent<Collider>();

            if (_trigger == null)
                Debug.LogWarning($"[{nameof(EnemyDamageSource)}] {name} has no trigger collider assigned or on this object.", this);
            else if (!_trigger.isTrigger)
                Debug.LogWarning($"[{nameof(EnemyDamageSource)}] {name}: trigger collider must be set to Is Trigger.", this);
        }

        // Trigger messages (OnTriggerEnter/Exit) are delivered to the object that owns the collider's Rigidbody,
        // or to the collider's own object when there is no Rigidbody. A holder object therefore needs a
        // kinematic Rigidbody to receive them. This is a property of Unity's routing, not of which message is used.
        private IDamageReceiver _receiverInside;

        void OnTriggerEnter(Collider other)
        {
            if (_damage == null || !other.CompareTag("Player")) return;

            _receiverInside = other.GetComponentInParent<IDamageReceiver>();
            if (_receiverInside == null) return;

            // Entry is a discrete hit. Staying inside re-hits once per cooldown, handled in Update.
            _nextHitTime = Time.time + _cooldown;
            _receiverInside.ReceiveHit(BuildHit());
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _receiverInside = null;
        }

        void Update()
        {
            if (_receiverInside == null || _damage == null) return;
            if (Time.time < _nextHitTime) return;

            _nextHitTime = Time.time + _cooldown;
            _receiverInside.ReceiveHit(BuildHit());
        }

        // The enemy's stat sheet supplies the status chances, so enchantments and traits on the enemy apply here.
        // Without an EnemyStats on this object or a parent, the DamageSO's own chances are used.
        // Knockback origin is the enemy's ROOT (its body), not this trigger: the trigger is the weapon head,
        // so using it made every push point toward the enemy's front. The hit point is the trigger.
        private DamageInfo BuildHit()
        {
            Vector3 origin = EnemyStats.RootPosition(transform);
            Vector3 hitPoint = transform.position;

            // The enemy's stats only add bonuses. The base chance is doctrine, computed inside DamageSO.Build.
            ActorStats stats = EnemyStats.Find(transform);
            if (stats == null)
                return _damage.Build(SourceId, 0f, false, origin, hitPoint);

            return _damage.Build(SourceId, 0f, false, origin, hitPoint,
                stats.GetStat(StatTypeEnum.StaggerChance),
                stats.GetStat(StatTypeEnum.BleedChance),
                stats.GetStat(StatTypeEnum.Penetration));
        }
    }
}
