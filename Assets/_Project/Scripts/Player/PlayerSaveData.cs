namespace Sol
{
    [System.Serializable]
    public class PlayerSaveData
    {
        public static PlayerSaveData CreateNewGame(PlayerProgressionSO progression)
        {
            return new PlayerSaveData
            {
                level = progression.startingLevel,
                talentPoints = progression.startingTalentPoints,
                attributePoints = 3, // For spending during character creation
                // All allocations default to 0 (meaning 10 Brawn, 10 Finesse, etc.)
            };
        }
        // Progression
        public int level;
        public int talentPoints;
        public int spentTalentPoints;
        public int prestigePoints;
        public int attributePoints;

        // Attribute allocations (delta above base, not final values)
        public int brawnAllocation;
        public int finesseAllocation;
        public int insightAllocation;
        public int focusAllocation;
        public int vigorAllocation;
        public int willpowerAllocation;

        // Selected talent IDs (resolved against TalentDataSO registry on load)
        public string[] selectedTalentIds;

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
