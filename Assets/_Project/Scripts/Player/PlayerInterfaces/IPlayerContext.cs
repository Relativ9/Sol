using UnityEngine;

namespace Sol
{
    public interface IPlayerContext
    {
        T GetService<T>() where T : class;
        Vector2 GetMovementInput();
        Vector3 GetCurrentVelocity();
        Vector2 GetLookInput();
        void ApplyMovement(Vector3 velocity);
        void SetStateValue<T>(string key, T value);
        T GetStateValue<T>(string key, T defaultValue);
        bool GetRunInput();
        bool GetJumpInput();
    }
    
}
