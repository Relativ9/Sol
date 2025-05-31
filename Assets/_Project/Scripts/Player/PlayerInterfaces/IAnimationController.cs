using UnityEngine;

namespace Sol
{
    public interface IAnimationController : IPlayerComponent
    {
        void UpdateAnimationParameters(Vector2 movementInput, Vector3 velocity);
        void UpdateAnimationParameters(Vector2 movementInput, Vector3 velocity, bool isRunning);
        void SetTrigger(string triggerName);
        void SetBool(string paramName, bool value);
        void SetFloat(string paramName, float value);
    }
}
