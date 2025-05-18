using UnityEngine;

namespace Sol
{
    public interface IBaseMovement
    {
        bool CanBeActivated();
        void ProcessMovement();
        void OnActivate();
        void OnDeactivate();
    }
}
