using System;
using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// The records of every cell this session has touched, and a version for each cell. A cell's records stay in memory for the
    /// whole session, so releasing a cell from the scene never loses a change. Each change bumps the cell's version. Writing to
    /// disk, per slot, is in CellStore.Flush.cs.
    /// </summary>
    public partial class CellStore
    {
        private readonly Dictionary<CellKey, List<CellRecord>> _cells = new();
        private readonly Dictionary<CellKey, long> _version = new();
        private long _nextVersion = 1;

        /// <summary>The records of a cell, or an empty list if it has none.</summary>
        public IReadOnlyList<CellRecord> RecordsOf(CellKey key)
        {
            return _cells.TryGetValue(key, out List<CellRecord> list) ? list : Array.Empty<CellRecord>();
        }

        /// <summary>
        /// Puts one record into a cell, replacing the record for the same object, and bumps the cell's version. An object is
        /// identified by its placed index, or by its GUID when it is dropped.
        /// </summary>
        public void Upsert(CellKey key, CellRecord record)
        {
            if (!_cells.TryGetValue(key, out List<CellRecord> list))
            {
                list = new List<CellRecord>();
                _cells[key] = list;
            }

            int existing = list.FindIndex(r => SameObject(r, record));
            if (existing >= 0) list[existing] = record;
            else list.Add(record);
            Bump(key);
        }

        /// <summary>
        /// Removes only the tombstone for a placed object from a cell, and leaves any placed record in that cell alone.
        /// Used when an object returns to its home cell, where its live record is also stored and must survive.
        /// </summary>
        public bool RemoveTombstone(CellKey key, uint objectIndex)
        {
            if (!_cells.TryGetValue(key, out List<CellRecord> list)) return false;
            int existing = list.FindIndex(r => r.Kind == CellRecordKind.Tombstone && r.Index == objectIndex);
            if (existing < 0) return false;
            list.RemoveAt(existing);
            Bump(key);
            return true;
        }

        /// <summary>Removes the record for an object from a cell, if present, and bumps the cell's version.</summary>
        public bool Remove(CellKey key, CellRecord record)
        {
            if (!_cells.TryGetValue(key, out List<CellRecord> list)) return false;
            int existing = list.FindIndex(r => SameObject(r, record));
            if (existing < 0) return false;
            list.RemoveAt(existing);
            Bump(key);
            return true;
        }

        /// <summary>
        /// Seeds a cell from the saved records. Only applies to a cell this session has not changed, so a change made before the
        /// cell was streamed back in is never overwritten by the saved copy. Loading is not a change, so no slot is made dirty.
        /// </summary>
        public void SetBaseline(CellKey key, IEnumerable<CellRecord> records)
        {
            if (VersionOf(key) != 0) return;
            _cells[key] = new List<CellRecord>(records);
        }

        // Two records describe the same object when they share identity. Dropped items are identified by GUID, placed
        // objects by their build index. A dropped record never matches a placed one.
        private static bool SameObject(CellRecord a, CellRecord b)
        {
            bool aDropped = a.Kind == CellRecordKind.Dropped;
            bool bDropped = b.Kind == CellRecordKind.Dropped;
            if (aDropped || bDropped) return aDropped && bDropped && a.Guid == b.Guid;
            return a.Index == b.Index;
        }

        /// <summary>
        /// Removes a dropped record for this GUID from whichever cell holds it. A pile can have moved across a cell border since it
        /// was recorded, so every cell is checked. Returns true if a record was removed.
        /// </summary>
        public bool RemoveDroppedEverywhere(Guid guid)
        {
            bool removed = false;
            foreach (var pair in _cells)
            {
                int index = pair.Value.FindIndex(r => r.Kind == CellRecordKind.Dropped && r.Guid == guid);
                if (index < 0) continue;
                pair.Value.RemoveAt(index);
                Bump(pair.Key);
                removed = true;
            }
            return removed;
        }

        private void Bump(CellKey key)
        {
            _version[key] = _nextVersion++;
        }

        // The current version of a cell, or 0 if it has never changed this session.
        private long VersionOf(CellKey key) => _version.TryGetValue(key, out long v) ? v : 0;
    }
}
