namespace Sol
{
    /// <summary>
    /// The world's cell streamer, as seen by other systems. Placed items register through this, so they are found by cell and
    /// never by a scene scan.
    /// </summary>
    public interface IWorldStreamer
    {
        /// <summary>Registers a placed item with the cell it stands in.</summary>
        void Register(WorldItem item);

        /// <summary>Removes a placed item from its cell's list.</summary>
        void Unregister(WorldItem item);
    }
}
