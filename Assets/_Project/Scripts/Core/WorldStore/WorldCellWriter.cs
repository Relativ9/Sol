using System;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Turns world events into cell records. Pickups, drops, moves, and removed piles each write one record to the cell the object
    /// is in, so every object has one owner by position. A taken placed item is recorded as Taken in its home cell. Plain C#: it
    /// takes positions and indices, not GameObjects, so it can be tested without a scene.
    /// </summary>
    public class WorldCellWriter
    {
        private readonly CellStore _store;
        private readonly WorldObjectRegistry _objects;
        private readonly ItemTypeRegistry _itemTypes;

        public WorldCellWriter(CellStore store, WorldObjectRegistry objects, ItemTypeRegistry itemTypes)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _objects = objects ?? throw new ArgumentNullException(nameof(objects));
            _itemTypes = itemTypes ?? throw new ArgumentNullException(nameof(itemTypes));
        }

        /// <summary>
        /// A placed object was picked up. Its Taken record goes to its home cell, which is the cell it was authored in, so the
        /// cell that loads the authored object is the one that removes it. Returns false if the object is not registered.
        /// </summary>
        public bool RecordPlacedTaken(string objectGuid, Vector3 homePosition, uint generation)
        {
            int objectIndex = _objects.IndexOf(objectGuid);
            if (objectIndex <= 0) return false;

            CellKey home = CellKey.FromWorld(homePosition);
            _store.Upsert(home, new CellRecord
            {
                Kind = CellRecordKind.Placed,
                Flags = CellRecordFlags.Taken,
                Index = (uint)objectIndex,
                Generation = generation,
            });
            return true;
        }

        /// <summary>
        /// A placed object is at a new position. Its record goes to the cell it now stands in. Its home cell gets a tombstone, so the
        /// authored copy does not come back on load. A move within the home cell needs no tombstone.
        /// </summary>
        public bool RecordPlacedMoved(string objectGuid, Vector3 homePosition, Vector3 position, Quaternion rotation, uint generation)
        {
            int objectIndex = _objects.IndexOf(objectGuid);
            if (objectIndex <= 0) return false;

            CellKey home = CellKey.FromWorld(homePosition);
            CellKey now = CellKey.FromWorld(position);
            CellPose.Encode(position, rotation, out int x, out int y, out int z, out ushort yaw);

            _store.Upsert(now, new CellRecord
            {
                Kind = CellRecordKind.Placed,
                Flags = CellRecordFlags.Moved,
                Index = (uint)objectIndex,
                Generation = generation,
                X = x, Y = y, Z = z, YawTenths = yaw,
            });

            if (now != home)
            {
                _store.Upsert(home, new CellRecord
                {
                    Kind = CellRecordKind.Tombstone,
                    Index = (uint)objectIndex,
                    Generation = generation,
                    MovedCellX = now.X,
                    MovedCellZ = now.Z,
                });
            }
            return true;
        }

        /// <summary>
        /// A dropped item came to rest at a position. Its record goes to the cell it lies in. If it was recorded earlier in another
        /// cell, that record is removed first, so the pile is owned by exactly one cell.
        /// </summary>
        public bool RecordDropped(Guid guid, string itemId, Vector3 position, Quaternion rotation, uint generation)
        {
            ushort itemIndex = _itemTypes.IndexOf(itemId);
            if (itemIndex == 0) return false;

            _store.RemoveDroppedEverywhere(guid);

            CellPose.Encode(position, rotation, out int x, out int y, out int z, out ushort yaw);
            _store.Upsert(CellKey.FromWorld(position), new CellRecord
            {
                Kind = CellRecordKind.Dropped,
                Flags = CellRecordFlags.None,
                Index = itemIndex,
                Guid = guid,
                Generation = generation,
                X = x, Y = y, Z = z, YawTenths = yaw,
            });
            return true;
        }

        /// <summary>A dropped item was picked up again. Its record is removed from whichever cell holds it.</summary>
        public void RecordDroppedRemoved(Guid guid) => _store.RemoveDroppedEverywhere(guid);
    }
}
