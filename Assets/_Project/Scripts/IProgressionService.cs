namespace Sol
{
    public interface IProgressionService
    {
        int GetLevel();
        int GetTotalTalentPoints();
        int GetAvailableTalentPoints();
        bool SpendTalentPoint();
        void RefundTalentPoint();

        void Initialize(PlayerProgressionSO progressionSO, PlayerStatSO defaultStats, IStatsService statsService, int level, int totalTalentPoints, int totalAttributePoints, AttributeAllocationEntry[] attributeAllocations);
        void SetSpentTalentPoints(int spent);
        
        void ReloadAllBaseStats();

        // Attributes (core six). Used by the Player Stats tab.
        bool TrySpendAttributePoint(StatTypeEnum statType);
        bool TryRefundAttributePoint(StatTypeEnum statType);
        int GetAvailableAttributePoints();
        int GetTotalAttributePoints();
        int GetAllocatedAttributePoints(StatTypeEnum statType);
        
    }
}
