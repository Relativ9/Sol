namespace Sol
{
    /// <summary>Pickup and drop for the player. PlayerController routes the pickup input through this.</summary>
    public interface IPlayerPickup
    {
        /// <summary>Picks up the best world item in reach and in view, if any and if there is room.</summary>
        void TryPickUp();

        /// <summary>The item E would pick up right now, or null. Used by the glow and the name prompt.</summary>
        WorldItem CurrentTarget { get; }

        /// <summary>Drops a stored item at the player's feet, using the item's own world model. False if it cannot.</summary>
        bool TryDrop(string instanceId);

        /// <summary>The Grab action's hold completed. Picks up the item in view into the carry, without looting it.</summary>
        void BeginGrab();

        /// <summary>The Grab action was released. Ends the carry and leaves the item where it is.</summary>
        void EndGrab();
    }
}
