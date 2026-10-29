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

        /// <summary>The slot startup should read: the requested one if any, otherwise the manual slot. Consumed on read.</summary>
        public static string Consume()
        {
            string slot = string.IsNullOrEmpty(Slot) ? SaveCoordinator.ManualSlot : Slot;
            Slot = null;
            return slot;
        }
    }
}
