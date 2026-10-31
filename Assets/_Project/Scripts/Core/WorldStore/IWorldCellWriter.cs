using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Writes world events as cell records. Gameplay code depends on this, not on the concrete writer, so the record
    /// rules can change without touching pickup, drop or the motion tracker.
    /// </summary>
    public interface IWorldCellWriter
    {
        /// <summary>A placed object was picked up. Its Taken record goes to its home cell.</summary>
        bool RecordPlacedTaken(string objectGuid, Vector3 homePosition, uint generation);

        /// <summary>A placed object is at a new pose. Its record goes to the cell it stands in, with a tombstone at home if that differs.</summary>
        bool RecordPlacedMoved(string objectGuid, Vector3 homePosition, Vector3 position, Quaternion rotation, uint generation,
            Vector3 linearVelocity = default, Vector3 angularVelocity = default, bool moving = false);

        /// <summary>A dropped pile came to rest. Its record goes to the cell it lies in.</summary>
        bool RecordDropped(System.Guid guid, string itemId, Vector3 position, Quaternion rotation, uint generation);

        /// <summary>A dropped pile was picked up. Its record is removed from whichever cell holds it.</summary>
        void RecordDroppedRemoved(System.Guid guid);
    }
}
