using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Health and the damage intake for a non-player target. Implements IDamageReceiver, so every damage type,
    /// tag and status path that works on the player works here too. Logs damage to the combat log under the
    /// rules in CombatLogRules, and reports changes so the world health bar can update.
    /// </summary>
    public class TargetHealth : MonoBehaviour, ITargetHealth, IDamageReceiver, IStatusTarget
    {
        [SerializeField] private float _maxHealth = 100f;
        [Tooltip("Shown in the combat log. Leave empty to use this object's name.")]
        [SerializeField] private string _displayName = "";

        private float _current;
        private IChanceRoller _roller;
        private StatusController _status;
        private EnemyStats _enemyStats;

        // IStatusTarget. Health changes go through the same path as hits, so a bleed tick is logged and shown like any damage.
        public string StatusName => DisplayName;

        public void ApplyHealthDelta(float delta, string source)
        {
            if (IsDead) return;
            _current = Mathf.Clamp(_current + delta, 0f, _maxHealth);
            Changed?.Invoke(-delta);
            if (delta < 0f)
                LogRules.Report(source ?? "Status", DisplayName, $"-{-delta:0.#} Health", CombatLogKind.Damage, transform.position, false);
        }

        // Targets have no energy pool, so energy costs from statuses are ignored.
        public void ApplyEnergyDelta(float delta, string source) { }

        // Targets have no animator wired to statuses yet, so animation triggers are ignored.
        public void SetAnimatorTrigger(string trigger) { }

        // Targets have no combat state to cancel.
        public void CancelCombat() { }

        public bool IsImmuneTo(IStatusEffect effect) => _enemyStats != null && _enemyStats.IsImmuneTo(effect);

        public float Current => _current;
        public float Max => _maxHealth;
        public bool IsDead => _current <= 0f;
        public event Action<float> Changed;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        void Awake()
        {
            _current = _maxHealth;
            _enemyStats = GetComponentInParent<EnemyStats>();

            // This target is pushed into its own status controller, which must be on the same prefab. A target without one
            // reports it once and skips procs, rather than adding components at runtime.
            _status = GetComponent<StatusController>();
            if (_status == null)
                Debug.LogError($"[{nameof(TargetHealth)}] {name} has no StatusController. Its status procs are skipped. Add one to the prefab.", this);
            else
                _status.Initialize(this);
        }

        // Attach the world health bar once the UI exists. UIManager.Start has built the HUD by then, so
        // the shared document is ready. Retries each frame until it is, then stops.
        void Start()
        {
            TryAttachHealthBar();
        }

        private bool _barAttached;

        void Update()
        {
            if (!_barAttached) TryAttachHealthBar();
        }

        void TryAttachHealthBar()
        {
            if (_barAttached) return;

            IUIManager ui = ServiceLocator.TryGet<IUIManager>();
            VisualElement hud = ui?.HudRoot;
            if (hud == null) return;

            WorldHealthBar bar = gameObject.AddComponent<WorldHealthBar>();
            bar.Attach(this, hud);
            _barAttached = true;
        }

        public void ReceiveHit(DamageInfo info)
        {
            if (IsDead) return;
            _roller ??= ServiceLocator.TryGet<IChanceRoller>();
            if (_roller == null) return;

            // Targets have no stat sheet yet, so resistances read as zero and mitigation is a pass-through.
            // The stat source is injected here so enemies can later supply their own resistances.
            // Mitigation uses the target's stats through the same path as the player.
            DamageResult result = DamageCalculator.Calculate(info, _ => 0f, _roller);

            if (result.Deflected)
            {
                LogRules.Report(info.SourceId, DisplayName, "Deflected", CombatLogKind.Info, transform.position, info.SourceIsPlayer);
                return;
            }

            _current = Mathf.Max(0f, _current - result.FinalAmount);
            Changed?.Invoke(result.FinalAmount);

            LogRules.Report(info.SourceId, DisplayName,
                $"-{result.FinalAmount:0.#} Health ({info.Type})", CombatLogKind.Damage, transform.position, info.SourceIsPlayer);

            // Status procs are rolled once per hit, after damage. A dead target takes no more statuses.
            if (_current > 0f)
                StatusRoller.Roll(result, info, _roller, _status, DisplayName, info.SourceIsPlayer, transform.position);

            if (_current <= 0f)
            {
                LogRules.Report(info.SourceId, DisplayName, "Defeated", CombatLogKind.Warning, transform.position, info.SourceIsPlayer);
            }
        }

    }
}
