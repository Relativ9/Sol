using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// The stored items and the grid they sit in. Equipped items are not here: equipping moves an item out of the grid
    /// and into equipment, and unequipping moves it back. The grid is the only place storage geometry lives.
    /// </summary>
    public interface IInventoryService
    {
        int GridWidth { get; }
        int GridHeight { get; }

        /// <summary>Adds an item to the first free spot. Returns false if the grid has no room.</summary>
        bool TryStore(string instanceId, ItemDataSO item);

        /// <summary>Moves a stored item to a new cell. Returns false if the cells are blocked or out of bounds.</summary>
        bool TryMove(string instanceId, int x, int y, bool rotated);

        /// <summary>
        /// The nearest free origin for a stored item, aimed at a target cell. Used for drop previews and for the final
        /// drop, so what is highlighted is exactly where the item will land.
        /// </summary>
        bool TryFindNearestFit(string instanceId, int targetX, int targetY, bool rotated, out int x, out int y);

        /// <summary>Removes a stored item from the grid entirely (dropped or consumed).</summary>
        bool TryRemove(string instanceId);

        /// <summary>The stored item with this instance ID, or null.</summary>
        ItemDataSO GetStored(string instanceId);

        /// <summary>The stored entry occupying a grid cell, or null. Used to find a drag-merge target.</summary>
        string OwnerAt(int x, int y);

        /// <summary>
        /// Merges one whole stack into another stack of the same item. Every individual of the source joins the target,
        /// and the source's cell is freed. Fails if the two are not the same item, or the combined stack would exceed the
        /// item's stack limit.
        /// </summary>
        bool TryMerge(string sourceInstanceId, string targetInstanceId);

        IReadOnlyDictionary<string, ItemDataSO> GetAllStored();
        InventoryGrid.Placement? GetPlacement(string instanceId);

        /// <summary>True if an item of this footprint would fit somewhere in the grid right now.</summary>
        bool CanStoreAnywhere(int footprintWidth, int footprintHeight);

        /// <summary>Read-only. Which cells of a footprint at (x, y) are blocked for this item, for colouring a drop preview.
        /// True means blocked by another item, or off the grid.</summary>
        bool[,] BlockedCells(string instanceId, int x, int y, int footprintWidth, int footprintHeight);

        /// <summary>Raised after any successful store, move or remove, so open views can redraw.</summary>
        event System.Action Changed;

        /// <summary>How many individuals share this item's stack. A lone item is 1. An ID that is not stored is 0.</summary>
        int CountOf(string instanceId);

        /// <summary>Adds one individual to the first stack of the same item that has room. False if no stack has room.</summary>
        bool TryAddToStack(ItemDataSO item, string instanceId);

        /// <summary>Adds one individual to a specific stack. Used by pickup and by the load.</summary>
        bool TryJoinStack(string leadInstanceId, ItemDataSO item, string instanceId);

        /// <summary>The stack lead that holds this individual's cell, or null if it is not stored. A lone item leads itself.</summary>
        string StackLeadOf(string instanceId);

        /// <summary>Every individual in a stack, the lead first. Empty if the ID is not a stack lead.</summary>
        IReadOnlyList<string> MembersOf(string leadInstanceId);

        /// <summary>The individual that a single equip takes from a stack: the last member, or the item itself when alone.</summary>
        string IndividualToTake(string instanceId);

        /// <summary>
        /// Splits part of a stack onto a cell in one step. Checks the cell before changing anything, so a refused split
        /// leaves the stack as it was. Only called on an accepted drop.
        /// </summary>
        bool TrySplitOnto(string sourceInstanceId, int amount, int x, int y, bool rotated, out string newLead);

        /// <summary>Takes the last few individuals off a stack into a new stack, in a new cell. Both sides must keep at least one.</summary>
        bool TrySplit(string sourceInstanceId, int amount, out string newLead);

        /// <summary>Total weight of stored items, in kg. Equipped weight is added by the caller.</summary>
        float GetStoredWeightKg();
    }
}
