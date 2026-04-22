
namespace Sol
{
    public interface ITalentStateService
    {
        void Initialize(PlayerSaveData saveData);
        int GetAllocatedPoints(string nodeId);
        string[] GetAllocatedNodeIds();
        bool HasAllocatedPoints(string nodeId);
        bool TryAllocatePoint(string nodeId);
        bool TryRemovePoint(string nodeId);
        void ResetAll();
        PlayerSaveData BuildSaveData();
        // Remove OnStateChanged event - handled via GameEvent SO now
    }
}
