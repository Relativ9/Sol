using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Runtime current values for Health, Energy, Hunger and Thirst.
    /// Max values come from StatsService. Health and Energy are seeded from PlayerSaveData.
    /// Hunger and Thirst are not saved yet.
    /// </summary>
    public class PlayerVitals : MonoBehaviour, IPlayerVitals, IDamageReceiver
    {

        // Timed statuses (bleed). Owned here because they remove this component's health.
        private readonly StatusService _statuses = new StatusService();

        [Header("Hunger / Thirst (placeholder tuning)")]
        [Tooltip("Units lost per second. 0 = no decay until the design is finalised.")]
        [SerializeField] private float _hungerDecayPerSecond = 0f;
        [SerializeField] private float _thirstDecayPerSecond = 0f;

        private IStatsService _stats;
        private float _health;
        private float _energy;
        private float _hunger;
        private float _thirst;

        public bool IsInitialized { get; private set; }

        void Awake()
        {
            ServiceLocator.RegisterService<IPlayerVitals>(this);
        }

        // Called by GameInitializer after StatsService has been initialized.
        public void Initialize(PlayerSaveData saveData, IStatsService statsService)
        {
            _stats = statsService;

            // CreateNewGame leaves currentHealth/currentEnergy at 0, so 0 means "not set" and fills to max.
            _health = saveData != null && saveData.currentHealth > 0f ? saveData.currentHealth : MaxHealth;
            _energy = saveData != null && saveData.currentEnergy > 0f ? saveData.currentEnergy : MaxEnergy;
            _hunger = MaxHunger;
            _thirst = MaxThirst;

            IsInitialized = true;
            Clamp();
        }

        void Update()
        {
            if (!IsInitialized) return;

            float dt = Time.deltaTime;
            _health += _stats.GetStat(StatTypeEnum.HealthRegen) * dt;
            _energy += _stats.GetStat(StatTypeEnum.EnergyRegen) * dt;
            TickStatuses();
            _hunger -= _hungerDecayPerSecond * dt;
            _thirst -= _thirstDecayPerSecond * dt;
            Clamp();
        }

        public float CurrentHealth => _health;
        public float MaxHealth => _stats != null ? _stats.GetStat(StatTypeEnum.MaxHealth) : 0f;
        public float CurrentEnergy => _energy;
        public float MaxEnergy => _stats != null ? _stats.GetStat(StatTypeEnum.MaxEnergy) : 0f;
        public float CurrentHunger => _hunger;
        public float MaxHunger => _stats != null ? _stats.GetStat(StatTypeEnum.Hunger) : 0f;
        public float CurrentThirst => _thirst;
        public float MaxThirst => _stats != null ? _stats.GetStat(StatTypeEnum.Thirst) : 0f;

        public void ApplyHealthDelta(float delta, string source = null)
        {
            if (!IsInitialized) return;
            _health += delta;
            Clamp();
            Log("Health", delta, source);
        }

        public void ApplyEnergyDelta(float delta, string source = null)
        {
            if (!IsInitialized) return;
            _energy += delta;
            Clamp();
            Log("Energy", delta, source);
        }

        public void ApplyHungerDelta(float delta, string source = null)
        {
            if (!IsInitialized) return;
            _hunger += delta;
            Clamp();
            Log("Hunger", delta, source);
        }

        public void ApplyThirstDelta(float delta, string source = null)
        {
            if (!IsInitialized) return;
            _thirst += delta;
            Clamp();
            Log("Thirst", delta, source);
        }

        /// <summary>
        /// Runs a hit through the calculator, applies the health loss, and reports the result to the combat log.
        /// Status rolls (stagger, bleed) are returned to the caller via the log for now; the status system comes next.
        /// </summary>
        public void ReceiveHit(DamageInfo info)
        {
            if (!IsInitialized || _stats == null) return;

            IChanceRoller roller = ServiceLocator.TryGet<IChanceRoller>();
            if (roller == null)
            {
                Debug.LogError("[PlayerVitals] No IChanceRoller registered. Hit ignored.");
                return;
            }

            DamageResult result = DamageCalculator.Calculate(
                info,
                stat => _stats.GetStat(stat),
                roller);

            if (result.Deflected)
            {
                ServiceLocator.TryGet<ICombatLog>()?.Add($"{info.SourceId}: Deflected", CombatLogKind.Info);
                return;
            }

            _health -= result.FinalAmount;
            Clamp();
            ServiceLocator.TryGet<ICombatLog>()?.Add(
                $"{info.SourceId}: -{result.FinalAmount:0.#} Health ({info.Type})", CombatLogKind.Damage);

            // Status procs. Each is a chance through the shared roller, so every roll is in one place.
            if (roller.Roll(result.StaggerChance))
            {
                ServiceLocator.TryGet<ICombatLog>()?.Add($"{info.SourceId}: Staggered!", CombatLogKind.Warning);
            }

            if (roller.Roll(result.BleedChance))
            {
                _statuses.ApplyBleed(info.SourceId, info.Amount);
                ServiceLocator.TryGet<ICombatLog>()?.Add($"{info.SourceId}: Bleeding", CombatLogKind.Warning);
            }
        }

        // Advances statuses by scaled game time, so ticks stop while the game is paused (timeScale 0).
        // Each bleed tick is logged as it fires, so the log and the health loss come from the same event.
        void TickStatuses()
        {
            float lost = _statuses.Advance(Time.deltaTime);
            if (lost > 0f)
            {
                _health -= lost;
                Clamp();
                ServiceLocator.TryGet<ICombatLog>()?.Add($"Bleeding: -{lost:0.#} Health", CombatLogKind.Damage);
            }
        }

        // Only logs when a source is given, so per-frame drains and regen don't flood the log.
        private void Log(string vital, float delta, string source)
        {
            if (string.IsNullOrEmpty(source)) return;
            if (Mathf.Approximately(delta, 0f)) return;

            var log = ServiceLocator.Get<ICombatLog>();
            if (log == null) return;

            string sign = delta > 0f ? "+" : "";
            var kind = delta > 0f ? CombatLogKind.Restore : CombatLogKind.Damage;
            log.Add($"{source}: {sign}{delta:0.#} {vital}", kind);
        }

        void Clamp()
        {
            if (!IsInitialized) return;
            _health = Mathf.Clamp(_health, 0f, MaxHealth);
            _energy = Mathf.Clamp(_energy, 0f, MaxEnergy);
            _hunger = Mathf.Clamp(_hunger, 0f, MaxHunger);
            _thirst = Mathf.Clamp(_thirst, 0f, MaxThirst);
        }

        // Debug helpers: right-click this component in the Inspector while in Play mode.
        [ContextMenu("Debug/Damage 10 Health")]
        void DebugDamageHealth() => ApplyHealthDelta(-10f, "Debug");

        [ContextMenu("Debug/Spend 25 Energy")]
        void DebugSpendEnergy() => ApplyEnergyDelta(-25f, "Debug");

        [ContextMenu("Debug/Reduce Hunger 2")]
        void DebugReduceHunger() => ApplyHungerDelta(-2f, "Debug");

        [ContextMenu("Debug/Reduce Thirst 2")]
        void DebugReduceThirst() => ApplyThirstDelta(-2f, "Debug");

        [ContextMenu("Debug/Restore All")]
        void DebugRestoreAll()
        {
            if (!IsInitialized) return;
            _health = MaxHealth;
            _energy = MaxEnergy;
            _hunger = MaxHunger;
            _thirst = MaxThirst;
        }
    }
}
