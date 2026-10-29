namespace Sol
{
    /// <summary>
    /// Creates a dropped pile in the scene from its saved record. The streamer asks for this when a cell loads, so it never
    /// needs to know how an item's prefab is found. The scene's item spawner implements it.
    /// </summary>
    public interface IDroppedPileSpawner
    {
        /// <summary>Spawns the pile described by a dropped record, at its saved pose and with its saved identity.</summary>
        void SpawnDropped(CellRecord record);
    }
}
