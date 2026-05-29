using UnityEngine;

namespace Sol
{
    public enum ModifierCategory
    {
        Base,           // The original value from ScriptableObject
        Permanent,      // Permanent upgrades from progression
        Equipment,      // Modifiers from equipped items
        Temporary,      // Short-term buffs/debuffs
        Environmental   // Effects from the environment (e.g., terrain)
    }
}
