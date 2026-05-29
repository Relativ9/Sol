using UnityEngine;

namespace Sol
{
    public interface IStatCalculator
    {
        /// <summary>
        /// Computes final value from a single stat's base value and its active modifiers.
        /// </summary>
        float Calculate(StatTypeEnum statType, float baseValue, StatModifier[] modifiers);
    }
}
