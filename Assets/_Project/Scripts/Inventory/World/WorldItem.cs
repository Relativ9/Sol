using UnityEngine;

namespace Sol
{
    /// <summary>
    /// An item lying in the world. Placed on an item prefab. The item's ItemInstanceId is the identity it keeps all the
    /// way into the inventory: picking it up stores the item under this same ID, so the world record and the inventory
    /// always refer to the same thing.
    /// </summary>
    public class WorldItem : MonoBehaviour, IAimTarget
    {
        [Tooltip("The item this prefab represents. Its ItemId is what the save records.")]
        [SerializeField] private ItemDataSO _item;

        [Tooltip("Unique ID for this item instance. Scene placements must be unique and must never change once saved. " +
                 "Leave empty on a placed item to generate one automatically; drops always get a new ID.")]
        [UnityEngine.Serialization.FormerlySerializedAs("_worldItemId")]
        [SerializeField] private string _itemInstanceId;

        // True for an item spawned by a drop. Its world record must be removed on pickup. Placed items are marked picked up.
        private bool _isDropped;

        // Set the moment the item is taken. Stops a second pickup in the same frame, before Destroy has run.
        private bool _taken;

        public ItemDataSO Item => _item;
        public string ItemInstanceId => _itemInstanceId;
        public bool IsDropped => _isDropped;
        public bool IsTaken => _taken;

        // A world item can be aimed at until it is taken. Taken items are ignored by the aim consumers.
        public bool IsAimable => !_taken;

        /// <summary>Marks the item as taken. Call once it has been stored in the inventory.</summary>
        public void MarkTaken() => _taken = true;

        // Every placed item needs an ID, or a pickup could not be recorded and the item would reappear on reload.
        // A generated ID is only a fallback: a designer should set a fixed one on placed items so it is stable.
        private void Awake()
        {
            if (!_isDropped && string.IsNullOrEmpty(_itemInstanceId))
            {
                _itemInstanceId = BuildFallbackId();
                Debug.LogWarning($"[{nameof(WorldItem)}] '{name}' has no ItemInstanceId. A position-based one was generated. " +
                                 "It breaks if the item is moved. Set a fixed ID to keep it stable.", this);
            }
        }

        /// <summary>
        /// Removes every placed item the save says was picked up. Called once after the save is read, so a picked-up
        /// item does not come back on reload. Dropped items are not restored here.
        /// </summary>
        public static void ApplySavedState(IWorldItemService world, System.Collections.Generic.IEnumerable<string> heldInstanceIds)
        {
            if (world == null) return;

            // The player's own instance IDs (inventory and equipped), so an item already held is not also in the world.
            var held = new System.Collections.Generic.HashSet<string>(heldInstanceIds ?? System.Array.Empty<string>());

            foreach (WorldItem item in FindObjectsByType<WorldItem>(FindObjectsSortMode.None))
            {
                if (item.IsDropped) continue;
                if (world.IsPickedUp(item.ItemInstanceId) || held.Contains(item.ItemInstanceId))
                    Destroy(item.gameObject);
            }
        }

        // Where a placed item was authored. Set once, on its first Awake, so a moved item still knows the cell that holds its
        // authored copy. Its tombstone and its Taken record go to that cell.
        public Vector3 HomePosition { get; private set; }
        private bool _homeRecorded;

        private void RecordHome()
        {
            if (_homeRecorded) return;
            HomePosition = transform.position;
            _homeRecorded = true;
        }

        // Placed items register with the streamer by cell. Start runs after every Awake, so the streamer exists by then. Dropped
        // piles are spawned by the streamer's own apply step and register the same way.
        private void Start()
        {
            if (!_isDropped) RecordHome();
            ServiceLocator.TryGet<IWorldStreamer>()?.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.TryGet<IWorldStreamer>()?.Unregister(this);
        }

        // A stable ID for a placed item that has none set: its scene, hierarchy path and position (in cm).
        private string BuildFallbackId()
        {
            string path = name;
            for (Transform t = transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            Vector3 p = transform.position;
            return $"{gameObject.scene.name}/{path}@{Mathf.RoundToInt(p.x * 100f)},{Mathf.RoundToInt(p.y * 100f)},{Mathf.RoundToInt(p.z * 100f)}";
        }

        /// <summary>Sets the identity for a runtime-spawned item (a drop). Called before the item is used.</summary>
        public void Assign(ItemDataSO item, string itemInstanceId, bool isDropped = true)
        {
            _item = item;
            _itemInstanceId = itemInstanceId;
            _isDropped = isDropped;
        }
    }
}
