using UnityEngine;

namespace Sol
{
    public interface IRotationBehavior
    {
        void UpdateRotation(Transform transform, float deltaTime);
        void Initialize(Transform transform);
        bool IsActive { get; set; }
    }
}
