using UnityEngine;

namespace Sol
{
    public class StandardStatCalculator : IStatCalculator
    {
        public float Calculate(StatTypeEnum statType, float baseValue, StatModifier[] modifiers)
        {
            // Skip the reserved Base category; your runtime baseValue already covers that.
            float flatSum = 0f;
            float percentSum = 0f;
            foreach (var mod in modifiers)
            {
                if (mod.category == ModifierCategory.Base) continue;
                if (mod.type == ModifierType.FlatAdditive)
                    flatSum += mod.value;
                else if (mod.type == ModifierType.PercentAdditive)
                    percentSum += mod.value;
            }
            return (baseValue + flatSum) * (1f + percentSum);
        }
    }
}
