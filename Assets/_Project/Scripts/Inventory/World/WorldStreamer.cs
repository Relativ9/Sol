using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Keeps the cells around the player loaded. Each frame it works out the 3×3 ring of cells around the player, starts background
    /// reads for cells entering the ring, applies finished reads on the main thread, and releases cells that leave the ring once
    /// they are written. Placed items register with the streamer by cell, so nothing scans the scene.
    /// </summary>
    public class WorldStreamer : MonoBehaviour, IWorldStreamer
    {
        [Tooltip("Cells loaded in each direction from the player's cell. 1 gives a 3x3 ring, about 300 m across.")]
        [SerializeField, Min(0)] private int _ringRadius = 1;

        [Tooltip("Folder for the current slot's cell files. Set by the save system when a slot is loaded.")]
        [SerializeField] private string _cellsFolder;

        private readonly Dictionary<CellKey, Task<CellLoader.LoadResult>> _reading = new();
        private readonly HashSet<CellKey> _loaded = new();
        private readonly Dictionary<CellKey, List<WorldItem>> _itemsByCell = new();

        private Transform _player;
        private CellKey _playerCell;
        private bool _hasPlayerCell;

        /// <summary>The cells currently in memory. Read-only view for other systems.</summary>
        public IReadOnlyCollection<CellKey> Loaded => _loaded;

        // The pins that keep a cell loaded regardless of the ring. Owned here, so residency has one owner.
        private readonly CellPins _pins = new CellPins();

        /// <summary>Keeps a cell loaded while a moving body is in it. Counted, so several bodies can share a cell.</summary>
        public void Pin(CellKey key) => _pins.Add(key);

        /// <summary>Releases one pin on a cell. The cell is kept until its last pin is gone.</summary>
        public void Unpin(CellKey key) => _pins.Remove(key);

        public void SetPlayer(Transform player) => _player = player;

        // A new slot starts with no winners, so records from the previous slot can never decide the new one.
        public void SetCellsFolder(string folder)
        {
            _cellsFolder = folder;
            _placedWinners.Clear();
        }

        // Placed object indices that have been taken or displaced. A scene copy that shows up later, for example because its cell
        // loaded after the record was read, is removed as it registers, so a taken item can never come back as a duplicate.
        private readonly HashSet<uint> _removedPlaced = new();

        /// <summary>Registers a placed item with the cell it stands in, so it can be found without a scene scan.</summary>
        public void Register(WorldItem item)
        {
            // A placed copy whose record says it is gone is removed here, not left in the scene as a duplicate.
            uint index = IndexOfPlaced(item);
            if (index != 0 && _removedPlaced.Contains(index))
            {
                Destroy(item.gameObject);
                return;
            }

            CellKey key = CellKey.FromWorld(item.transform.position);
            if (!_itemsByCell.TryGetValue(key, out List<WorldItem> list))
            {
                list = new List<WorldItem>();
                _itemsByCell[key] = list;
            }
            if (!list.Contains(item)) list.Add(item);
        }

        // The object index of a placed item, or 0 if it is not a placed item or is not registered.
        private uint IndexOfPlaced(WorldItem item)
        {
            if (item.IsDropped || _objectRegistry == null) return 0;
            return (uint)_objectRegistry.IndexOf(item.ItemInstanceId);
        }

        /// <summary>Removes an item from its cell's list, for example when it is taken.</summary>
        public void Unregister(WorldItem item)
        {
            CellKey key = CellKey.FromWorld(item.transform.position);
            if (_itemsByCell.TryGetValue(key, out List<WorldItem> list)) list.Remove(item);
        }

        /// <summary>The placed items registered in a cell. The list is the streamer's own; do not hold it.</summary>
        public IReadOnlyList<WorldItem> ItemsIn(CellKey key)
        {
            return _itemsByCell.TryGetValue(key, out List<WorldItem> list) ? list : System.Array.Empty<WorldItem>();
        }

        void Update()
        {
            if (_player == null || string.IsNullOrEmpty(_cellsFolder)) return;

            CellKey current = CellKey.FromWorld(_player.position);
            if (!_hasPlayerCell || current != _playerCell)
            {
                _playerCell = current;
                _hasPlayerCell = true;
                RequestRing(current);
            }

            ApplyFinishedReads();
            ReleaseOutsideRing();
        }

        // Starts a background read for every cell in the ring that is neither loaded nor already being read.
        private void RequestRing(CellKey center)
        {
            for (int dx = -_ringRadius; dx <= _ringRadius; dx++)
            {
                for (int dz = -_ringRadius; dz <= _ringRadius; dz++)
                {
                    var key = new CellKey(center.X + dx, center.Z + dz);
                    if (_loaded.Contains(key) || _reading.ContainsKey(key)) continue;
                    _reading[key] = CellLoader.LoadAsync(_cellsFolder, key);
                }
            }
        }

        // Applies reads that have finished. Runs on the main thread, so records touch GameObjects only here.
        private void ApplyFinishedReads()
        {
            var done = new List<CellKey>();
            foreach (var pair in _reading)
            {
                if (pair.Value.IsCompleted) done.Add(pair.Key);
            }

            foreach (CellKey key in done)
            {
                CellLoader.LoadResult result = _reading[key].Result;
                _reading.Remove(key);

                // A cell that left the ring while it was being read is not kept, so memory stays bounded to the ring.
                if (!InRing(key)) continue;

                _loaded.Add(key);
                if (result.Found)
                {
                    // The saved records become the cell's baseline in the store, so later changes build on what was saved.
                    Store?.SetBaseline(key, result.Data.Records);
                    ApplyCell(key, result);
                }
            }
        }

        // Releases loaded cells outside the ring. A pinned cell is kept. The cell's records stay in the store for the whole session,
        // so releasing the scene objects never loses a change. Saving is separate, and is done by the save system.
        private void ReleaseOutsideRing()
        {
            var leaving = new List<CellKey>();
            foreach (CellKey key in _loaded)
            {
                if (!InRing(key) && !_pins.IsPinned(key)) leaving.Add(key);
            }

            foreach (CellKey key in leaving)
            {
                _loaded.Remove(key);
                _itemsByCell.Remove(key);
            }
        }

        /// <summary>The cell store the streamer reads and changes. Set by the save system when a slot is loaded.</summary>
        public CellStore Store { get; set; }

        private bool InRing(CellKey key)
        {
            return System.Math.Abs(key.X - _playerCell.X) <= _ringRadius
                && System.Math.Abs(key.Z - _playerCell.Z) <= _ringRadius;
        }

        // Applies a cell's records to the world. A Taken placed record removes its item from the scene. A Dropped record spawns
        // its pile, unless one is already in the scene (for example a pile carried over from before the cell was released).
        // A Tombstone removes the displaced placed item here, since it has left its home cell. Records are copied into the
        // store, so the store and the scene agree once the cell is loaded.
        private void ApplyCell(CellKey key, CellLoader.LoadResult result)
        {
            if (Store == null) return;

            foreach (CellRecord record in result.Data.Records)
            {
                switch (record.Kind)
                {
                    case CellRecordKind.Placed:
                        // Only the live copy of a placed object applies. A stale copy in another cell is ignored (design 6.3).
                        bool live = _placedWinners.Offer(key, record, IsOwnCell(key, record), out string tie);
                        if (live) ApplyPlaced(key, record);
                        if (tie != null) Debug.LogWarning($"[{nameof(WorldStreamer)}] {tie}");
                        break;
                    case CellRecordKind.Dropped:
                        ApplyDropped(key, record);
                        break;
                    case CellRecordKind.Tombstone:
                        if (_placedWinners.Offer(key, record, IsOwnCell(key, record), out string tombTie))
                            RemovePlacedAt(record.Index);
                        if (tombTie != null) Debug.LogWarning($"[{nameof(WorldStreamer)}] {tombTie}");
                        break;
                }
            }
        }

        // The live record for each placed object across every cell. Cleared when a slot loads, so one slot never decides another.
        private readonly PlacedGenerations _placedWinners = new PlacedGenerations();

        // A record is in its own cell when the cell is the one that contains the record's saved position.
        private static bool IsOwnCell(CellKey key, CellRecord record)
        {
            return key.Equals(CellKey.FromWorld(CellPose.PositionOf(record)));
        }

        // A placed record that is taken is removed from the scene. A placed record that is not taken, but has moved, is placed at
        // its saved pose. A resting placed record with no change needs nothing, since the authored object is already there.
        private void ApplyPlaced(CellKey key, CellRecord record)
        {
            WorldItem item = FindPlacedByIndex(record.Index);
            if (item == null) return;

            if (record.IsTaken)
            {
                RemovePlacedAt(record.Index);
                return;
            }

            // A placed object that has moved carries a pose. The Moved flag says so; other flags do not imply a move.
            if ((record.Flags & CellRecordFlags.Moved) != 0)
            {
                // Moved through the Rigidbody, not the transform. A transform move leaves the body's contacts and sleep state
                // from before the move, so it can resume in the air or inside a surface. Its velocity is restored from the record.
                Vector3 position = CellPose.PositionOf(record);
                Quaternion rotation = CellPose.RotationOf(record);
                Rigidbody body = item.GetComponent<Rigidbody>();
                if (body == null)
                {
                    item.transform.SetPositionAndRotation(position, rotation);
                    return;
                }

                body.position = position;
                body.rotation = rotation;
                body.linearVelocity = record.HasVelocity ? CellPose.LinearVelocityOf(record) : Vector3.zero;
                body.angularVelocity = record.HasVelocity ? CellPose.AngularVelocityOf(record) : Vector3.zero;
                body.WakeUp();
            }
        }

        // Recreates a dropped pile at its saved pose, unless it is already in the scene with the same identity.
        private void ApplyDropped(CellKey key, CellRecord record)
        {
            string guid = record.Guid.ToString("N");
            if (_itemsByCell.TryGetValue(key, out List<WorldItem> list))
            {
                foreach (WorldItem existing in list)
                {
                    if (existing != null && existing.ItemInstanceId == guid) return;
                }
            }

            if (_spawner == null) return;
            _spawner.SpawnDropped(record);
        }

        // Finds the placed item registered for an object index. Placed items carry their index through the object registry.
        private WorldItem FindPlacedByIndex(uint index)
        {
            if (_objectRegistry == null) return null;
            string guid = _objectRegistry.GuidOf((int)index);
            if (string.IsNullOrEmpty(guid)) return null;

            foreach (var list in _itemsByCell.Values)
            {
                foreach (WorldItem item in list)
                {
                    if (item != null && item.ItemInstanceId == guid) return item;
                }
            }
            return null;
        }

        // Removes the placed copy now if it is in the scene, and remembers the index so a copy that registers later is removed too.
        private void RemovePlacedAt(uint index)
        {
            _removedPlaced.Add(index);
            WorldItem item = FindPlacedByIndex(index);
            if (item != null) Destroy(item.gameObject);
        }

        /// <summary>The object registry, for translating a placed record's index to its item. Set by the save system.</summary>
        public WorldObjectRegistry ObjectRegistry { get => _objectRegistry; set => _objectRegistry = value; }
        private WorldObjectRegistry _objectRegistry;

        /// <summary>Spawns dropped piles from records. Implemented by the scene's item spawner.</summary>
        public IDroppedPileSpawner Spawner { get => _spawner; set => _spawner = value; }
        private IDroppedPileSpawner _spawner;
    }
}
