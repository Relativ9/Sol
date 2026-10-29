using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Writing cells to a slot. Each slot remembers the version it last wrote per cell, so a save writes only what changed since
    /// that slot was last saved. A failed write leaves the slot's record unchanged, so the next save retries it.
    /// </summary>
    public partial class CellStore
    {
        // Per slot: the version each cell had when that slot was last written. A missing entry means never written.
        private readonly Dictionary<string, Dictionary<CellKey, long>> _writtenVersion = new();

        /// <summary>Whether a cell has a change this slot has not yet written.</summary>
        public bool IsDirtyFor(string slot, CellKey key)
        {
            long current = VersionOf(key);
            if (current == 0) return false;
            if (!_writtenVersion.TryGetValue(slot, out var written)) return true;
            return !written.TryGetValue(key, out long w) || w < current;
        }

        /// <summary>The cells this slot has not yet written.</summary>
        public List<CellKey> DirtyFor(string slot)
        {
            var result = new List<CellKey>();
            foreach (CellKey key in _version.Keys)
            {
                if (IsDirtyFor(slot, key)) result.Add(key);
            }
            return result;
        }

        /// <summary>
        /// Writes every cell this slot has not yet written, into the slot's cells folder. Returns the number of cells written. A
        /// cell whose write fails stays dirty for this slot, so the next save retries it.
        /// </summary>
        public int FlushSlot(string slot, string cellsFolder)
        {
            int written = 0;
            foreach (CellKey key in DirtyFor(slot))
            {
                if (WriteCell(slot, cellsFolder, key)) written++;
            }
            return written;
        }

        private bool WriteCell(string slot, string cellsFolder, CellKey key)
        {
            long versionAtWrite = VersionOf(key);
            IReadOnlyList<CellRecord> records = RecordsOf(key);
            string path = Path.Combine(cellsFolder, key.FileName);

            try
            {
                if (records.Count == 0)
                {
                    // A cell with nothing left in it has no file.
                    if (File.Exists(path)) File.Delete(path);
                }
                else if (!AtomicFile.Write(path, CellCodec.Write(key.X, key.Z, records), file => CellCodec.Read(file).Ok))
                {
                    Debug.LogError($"[{nameof(CellStore)}] Cell {key} failed its read-back check and was not written.");
                    return false;
                }

                MarkWritten(slot, key, versionAtWrite);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(CellStore)}] Could not write cell {key}: {e.Message}");
                return false;
            }
        }

        private void MarkWritten(string slot, CellKey key, long version)
        {
            if (!_writtenVersion.TryGetValue(slot, out var written))
            {
                written = new Dictionary<CellKey, long>();
                _writtenVersion[slot] = written;
            }
            written[key] = version;
        }
    }
}
