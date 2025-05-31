using UnityEngine;

namespace Sol
{
    public interface ICameraController
    {
        void Initialize(IPlayerContext context);
        void SetSensitivity(float sensitivity);
        void SetInvertY(bool invertY);
        void LockCursor(bool locked);
        Transform GetCameraTransform();
        Vector3 GetCameraForward();
        Vector3 GetCameraRight();
        float GetCameraYaw();
        
    }
}
