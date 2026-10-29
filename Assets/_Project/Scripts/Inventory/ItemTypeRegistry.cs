using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// One item type: its designer-facing ItemId and the build-time index that cell files store in place of the string.
    /// </summary>
    [System.Serializable]
    public struct ItemTypeEntry
    {
        public string itemId;
        public ushort itemIndex;

        // Set when the item asset is deleted. The index stays reserved, so a cell file that still names it is never misread.
        public bool retired;
    }

    /// <summary>
    /// The build-time table mapping item types to the small indices used in cell files. Indices are issued once and never
    /// reused. The string ItemId stays the designer-facing name and is still what the player file stores.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemTypeRegistry", menuName = "Sol/Items/Item Type Registry")]
    public class ItemTypeRegistry : ScriptableObject
    {
        // The next index to issue. Starts at 1 so 0 can mean "no item" in a record. Only ever increases.
        [SerializeField] private int _nextIndex = 1;

        [SerializeField] private List<ItemTypeEntry> _entries = new List<ItemTypeEntry>();

        private Dictionary<string, ushort> _indexById;
        private Dictionary<ushort, string> _idByIndex;

        public IReadOnlyList<ItemTypeEntry> Entries => _entries;

        /// <summary>The index for an ItemId, or 0 if it is not registered. Index 0 is never issued.</summary>
        public ushort IndexOf(string itemId)
        {
            EnsureLookup();
            return !string.IsNullOrEmpty(itemId) && _indexById.TryGetValue(itemId, out ushort index) ? index : (ushort)0;
        }

        /// <summary>The ItemId for an index, or null if no live item type holds it.</summary>
        public string ItemIdOf(ushort index)
        {
            EnsureLookup();
            return _idByIndex.TryGetValue(index, out string id) ? id : null;
        }

        /// <summary>
        /// Registers an ItemId and returns its index. An ItemId that is already registered keeps its index. A new one takes
        /// the next free index, which is never reused. Fails if the index space (uint16) is exhausted.
        /// </summary>
        public ushort Register(string itemId)
        {
            EnsureLookup();
            if (_indexById.TryGetValue(itemId, out ushort existing)) return existing;

            if (_nextIndex > ushort.MaxValue)
            {
                Debug.LogError($"[{nameof(ItemTypeRegistry)}] Out of item indices. '{itemId}' was not registered.", this);
                return 0;
            }

            ushort index = (ushort)_nextIndex++;
            _entries.Add(new ItemTypeEntry { itemId = itemId, itemIndex = index, retired = false });
            _indexById[itemId] = index;
            _idByIndex[index] = itemId;
            return index;
        }

        /// <summary>
        /// Marks an item type as removed. Its index stays reserved. Returns false if the ItemId is not registered.
        /// </summary>
        public bool Retire(string itemId)
        {
            EnsureLookup();
            if (!_indexById.TryGetValue(itemId, out ushort index)) return false;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].itemId != itemId) continue;
                ItemTypeEntry entry = _entries[i];
                entry.retired = true;
                _entries[i] = entry;
                break;
            }
            _idByIndex.Remove(index);
            return true;
        }

        // Rebuilds the lookup maps from the serialized list. Built lazily, so a read never sees a stale map.
        private void EnsureLookup()
        {
            if (_indexById != null) return;

            _indexById = new Dictionary<string, ushort>();
            _idByIndex = new Dictionary<ushort, string>();
            foreach (ItemTypeEntry entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.itemId) || entry.itemIndex == 0) continue;
                _indexById[entry.itemId] = entry.itemIndex;
                if (!entry.retired) _idByIndex[entry.itemIndex] = entry.itemId;
            }
        }

        // Drops the cached maps after the asset is edited or reloaded, so the next read rebuilds them.
        private void OnValidate()
        {
            _indexById = null;
            _idByIndex = null;
        }
    }
}
