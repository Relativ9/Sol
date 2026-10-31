namespace Sol
{
    public interface ISaveService
    {
        // Every call names its slot. The slot names live in SaveCoordinator, so there is no default here to drift from them.
        bool SaveExists(string slotId);
        PlayerSaveData Load(string slotId);
        // Returns false when the save did not reach disk. The previous copy is kept in that case.
        bool Save(PlayerSaveData data, string slotId);
        void Delete(string slotId);
        string Backup(string slotId);

        /// <summary>The slot holding the most recently written save, or null if there is none.</summary>
        string NewestSlot();
    }
}
