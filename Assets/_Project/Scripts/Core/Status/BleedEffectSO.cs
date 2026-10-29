using UnityEngine;

namespace Sol
{
    /// <summary>
    /// A bleed: damage over time. Each tick deals a share of the raw hit damage. One asset per variant
    /// (e.g. "Light Bleed", "Heavy Bleed").
    /// </summary>
    [CreateAssetMenu(fileName = "New Bleed", menuName = "Sol/Status/Bleed")]
    public class BleedEffectSO : StatusEffectSO
    {
        [Header("Bleed")]
        [Tooltip("Fraction of the raw hit damage dealt over the full duration. 0.3 = 30%.")]
        [SerializeField] private float _damageFractionOfRaw = 0.3f;
        [Tooltip("Scaled game seconds between ticks.")]
        [SerializeField] private float _tickIntervalSeconds = 1f;

        public override float DamageFractionOfRaw => _damageFractionOfRaw;
        public override float TickIntervalSeconds => _tickIntervalSeconds;
    }
}
