using UnityEngine;

namespace Sol
{
    public interface IStatsService
    {
        float GetStat(string statName);
        string ApplyModifier(string statName, StatModifier modifier, ModifierCatagory modifierCatagory);
        void RemoveModifier(string statName, string modifierId);
        void RemoveAllModifiersOfType(string statName, ModifierCatagory modifierCatagory);
    }
}
