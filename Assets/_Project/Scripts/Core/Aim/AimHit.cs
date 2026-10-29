using UnityEngine;

namespace Sol
{
    /// <summary>What an aim ray hit. Plain data, so any system can read it without knowing about the physics query.</summary>
    public struct AimHit
    {
        public Collider Collider;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;

        /// <summary>
        /// The aim target on the hit object, or null if the object is not one. Found from the collider's root or parent,
        /// so a child collider (a hitbox or a model part) still resolves to the object that owns it.
        /// </summary>
        public IAimTarget Target => Collider != null ? Collider.GetComponentInParent<IAimTarget>() : null;
    }
}
