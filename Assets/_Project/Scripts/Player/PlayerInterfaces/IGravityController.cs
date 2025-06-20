using UnityEngine;

namespace Sol
{
    public interface IGravityController
    {
        void SetGravityScale(float scale);
        void SetCustomGravityDirection(Vector3 gravity);
        void ResetToDefaultGravity();
        float GetCurrentGravityScale();
        Vector3 GetCurrentGravity();
        void EnableGravity(bool enable);
    }
}
