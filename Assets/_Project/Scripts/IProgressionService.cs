namespace Sol
{
    public interface IProgressionService
    {
        int GetLevel();
        int GetTotalTalentPoints();
        int GetAvailableTalentPoints();
        bool SpendTalentPoint();
        void RefundTalentPoint();
        void Initialize(PlayerProgressionSO progressionSO, PlayerSaveData saveData);
    }
}
