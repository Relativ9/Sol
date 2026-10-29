using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// Pure grid model for stored items. Plain C#, no Unity dependencies, so placement rules can be tested directly.
    /// Each item occupies a rectangle of cells. Rotation swaps width and height. Placement is manual, never auto-sorted.
    /// </summary>
    public class InventoryGrid
    {
        public int Width { get; }
        public int Height { get; }

        // Which item (by instance ID) owns each cell. Null means the cell is empty.
        private readonly string[,] _cells;

        // Where each placed item sits, so it can be found, moved and removed by ID.
        private readonly Dictionary<string, Placement> _placements = new();

        public struct Placement
        {
            public int X;
            public int Y;
            public int Width;   // Footprint after rotation.
            public int Height;  // Footprint after rotation.
            public bool Rotated;
        }

        public InventoryGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new string[width, height];
        }

        public IReadOnlyDictionary<string, Placement> Placements => _placements;

        /// <summary>True when the footprint fits fully inside the grid and every cell under it is empty.</summary>
        public bool CanPlace(string instanceId, int x, int y, int footprintWidth, int footprintHeight)
        {
            if (footprintWidth <= 0 || footprintHeight <= 0) return false;
            if (x < 0 || y < 0 || x + footprintWidth > Width || y + footprintHeight > Height) return false;

            for (int dx = 0; dx < footprintWidth; dx++)
            {
                for (int dy = 0; dy < footprintHeight; dy++)
                {
                    string owner = _cells[x + dx, y + dy];
                    if (owner != null && owner != instanceId) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Read-only: for each cell of a footprint at (x, y), true when a different item already occupies it, or the cell
        /// is off the grid. Used to colour a drop preview, so the player can see exactly which cells block a drop.
        /// Cells owned by <paramref name="instanceId"/> itself count as free, so an item can be previewed over its own spot.
        /// </summary>
        public bool[,] OccupiedCells(string instanceId, int x, int y, int footprintWidth, int footprintHeight)
        {
            var blocked = new bool[footprintWidth, footprintHeight];
            for (int dx = 0; dx < footprintWidth; dx++)
            {
                for (int dy = 0; dy < footprintHeight; dy++)
                {
                    int cx = x + dx;
                    int cy = y + dy;
                    if (cx < 0 || cy < 0 || cx >= Width || cy >= Height) { blocked[dx, dy] = true; continue; }
                    string owner = _cells[cx, cy];
                    blocked[dx, dy] = owner != null && owner != instanceId;
                }
            }
            return blocked;
        }

        public bool TryPlace(string instanceId, int x, int y, int footprintWidth, int footprintHeight, bool rotated)
        {
            // Moving an item: check the target with the item's own cells treated as free, so it can move a short
            // distance over itself. Nothing is lifted out unless the move is valid, so a failed move never loses it.
            if (!CanPlace(instanceId, x, y, footprintWidth, footprintHeight)) return false;

            if (_placements.ContainsKey(instanceId)) Remove(instanceId);

            for (int dx = 0; dx < footprintWidth; dx++)
                for (int dy = 0; dy < footprintHeight; dy++)
                    _cells[x + dx, y + dy] = instanceId;

            _placements[instanceId] = new Placement
            {
                X = x, Y = y, Width = footprintWidth, Height = footprintHeight, Rotated = rotated,
            };
            return true;
        }

        public bool Remove(string instanceId)
        {
            if (!_placements.TryGetValue(instanceId, out Placement p)) return false;

            for (int dx = 0; dx < p.Width; dx++)
                for (int dy = 0; dy < p.Height; dy++)
                    _cells[p.X + dx, p.Y + dy] = null;

            _placements.Remove(instanceId);
            return true;
        }

        public bool Contains(string instanceId) => _placements.ContainsKey(instanceId);

        /// <summary>First-fit search for a free spot. Used when an item is picked up or unequipped.</summary>
        public bool TryFindFreeSpot(int footprintWidth, int footprintHeight, out int x, out int y)
        {
            for (int ty = 0; ty < Height; ty++)
            {
                for (int tx = 0; tx < Width; tx++)
                {
                    if (CanPlace("", tx, ty, footprintWidth, footprintHeight))
                    {
                        x = tx;
                        y = ty;
                        return true;
                    }
                }
            }
            x = -1;
            y = -1;
            return false;
        }

        /// <summary>
        /// Finds the free position nearest to a target cell for a footprint. The item's own cells count as free, so a
        /// move can shift a short way over itself. The target is the cell the top-left corner aims at. Searches outward
        /// by distance, so the closest fit always wins. Returns false only when nothing fits anywhere.
        /// </summary>
        public bool TryFindNearestFit(string instanceId, int targetX, int targetY, int footprintWidth, int footprintHeight,
            out int x, out int y)
        {
            // Consider every legal origin and keep the one closest to the target. Grids are small, so the full scan is
            // cheap, and it is simpler to reason about than a ring search.
            int bestDistance = int.MaxValue;
            x = -1;
            y = -1;

            for (int ty = 0; ty <= Height - footprintHeight; ty++)
            {
                for (int tx = 0; tx <= Width - footprintWidth; tx++)
                {
                    if (!CanPlace(instanceId, tx, ty, footprintWidth, footprintHeight)) continue;

                    // Manhattan distance keeps the result stable and easy to predict for players.
                    int distance = System.Math.Abs(tx - targetX) + System.Math.Abs(ty - targetY);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        x = tx;
                        y = ty;
                    }
                }
            }
            return bestDistance != int.MaxValue;
        }

        /// <summary>The item in a cell, or null.</summary>
        public string OwnerAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
            return _cells[x, y];
        }
    }
}
