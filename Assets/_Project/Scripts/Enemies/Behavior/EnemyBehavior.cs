using UnityEngine;
using System;

namespace Sol
{
    /// <summary>
    /// State machine for enemy AI: Idle → Alerted → Pursuing → Attacking → Dead.
    /// Queries perception, commands movement, and triggers attacks.
    /// </summary>
    public class EnemyBehavior : MonoBehaviour
    {
        public enum State { Idle, Alerted, Pursuing, Attacking, Dead }

        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _alertTimeoutSeconds = 5f;
        [SerializeField] private float _attackCooldownSeconds = 1.5f;

        private State _state = State.Idle;
        private Vector3 _lastKnownPlayerPos;
        private float _alertedTime;
        private float _nextAttackTime;

        private ISenses _senses;
        private EnemyLocomotion _locomotion;
        private EnemyMeleeAttack _attack;
        private TargetHealth _health;
        private StatusController _statusController;
        private bool _initialized;

        public State CurrentState => _state;
        public event Action<State> StateChanged;

        void Awake()
        {
            _health = GetComponentInParent<TargetHealth>();
            _statusController = GetComponentInParent<StatusController>();
        }

        void Start()
        {
            _senses = GetComponentInParent<EnemySenses>();
            _locomotion = GetComponentInParent<EnemyLocomotion>();
            _attack = GetComponentInParent<EnemyMeleeAttack>();

            if (_senses == null || _locomotion == null || _attack == null)
            {
                Debug.LogError("[EnemyBehavior] Missing subsystem", this);
                enabled = false;
                return;
            }

            if (_statusController != null)
                _statusController.StatusApplied += OnStatusApplied;

            _initialized = true;
            SetState(State.Idle);
        }

        void OnDestroy()
        {
            if (_statusController != null)
                _statusController.StatusApplied -= OnStatusApplied;
        }

        void Update()
        {
            if (!_initialized || _health == null) return;
            if (_health.IsDead) { SetState(State.Dead); return; }

            switch (_state)
            {
                case State.Idle: UpdateIdle(); break;
                case State.Alerted: UpdateAlerted(); break;
                case State.Pursuing: UpdatePursuing(); break;
                case State.Attacking: UpdateAttacking(); break;
                case State.Dead: break;
            }
        }

        private void UpdateIdle()
        {
            if (!_senses.IsAware) return;
            SetState(State.Alerted);
            _alertedTime = Time.time;
            _lastKnownPlayerPos = _senses.LastSeenPlayerPosition;
            _locomotion.Stop();
        }

        private void UpdateAlerted()
        {
            // Always update last known position while player is aware to reduce staleness
            if (_senses.CanSee)
            {
                _lastKnownPlayerPos = _senses.LastSeenPlayerPosition;
                _alertedTime = Time.time;
            }
            else if (_senses.IsAware)
            {
                // Even if not visible, update position if still hearing them
                _lastKnownPlayerPos = _senses.LastSeenPlayerPosition;
            }
            
            if (_senses.DistanceToPlayer <= _attackRange)
            {
                SetState(State.Attacking);
                _nextAttackTime = Time.time;
                return;
            }
            if (_senses.IsAware)
            {
                SetState(State.Pursuing);
                return;
            }
            if (Time.time - _alertedTime > _alertTimeoutSeconds)
            {
                SetState(State.Idle);
                _locomotion.Stop();
            }
        }

        private void UpdatePursuing()
        {
            _locomotion.MoveTo(_lastKnownPlayerPos);
            if (_senses.CanSee)
                _lastKnownPlayerPos = _senses.LastSeenPlayerPosition;
            if (_senses.DistanceToPlayer <= _attackRange)
            {
                SetState(State.Attacking);
                _nextAttackTime = Time.time;
                _locomotion.Stop();
                return;
            }
            if (!_senses.IsAware)
            {
                SetState(State.Alerted);
                _alertedTime = Time.time;
            }
        }

        private void UpdateAttacking()
        {
            _locomotion.Stop();
            
            // Keep updating target position while attacking (so we track moving targets)
            if (_senses.CanSee)
                _lastKnownPlayerPos = _senses.LastSeenPlayerPosition;
            
            if (_senses.DistanceToPlayer > _attackRange)
            {
                SetState(State.Pursuing);
                return;
            }
            if (Time.time >= _nextAttackTime && _attack != null)
            {
                _attack.PlayAttack();
                _nextAttackTime = Time.time + _attackCooldownSeconds;
            }
            if (!_senses.IsAware)
            {
                SetState(State.Alerted);
                _alertedTime = Time.time;
            }
        }

        private void SetState(State newState)
        {
            if (_state == newState) return;
            State oldState = _state;
            _state = newState;
            StateChanged?.Invoke(newState);
            Debug.Log($"[EnemyBehavior] {name}: {oldState} → {newState}", this);
        }

        private void OnStatusApplied(IStatusEffect effect)
        {
            if (effect == null || !effect.LocksCombat) return;
            _nextAttackTime = Time.time + _attackCooldownSeconds;
            SetState(State.Idle);
        }
    }
}
