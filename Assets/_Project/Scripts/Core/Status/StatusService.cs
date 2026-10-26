using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Holds the timed statuses on the player. Bleed deals discrete ticks: one tick per full second of scaled
    /// game time, so the damage per tick is fixed and independent of frame rate. Scaled time means pausing
    /// (timeScale 0) stops ticks, matching TimeManager. Stagger is a one-shot event and is not tracked here.
    /// Plain C#: driven by PlayerVitals.
    /// </summary>
    public class StatusService
    {
        // Bleed lasts 5 seconds and deals 30% of raw damage over that time (doc 4.2). One tick per second.
        public const float BleedDurationSeconds = 5f;
        public const float BleedFractionOfRaw = 0.30f;
        public const float TickIntervalSeconds = 1f;

        private readonly List<StatusInstance> _active = new();

        public IReadOnlyList<StatusInstance> Active => _active;

        /// <summary>Adds a bleed stack from a source, or refreshes its duration if that source already bleeds the target.</summary>
        public void ApplyBleed(string sourceId, float rawDamage)
        {
            // Total damage over the bleed's life, split evenly across its ticks. Fixed per tick, so the total is exact.
            float perTick = rawDamage * BleedFractionOfRaw / (BleedDurationSeconds / TickIntervalSeconds);

            StatusInstance existing = _active.Find(s => s.Kind == StatusKind.Bleed && s.SourceId == sourceId);
            if (existing != null)
            {
                existing.Stacks++;
                existing.RemainingSeconds = BleedDurationSeconds;
                existing.DamagePerTickPerStack = perTick;
                return;
            }

            _active.Add(new StatusInstance
            {
                Kind = StatusKind.Bleed,
                SourceId = sourceId,
                Stacks = 1,
                RemainingSeconds = BleedDurationSeconds,
                SecondsUntilNextTick = TickIntervalSeconds,
                DamagePerTickPerStack = perTick,
            });
        }

        /// <summary>
        /// Advances statuses by scaled game time. Every tick interval that comes due fires once per stack.
        /// A long frame can cover more than one tick. Returns the health removed by ticks in this call.
        /// </summary>
        public float Advance(float scaledDeltaSeconds)
        {
            float damage = 0f;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                StatusInstance s = _active[i];

                // Walk time forward in tick-sized steps. A tick only fires if it falls inside the status's life.
                float timeLeft = scaledDeltaSeconds;
                while (s.RemainingSeconds > 0f && timeLeft > 0f)
                {
                    float step = Mathf.Min(timeLeft, s.SecondsUntilNextTick, s.RemainingSeconds);
                    s.SecondsUntilNextTick -= step;
                    s.RemainingSeconds -= step;
                    timeLeft -= step;

                    if (s.SecondsUntilNextTick <= 0f && s.RemainingSeconds >= 0f)
                    {
                        damage += s.Stacks * s.DamagePerTickPerStack;
                        s.SecondsUntilNextTick += TickIntervalSeconds;
                    }
                }

                if (s.RemainingSeconds <= 0f) _active.RemoveAt(i);
            }
            return damage;
        }

        /// <summary>Removes every status from a source, e.g. when the source is healed or cleansed.</summary>
        public void Clear(StatusKind kind)
        {
            _active.RemoveAll(s => s.Kind == kind);
        }
    }
}
