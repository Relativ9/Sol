using UnityEngine;
using System.Collections.Generic;

namespace Sol
{
    public class AnimationEventDispatcher : MonoBehaviour
    {
        private IPlayerContext _context;
        private Dictionary<string, List<IAnimationEventReceiver>> _eventReceivers = 
            new Dictionary<string, List<IAnimationEventReceiver>>();
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            Debug.Log($"Animation Event Dispatcher initialized on {gameObject.name}");
        }
        
        // Called by Animation Events
        public void DispatchAnimationEvent(string eventName)
        {
            Debug.Log($"Animation event received: {eventName} on {gameObject.name}");
            
            // Notify specific receivers for this event
            if (_eventReceivers.TryGetValue(eventName, out var receivers))
            {
                foreach (var receiver in receivers)
                {
                    receiver.OnAnimationEvent(eventName);
                }
            }
            
            // Also notify any receivers that registered for all events
            if (_eventReceivers.TryGetValue("*", out var globalReceivers))
            {
                foreach (var receiver in globalReceivers)
                {
                    receiver.OnAnimationEvent(eventName);
                }
            }
        }
        
        public void RegisterReceiver(IAnimationEventReceiver receiver, string eventName)
        {
            if (!_eventReceivers.ContainsKey(eventName))
            {
                _eventReceivers[eventName] = new List<IAnimationEventReceiver>();
            }
            
            if (!_eventReceivers[eventName].Contains(receiver))
            {
                _eventReceivers[eventName].Add(receiver);
                Debug.Log($"Registered receiver for event: {eventName}");
            }
        }
        
        public void UnregisterReceiver(IAnimationEventReceiver receiver, string eventName)
        {
            if (_eventReceivers.ContainsKey(eventName))
            {
                _eventReceivers[eventName].Remove(receiver);
                Debug.Log($"Unregistered receiver for event: {eventName}");
            }
        }
        
        public void UnregisterAllEvents(IAnimationEventReceiver receiver)
        {
            foreach (var eventList in _eventReceivers.Values)
            {
                eventList.Remove(receiver);
            }
            
            Debug.Log("Unregistered receiver from all events");
        }
    }
}
