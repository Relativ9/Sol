using UnityEngine;

namespace Sol
{
    public interface IAnimationEventReceiver
    {
        void OnAnimationEvent(string eventName);
    }
}
