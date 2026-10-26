namespace Sol
{
    /// <summary>
    /// Detects saves that are almost certainly broken (e.g. talent pool 0 with no allocations),
    /// so the player can choose to repair them or start a new game.
    /// </summary>
    public static class SaveValidator
    {
        public static bool IsSuspect(PlayerSaveData data, PlayerProgressionSO progression, out string reason)
        {
            reason = null;
            if (data == null || progression == null) return false;

            bool hasAllocations = data.talentAllocations != null && data.talentAllocations.Length > 0;
            bool hasAttributes = data.attributeAllocations != null && data.attributeAllocations.Length > 0;

            // Expected pool at least covers the starting value. A save below it with nothing spent is inconsistent.
            if (data.totalTalentPoints < progression.startingTalentPoints && !hasAllocations && !hasAttributes
                && data.level <= progression.startingLevel)
            {
                reason = $"Talent points are {data.totalTalentPoints}, but a new character starts with {progression.startingTalentPoints}, " +
                         "and nothing has been allocated.";
                return true;
            }

            // The attribute pool is not checked here: it is derived from level on load (ProgressionService),
            // so a stale saved value cannot block spending and needs no prompt.
            return false;
        }
    }
}
