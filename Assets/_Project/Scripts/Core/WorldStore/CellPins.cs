using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// Reference-counted pins on loaded cells. A cell with any pin is never released, however far the player moves away.
    /// Pins are held by things that must stay loaded: a moving body in the cell, a displaced placed item, or a dropped item
    /// being carried. A cell is released only when its last pin is removed. Plain C#.
    /// </summary>
    public class CellPins
    {
        private readonly Dictionary<CellKey, int> _pins = new();

        public void Add(CellKey key)
        {
            _pins.TryGetValue(key, out int n);
            _pins[key] = n + 1;
        }

        public void Remove(CellKey key)
        {
            if (!_pins.TryGetValue(key, out int n)) return;
            if (n <= 1) _pins.Remove(key);
            else _pins[key] = n - 1;
        }

        public bool IsPinned(CellKey key) => _pins.ContainsKey(key);

        public int CountOf(CellKey key) => _pins.TryGetValue(key, out int n) ? n : 0;
    }
}
