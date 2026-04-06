using UnityEngine;
using UnityEngine.InputSystem;

namespace Sol
{
    public class UIInputController : MonoBehaviour
    {
        private IUIManager _uiManager;
        
        private void Start()
        {
            _uiManager = ServiceLocator.Get<IUIManager>();
        }
        
        public void OnOpenTalentWheel(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _uiManager.TogglePanel("talent-wheel-root");
                if(_uiManager == null) Debug.LogError("UI Manager not found!");
            }
        }
    }
}
