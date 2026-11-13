using UnityEngine;

namespace Sol
{
    public interface IAnimationController : IPlayerComponent
    {
        void UpdateAnimationParameters(Vector2 movementInput, Vector3 velocity);
        void UpdateAnimationParameters(Vector2 movementInput, Vector3 velocity, bool isRunning);
        void SetTrigger(string triggerName);
        /// <summary>Clears a trigger that was set but never consumed, so it cannot fire later out of sequence.</summary>
        void ResetTrigger(string triggerName);
        void SetBool(string paramName, bool value);
        void SetFloat(string paramName, float value);
        void SetInteger(string paramName, int value);
        /// <summary>Sets the weight of an animator layer (0 = off, 1 = full strength).</summary>
        void SetLayerWeight(int layerIndex, float weight);
        /// <summary>Gets the current weight of an animator layer.</summary>
        float GetLayerWeight(int layerIndex);
    }
}
