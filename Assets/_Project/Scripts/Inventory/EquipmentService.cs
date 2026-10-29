using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Plain C# equipment state. Each piece's authored stat modifiers are Equipment modifiers keyed by its instance ID,
    /// so unequip removes exactly what that piece added. Armour weight and piece counts are totals written as base
    /// values, because DerivedStatResolver reads base values to derive resistance and deflection.
    /// </summary>
    public class EquipmentService : IEquipmentService
    {
        private readonly IStatsService _stats;
        private readonly System.Action _recalculateDerived;

        public event System.Action Changed;
        private readonly Dictionary<EquipmentSlot, EquippedPiece> _slots = new();

        private class EquippedPiece
        {
            public string InstanceId;
            public EquipmentDataSO Item;
        }

        /// <param name="stats">The stats the equipment writes modifiers to.</param>
        /// <param name="recalculateDerived">Re-derives resistance and deflection from weight and piece counts.
        /// Supplied by ProgressionService, which owns the one DerivedStatResolver.</param>
        public EquipmentService(IStatsService stats, System.Action recalculateDerived)
        {
            _stats = stats;
            _recalculateDerived = recalculateDerived;
        }

        public bool TryEquip(string instanceId, EquipmentDataSO item)
        {
            return TryEquip(instanceId, item, SlotFor(item));
        }

        /// <summary>Equips into a chosen slot. The item must list that slot as valid. Used by drag and drop.</summary>
        public bool TryEquip(string instanceId, EquipmentDataSO item, EquipmentSlot slot)
        {
            if (item == null || string.IsNullOrEmpty(instanceId)) return false;
            if (slot == EquipmentSlot.None) return false;
            if (item.validSlots == null || System.Array.IndexOf(item.validSlots, slot) < 0) return false;

            // A taken slot refuses the item. Swapping is a separate step: unequip first, then equip.
            if (_slots.ContainsKey(slot)) return false;

            // Guard against one instance being in two slots at once.
            foreach (var piece in _slots.Values)
            {
                if (piece.InstanceId == instanceId) return false;
            }

            _slots[slot] = new EquippedPiece { InstanceId = instanceId, Item = item };
            ApplyContribution(instanceId, item);
            RecalculateArmourTotals();
            _recalculateDerived?.Invoke();
            Changed?.Invoke();
            return true;
        }

        public bool TryUnequip(EquipmentSlot slot)
        {
            if (!_slots.TryGetValue(slot, out EquippedPiece piece)) return false;

            _stats.RemoveModifiersFromSource(piece.InstanceId);
            _slots.Remove(slot);
            RecalculateArmourTotals();
            _recalculateDerived?.Invoke();
            Changed?.Invoke();
            return true;
        }

        public EquipmentDataSO GetEquipped(EquipmentSlot slot)
        {
            return _slots.TryGetValue(slot, out EquippedPiece piece) ? piece.Item : null;
        }

        public string GetEquippedInstanceId(EquipmentSlot slot)
        {
            return _slots.TryGetValue(slot, out EquippedPiece piece) ? piece.InstanceId : null;
        }

        public IReadOnlyDictionary<EquipmentSlot, EquipmentDataSO> GetAllEquipped()
        {
            var result = new Dictionary<EquipmentSlot, EquipmentDataSO>();
            foreach (var entry in _slots)
                result[entry.Key] = entry.Value.Item;
            return result;
        }

        // The stat modifiers authored on the item asset (e.g. +20 Health, +20 Armor). Authored as names, so each name
        // is resolved here. A name that matches no stat is logged, so a typo is visible instead of silently ignored.
        private void ApplyAuthoredModifiers(string instanceId, EquipmentDataSO item)
        {
            if (item.statModifiers == null) return;

            foreach (EquipmentDataSO.StatModifierDefinition def in item.statModifiers)
            {
                if (def == null) continue;

                _stats.ApplyOrReplaceModifier(new StatModifier(
                    type: def.modifierType,
                    category: ModifierCategory.Equipment,
                    statType: def.statName,
                    value: def.value,
                    sourceId: instanceId,
                    duration: -1f));
            }
        }

        // What one piece adds. Its own stat modifiers are applied as Equipment modifiers, keyed by instance ID.
        private void ApplyContribution(string instanceId, EquipmentDataSO item)
        {
            ApplyAuthoredModifiers(instanceId, item);
        }

        // Weight and piece counts are totals across every equipped piece. They are written as BASE values, not
        // modifiers, because DerivedStatResolver reads base values. Recomputed from the slots on every change, so an
        // equip or unequip always leaves the totals correct.
        // Armour value per piece = Weight x Type Multiplier x Material Efficiency (design doc, section 2).
        // Plate is 4x, flex 3x, cloth 1x. Material efficiency is 1 until items carry it.
        public const float PlateMultiplier = 4f;
        public const float FlexMultiplier = 3f;
        public const float ClothMultiplier = 1f;

        public static float ArmorValueFor(WearableDataSO item)
        {
            float multiplier = item._armorType switch
            {
                ArmorType.Plate => PlateMultiplier,
                ArmorType.Flex => FlexMultiplier,
                ArmorType.Cloth => ClothMultiplier,
                _ => 0f,
            };
            return item.Weight * multiplier * item.MaterialEfficiency;
        }

        private void RecalculateArmourTotals()
        {
            float plateKg = 0f, flexKg = 0f, armourValue = 0f;
            int plateCount = 0, flexCount = 0, clothCount = 0;

            foreach (EquippedPiece piece in _slots.Values)
            {
                // Weapons and off-hand items are not armour. Only wearables add to the armour totals.
                if (piece.Item is not WearableDataSO wearable) continue;
                // Accessories (rings and the like) leave the armour category at its default, so only armour counts here.
                if (wearable._wearableCategory != WearableCategory.Armor) continue;

                ArmorType type = wearable._armorType;
                if (type == ArmorType.Plate) { plateKg += wearable.Weight; plateCount++; }
                else if (type == ArmorType.Flex) { flexKg += wearable.Weight; flexCount++; }
                else if (type == ArmorType.Cloth) { clothCount++; }

                armourValue += ArmorValueFor(wearable);
            }

            _stats.SetDerivedBase(StatTypeEnum.RigidWeight, plateKg);
            _stats.SetDerivedBase(StatTypeEnum.FlexWeight, flexKg);
            _stats.SetDerivedBase(StatTypeEnum.PlateCount, plateCount);
            _stats.SetDerivedBase(StatTypeEnum.FlexCount, flexCount);
            _stats.SetDerivedBase(StatTypeEnum.ClothCount, clothCount);
            // ArmorValue's base is the character's own starting armour, captured once. Equipped armour is then set on
            // top of that each time, so repeated equips and unequips never compound.
            _baseArmourValue ??= _stats.GetBaseStat(StatTypeEnum.ArmorValue);
            _stats.SetDerivedBase(StatTypeEnum.ArmorValue, _baseArmourValue.Value + armourValue);
        }

        private float? _baseArmourValue;

        // Slot comes from the item's first valid slot. Items with no slot cannot be equipped.
        private static EquipmentSlot SlotFor(EquipmentDataSO item)
        {
            if (item.validSlots == null || item.validSlots.Length == 0) return EquipmentSlot.None;
            return item.validSlots[0];
        }
    }
}
