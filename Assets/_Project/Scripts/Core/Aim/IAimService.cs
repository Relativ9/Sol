using UnityEngine;

namespace Sol
{
    /// <summary>
    /// The one place the game casts a ray to find what something is aimed at. Pickup, archery and spells all use this,
    /// so the aiming rules (reach, layers to ignore, which hits count) are defined once.
    /// Plain C#. Owns no game logic: it reports what a ray hit, and the caller decides what that means.
    /// </summary>
    public interface IAimService
    {
        /// <summary>
        /// Casts from the centre of the view along the camera's forward. The first hit within reach, on a layer that
        /// aiming uses, is returned. Triggers are ignored.
        /// </summary>
        bool TryAimAtCentre(float maxDistance, out AimHit hit);

        /// <summary>
        /// Casts an arbitrary ray with the aiming layer rules applied. For archery and spells that aim from somewhere
        /// other than the view centre.
        /// </summary>
        bool TryCast(Vector3 origin, Vector3 direction, float maxDistance, out AimHit hit);
    }
}
