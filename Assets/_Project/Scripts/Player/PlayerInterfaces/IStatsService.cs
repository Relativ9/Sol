using UnityEngine;

namespace Sol
{
    public interface IStatsService
    {
        float GetStat(string statName);
        string ApplyModifier(string statName, StatModifier modifier, ModifierCatagory modifierCatagory);
        string ApplyOrReplaceModifier(string statName, StatModifier modifier, ModifierCatagory category, string sourceId);
        void RemoveModifier(string statName, string modifierId);
        void RemoveModifiersFromSource(string statName, string sourceId);
        void RemoveAllModifiersOfType(string statName, ModifierCatagory modifierCatagory);
        float GetSpeedMultiplier();
    }
}
