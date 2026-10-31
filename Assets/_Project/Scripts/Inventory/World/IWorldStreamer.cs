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

        /// <summary>Keeps a cell loaded while something in it needs to stay live, such as a moving body. Pins are counted.</summary>
        void Pin(CellKey key);

        /// <summary>Releases one pin on a cell. The cell is released only when its last pin is removed and it is outside the ring.</summary>
        void Unpin(CellKey key);
    }
}
