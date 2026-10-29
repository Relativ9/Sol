using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Owns the player's active status effects. It only knows IStatusEffect, so a new effect type needs no change here.
    /// The receiver (this) applies its own resistance to the attacker's base chance and rolls once.
    /// Lives on the player; movement and combat read its locks through the player's service registry.
    /// Health and energy changes go through IPlayerVitals, so PlayerVitals stays the only writer of them.
    /// Runs on scaled time, so statuses pause with the game (timeScale 0), like TimeManager.
    /// </summary>
    public class StatusController : MonoBehaviour, IStatusController, IPlayerComponent
    {
        private readonly List<StatusInstance> _active = new List<StatusInstance>();
        private IAnimationController _animation;
        private ICombatController _combat;
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

        void Awake()
        {
            // Global registration so PlayerVitals can reach this at hit time. Movement and combat use the player context.
            ServiceLocator.RegisterService<IStatusController>(this);
        }

        public void Initialize(IPlayerContext context)
        {
            _animation = context.GetService<IAnimationController>();
            _combat = context.GetService<ICombatController>();
        }

        void Update()
        {
            if (_active.Count == 0) return;

            float lost = Advance(Time.deltaTime);
            if (lost > 0f)
                ServiceLocator.TryGet<IPlayerVitals>()?.ApplyHealthDelta(-lost, "Bleeding");
        }

        /// <summary>
        /// Rolls the hit's status procs. The attacker supplies the base chance and the effect (in info.Statuses).
        /// The receiver's armour resistance is already in result.StaggerChance / BleedChance, so the roll
        /// uses the receiver's reduced chance, once per status.
        /// </summary>
        public void RollHitProcs(DamageResult result, DamageInfo info, IChanceRoller roller)
        {
            StatusAttack attack = info.Statuses;

            if (attack.StaggerEffect != null && roller.Roll(result.StaggerChance))
                Apply(attack.StaggerEffect, info.SourceId, info.SourceOrigin, info.HitPoint);

            // Bleed scales from the raw hit (fraction set on the effect), so the raw amount is passed in.
            if (attack.BleedEffect != null && roller.Roll(result.BleedChance))
                Apply(attack.BleedEffect, info.SourceId, info.SourceOrigin, info.HitPoint, info.Amount);
        }

        public void Apply(IStatusEffect effect, string sourceId, Vector3 sourceOrigin, Vector3 hitPoint, float rawDamage = 0f)
        {
            if (effect == null) return;

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
                ServiceLocator.TryGet<IPlayerVitals>()?.ApplyEnergyDelta(-effect.EnergyCostOnApply, effect.DisplayName);

            // Cancel a swing in progress when the effect locks combat, so the hit does not resolve.
            if (effect.LocksCombat)
                _combat?.CancelAttack();

            // Horizontal push away from the attacker's ORIGIN (its root), not the weapon. Stored once and handed out
            // by TryConsumeImpulse. If the player is exactly over the origin, fall back to the hit point.
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
                _animation?.SetTrigger(effect.AnimatorTrigger);

            ServiceLocator.TryGet<ICombatLog>()?.Add($"{sourceId}: {effect.DisplayName}", CombatLogKind.Warning);
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


