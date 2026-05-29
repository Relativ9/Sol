namespace Sol
{
    [System.Serializable]
    public struct TalentAllocationEntry
    {
        public string nodeId;
        public int allocatedPoints;
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

        // Equipment (instance IDs, resolved against ItemInstance registry on load)
        public string equippedHeadId;
        public string equippedFaceId;
        public string equippedNeckId;
        public string equippedTorsoId;
        public string equippedHandsId;
        public string equippedLegsId;
        public string equippedFeetId;
        public string equippedRingLeftId;
        public string equippedRingRightId;
        public string equippedMainHandId;
        public string equippedOffhandId;

        // Inventory contents (to be expanded when ItemInstance is designed)
        public string[] inventoryInstanceIds;
    }
}
