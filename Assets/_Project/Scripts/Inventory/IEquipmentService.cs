using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// Owns what the player has equipped. Each equipped piece adds Equipment modifiers under its instance ID, so
    /// unequipping removes exactly what that piece added, and nothing else.
    /// </summary>
    public interface IEquipmentService
    {
        /// <summary>Equips an item into its slot. Returns false if the slot is taken or the item cannot be equipped.</summary>
        bool TryEquip(string instanceId, EquipmentDataSO item);

        /// <summary>Equips into a chosen slot. Used by drag and drop, so the drop target decides the slot.</summary>
        bool TryEquip(string instanceId, EquipmentDataSO item, EquipmentSlot slot);

        /// <summary>Removes whatever is in the slot and everything it contributed. Returns false if the slot was empty.</summary>
        bool TryUnequip(EquipmentSlot slot);

        /// <summary>The item in a slot, or null.</summary>
        EquipmentDataSO GetEquipped(EquipmentSlot slot);

        IReadOnlyDictionary<EquipmentSlot, EquipmentDataSO> GetAllEquipped();

        /// <summary>The instance ID of the item in a slot, or null.</summary>
        string GetEquippedInstanceId(EquipmentSlot slot);

        /// <summary>Raised after an item is equipped or unequipped, so the slot display can redraw.</summary>
        event System.Action Changed;
    }
}
