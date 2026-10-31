using UnityEngine;

namespace Sol
{
    /// <summary>
    /// An item lying in the world. Placed on an item prefab. The item's ItemInstanceId is the identity it keeps all the
    /// way into the inventory: picking it up stores the item under this same ID, so the world record and the inventory
    /// always refer to the same thing.
    /// </summary>
    // BoxCollider matches every item prefab. RequireComponent cannot take an abstract type like Collider, so it is named here.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class WorldItem : MonoBehaviour, IAimTarget
    {
        // Physics layer of the player's body colliders. World items ignore these so they never push or catch on the player.
        private const string PlayerMeshLayerName = "PlayerMesh";
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
            ConfigurePhysics();
            ApplyWeight();

            if (!_isDropped && string.IsNullOrEmpty(_itemInstanceId))
            {
                _itemInstanceId = BuildFallbackId();
                Debug.LogWarning($"[{nameof(WorldItem)}] '{name}' has no ItemInstanceId. A position-based one was generated. " +
                                 "It breaks if the item is moved. Set a fixed ID to keep it stable.", this);
            }
        }

        // Where a placed item was authored. Set once, on its first Awake, so a moved item still knows the cell that holds its
        // authored copy. Its tombstone and its Taken record go to that cell.
        public Vector3 HomePosition { get; private set; }
        private bool _homeRecorded;

        // Increases on every change to this object's record. Starts at 0 for an authored item. The cell files use it to
        // decide which copy of an object is live when two cells hold one.
        public uint Generation { get; set; }

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
            ServiceLocator.TryGet<IWorldMotionTracker>()?.Track(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.TryGet<IWorldStreamer>()?.Unregister(this);
        }

        // A resting body is untracked once it settles, so a later knock would never be saved. A collision is what wakes it,
        // so it registers again here. Track ignores an item that is already tracked, so this is cheap on repeated hits.
        private void OnCollisionEnter(Collision collision)
        {
            if (_taken) return;
            ServiceLocator.TryGet<IWorldMotionTracker>()?.Track(this);
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
            ApplyWeight();
        }

        // The rigidbody's mass is the item's weight, so a heavier item is harder to push and carry. A weight of zero leaves the
        // prefab's own mass in place, so a missing weight can never make the body weightless.
        private void ApplyWeight()
        {
            if (_item == null || _item.Weight <= 0f) return;
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null) body.mass = _item.Weight;
        }

        // Designers do not set physics by hand. Interpolation smooths motion between physics steps. Continuous Dynamic
        // stops fast-moving items tunnelling through thin colliders. Both are set here, so every item is configured the same.
        private void ConfigurePhysics()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        /// <summary>
        /// Stops or restores collisions between this item and the player's body. PlayerPickup turns it on for the length of a
        /// grab, so a carried item passes through the player's hands, and turns it off on release. While off, the player
        /// collides normally, so knocking into a resting item still moves it.
        /// </summary>
        public void SetPlayerCollisionIgnored(bool ignored)
        {
            int playerLayer = LayerMask.NameToLayer(PlayerMeshLayerName);
            if (playerLayer < 0)
            {
                Debug.LogWarning($"[{nameof(WorldItem)}] '{name}': layer '{PlayerMeshLayerName}' does not exist. The player is not excluded.", this);
                return;
            }

            Collider[] own = GetComponents<Collider>();
            if (own.Length == 0) return;

            // Shared by every item. Rebuilt when empty or when any cached collider has been destroyed, so a grab after a scene
            // reload finds the new player's colliders rather than the old ones.
            if (NeedsPlayerColliderRefresh()) _playerColliders = FindPlayerColliders(playerLayer);
            foreach (Collider player in _playerColliders)
            {
                if (player == null) continue;
                foreach (Collider mine in own) Physics.IgnoreCollision(mine, player, ignored);
            }
        }

        private static Collider[] _playerColliders;

        private static bool NeedsPlayerColliderRefresh()
        {
            if (_playerColliders == null || _playerColliders.Length == 0) return true;
            foreach (Collider c in _playerColliders)
            {
                if (c == null) return true;
            }
            return false;
        }

        // The cache belongs to one scene. It is cleared on load so no collider from a previous scene is ever used.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubscribeClearPlayerColliderCache()
        {
            _playerColliders = null;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= ClearPlayerColliderCacheOnUnload;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += ClearPlayerColliderCacheOnUnload;
        }

        private static void ClearPlayerColliderCacheOnUnload(UnityEngine.SceneManagement.Scene scene) => _playerColliders = null;

        private static Collider[] FindPlayerColliders(int playerLayer)
        {
            var found = new System.Collections.Generic.List<Collider>();
            foreach (Collider c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c.gameObject.layer == playerLayer) found.Add(c);
            }
            return found.ToArray();
        }
    }
}
