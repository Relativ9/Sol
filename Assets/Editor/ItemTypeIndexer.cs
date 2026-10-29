using System.Collections.Generic;
using Sol;
using UnityEditor;

namespace Sol.Editor
{
    /// <summary>
    /// Keeps the ItemTypeRegistry in step with the item assets. Any imported or saved item asset registers its ItemId, and
    /// after an import pass any ItemId whose asset is gone is retired. Designers never touch the registry.
    /// </summary>
    public class ItemTypeIndexer : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool touchedItems = false;

            foreach (string path in imported)
            {
                if (!path.EndsWith(".asset")) continue;
                var item = AssetDatabase.LoadAssetAtPath<ItemDataSO>(path);
                if (item == null) continue;
                touchedItems = true;
                RegisterItem(item, path);
            }

            // A deleted item asset cannot be loaded, so a deletion is detected by a pass over every live item asset.
            if (deleted.Length > 0) touchedItems = true;

            if (touchedItems) RetireDeleted();
        }

        // Registers an item's ItemId. An item with no ItemId is reported and skipped, since an index for it would name nothing.
        private static void RegisterItem(ItemDataSO item, string path)
        {
            if (string.IsNullOrEmpty(item.ItemId))
            {
                UnityEngine.Debug.LogWarning($"[{nameof(ItemTypeIndexer)}] '{path}' has no ItemId, so it has no item index. Set ItemId on the asset.", item);
                return;
            }
            ItemTypeRegistryAccess.Register(item.ItemId);
        }

        // Retires any registered ItemId that no longer has an asset. Every item asset in the project is read to find the
        // live set, so an ItemId is retired only when no asset holds it.
        private static void RetireDeleted()
        {
            var live = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemDataSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemDataSO>(path);
                if (item != null && !string.IsNullOrEmpty(item.ItemId)) live.Add(item.ItemId);
            }
            ItemTypeRegistryAccess.RetireMissing(live);
        }
    }
}
