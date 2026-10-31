namespace Sol
{
    [System.Serializable]
    public struct TalentAllocationEntry
    {
        public string nodeId;
        public int allocatedPoints;
    }

    // One worn item. instanceId is the stat-modifier source ID, so it must survive a save unchanged.
    [System.Serializable]
    public struct EquippedSaveEntry
    {
        public string slot;        // EquipmentSlot name, e.g. "Torso". A name, not an int, so reordering the enum cannot misplace items.
        public string itemId;      // ItemDataSO.ItemId, resolved through ItemRegistry on load.
        public string instanceId;
    }

    // One stored item on the grid.
    [System.Serializable]
    public struct InventorySaveEntry
    {
        public string itemId;
        public string instanceId;
        public int x;
        public int y;
        public bool rotated;
        // The stack this individual belongs to. Its cell is held by the lead, so each individual names the lead. A lone
        // item, or a save written before stacking, has no lead and stands alone. Each individual keeps its own
        // instanceId, so a dagger is always one specific dagger.
        public string stackLeadId;
    }

    [System.Serializable]
    public class AttributeAllocationEntry
    {
        public StatTypeEnum statType;
        public int pointsAllocated; //points for attributes above (or below) the starting default
    }
    
    [System.Serializable]
    public class PlayerSaveData
    {
        public static PlayerSaveData CreateNewGame(PlayerProgressionSO progression)
        {
            return new PlayerSaveData
            {
                level = progression.startingLevel,
                totalTalentPoints = progression.startingTalentPoints,
                totalAttributePoints = progression.startingAttributePoints,
                talentAllocations = System.Array.Empty<TalentAllocationEntry>(),
                // All allocations default to 0 (meaning 10 Brawn, 10 Finesse, etc.)
            };
        }
        
        // Progression
        public int level;
        public int totalTalentPoints;
        public int prestigePoints;
        public int totalAttributePoints;

        
        // Selected talent IDs (resolved against TalentDataSO registry on load)
        public TalentAllocationEntry[] talentAllocations;
        public AttributeAllocationEntry[] attributeAllocations;

        // Current resource state (mutable gameplay values, not derived from stats)
        public float currentHealth;
        public float currentEnergy;

        // Equipped items, one entry per occupied slot. Item identity is the item's ItemId, resolved on load through
        // ItemRegistry. Stat effects are never saved: they are rebuilt by re-equipping on load.
        public EquippedSaveEntry[] equipped = System.Array.Empty<EquippedSaveEntry>();

        // Stored items on the grid, with their cells. Layout is saved, because it cannot be rebuilt from a list.
        public InventorySaveEntry[] inventory = System.Array.Empty<InventorySaveEntry>();

        // Where the player stood when saved. Restored on load, so Load returns the player to the same place, not the scene spawn.
        // Position is in metres. Yaw is the facing around the vertical axis, in degrees.
        public float posX;
        public float posY;
        public float posZ;
        public float yawDegrees;
        public bool hasPosition;

        // World state is not stored here. It lives in per-slot cell files (see CellStore and SaveManager.SlotCellsFolder).
    }
}
