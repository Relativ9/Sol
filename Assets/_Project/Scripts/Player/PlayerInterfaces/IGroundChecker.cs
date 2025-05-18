using System;
using UnityEngine;

namespace Sol
{
    public interface IGroundChecker
    {
        bool IsGrounded { get; }
        float DistanceToGround { get; }
        float GroundNormalSlope { get; }
        RaycastHit GroundHit { get; }
        event Action<bool> OnGroundedStateChanged;
    }
}
