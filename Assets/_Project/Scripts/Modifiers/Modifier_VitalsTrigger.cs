using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Environmental trigger that changes Health, Energy, Hunger or Thirst while the player stays inside the collider.
    /// Negative rates drain, positive rates restore. Effects are logged to the combat log.
    /// Put on a GameObject with a Collider set to Is Trigger, tagged so the player matches "Player".
    /// </summary>
    public class Modifier_VitalsTrigger : MonoBehaviour
    {
        public enum VitalType
        {
            Health,
            Energy,
            Hunger,
            Thirst
        }

        [Header("Vital Effect")]
        [SerializeField] private VitalType _vital = VitalType.Health;

        [Tooltip("Units per second while inside. Negative drains, positive restores.")]
        [SerializeField] private float _ratePerSecond = -10f;

        [Tooltip("Name shown in the combat log, e.g. 'Poison Gas', 'Frost Zone'.")]
        [SerializeField] private string _displayName = "Environment";

        [Tooltip("Log a message on enter and exit. The per-frame change is only logged as a total on exit.")]
        [SerializeField] private bool _logEnterExit = true;

        private bool _playerInside;
        private float _accumulated; // Total change while inside, logged on exit.

        // Resolved lazily: scene Start() can run before GameInitializer has registered/seeded the services.
        // TryGet: OnTriggerStay runs every frame, so this must not log when a service isn't ready.
        private IPlayerVitals Vitals => ServiceLocator.TryGet<IPlayerVitals>();
        private ICombatLog Log => ServiceLocator.TryGet<ICombatLog>();

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInside = true;
            _accumulated = 0f;

            if (_logEnterExit)
                Log?.Add($"Entered {_displayName} ({DescribeRate()})", CombatLogKind.Warning);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player") || !_playerInside) return;

            IPlayerVitals vitals = Vitals;
            if (vitals == null || !vitals.IsInitialized) return;

            float delta = _ratePerSecond * Time.deltaTime;
            _accumulated += delta;
            ApplyToVital(vitals, delta);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInside = false;

            if (_logEnterExit && !Mathf.Approximately(_accumulated, 0f))
                Log?.Add($"Left {_displayName}: {FormatSigned(_accumulated)} {_vital}",
                    _accumulated > 0f ? CombatLogKind.Restore : CombatLogKind.Damage);

            _accumulated = 0f;
        }

        private void ApplyToVital(IPlayerVitals vitals, float delta)
        {
            // source = null keeps per-frame changes out of the log. Enter/exit lines report the totals.
            switch (_vital)
            {
                case VitalType.Health: vitals.ApplyHealthDelta(delta); break;
                case VitalType.Energy: vitals.ApplyEnergyDelta(delta); break;
                case VitalType.Hunger: vitals.ApplyHungerDelta(delta); break;
                case VitalType.Thirst: vitals.ApplyThirstDelta(delta); break;
            }
        }

        private string DescribeRate()
        {
            return $"{FormatSigned(_ratePerSecond)} {_vital}/s";
        }

        private static string FormatSigned(float value)
        {
            return (value > 0f ? "+" : "") + value.ToString("0.##");
        }
    }
}
