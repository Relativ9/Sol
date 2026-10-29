using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Creates dropped piles in the scene from cell records. It maps the record's item-type index back to an item asset, then
    /// spawns that item's world prefab at the saved pose with the record's own GUID. It implements IDroppedPileSpawner so the
    /// streamer never needs to know how prefabs are found.
    /// </summary>
    public class DroppedPileSpawner : IDroppedPileSpawner
    {
        private readonly ItemTypeRegistry _itemTypes;
        private readonly Dictionary<ushort, ItemDataSO> _itemByIndex = new();
        private readonly Func<string, ItemDataSO> _itemById;

        /// <param name="itemTypes">Maps an item index in a record to its ItemId.</param>
        /// <param name="itemById">Resolves an ItemId to its asset. Usually ItemRegistry.Resolve.</param>
        public DroppedPileSpawner(ItemTypeRegistry itemTypes, Func<string, ItemDataSO> itemById)
        {
            _itemTypes = itemTypes ?? throw new ArgumentNullException(nameof(itemTypes));
            _itemById = itemById ?? throw new ArgumentNullException(nameof(itemById));
        }

        /// <summary>Spawns the pile a dropped record describes. Skipped with a warning if its item or its world model is missing.</summary>
        public void SpawnDropped(CellRecord record)
        {
            ItemDataSO item = ItemFor((ushort)record.Index);
            if (item == null)
            {
                Debug.LogWarning($"[{nameof(DroppedPileSpawner)}] Dropped record {record.Guid:N} names an unknown item index {record.Index}. It was not spawned.");
                return;
            }

            GameObject prefab = item.WorldPrefab;
            if (prefab == null || prefab.GetComponent<WorldItem>() == null)
            {
                Debug.LogWarning($"[{nameof(DroppedPileSpawner)}] '{item.DisplayName}' has no world model. Dropped record {record.Guid:N} was not spawned.");
                return;
            }

            GameObject spawned = UnityEngine.Object.Instantiate(
                prefab, CellPose.PositionOf(record), CellPose.RotationOf(record));
            spawned.GetComponent<WorldItem>().Assign(item, record.Guid.ToString("N"), true);
        }

        // Caches item lookups, since a cell can hold many piles of the same item.
        private ItemDataSO ItemFor(ushort index)
        {
            if (_itemByIndex.TryGetValue(index, out ItemDataSO cached)) return cached;

            string itemId = _itemTypes.ItemIdOf(index);
            ItemDataSO item = itemId != null ? _itemById(itemId) : null;
            if (item != null) _itemByIndex[index] = item;
            return item;
        }
    }
}
