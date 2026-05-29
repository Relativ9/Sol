using UnityEngine;

namespace Sol
{
    public interface IStatsService
    {
        float GetStat(StatTypeEnum statType);
        float GetBaseStat(StatTypeEnum statType);
        void SetBaseStat(StatTypeEnum statType, float value);
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
