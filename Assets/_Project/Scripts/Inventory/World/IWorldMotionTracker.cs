namespace Sol
{
    /// <summary>
    /// Tracks physics-driven world items so their poses reach the cell store. Items register when they start, and a save
    /// asks the tracker to write the live poses first.
    /// </summary>
    public interface IWorldMotionTracker
    {
        /// <summary>Starts tracking a physics-driven item. Items already tracked are ignored.</summary>
        void Track(WorldItem item);

        /// <summary>Writes the live pose of every tracked body, so a save reflects the frame it is taken on.</summary>
        void FlushLive();

        /// <summary>
        /// Marks an item as carried by the player. A carried item is not settled or sampled, so a slow hold in the air is never
        /// recorded as a rest pose. Its pose is written once, when it is released.
        /// </summary>
        void SetCarried(WorldItem item, bool carried);
    }
}
