namespace Sol
{
    public interface ISaveService
    {
        bool SaveExists(string slotId = "default");
        PlayerSaveData Load(string slotId = "default");
        void Save(PlayerSaveData data, string slotId = "default");
        void Delete(string slotId = "default");
        string GetLastSaveTime(string slotId = "default");
    }
}
