using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// Stored items on a grid. Plain C#. The grid holds placement, this class holds which item is which.
    /// Width is fixed; height is derived from InventorySpace (total cells) at construction, so a stat change rebuilds it.
    /// </summary>
    public class InventoryService : IInventoryService
    {
        // Fixed width of the stored grid. Height grows with InventorySpace (total cells / width).
        public const int GridColumns = 8;

        private readonly Dictionary<string, ItemDataSO> _items = new();

        // Every stored item, one entry per individual. Each has its own instance ID, even when it shares a stack.
        // A stack is one cell on the grid, held by its lead. _leadOf maps each individual to its lead (a lead maps to
        // itself), and _members lists each lead's stack in order, the lead first.
        private readonly Dictionary<string, string> _leadOf = new();
        private readonly Dictionary<string, List<string>> _members = new();

        public event System.Action Changed;
        private InventoryGrid _grid;

        public int GridWidth => _grid.Width;
        public int GridHeight => _grid.Height;

        public InventoryService(int totalCells)
        {
            Resize(totalCells);
        }

        /// <summary>Rebuilds the grid for a new size. Refuses to shrink below the items already stored.</summary>
        public bool Resize(int totalCells)
        {
            int height = System.Math.Max(1, totalCells / GridColumns);
            var next = new InventoryGrid(GridColumns, height);

            // Re-place every stack at its current cell. Only leads hold cells, so each stack is placed once. If one does
            // not fit, the resize is refused and nothing changes, so an item can never be dropped by a stat change.
            if (_grid != null)
            {
                foreach (var entry in _grid.Placements)
                {
                    InventoryGrid.Placement p = entry.Value;
                    if (!next.TryPlace(entry.Key, p.X, p.Y, p.Width, p.Height, p.Rotated)) return false;
                }
            }

            _grid = next;
            return true;
        }

        public bool TryStore(string instanceId, ItemDataSO item)
        {
            if (item == null || string.IsNullOrEmpty(instanceId) || _items.ContainsKey(instanceId)) return false;
            if (!_grid.TryFindFreeSpot(item.GridWidth, item.GridHeight, out int x, out int y)) return false;
            if (!_grid.TryPlace(instanceId, x, y, item.GridWidth, item.GridHeight, false)) return false;

            _items[instanceId] = item;
            _leadOf[instanceId] = instanceId;
            _members[instanceId] = new List<string> { instanceId };
            Changed?.Invoke();
            return true;
        }

        public bool TryMove(string instanceId, int x, int y, bool rotated)
        {
            // Only a stack's lead holds a cell, so moving any member moves the whole stack.
            string lead = StackLeadOf(instanceId);
            if (lead == null || !_items.TryGetValue(lead, out ItemDataSO item)) return false;

            // Rotation swaps the footprint. Only allowed when the item can be rotated.
            if (rotated && !item.Rotatable) return false;
            int w = rotated ? item.GridHeight : item.GridWidth;
            int h = rotated ? item.GridWidth : item.GridHeight;

            if (!_grid.TryPlace(lead, x, y, w, h, rotated)) return false;
            Changed?.Invoke();
            return true;
        }

        public bool TryFindNearestFit(string instanceId, int targetX, int targetY, bool rotated, out int x, out int y)
        {
            x = -1;
            y = -1;
            string lead = StackLeadOf(instanceId);
            if (lead == null || !_items.TryGetValue(lead, out ItemDataSO item)) return false;

            // Rotation swaps the footprint. A non-rotatable item cannot be placed rotated.
            if (rotated && !item.Rotatable) return false;
            int w = rotated ? item.GridHeight : item.GridWidth;
            int h = rotated ? item.GridWidth : item.GridHeight;
            return _grid.TryFindNearestFit(lead, targetX, targetY, w, h, out x, out y);
        }

        // Removes one individual. A member leaves its stack and the cell stays with the lead. When the lead leaves, the
        // next member takes the cell at the same spot, so the rest of the stack keeps its place. Removing the last
        // individual frees the cell.
        public bool TryRemove(string instanceId)
        {
            string lead = StackLeadOf(instanceId);
            if (lead == null) return false;

            if (instanceId != lead)
            {
                _members[lead].Remove(instanceId);
                _items.Remove(instanceId);
                _leadOf.Remove(instanceId);
            }
            else
            {
                List<string> members = _members[lead];
                members.Remove(lead);
                _items.Remove(lead);
                _leadOf.Remove(lead);
                _members.Remove(lead);

                if (members.Count == 0)
                {
                    _grid.Remove(lead);
                }
                else
                {
                    // The cell is freed by Remove, so placing the next lead there cannot fail.
                    InventoryGrid.Placement p = _grid.Placements[lead];
                    _grid.Remove(lead);
                    string next = members[0];
                    _grid.TryPlace(next, p.X, p.Y, p.Width, p.Height, p.Rotated);
                    _members[next] = members;
                    foreach (string id in members) _leadOf[id] = next;
                }
            }

            Changed?.Invoke();
            return true;
        }

        public string OwnerAt(int x, int y) => _grid.OwnerAt(x, y);

        // Merges a whole stack into another stack of the same item. Every member of the source joins the target, and the
        // source's cell is freed. Refused if the combined stack would exceed the item's stack limit.
        public bool TryMerge(string sourceInstanceId, string targetInstanceId)
        {
            string sourceLead = StackLeadOf(sourceInstanceId);
            string targetLead = StackLeadOf(targetInstanceId);
            if (sourceLead == null || targetLead == null || sourceLead == targetLead) return false;

            ItemDataSO target = _items[targetLead];
            if (_items[sourceLead].ItemId != target.ItemId) return false;

            List<string> sourceMembers = _members[sourceLead];
            List<string> targetMembers = _members[targetLead];
            if (sourceMembers.Count + targetMembers.Count > target.MaxStackSize) return false;

            foreach (string id in sourceMembers)
            {
                _leadOf[id] = targetLead;
                targetMembers.Add(id);
            }
            _members.Remove(sourceLead);
            _grid.Remove(sourceLead);
            Changed?.Invoke();
            return true;
        }

        public ItemDataSO GetStored(string instanceId)
        {
            return _items.TryGetValue(instanceId, out ItemDataSO item) ? item : null;
        }

        public IReadOnlyDictionary<string, ItemDataSO> GetAllStored() => _items;

        public InventoryGrid.Placement? GetPlacement(string instanceId)
        {
            if (_grid.Placements.TryGetValue(instanceId, out InventoryGrid.Placement p)) return p;
            return null;
        }

        public bool CanStoreAnywhere(int footprintWidth, int footprintHeight)
        {
            return _grid.TryFindFreeSpot(footprintWidth, footprintHeight, out _, out _);
        }

        // How many individuals share this entry's stack. A lone item is 1. An ID that is not stored is 0.
        public int CountOf(string instanceId)
        {
            string lead = StackLeadOf(instanceId);
            return lead != null ? _members[lead].Count : 0;
        }

        // The lead of the stack this individual belongs to. A lone item leads itself. Null if not stored.
        public string StackLeadOf(string instanceId) => _leadOf.TryGetValue(instanceId, out string lead) ? lead : null;

        public IReadOnlyList<string> MembersOf(string leadInstanceId)
        {
            if (!string.IsNullOrEmpty(leadInstanceId) && _members.TryGetValue(leadInstanceId, out List<string> members))
                return members;
            return System.Array.Empty<string>();
        }

        // The individual a single equip takes from a stack. It is the last member, so the lead stays in the cell and the
        // stack keeps its place. A lone item is its own individual.
        public string IndividualToTake(string instanceId)
        {
            string lead = StackLeadOf(instanceId);
            if (lead == null) return null;
            List<string> members = _members[lead];
            return members[members.Count - 1];
        }


        // Adds one individual to the first stack of the same item that has room. Returns false if none has room.
        public bool TryAddToStack(ItemDataSO item, string instanceId)
        {
            if (item == null) return false;
            foreach (KeyValuePair<string, List<string>> stack in _members)
            {
                if (!_items.TryGetValue(stack.Key, out ItemDataSO lead) || lead.ItemId != item.ItemId) continue;
                if (stack.Value.Count >= lead.MaxStackSize) continue;
                return TryJoinStack(stack.Key, item, instanceId);
            }
            return false;
        }

        // Adds one individual to a specific stack. It must be the same item, the stack must have room, and the ID must
        // be new. Used by pickup and by the load, which puts each saved individual back into its own stack.
        public bool TryJoinStack(string leadInstanceId, ItemDataSO item, string instanceId)
        {
            if (item == null || string.IsNullOrEmpty(instanceId) || _items.ContainsKey(instanceId)) return false;
            if (string.IsNullOrEmpty(leadInstanceId) || !_members.TryGetValue(leadInstanceId, out List<string> members)) return false;

            ItemDataSO lead = _items[leadInstanceId];
            if (lead.ItemId != item.ItemId || members.Count >= lead.MaxStackSize) return false;

            _items[instanceId] = item;
            _leadOf[instanceId] = leadInstanceId;
            members.Add(instanceId);
            Changed?.Invoke();
            return true;
        }

        // Takes the last few individuals off a stack into a new stack, in a new cell. The source keeps the rest. The
        // moved individuals keep their own instance IDs, so nothing is copied or renamed.
        // newLead is the lead of the new stack, which is the first moved individual. A caller can carry that stack.
        public bool TrySplit(string sourceInstanceId, int amount, out string newLead)
        {
            newLead = null;
            string lead = StackLeadOf(sourceInstanceId);
            if (lead == null) return false;

            List<string> members = _members[lead];
            if (amount <= 0 || amount >= members.Count) return false;

            ItemDataSO item = _items[lead];
            if (!_grid.TryFindFreeSpot(item.GridWidth, item.GridHeight, out int x, out int y)) return false;

            // The first of the moved individuals leads the new stack, so it is the one given the cell.
            int keep = members.Count - amount;
            string candidate = members[keep];
            if (!_grid.TryPlace(candidate, x, y, item.GridWidth, item.GridHeight, false)) return false;

            List<string> moved = members.GetRange(keep, amount);
            members.RemoveRange(keep, amount);
            _members[candidate] = moved;
            foreach (string id in moved) _leadOf[id] = candidate;
            newLead = candidate;
            Changed?.Invoke();
            return true;
        }

        // Splits part of a stack onto a cell, in one step. The cell is checked before anything changes: if it is blocked,
        // or the amount is not a valid split, nothing is touched. The moved individuals keep their own instance IDs. The
        // drag only records the intent; this is the one call that carries it out, and it runs only on an accepted drop.
        public bool TrySplitOnto(string sourceInstanceId, int amount, int x, int y, bool rotated, out string newLead)
        {
            newLead = null;
            string lead = StackLeadOf(sourceInstanceId);
            if (lead == null) return false;

            List<string> members = _members[lead];
            if (amount <= 0 || amount >= members.Count) return false;

            ItemDataSO item = _items[lead];
            if (rotated && !item.Rotatable) return false;
            int w = rotated ? item.GridHeight : item.GridWidth;
            int h = rotated ? item.GridWidth : item.GridHeight;

            // The cell is checked on a dry run first. Nothing has changed yet, so a refusal leaves the stack exactly as it was.
            if (!_grid.CanPlace(lead, x, y, w, h)) return false;

            int keep = members.Count - amount;
            string candidate = members[keep];
            List<string> moved = members.GetRange(keep, amount);
            members.RemoveRange(keep, amount);

            _members[candidate] = moved;
            foreach (string id in moved) _leadOf[id] = candidate;

            // The new stack's cell can only be taken now that the moved members are no longer in the source.
            if (!_grid.TryPlace(candidate, x, y, w, h, rotated))
            {
                // Cannot happen after CanPlace, but undo fully so nothing is lost if it ever does.
                members.AddRange(moved);
                foreach (string id in moved) _leadOf[id] = lead;
                _members.Remove(candidate);
                return false;
            }

            newLead = candidate;
            Changed?.Invoke();
            return true;
        }

        public bool[,] BlockedCells(string instanceId, int x, int y, int footprintWidth, int footprintHeight)
        {
            return _grid.OccupiedCells(StackLeadOf(instanceId) ?? instanceId, x, y, footprintWidth, footprintHeight);
        }

        public float GetStoredWeightKg()
        {
            float total = 0f;
            foreach (var item in _items.Values)
                total += item.Weight;
            return total;
        }
    }
}
