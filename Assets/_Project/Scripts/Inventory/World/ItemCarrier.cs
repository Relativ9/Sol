using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Carries a held item toward a point in front of the view, by applying a force each physics step. The item stays a
    /// physics body: it collides with walls, terrain and other objects, and cannot pass through them. Carrying is only a
    /// steering input, so physics stays authoritative.
    ///
    /// The force is mass-aware. It is the force that would give the item the desired acceleration, so every item reaches its
    /// target at the same rate for the same strength, and a heavier item needs a proportionally larger force. The carrier's
    /// strength caps the force, so a weak carrier cannot lift a heavy item. Gravity is not cancelled here: the carrier must
    /// push against it, so a weak carrier cannot hold a heavy item up.
    ///
    /// Plain C#. It holds no GameObjects of its own: the host passes the body in, and calls Steer from FixedUpdate.
    /// </summary>
    public class ItemCarrier
    {
        private readonly float _maxSpeed;
        private readonly float _gain;
        private readonly float _maxAcceleration;

        /// <param name="maxSpeed">The fastest a carried item can be moved, in metres per second.</param>
        /// <param name="gain">How strongly the item is pulled toward the target. Higher is snappier, and less stable.</param>
        /// <param name="maxAcceleration">
        /// The most acceleration the carrier can give at base strength, in metres per second squared. This is the force limit
        /// per kilogram. The item's mass decides how much force that is.
        /// </param>
        public ItemCarrier(float maxSpeed, float gain, float maxAcceleration)
        {
            _maxSpeed = maxSpeed > 0f ? maxSpeed : 6f;
            _gain = gain > 0f ? gain : 8f;
            _maxAcceleration = maxAcceleration > 0f ? maxAcceleration : 30f;
        }

        /// <summary>
        /// Applies the force that moves the body toward the target. The desired velocity is proportional to the distance and
        /// capped by the carrier's speed. The force that reaches it is mass times the change in velocity, and it is capped at
        /// the carrier's strength times the body's mass, so the cap also scales with mass.
        /// </summary>
        /// <param name="strength">
        /// How strong the carrier is, as a multiplier. 1 is the base strength. It scales the speed, the pull and the force limit.
        /// It is clamped so the carry stays controllable.
        /// </param>
        public void Steer(Rigidbody body, Vector3 target, float strength)
        {
            if (body == null) return;

            // Only a floor. The carrier's strength is already shaped by the stat curve in PlayerPickup, so there is no ceiling here
            // that would clip a strong character back to the base.
            float s = Mathf.Max(strength, 0.25f);
            float maxSpeed = _maxSpeed * s;
            float gain = _gain * s;

            // The velocity the item should have now, in full 3D so the vertical follows the view as well.
            Vector3 offset = target - body.position;
            Vector3 desiredVelocity = offset * gain;
            if (desiredVelocity.magnitude > maxSpeed) desiredVelocity = desiredVelocity.normalized * maxSpeed;

            // The change in velocity needed this step, and the force for it. Mass is what turns acceleration into force.
            Vector3 velocityError = desiredVelocity - body.linearVelocity;
            Vector3 force = velocityError * body.mass * gain;

            // The carrier's force limit is its strength times the body's mass, so heavier items need more strength to move freely.
            float forceLimit = _maxAcceleration * s * body.mass;
            if (force.magnitude > forceLimit) force = force.normalized * forceLimit;

            body.AddForce(force, ForceMode.Force);
            body.WakeUp();
        }
    }
}
