using UnityEngine;

namespace Sol
{
    public struct StatModifier
    {
        public string id;
        public ModifierType type;
        public ModifierCatagory catagory;
        public float value;
        public object source;
        public float duration;

    
        public StatModifier(ModifierType type, ModifierCatagory catagory, float value, object source = null, float duration = -1f)
        {
            this.id = null;
            this.type = type;
            this.value = value;
            this.source = source;
            this.duration = duration;
            this.catagory = catagory;
        }
    }

    public enum ModifierCatagory
    {
        Base,           // The original value from ScriptableObject
        Permanent,      // Permanent upgrades from progression
        Equipment,      // Modifiers from equipped items
        Temporary,      // Short-term buffs/debuffs
        Environmental   // Effects from the environment (e.g., terrain)
    }
    
    public enum ModifierType
    {
        Additive,       // Add to base value
        Subtractive,    // Subtract to base value
        Multiplicative  // Multiply the result
    }
}
