using System;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// The aiming ray. Plain C#, so it has no lifecycle of its own. It is created by the host that owns the camera, and
    /// given the camera through a delegate, so the camera can be swapped or mocked without changing aiming.
    /// The layer mask decides what aiming can hit. The Environment layer is left out of it, so walls and terrain do not
    /// stop a pickup or an arrow that is aimed at an item behind them. Ground checks use their own casts, not this.
    /// </summary>
    public class AimService : IAimService
    {
        private readonly Func<Transform> _viewSource;
        private readonly LayerMask _aimMask;

        /// <param name="viewSource">Returns the transform the aim ray starts from and points along. Usually the camera.</param>
        /// <param name="aimMask">The layers aiming can hit. Anything outside it is passed through.</param>
        public AimService(Func<Transform> viewSource, LayerMask aimMask)
        {
            _viewSource = viewSource ?? throw new ArgumentNullException(nameof(viewSource));
            _aimMask = aimMask;
        }

        public bool TryAimAtCentre(float maxDistance, out AimHit hit)
        {
            hit = default;
            Transform view = _viewSource();
            if (view == null) return false;
            return TryCast(view.position, view.forward, maxDistance, out hit);
        }

        public bool TryCast(Vector3 origin, Vector3 direction, float maxDistance, out AimHit hit)
        {
            hit = default;
            if (maxDistance <= 0f || direction.sqrMagnitude < 0.0001f) return false;

            // Triggers are ignored, so a trigger zone cannot take the target from the object behind it.
            bool didHit = Physics.Raycast(origin, direction.normalized, out RaycastHit raw, maxDistance, _aimMask,
                QueryTriggerInteraction.Ignore);
            if (!didHit) return false;

            hit = new AimHit
            {
                Collider = raw.collider,
                Point = raw.point,
                Normal = raw.normal,
                Distance = raw.distance,
            };
            return true;
        }
    }
}
