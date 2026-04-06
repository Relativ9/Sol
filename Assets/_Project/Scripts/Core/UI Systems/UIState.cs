using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "UIState", menuName = "Sol/UI/UI State")]
    public class UIState : ScriptableObject
    {
        public bool IsTalentWheelActive;
        public bool IsTalentWheelReady;
    }
}
