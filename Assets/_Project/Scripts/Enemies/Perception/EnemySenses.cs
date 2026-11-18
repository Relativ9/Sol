using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Perception system for an enemy: vision (raycast line-of-sight) and hearing (movement-based detection).
    /// Vision requires a clear line between the enemy's head and the player's head, within adjusted range.
    /// Hearing detects movement based on player speed: walking is quiet, sprinting is loud.
    /// Both ranges can be boosted via ActorStatSO.perceptionRangeBonus.
    /// 
    /// Pure data-driven: no behavior decisions, only reads and reports awareness.
    /// </summary>
    public class EnemySenses : MonoBehaviour, ISenses
    {
        [SerializeField]
        [Tooltip("Base vision range in meters. This is multiplied by (1 + ActorStatSO.perceptionRangeBonus).")]
        private float _baseVisionRange = 20f;

        [SerializeField]
        [Tooltip("Base hearing range when walking. Multiplied by perception bonus.")]
        private float _baseHearingRange = 10f;

        [SerializeField]
        [Tooltip("Player movement speed above this is considered sprinting. Hearing 1.5x when sprinting.")]
        private float _sprintThreshold = 5f;

        [SerializeField]
        [Tooltip("How often to raycast for vision, in seconds.")]
        private float _visionCheckInterval = 0.1f;

        [SerializeField]
        private LayerMask _visionRaycastMask = ~0;

        private Transform _headTransform;
        private Transform _playerTransform;
        private IMovement _playerMovement;
        private EnemyStats _stats;

        private bool _canSee;
        private bool _canHear;
        private Vector3 _lastSeenPlayerPos;
        private float _nextVisionCheckTime;
        private float _distanceToPlayer;

        public bool CanSee => _canSee;
        public bool CanHear => _canHear;
        public bool IsAware => _canSee || _canHear;
        public Vector3 LastSeenPlayerPosition => _lastSeenPlayerPos;
        public float DistanceToPlayer => _distanceToPlayer;

        void Awake()
        {
            _stats = GetComponentInParent<EnemyStats>();
            if (_stats == null)
                Debug.LogError($"[EnemySenses] {name} has no EnemyStats parent.", this);

            _headTransform = transform.Find("Head");
            if (_headTransform == null)
                _headTransform = transform;
        }

        void Start()
        {
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO == null)
            {
                Debug.LogWarning($"[EnemySenses] No Player found. Perception disabled.", this);
                enabled = false;
                return;
            }

            _playerTransform = playerGO.transform;
            _playerMovement = playerGO.GetComponent<IMovement>();
            _lastSeenPlayerPos = _playerTransform.position;
            _nextVisionCheckTime = Time.time + _visionCheckInterval;
        }

        void Update()
        {
            if (_playerTransform == null) return;

            _distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (Time.time >= _nextVisionCheckTime)
            {
                UpdateVision();
                _nextVisionCheckTime = Time.time + _visionCheckInterval;
            }

            UpdateHearing();

            if (_canSee)
                _lastSeenPlayerPos = _playerTransform.position;
        }

        private void UpdateVision()
        {
            if (_playerTransform == null) return;

            float effectiveRange = GetEffectiveVisionRange();
            if (_distanceToPlayer > effectiveRange)
            {
                _canSee = false;
                return;
            }

            Vector3 playerHeadPos = _playerTransform.position + Vector3.up * 1.7f;
            Vector3 directionToPlayer = (playerHeadPos - _headTransform.position).normalized;
            float rayDistance = Vector3.Distance(_headTransform.position, playerHeadPos);

            _canSee = Physics.Raycast(_headTransform.position, directionToPlayer, rayDistance, _visionRaycastMask);
        }

        private void UpdateHearing()
        {
            if (_playerTransform == null) return;

            float playerSpeed = _playerMovement != null 
                ? _playerMovement.Velocity.magnitude 
                : 0f;

            float effectiveRange = GetEffectiveHearingRange(playerSpeed);
            if (playerSpeed > _sprintThreshold)
                effectiveRange *= 1.5f;

            _canHear = _distanceToPlayer <= effectiveRange;
        }

        private float GetEffectiveVisionRange()
        {
            if (_stats == null || _stats.Stats == null)
                return _baseVisionRange;

            float perceptionBonus = _stats.Stats.GetStat(StatTypeEnum.Perception);
            return _baseVisionRange * (1f + perceptionBonus);
        }

        private float GetEffectiveHearingRange(float playerSpeed)
        {
            if (_stats == null || _stats.Stats == null)
                return _baseHearingRange;

            float perceptionBonus = _stats.Stats.GetStat(StatTypeEnum.Perception);
            return _baseHearingRange * (1f + perceptionBonus);
        }

        void OnDrawGizmosSelected()
        {
            if (!enabled) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _baseVisionRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _baseHearingRange);

            if (Application.isPlaying && _playerTransform != null && _canSee)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _playerTransform.position);
            }
        }
    }
}
