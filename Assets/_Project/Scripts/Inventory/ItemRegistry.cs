using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Maps saved item IDs back to item assets. Built once from a list asset. A saved ID that no longer resolves is
    /// logged and skipped, so a renamed or deleted asset cannot break the whole load.
    /// </summary>
    public class ItemRegistry
    {
        private readonly Dictionary<string, ItemDataSO> _byId = new();

        public ItemRegistry(IEnumerable<ItemDataSO> items)
        {
            foreach (ItemDataSO item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;
                if (_byId.ContainsKey(item.ItemId))
                {
                    Debug.LogWarning($"[ItemRegistry] Duplicate ItemId '{item.ItemId}' on '{item.name}'. The first one is kept.");
                    continue;
                }
                _byId[item.ItemId] = item;
            }
        }

        /// <summary>The asset for a saved ID, or null (and a warning) when it no longer resolves.</summary>
        public ItemDataSO Resolve(string itemId)
        {
            if (_byId.TryGetValue(itemId, out ItemDataSO item)) return item;
            Debug.LogWarning($"[ItemRegistry] Saved item '{itemId}' no longer exists in the registry. It was skipped.");
            return null;
        }
    }
}
