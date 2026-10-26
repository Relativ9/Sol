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
    public class TargetHealth : MonoBehaviour, ITargetHealth, IDamageReceiver
    {
        [SerializeField] private float _maxHealth = 100f;
        [Tooltip("Shown in the combat log. Leave empty to use this object's name.")]
        [SerializeField] private string _displayName = "";

        private float _current;
        private IChanceRoller _roller;

        public float Current => _current;
        public float Max => _maxHealth;
        public bool IsDead => _current <= 0f;
        public event Action<float> Changed;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        void Awake()
        {
            _current = _maxHealth;
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
            // Zero mitigation until enemies have stats (see the knowledgebase: ActorStatSO, planned).
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

            if (_current <= 0f)
            {
                LogRules.Report(info.SourceId, DisplayName, "Defeated", CombatLogKind.Warning, transform.position, info.SourceIsPlayer);
            }
        }
    }
}
