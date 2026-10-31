namespace Sol
{
    /// <summary>
    /// The save slot the next startup should load, chosen before a scene restart. A restart is how a load takes effect:
    /// startup reads the chosen slot instead of the default. It is cleared once startup has read it, so a normal boot
    /// always loads the default slot.
    /// </summary>
    public static class PendingLoad
    {
        public static string Slot { get; private set; }

        /// <summary>Marks a slot to load on the next startup, and keeps it through the scene restart.</summary>
        public static void Request(string slot)
        {
            Slot = string.IsNullOrEmpty(slot) ? SaveCoordinator.ManualSlot : slot;
        }

        /// <summary>
        /// The slot startup should read. A requested slot is used if one was set. Otherwise the newest save across all
        /// slots is used, so a normal boot always resumes from the latest state. Falls back to the manual slot when no save
        /// exists at all, so a new game is created there. Consumed on read.
        /// </summary>
        public static string Consume(ISaveService saves)
        {
            string slot = Slot;
            if (string.IsNullOrEmpty(slot)) slot = saves?.NewestSlot();
            if (string.IsNullOrEmpty(slot)) slot = SaveCoordinator.ManualSlot;

            Slot = null;
            return slot;
        }
    }
}
