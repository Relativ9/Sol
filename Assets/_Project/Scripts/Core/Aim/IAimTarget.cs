namespace Sol
{
    /// <summary>
    /// Something a ray can be aimed at. A world item, an enemy, a breakable, an NPC. Each implements this to say whether
    /// it can be aimed at right now, so the aim consumer (pickup, archery, spell) decides what to do with it.
    /// </summary>
    public interface IAimTarget
    {
        /// <summary>True when this target can currently be aimed at (for example, not already taken).</summary>
        bool IsAimable { get; }
    }
}
