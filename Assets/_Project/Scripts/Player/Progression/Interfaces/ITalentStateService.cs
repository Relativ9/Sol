
namespace Sol
{
    public interface ITalentStateService
    {
        void Initialize(PlayerSaveData saveData, IStatsService statsService);
        int GetAllocatedPoints(string nodeId);
        string[] GetAllocatedNodeIds();
        bool HasAllocatedPoints(string nodeId);
        bool TryAllocatePoint(string nodeId);
        bool TryRemovePoint(string nodeId);
        void ResetAll();
        int GetTotalAllocatedPoints();
    }
}
