using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// Decides which record is live for each placed object when the object's records are in more than one cell. The rule is
    /// from the design, section 6.3: the higher generation wins. A tie is a bug. It is reported, and the copy in the cell that
    /// holds the object's saved position is kept.
    ///
    /// Plain C#. It never touches GameObjects, so the rule can be read and tested on its own. Records are offered one at a
    /// time as cells load, and the winner for each object index is kept.
    /// </summary>
    public class PlacedGenerations
    {
        private readonly Dictionary<uint, Entry> _winners = new();

        private struct Entry
        {
            public CellKey Cell;
            public CellRecord Record;
        }

        /// <summary>
        /// Offers a placed-object record (a moved, taken or tombstone record for one object index) from a cell. Returns true if
        /// this record is now the live one for that object. A losing record must not be applied.
        /// </summary>
        /// <param name="isInItsOwnCell">True when the cell is the one that contains the record's saved position.</param>
        public bool Offer(CellKey cell, CellRecord record, bool isInItsOwnCell, out string tieNote)
        {
            tieNote = null;
            if (!_winners.TryGetValue(record.Index, out Entry current))
            {
                _winners[record.Index] = new Entry { Cell = cell, Record = record };
                return true;
            }

            if (record.Generation > current.Record.Generation)
            {
                _winners[record.Index] = new Entry { Cell = cell, Record = record };
                return true;
            }

            if (record.Generation < current.Record.Generation) return false;

            // Equal generations. The design calls this a bug, so it is reported. The copy in the cell that holds the saved
            // position is kept. A record that is not in its own cell never replaces one that is.
            tieNote = $"Object {record.Index} has two records at generation {record.Generation} (cells {current.Cell} and {cell}).";
            if (isInItsOwnCell && !current.Cell.Equals(cell))
            {
                _winners[record.Index] = new Entry { Cell = cell, Record = record };
                return true;
            }
            return false;
        }

        /// <summary>The cell that holds the live record for an object index, if one has been offered.</summary>
        public bool TryGetWinnerCell(uint index, out CellKey cell)
        {
            if (_winners.TryGetValue(index, out Entry entry))
            {
                cell = entry.Cell;
                return true;
            }
            cell = default;
            return false;
        }

        /// <summary>Forgets every winner. Used when a save slot is loaded, so one slot's records never decide another's.</summary>
        public void Clear() => _winners.Clear();
    }
}
