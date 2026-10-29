using UnityEngine;
using System;

namespace Sol
{
    public abstract class EquipmentDataSO : ItemDataSO
    {
        [Header("Equipment")]
        public EquipmentSlot[] validSlots;
        public StatModifierDefinition[] statModifiers;

        [Header("Attack")]
        [Tooltip("Flat physical damage. Weapons and offhands both have one (a shield bash, a dagger stab). 0 for gear with no attack.")]
        [Min(0f)] public float physicalAttack;

        [Tooltip("Extra elemental damage on top of physical, for example fire. Leave empty for a purely physical attack.")]
        public ElementalAttack[] elementalAttacks = Array.Empty<ElementalAttack>();

        [Serializable]
        public class ElementalAttack
        {
            public DamageType damageType;
            [Min(0f)] public float damage;
        }

        // Equipment footprints come from the slot rule (EquipmentSilhouette), not from the asset. So every item that fits
        // the same slot has the same size, and a designer cannot set a different one. Falls back to the asset value only
        // for an equipment item with no slot, which should not happen for a valid item.
        public override int GridWidth => SlotFootprint(out int w, out _) ? w : base.GridWidth;
        public override int GridHeight => SlotFootprint(out _, out int h) ? h : base.GridHeight;

        // The footprint of the item's primary slot. Its first valid slot decides the size.
        private bool SlotFootprint(out int width, out int height)
        {
            width = 0;
            height = 0;
            if (validSlots == null || validSlots.Length == 0) return false;
            return EquipmentSilhouette.TryFootprintFor(validSlots[0], out width, out height);
        }
        
        [Serializable]
        public class StatModifierDefinition
        {
            // A dropdown of every stat, so designers pick a real stat instead of typing a name that may not match.
            public StatTypeEnum statName;
            public ModifierType modifierType;
            // Not a designer field: every item modifier is an Equipment modifier, always. See EquipmentService.
            public float value;
        }
    }
}


