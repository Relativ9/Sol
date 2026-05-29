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

        bool IsGroundedStrict { get; }     // Grounded with no coyote time, used for stickiness logic
        Vector3 GetSmoothedGroundNormal(); // Blended current + look-ahead normal, used for slope projection
        void SetLookAheadDirection(Vector3 worldDirection); // Called by Movement to aim the look-ahead probe
        
        event Action<bool> OnGroundedStateChanged;
        
        bool HasLookAheadHit { get; }
        RaycastHit LookAheadHit { get; }

        
    }
}
