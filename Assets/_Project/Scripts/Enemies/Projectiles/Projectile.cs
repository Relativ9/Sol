using UnityEngine;

namespace Sol
{
    /// <summary>
    /// A moving hit. Travels straight at a fixed speed, delivers its DamageSO once on the first receiver it
    /// touches, then despawns. Requires a Collider set to Is Trigger. Uses the same receiver path as every other hit.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private DamageSO _damage;
        [SerializeField] private float _speed = 20f;
        [SerializeField] private float _lifetime = 5f;
        [Tooltip("Combat log name. Leave empty to use this object's name.")]
        [SerializeField] private string _sourceName = "";

        private Rigidbody _body;
        private bool _spent;
        private string _sourceId;

        // Unity only sends trigger events when at least one side has a Rigidbody. Projectiles need one so they
        // register against static targets. Kinematic: moved by script, not by physics.
        void Awake()
        {
            _body = GetComponent<Rigidbody>();
            if (_body == null)
            {
                _body = gameObject.AddComponent<Rigidbody>();
            }
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void Launch(DamageSO damage, string sourceId)
        {
            _damage = damage;
            _sourceId = sourceId;
            Destroy(gameObject, _lifetime);
        }

        void FixedUpdate()
        {
            if (_spent) return;
            _body.MovePosition(_body.position + transform.forward * (_speed * Time.fixedDeltaTime));
        }

        void OnTriggerEnter(Collider other)
        {
            if (_spent || _damage == null) return;

            // Ignore the shooter and any other projectile.
            if (other.GetComponentInParent<Projectile>() != null) return;

            IDamageReceiver receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver == null) return;

            bool fromPlayer = false; // Turrets and enemies are never the player.
            string source = string.IsNullOrEmpty(_sourceName) ? (_sourceId ?? name) : _sourceName;
            receiver.ReceiveHit(_damage.Build(source, 0f, fromPlayer, transform.position));

            _spent = true;
            Destroy(gameObject);
        }
    }
}
