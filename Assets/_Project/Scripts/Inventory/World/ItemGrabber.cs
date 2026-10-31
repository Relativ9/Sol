using UnityEngine;

namespace Sol
{
    /// <summary>
    /// The carry state for the player's grab. The Input System decides when a grab starts (a Hold interaction on the Grab
    /// action), so this class does not time the press. It holds which item is carried, and ends the carry when released.
    ///
    /// Plain C#. It never moves a GameObject: it reports a target point, and PlayerPickup drives the body toward it. So the
    /// rules can be read and tested without a scene.
    /// </summary>
    public class ItemGrabber
    {
        private readonly float _carryDistance;

        /// <summary>The item being carried, or null when nothing is held.</summary>
        public WorldItem Carried { get; private set; }

        /// <param name="carryDistance">Metres in front of the view that the carried item is held at.</param>
        public ItemGrabber(float carryDistance)
        {
            _carryDistance = carryDistance > 0f ? carryDistance : 1f;
        }

        /// <summary>True while an item is carried.</summary>
        public bool IsGrabbing => Carried != null;

        /// <summary>
        /// Starts carrying an item. Returns false if an item is already carried, or if there is no item to carry, so a second
        /// grab can never replace the first one.
        /// </summary>
        public bool Begin(WorldItem target)
        {
            if (IsGrabbing || target == null) return false;
            Carried = target;
            return true;
        }

        /// <summary>Ends the carry. Returns the item that was carried, or null if nothing was.</summary>
        public WorldItem End()
        {
            WorldItem released = Carried;
            Carried = null;
            return released;
        }

        /// <summary>Ends the carry without returning the item. Used when the item is destroyed or the game is paused.</summary>
        public void Cancel() => Carried = null;

    }
}
