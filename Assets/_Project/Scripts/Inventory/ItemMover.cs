namespace Sol
{
    /// <summary>
    /// Moves an item between the grid and an equipment slot. The only class that knows about both services, so
    /// neither service depends on the other. Every move is all-or-nothing: if either side refuses, nothing changes.
    /// </summary>
    public class ItemMover
    {
        private readonly IInventoryService _inventory;
        private readonly IEquipmentService _equipment;

        public ItemMover(IInventoryService inventory, IEquipmentService equipment)
        {
            _inventory = inventory;
            _equipment = equipment;
        }

        /// <summary>
        /// Equips a stored item. It leaves the grid only once the slot has accepted it. If the slot refuses, the
        /// item stays exactly where it was.
        /// </summary>
        public bool TryEquipFromGrid(string instanceId)
        {
            string individual = _inventory.IndividualToTake(instanceId);
            if (individual == null) return false;

            ItemDataSO stored = _inventory.GetStored(individual);
            if (stored is not EquipmentDataSO equipment) return false;

            if (!_equipment.TryEquip(individual, equipment)) return false;

            // The slot accepted the item, so remove it from the grid. Removal cannot fail for a stored item.
            _inventory.TryRemove(individual);
            return true;
        }

        /// <summary>
        /// Equips a stored item into a specific slot, as chosen by a drop. The item must list that slot as valid, so a
        /// helm cannot be dropped on the boots. The grid is changed only after the equipment accepts the item.
        /// </summary>
        public bool TryEquipFromGrid(string instanceId, EquipmentSlot slot)
        {
            // A stack gives up one individual. The one taken is the stack's last member, so the rest of the stack keeps
            // its cell. A lone item is its own individual. The individual is what gets equipped, under its own ID.
            string individual = _inventory.IndividualToTake(instanceId);
            if (individual == null) return false;

            ItemDataSO stored = _inventory.GetStored(individual);
            if (stored is not EquipmentDataSO equipment) return false;

            // The equipment checks the slot is valid for this item and the slot is free. The grid changes only after.
            if (!_equipment.TryEquip(individual, equipment, slot)) return false;

            _inventory.TryRemove(individual);
            return true;
        }

        /// <summary>
        /// Moves a worn item from one slot to another. All or nothing: if the target refuses, the item goes back to
        /// its original slot, so nothing is lost or left half-moved.
        /// </summary>
        public bool TryMoveBetweenSlots(EquipmentSlot from, EquipmentSlot to)
        {
            if (from == to) return false;

            EquipmentDataSO worn = _equipment.GetEquipped(from);
            string instanceId = _equipment.GetEquippedInstanceId(from);
            if (worn == null || string.IsNullOrEmpty(instanceId)) return false;

            if (!_equipment.TryUnequip(from)) return false;
            if (_equipment.TryEquip(instanceId, worn, to)) return true;

            // The target refused, so restore the item to where it came from.
            _equipment.TryEquip(instanceId, worn, from);
            return false;
        }

        /// <summary>
        /// Unequips a slot into the grid. The item leaves the slot only if the grid has room for it. If there is no
        /// room, the item stays equipped.
        /// </summary>
        public bool TryUnequipToGrid(EquipmentSlot slot)
        {
            EquipmentDataSO worn = _equipment.GetEquipped(slot);
            if (worn == null) return false;

            // Find the instance ID of what is in the slot. Equipment tracks it, so look it up from the slot map.
            string instanceId = _equipment.GetEquippedInstanceId(slot);
            if (string.IsNullOrEmpty(instanceId)) return false;

            // Check room first, using the item's footprint. If there is none, do not unequip.
            if (!_inventory.CanStoreAnywhere(worn.GridWidth, worn.GridHeight)) return false;

            if (!_equipment.TryUnequip(slot)) return false;
            return _inventory.TryStore(instanceId, worn);
        }

        /// <summary>
        /// Takes a worn item off into the grid, placed at the nearest spot to a chosen cell. All or nothing: if the item
        /// cannot be stored, it is put back in its slot, so it is never lost.
        /// </summary>
        public bool TryUnequipToGridAt(EquipmentSlot slot, int targetX, int targetY, bool rotated)
        {
            EquipmentDataSO worn = _equipment.GetEquipped(slot);
            string instanceId = _equipment.GetEquippedInstanceId(slot);
            if (worn == null || string.IsNullOrEmpty(instanceId)) return false;

            if (!_equipment.TryUnequip(slot)) return false;

            // The item is not in the grid yet, so it is stored first at any free spot, then moved to the chosen cell.
            if (_inventory.TryStore(instanceId, worn)
                && _inventory.TryFindNearestFit(instanceId, targetX, targetY, rotated, out int x, out int y)
                && _inventory.TryMove(instanceId, x, y, rotated))
            {
                return true;
            }

            // Could not place it. Undo: remove it from the grid if it was stored, and put it back in its slot.
            if (_inventory.GetStored(instanceId) != null) _inventory.TryRemove(instanceId);
            _equipment.TryEquip(instanceId, worn, slot);
            return false;
        }
    }
}
