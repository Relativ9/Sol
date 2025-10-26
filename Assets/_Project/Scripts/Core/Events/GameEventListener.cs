using UnityEngine;
using UnityEngine.Events;

namespace Sol
{
    public class GameEventListener : MonoBehaviour
    {
        [Header("Event Configuration")]
        [SerializeField] private GameEvent _gameEvent;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_gameEvent != null)
                _gameEvent.RegisterListener(this);
        }

        private void OnDisable()
        {
            if (_gameEvent != null)
                _gameEvent.UnregisterListener(this);
        }

        public void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}