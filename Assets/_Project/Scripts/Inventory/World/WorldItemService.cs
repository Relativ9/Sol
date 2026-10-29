using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Plain C# record of world items. Picked-up placed items are kept as a set of IDs, and dropped items as entries
    /// with their positions. Changes raise Changed so a save can be queued.
    /// </summary>
    public class WorldItemService : IWorldItemService
    {
        private readonly HashSet<string> _pickedUp = new();
        private readonly List<WorldDropEntry> _dropped = new();

        public event System.Action Changed;

        public void MarkPickedUp(string itemInstanceId)
        {
            if (string.IsNullOrEmpty(itemInstanceId)) return;
            if (_pickedUp.Add(itemInstanceId)) Changed?.Invoke();
        }

        public void AddDropped(string itemInstanceId, string itemId, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrEmpty(itemInstanceId) || string.IsNullOrEmpty(itemId)) return;

            var entry = new WorldDropEntry { itemInstanceId = itemInstanceId, itemId = itemId };
            entry.Position = position;
            entry.Rotation = rotation;
            _dropped.Add(entry);
            Changed?.Invoke();
        }

        // Updates where a dropped pile lies, so the save records the pile where it came to rest, not where it was spawned.
        public void UpdateDropped(string itemInstanceId, Vector3 position, Quaternion rotation)
        {
            for (int i = 0; i < _dropped.Count; i++)
            {
                if (_dropped[i].itemInstanceId != itemInstanceId) continue;
                WorldDropEntry entry = _dropped[i];
                entry.Position = position;
                entry.Rotation = rotation;
                _dropped[i] = entry;
                Changed?.Invoke();
                return;
            }
        }

        public void RemoveDropped(string itemInstanceId)
        {
            if (_dropped.RemoveAll(e => e.itemInstanceId == itemInstanceId) > 0) Changed?.Invoke();
        }

        public bool IsPickedUp(string itemInstanceId) => _pickedUp.Contains(itemInstanceId);

        public IReadOnlyList<WorldDropEntry> GetDropped() => _dropped;

        public IReadOnlyCollection<string> GetPickedUpIds() => _pickedUp;

        public void Restore(IEnumerable<string> pickedUpIds, IEnumerable<WorldDropEntry> dropped)
        {
            _pickedUp.Clear();
            _dropped.Clear();
            if (pickedUpIds != null)
                foreach (string id in pickedUpIds) _pickedUp.Add(id);
            if (dropped != null)
                _dropped.AddRange(dropped);
        }
    }
}
