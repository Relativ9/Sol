using UnityEngine;

namespace Sol
{
    public interface IStatsService
    {
        float GetStat(StatTypeEnum statType);
        float GetBaseStat(StatTypeEnum statType);
        /// <summary>Sets a player-spent attribute's base value. Only Attribute stats are accepted, so progression is the only writer.</summary>
        void SetAttributeBase(StatTypeEnum statType, float value);

        /// <summary>Sets a derived stat's base total (armour weight, piece counts, armour value). Only Derived stats are
        /// accepted, so the derivation and equipment are the only writers.</summary>
        void SetDerivedBase(StatTypeEnum statType, float value);
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
