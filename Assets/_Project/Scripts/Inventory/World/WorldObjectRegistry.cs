using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// One placed world object: its designer-facing GUID, its build-time index used in cell files, and the item it is.
    /// Filled in by the editor when a placed WorldItem is given its ID. Designers never edit these by hand.
    /// </summary>
    [System.Serializable]
    public struct WorldObjectEntry
    {
        public string guid;
        public int objectIndex;
        public string itemId;

        // Set when the object is deleted. Its index is kept so it is never reissued, and old saves stay unambiguous.
        public bool retired;
    }

    /// <summary>
    /// The build-time table that maps placed objects to the integer indices used in cell files. Indices are issued once
    /// and never reused. Runtime code reads this table to translate a save index back to a GUID, and never changes it.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldObjectRegistry", menuName = "Sol/World/World Object Registry")]
    public class WorldObjectRegistry : ScriptableObject
    {
        // The next index to issue. Only ever increases, so a retired index is never handed out again.
        [SerializeField] private int _nextIndex = 1;

        [SerializeField] private List<WorldObjectEntry> _entries = new List<WorldObjectEntry>();

        private Dictionary<string, int> _indexByGuid;
        private Dictionary<int, string> _guidByIndex;

        public IReadOnlyList<WorldObjectEntry> Entries => _entries;
        public int NextIndex => _nextIndex;

        /// <summary>The index for a GUID, or 0 if the object is not registered. Index 0 is never issued.</summary>
        public int IndexOf(string guid)
        {
            EnsureLookup();
            return !string.IsNullOrEmpty(guid) && _indexByGuid.TryGetValue(guid, out int index) ? index : 0;
        }

        /// <summary>The GUID for an index, or null if no live object holds it.</summary>
        public string GuidOf(int index)
        {
            EnsureLookup();
            return _guidByIndex.TryGetValue(index, out string guid) ? guid : null;
        }
        /// <summary>
        /// Registers a GUID and returns its index. A GUID that is already registered keeps its index. Only the editor
        /// calls this, once per object, at the moment the object gets its ID.
        /// </summary>
        public int Register(string guid, string itemId)
        {
            EnsureLookup();
            if (_indexByGuid.TryGetValue(guid, out int existing))
            {
                // The index never changes. The item id is kept current in case a placed object's item type changed.
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].guid != guid) continue;
                    WorldObjectEntry entry = _entries[i];
                    entry.itemId = itemId;
                    _entries[i] = entry;
                    break;
                }
                return existing;
            }

            int index = _nextIndex++;
            _entries.Add(new WorldObjectEntry { guid = guid, objectIndex = index, itemId = itemId, retired = false });
            _indexByGuid[guid] = index;
            _guidByIndex[index] = guid;
            return index;
        }

        /// <summary>
        /// Marks an object as deleted. Its index stays reserved, so an old save that mentions it is never confused with a
        /// new object. Returns false if the GUID is not registered.
        /// </summary>
        public bool Retire(string guid)
        {
            EnsureLookup();
            if (!_indexByGuid.TryGetValue(guid, out int index)) return false;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].guid != guid) continue;
                WorldObjectEntry entry = _entries[i];
                entry.retired = true;
                _entries[i] = entry;
                break;
            }
            _guidByIndex.Remove(index);
            return true;
        }

        // Rebuilds the lookup maps from the serialized list. Built lazily, so a read never sees a stale map.
        private void EnsureLookup()
        {
            if (_indexByGuid != null) return;

            _indexByGuid = new Dictionary<string, int>();
            _guidByIndex = new Dictionary<int, string>();
            foreach (WorldObjectEntry entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.guid) || entry.objectIndex <= 0) continue;
                _indexByGuid[entry.guid] = entry.objectIndex;
                if (!entry.retired) _guidByIndex[entry.objectIndex] = entry.guid;
            }
        }

        // Drops the cached maps after the asset is edited or reloaded, so the next read rebuilds them.
        private void OnValidate()
        {
            _indexByGuid = null;
            _guidByIndex = null;
        }
    }
}


