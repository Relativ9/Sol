namespace Sol
{
    /// <summary>
    /// Kinds of timed status. Saved as integers if ever persisted, so append only.
    /// Stagger is a one-shot interrupt, so it is not tracked over time: see StatusService.TriggerStagger.
    /// </summary>
    public enum StatusKind
    {
        Bleed,
    }
}
