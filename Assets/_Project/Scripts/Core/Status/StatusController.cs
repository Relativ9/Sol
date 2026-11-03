using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Owns one target's active status effects. The same controller runs on the player and on enemies. It only knows
    /// IStatusEffect and IStatusTarget, so a new effect or a new kind of target needs no change here.
    ///
    /// Effects reach the target through IStatusTarget, so every hook is supplied by the target (health, energy, animation,
    /// combat, knockback). Immunities are read from the host, so an enemy type or a temporary player modifier can block an effect.
    ///
    /// Each target gets its own instance on its own GameObject, so enemies find theirs by component reference. Only the
    /// player registers globally with the ServiceLocator (IStatusController), because the player has one and systems read it
    /// without a reference. Runs on scaled time, so statuses pause with the game.
    /// </summary>
    public class StatusController : MonoBehaviour, IStatusController
    {
        private readonly List<StatusInstance> _active = new List<StatusInstance>();
        private IStatusTarget _target;
        private Vector3 _pendingImpulse;
        private bool _impulsePending;

        public IReadOnlyList<StatusInstance> Active => _active;

        public bool IsMovementLocked
        {
            get
            {
                foreach (StatusInstance s in _active)
                    if (s.Effect.LocksMovement) return true;
                return false;
            }
        }

        public bool IsCombatLocked
        {
            get
            {
                foreach (StatusInstance s in _active)
                    if (s.Effect.LocksCombat) return true;
                return false;
            }
        }

        /// <summary>
        /// Gives this controller the target it acts on. Called by the composition root: PlayerController for the player,
        /// TargetHealth for enemies. The controller never searches for its target.
        /// </summary>
        public void Initialize(IStatusTarget target)
        {
            _target = target;
        }

        void Update()
        {
            if (_active.Count == 0) return;

            float lost = Advance(Time.deltaTime);
            if (lost > 0f)
                _target?.ApplyHealthDelta(-lost, "Bleeding");
        }

        /// <summary>The target's immunity to an effect, read from its host. A target with no host is not immune.</summary>
        public bool IsImmuneTo(IStatusEffect effect) => _target != null && effect != null && _target.IsImmuneTo(effect);

        /// <summary>
        /// Applies a status to this target. An effect the target is immune to is refused, and nothing is applied or logged.
        /// Stacking and duration follow the effect's own rules.
        /// </summary>
        public void Apply(IStatusEffect effect, string sourceId, Vector3 sourceOrigin, Vector3 hitPoint, float rawDamage = 0f)
        {
            if (effect == null) return;
            if (IsImmuneTo(effect)) return;

            bool hasTicks = effect.DamageFractionOfRaw > 0f && effect.TickIntervalSeconds > 0f;
            float ticksPerLife = hasTicks ? effect.DurationSeconds / effect.TickIntervalSeconds : 1f;
            float perTick = hasTicks ? rawDamage * effect.DamageFractionOfRaw / ticksPerLife : 0f;
            bool perSource = effect.StackingMode == StatusStackingMode.StackPerSource;

            StatusInstance existing = perSource
                ? _active.Find(s => s.Effect == effect && s.SourceId == sourceId)
                : _active.Find(s => s.Effect == effect);

            if (existing != null)
            {
                if (perSource) existing.Stacks++;
                existing.RemainingSeconds = effect.DurationSeconds;
                existing.DamagePerTickPerStack = perTick;
            }
            else
            {
                _active.Add(new StatusInstance
                {
                    Effect = effect,
                    SourceId = sourceId,
                    Stacks = 1,
                    RemainingSeconds = effect.DurationSeconds,
                    SecondsUntilNextTick = effect.TickIntervalSeconds,
                    DamagePerTickPerStack = perTick,
                });
            }

            if (effect.EnergyCostOnApply > 0f)
                _target?.ApplyEnergyDelta(-effect.EnergyCostOnApply, effect.DisplayName);

            // Cancel a swing in progress when the effect locks combat, so the hit does not resolve.
            if (effect.LocksCombat)
                _target?.CancelCombat();

            // Horizontal push away from the attacker's ORIGIN, not the weapon. Stored once and handed out by TryConsumeImpulse.
            // If the target is exactly over the origin, fall back to the hit point.
            if (effect.KnockbackSpeed > 0f)
            {
                Vector3 away = transform.position - sourceOrigin;
                away.y = 0f;
                if (away.sqrMagnitude <= 0.0001f)
                {
                    away = transform.position - hitPoint;
                    away.y = 0f;
                }

                if (away.sqrMagnitude > 0.0001f)
                {
                    _pendingImpulse = away.normalized * effect.KnockbackSpeed;
                    _impulsePending = true;
                }
            }

            if (!string.IsNullOrEmpty(effect.AnimatorTrigger))
                _target?.SetAnimatorTrigger(effect.AnimatorTrigger);

            ServiceLocator.TryGet<ICombatLog>()?.Add($"{sourceId}: {effect.DisplayName} on {_target?.StatusName ?? name}", CombatLogKind.Warning);
        }

        public bool TryConsumeImpulse(out Vector3 impulse)
        {
            impulse = _pendingImpulse;
            bool had = _impulsePending;
            _pendingImpulse = Vector3.zero;
            _impulsePending = false;
            return had;
        }

        /// <summary>
        /// Advances every status by scaled game time. Each tick that comes due fires once per stack, and a long frame
        /// can cover more than one tick. Returns the health removed by ticks in this call.
        /// </summary>
        private float Advance(float scaledDeltaSeconds)
        {
            float damage = 0f;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                StatusInstance s = _active[i];
                IStatusEffect e = s.Effect;
                bool hasTicks = e.DamageFractionOfRaw > 0f && e.TickIntervalSeconds > 0f;
                float timeLeft = scaledDeltaSeconds;

                while (s.RemainingSeconds > 0f && timeLeft > 0f)
                {
                    float step = Mathf.Min(timeLeft, s.RemainingSeconds);
                    if (hasTicks) step = Mathf.Min(step, s.SecondsUntilNextTick);

                    s.RemainingSeconds -= step;
                    timeLeft -= step;

                    if (hasTicks)
                    {
                        s.SecondsUntilNextTick -= step;
                        if (s.SecondsUntilNextTick <= 0f && s.RemainingSeconds >= 0f)
                        {
                            damage += s.Stacks * s.DamagePerTickPerStack;
                            s.SecondsUntilNextTick += e.TickIntervalSeconds;
                        }
                    }
                }

                if (s.RemainingSeconds <= 0f)
                {
                    _active.RemoveAt(i);
                    if (!string.IsNullOrEmpty(e.ExpireMessage))
                        ServiceLocator.TryGet<ICombatLog>()?.Add(e.ExpireMessage, CombatLogKind.Info);
                }
            }
            return damage;
        }
    }
}
