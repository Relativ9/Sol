using System.Collections.Generic;
using Sol;
using UnityEditor;
using UnityEngine;

namespace Sol.Editor
{
    /// <summary>
    /// The one editor entry point to the ItemTypeRegistry asset. Finds it, or creates it on first use, so designers never
    /// create or link it by hand.
    /// </summary>
    public static class ItemTypeRegistryAccess
    {
        // Fixed location, next to the object registry. Its path is part of the project layout, so it does not move.
        public const string AssetPath = "Assets/_Project/Systems/ItemTypeRegistry.asset";

        private static ItemTypeRegistry _cached;

        public static ItemTypeRegistry Get()
        {
            if (_cached != null) return _cached;

            _cached = AssetDatabase.LoadAssetAtPath<ItemTypeRegistry>(AssetPath);
            if (_cached == null)
            {
                _cached = ScriptableObject.CreateInstance<ItemTypeRegistry>();
                AssetDatabase.CreateAsset(_cached, AssetPath);
                AssetDatabase.SaveAssets();
            }
            return _cached;
        }

        /// <summary>Registers an ItemId and returns its index. Marks the registry dirty so it is saved.</summary>
        public static ushort Register(string itemId)
        {
            ItemTypeRegistry registry = Get();
            ushort index = registry.Register(itemId);
            EditorUtility.SetDirty(registry);
            return index;
        }

        /// <summary>
        /// Retires every registered ItemId that no longer has an item asset in the project. Its index stays reserved, so a
        /// cell file that still names it is never misread as a different item.
        /// </summary>
        public static int RetireMissing(IReadOnlyCollection<string> liveItemIds)
        {
            ItemTypeRegistry registry = Get();
            var live = new HashSet<string>(liveItemIds);

            var stale = new List<string>();
            foreach (ItemTypeEntry entry in registry.Entries)
            {
                if (entry.retired) continue;
                if (!live.Contains(entry.itemId)) stale.Add(entry.itemId);
            }

            foreach (string id in stale) registry.Retire(id);
            if (stale.Count > 0) EditorUtility.SetDirty(registry);
            return stale.Count;
        }
    }
}
