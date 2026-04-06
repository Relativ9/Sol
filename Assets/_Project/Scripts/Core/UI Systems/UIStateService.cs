using UnityEngine;

namespace Sol
{
    public class UIStateService : MonoBehaviour, IUIStateService
    {
        [SerializeField] private UIState _uiState;
        
        void Awake()
        {
            ServiceLocator.RegisterService<IUIStateService>(this); //Registers itself with the service locator.
        }

        void Start()
        {
            
        }
        
        public void SetTalentWheelActive(bool value)
        {
            _uiState.IsTalentWheelActive = value;
        }

        public void SetTalentWheelReady(bool value)
        {
            _uiState.IsTalentWheelReady = value;
        }

        public bool IsTalentWheelActive()
        {
            return _uiState.IsTalentWheelActive;
        }

        public bool IsTalentWheelReady()
        {
            return _uiState.IsTalentWheelReady;
        }
    }
}
