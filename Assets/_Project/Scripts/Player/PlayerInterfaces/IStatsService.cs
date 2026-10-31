using UnityEngine;

namespace Sol
{
    public interface IStatsService
    {
        float GetStat(StatTypeEnum statType);
        float GetBaseStat(StatTypeEnum statType);

        /// <summary>Sets a player-spent attribute's base value. Only Attribute stats are accepted, so progression is the only writer.</summary>
        void SetAttributeBase(StatTypeEnum statType, float value);

        /// <summary>Sets the minimum total for a Derived stat, such as a multiplier that must never drop below its design
        /// value. A total below the floor is raised to it; a total above the floor is unchanged. Only Derived stats are
        /// accepted.</summary>
        void SetDerivedFloor(StatTypeEnum statType, float floor);

        /// <summary>
        /// Sets what one source contributes to a stat, as a flat modifier. Calling it again with the same sourceId
        /// replaces the previous contribution, so a source's total is always exactly the last value it set. Works for
        /// every role: equipment, talents, potions and effects all use this path to set Tuning and Derived stats.
        /// Remove a source's contributions with RemoveModifiersFromSource.
        /// </summary>
        void SetSourceContribution(StatTypeEnum statType, string sourceId, float value, ModifierCategory category);
        string ApplyModifier(StatModifier modifier);
        string ApplyOrReplaceModifier(StatModifier modifier);
        void RemoveModifier(string modifierId);
        void RemoveModifiersFromSource(string sourceID);
        void RemoveAllModifiersOfType(StatTypeEnum statType, ModifierCategory category);
        float GetSpeedMultiplier();
        
        // // Progression
        // int GetLevel();
        // int GetAvailableTalentPoints();
        // int GetTotalTalentPoints();
        // bool SpendTalentPoint();
        // void RefundTalentPoint();
    }
}
