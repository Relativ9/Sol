using System;
using UnityEngine;

namespace Sol
{
    public struct StatModifier
    {
        public string id;
        public string sourceId; // Used to identify the source (e.g., "SpeedBoostPad1")
        public ModifierType type; // Describes the type of modifier it is, meaning if it's addative, subtracive or multiplicative, important for balancing.
        public ModifierCategory category; //Describes the catagory of modifier, if it's a permanent boost it's likely from skill or mastery points, if its base it's from the staring class/race selection, if its equipment bonuses, if its temporary it is from gameplay effects such as spells and tiggered passives, and if its enviromental its either form hazards or enviromental buffs (standing in tall water gives immunity to fire, speed boost ect).
        public StatTypeEnum statType; 
        public float value;
        public float duration; //-1f = infinite

    
        public StatModifier(ModifierType type, ModifierCategory category, StatTypeEnum statType, float value, string sourceId = null, float duration = -1f)
        {
            this.id = Guid.NewGuid().ToString(); //or it's passed in (most likely)
            this.sourceId = sourceId;
            this.type = type;
            this.statType = statType;
            this.value = value;
            this.duration = duration;
            this.category = category;
        }
    }
}
