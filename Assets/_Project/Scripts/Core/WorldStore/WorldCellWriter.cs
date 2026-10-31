using System;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Turns world events into cell records. Pickups, drops, moves, and removed piles each write one record to the cell the object
    /// is in, so every object has one owner by position. A taken placed item is recorded as Taken in its home cell. Plain C#: it
    /// takes positions and indices, not GameObjects, so it can be tested without a scene.
    /// </summary>
    public class WorldCellWriter : IWorldCellWriter
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
            if (objectIndex <= 0)
            {
                // Without an index the pickup cannot be saved, and the item would come back on reload as a duplicate.
                UnityEngine.Debug.LogError($"[{nameof(WorldCellWriter)}] Placed item '{objectGuid}' is not in the WorldObjectRegistry, so its pickup was not saved and it will return on load.");
                return false;
            }

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
        public bool RecordPlacedMoved(string objectGuid, Vector3 homePosition, Vector3 position, Quaternion rotation, uint generation,
            Vector3 linearVelocity = default, Vector3 angularVelocity = default, bool moving = false)
        {
            int objectIndex = _objects.IndexOf(objectGuid);
            if (objectIndex <= 0) return false;

            CellKey home = CellKey.FromWorld(homePosition);
            CellKey now = CellKey.FromWorld(position);
            CellPose.Encode(position, rotation, out int x, out int y, out int z, out ushort yaw);

            // A moving body keeps its velocity, so a load can resume the motion. A resting body stores none.
            var flags = CellRecordFlags.Moved;
            var record = new CellRecord
            {
                Kind = CellRecordKind.Placed,
                Index = (uint)objectIndex,
                Generation = generation,
                X = x, Y = y, Z = z, YawTenths = yaw,
            };
            if (moving)
            {
                flags |= CellRecordFlags.Moving;
                if (linearVelocity.sqrMagnitude > 0f || angularVelocity.sqrMagnitude > 0f)
                {
                    flags |= CellRecordFlags.HasVelocity;
                    CellPose.EncodeVelocity(linearVelocity, angularVelocity, out record.LinearVelocity, out record.AngularVelocity);
                }
            }
            record.Flags = flags;

            _store.Upsert(now, record);

            if (now != home)
            {
                // The tombstone only needs the object's index. Where it went is in the moved record in the destination cell.
                _store.Upsert(home, new CellRecord
                {
                    Kind = CellRecordKind.Tombstone,
                    Index = (uint)objectIndex,
                    Generation = generation,
                });
            }
            else
            {
                // Back in its home cell: the tombstone that marked it as displaced is no longer needed. Only the tombstone is
                // removed, so the live placed record just written to this cell is kept.
                _store.RemoveTombstone(home, (uint)objectIndex);
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
