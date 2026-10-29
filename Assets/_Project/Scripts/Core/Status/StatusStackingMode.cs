namespace Sol
{
    /// <summary>How repeat applications of a status combine. Saved as integers if ever persisted, so append only.</summary>
    public enum StatusStackingMode
    {
        RefreshSingle,  // One instance, duration refreshed. Used by stagger.
        StackPerSource, // One instance per source; repeat hits from that source add a stack. Used by bleed.
    }
}
