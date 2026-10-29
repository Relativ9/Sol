using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Tracks items that are lying in the world. Placed items are present unless picked up. Dropped items are
    /// recorded with their position. Saved, so pickups stay picked up and drops reappear after a reload.
    /// </summary>
    public interface IWorldItemService
    {
        /// <summary>Records that a placed world item was picked up. It will not be spawned on load.</summary>
        void MarkPickedUp(string itemInstanceId);

        /// <summary>Records a dropped item at a position and rotation, so it is restored on load.</summary>
        void AddDropped(string itemInstanceId, string itemId, Vector3 position, Quaternion rotation);

        /// <summary>Updates a dropped item's recorded pose to where it has come to rest.</summary>
        void UpdateDropped(string itemInstanceId, Vector3 position, Quaternion rotation);

        /// <summary>Removes a dropped item once it has been picked up again.</summary>
        void RemoveDropped(string itemInstanceId);

        bool IsPickedUp(string itemInstanceId);

        IReadOnlyList<WorldDropEntry> GetDropped();
        IReadOnlyCollection<string> GetPickedUpIds();

        /// <summary>Raised when a pickup or drop changes the world record, so a save can be queued.</summary>
        event System.Action Changed;

        /// <summary>Restores state from a save. Called once during startup.</summary>
        void Restore(IEnumerable<string> pickedUpIds, IEnumerable<WorldDropEntry> dropped);
    }
}
