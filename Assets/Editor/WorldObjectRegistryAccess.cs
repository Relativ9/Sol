using Sol;
using UnityEditor;
using UnityEngine;

namespace Sol.Editor
{
    /// <summary>
    /// The one editor entry point to the WorldObjectRegistry asset. Finds it, or creates it on first use, so designers never
    /// create or link it by hand. Every placed object's GUID is registered through here when the GUID is assigned.
    /// </summary>
    public static class WorldObjectRegistryAccess
    {
        // Fixed location, next to the other system assets. Its path is part of the project layout, so it does not move.
        public const string AssetPath = "Assets/_Project/Systems/WorldObjectRegistry.asset";

        private static WorldObjectRegistry _cached;

        public static WorldObjectRegistry Get()
        {
            if (_cached != null) return _cached;

            _cached = AssetDatabase.LoadAssetAtPath<WorldObjectRegistry>(AssetPath);
            if (_cached == null)
            {
                _cached = ScriptableObject.CreateInstance<WorldObjectRegistry>();
                AssetDatabase.CreateAsset(_cached, AssetPath);
                AssetDatabase.SaveAssets();
            }
            return _cached;
        }

        /// <summary>
        /// Retires registered GUIDs that no longer exist in any open scene. Run when a scene is saved, so a deleted object's
        /// index is kept reserved and never reissued. Only the editor runs this, and it never touches live play state.
        /// </summary>
        public static int RetireMissing(WorldObjectRegistry registry, System.Collections.Generic.ISet<string> liveGuids)
        {
            var stale = new System.Collections.Generic.List<string>();
            foreach (WorldObjectEntry entry in registry.Entries)
            {
                if (entry.retired) continue;
                if (!liveGuids.Contains(entry.guid)) stale.Add(entry.guid);
            }

            foreach (string guid in stale) registry.Retire(guid);
            if (stale.Count > 0) EditorUtility.SetDirty(registry);
            return stale.Count;
        }

        /// <summary>Registers a placed object's GUID and returns its build index. Marks the registry dirty so it is saved.</summary>
        public static int Register(string guid, string itemId)
        {
            WorldObjectRegistry registry = Get();
            int index = registry.Register(guid, itemId);
            EditorUtility.SetDirty(registry);
            return index;
        }
    }
}
