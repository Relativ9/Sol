using UnityEngine;

namespace Sol
{
    public interface IUIStateService
    { 
        void SetTalentWheelActive(bool value);
        void SetTalentWheelReady(bool value);
        bool IsTalentWheelActive();
        bool IsTalentWheelReady();
    }
}
