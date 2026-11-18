using UnityEngine;
using UnityEngine.AI;

namespace Sol
{
    /// <summary>
    /// Locomotion system for enemies: wraps NavMeshAgent for pathfinding and movement.
    /// Commands from EnemyBehavior (MoveTo, Stop) drive the agent. Animation state is synced via Speed parameter.
    /// 
    /// Single responsibility: movement only. Does not decide where to move.
    /// </summary>
    public class EnemyLocomotion : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Distance from destination to consider arrived. Meters.")]
        private float _stoppingDistance = 0.5f;

        [SerializeField]
        [Tooltip("Maximum movement speed. Meters/second.")]
        private float _maxSpeed = 3.5f;

        [SerializeField]
        [Tooltip("Animator parameter for locomotion speed (0 = idle, 1 = walk).")]
        private string _speedParam = "Speed";

        private NavMeshAgent _agent;
        private Animator _animator;

        public bool HasArrived => !_agent.hasPath || _agent.remainingDistance <= _stoppingDistance;
        public float Speed => _agent.velocity.magnitude;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
                Debug.LogError($"[EnemyLocomotion] {name} has no NavMeshAgent. Add one to enable pathfinding.", this);

            _animator = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            if (_agent != null)
            {
                _agent.stoppingDistance = _stoppingDistance;
                _agent.speed = _maxSpeed;
            }
        }

        void Update()
        {
            if (_agent == null || !_agent.enabled) return;

            // Sync animation with movement speed
            float speed = _agent.hasPath && _agent.velocity.sqrMagnitude > 0.1f ? 1f : 0f;
            _animator?.SetFloat(_speedParam, speed);
        }

        /// <summary>Set a destination for the agent to pathfind to.</summary>
        public void MoveTo(Vector3 target)
        {
            if (_agent == null || !_agent.enabled) return;

            _agent.SetDestination(target);
        }

        /// <summary>Stop the agent and clear its path.</summary>
        public void Stop()
        {
            if (_agent == null || !_agent.enabled) return;

            _agent.ResetPath();
            _animator?.SetFloat(_speedParam, 0f);
        }
    }
}
