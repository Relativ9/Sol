using UnityEngine;
using System;

namespace Sol
{
    public abstract class EquipmentDataSO : ItemDataSO
    {
        [Header("Equipment")]
        public EquipmentSlot[] validSlots;
        public StatModifierDefinition[] statModifiers;
        
        [Serializable]
        public class StatModifierDefinition
        {
            public string statName;
            public ModifierType modifierType;
            public ModifierCatagory modifierCategory;
            public float value;
        }
    }
}


