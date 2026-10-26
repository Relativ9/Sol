using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Fires a Projectile at a fixed interval, in the direction it faces. Needs no animation, so it can be tested now.
    /// Its shots are not the player's, so the combat log rules apply to them.
    /// </summary>
    public class ProjectileTurret : MonoBehaviour
    {
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private DamageSO _damage;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private float _interval = 2f;
        [SerializeField] private string _sourceName = "Turret";

        private float _nextShotTime;

        void Start()
        {
            _nextShotTime = Time.time + _interval;
        }

        void Update()
        {
            if (_projectilePrefab == null || _damage == null) return;
            if (Time.time < _nextShotTime) return;

            _nextShotTime = Time.time + _interval;
            Transform origin = _muzzle != null ? _muzzle : transform;
            Projectile shot = Instantiate(_projectilePrefab, origin.position, origin.rotation);
            shot.Launch(_damage, _sourceName);
        }
    }
}
