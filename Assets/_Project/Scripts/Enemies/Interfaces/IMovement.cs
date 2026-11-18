using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Read-only movement query interface. Allows perception systems to check player speed/velocity
    /// without depending on PlayerController directly.
    /// </summary>
    public interface IMovement
    {
        /// <summary>Current velocity vector of the target, in m/s.</summary>
        Vector3 Velocity { get; }
    }
}
