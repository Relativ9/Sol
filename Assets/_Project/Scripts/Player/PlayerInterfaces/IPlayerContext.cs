using UnityEngine;

namespace Sol
{
    public interface IPlayerContext
    {
        T GetService<T>() where T : class;
        Vector2 GetMovementInput();
        Vector3 GetCurrentVeolocity();
        void ApplyMovement(Vector3 velocity);
        void SetStateValue<T>(string key, T value);
        T GetStateValue<T>(string key, T defaultValue);
        
        // void Initialize(IPlayerContext context);
        // void OnEnable();
        // void OnDisable();
    }
    
}
