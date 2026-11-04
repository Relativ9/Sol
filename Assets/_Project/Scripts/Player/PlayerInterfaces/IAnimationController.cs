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
        /// <summary>Sets the animator's playback speed. 1 is normal, lower is slower. Used for the combo hold.</summary>
        void SetAnimatorSpeed(float speed);
        /// <summary>Crossfades a layer straight into a named state. Used to chain combo swings without an Animator transition.</summary>
        void PlayState(string stateName, int layerIndex, float crossFadeSeconds);
    }
}
