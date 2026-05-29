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
        
    }
}
