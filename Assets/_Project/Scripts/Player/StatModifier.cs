using UnityEngine;

namespace Sol
{
    public struct StatModifier
    {
        public string id;
        public string sourceId; // Used to identify the source (e.g., "SpeedBoostPad1")
        public ModifierType type; // Describes the type of modifier it is, meaning if it's addative, subtracive or multiplicative, important for balancing.
        public ModifierCatagory catagory; //Describes the catagory of modifier, if it's a permanent boost it's likely from skill or mastery points, if its base it's from the staring class/race selection, if its equipment equipment bonuses, if its temporary its from gameplay effects such as spells and tiggered passives, and if its enviromental its either form hazards or enviromental buffs (standing in tall water gives immunity to fire, speed boost ect).
        public float value;
        public object source;
        public float duration;

    
        public StatModifier(ModifierType type, ModifierCatagory catagory, float value, object source = null, float duration = -1f)
        {
            this.id = null;
            this.sourceId = null;
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
