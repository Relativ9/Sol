using System;
using System.Collections;
using UnityEngine;

namespace Sol
{
    public class CombatStateController : MonoBehaviour, ICombatController
    {
        [Header("Combat Settings")]
        [SerializeField] private float _unsheatheTime = 0.5f;
        [SerializeField] private float _sheatheTime = 0.5f;
        [SerializeField] private bool _debugCombat = true;
        
        // Dependencies
        private IPlayerContext _context;
        private IAnimationController _animationController;
        private IWeaponService _weaponService;
        
        // State
        private CombatState _currentState = CombatState.Sheathed;
        private Coroutine _stateTransitionCoroutine;
        
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            
            _animationController = context.GetService<IAnimationController>();
            _weaponService = context.GetService<IWeaponService>();
            
            if (_animationController == null)
            {
                Debug.LogError("Animation Controller not found! Combat animations won't work.");
            }
            
            if (_weaponService == null)
            {
                Debug.LogWarning("Weapon Service not found! Using default weapon settings.");
            }
            
            // Initialize in sheathed state
            SetCombatState(CombatState.Sheathed);
            
            Debug.Log("Combat State Controller initialized");
        }
        
        public bool CanBeActivated()
        {
            // Combat controller can always be active
            return true;
        }
        
        public void OnActivate()
        {
            // Nothing special needed when activated
            Debug.Log("Combat controller activated");
        }
        
        public void OnDeactivate()
        {
            // Force sheathe weapon when deactivated
            if (_currentState != CombatState.Sheathed)
            {
                ForceSheatheWeapon();
            }
            
            Debug.Log("Combat controller deactivated");
        }
        
        public void ToggleWeaponState()
        {
            // Only allow toggling if we're in a stable state
            if (_currentState == CombatState.Sheathed || _currentState == CombatState.Unsheathed)
            {
                if (_currentState == CombatState.Sheathed)
                {
                    UnsheatheWeapon();
                }
                else
                {
                    SheatheWeapon();
                }
            }
            else
            {
                Debug.Log($"Cannot toggle weapon during state: {_currentState}");
            }
        }
        
        public bool IsWeaponDrawn()
        {
            return _currentState != CombatState.Sheathed && _currentState != CombatState.Sheathing;
        }
        
        public void HandleAttackInput()
        {
            // We'll implement this later for attacks
            if (_debugCombat)
            {
                Debug.Log("Attack input received, but attack handling not yet implemented");
            }
        }
        
        public CombatState GetCurrentCombatState()
        {
            return _currentState;
        }
        
        private void UnsheatheWeapon()
        {
            if (_currentState != CombatState.Sheathed)
            {
                Debug.LogWarning($"Cannot unsheathe from state: {_currentState}");
                return;
            }
            
            // Start transition to unsheathed state
            SetCombatState(CombatState.Unsheathing);
            
            // Get weapon type from service or use default
            WeaponType weaponType = _weaponService != null ? 
                _weaponService.GetEquippedWeaponType() : WeaponType.OneHanded;
            
            // Set animation parameters
            if (_animationController != null)
            {
                _animationController.SetTrigger("Unsheathe");
                
                // Set weapon type in animator
                switch (weaponType)
                {
                    case WeaponType.OneHanded:
                        _animationController.SetBool("OneHanded", true);
                        break;
                    case WeaponType.TwoHanded:
                        _animationController.SetBool("TwoHanded", true);
                        break;
                    // Add other weapon types as needed
                }
            }
            
            // Start coroutine to complete unsheathing after animation time
            if (_stateTransitionCoroutine != null)
            {
                StopCoroutine(_stateTransitionCoroutine);
            }
            
            _stateTransitionCoroutine = StartCoroutine(CompleteUnsheathe());
            
            // Update context state
            _context.SetStateValue("IsWeaponDrawn", true);
            
            if (_debugCombat)
            {
                Debug.Log($"Unsheathing weapon of type: {weaponType}");
            }
        }
        
        private IEnumerator CompleteUnsheathe()
        {
            // Wait for unsheathe animation to complete
            yield return new WaitForSeconds(_unsheatheTime);
            
            // Complete transition to unsheathed state
            SetCombatState(CombatState.Unsheathed);
            
            if (_debugCombat)
            {
                Debug.Log("Weapon fully unsheathed");
            }
            
            _stateTransitionCoroutine = null;
        }
        
        private void SheatheWeapon()
        {
            if (_currentState != CombatState.Unsheathed)
            {
                Debug.LogWarning($"Cannot sheathe from state: {_currentState}");
                return;
            }
            
            // Start transition to sheathed state
            SetCombatState(CombatState.Sheathing);
            
            // Set animation parameters
            if (_animationController != null)
            {
                _animationController.SetTrigger("Sheathe");
            }
            
            // Start coroutine to complete sheathing after animation time
            if (_stateTransitionCoroutine != null)
            {
                StopCoroutine(_stateTransitionCoroutine);
            }
            
            _stateTransitionCoroutine = StartCoroutine(CompleteSheathe());
            
            // Update context state
            _context.SetStateValue("IsWeaponDrawn", false);
            
            if (_debugCombat)
            {
                Debug.Log("Sheathing weapon");
            }
        }
        
        private IEnumerator CompleteSheathe()
        {
            // Wait for sheathe animation to complete
            yield return new WaitForSeconds(_sheatheTime);
            
            // Complete transition to sheathed state
            SetCombatState(CombatState.Sheathed);
            
            // Reset weapon type booleans in animator
            if (_animationController != null)
            {
                _animationController.SetBool("OneHanded", false);
                _animationController.SetBool("TwoHanded", false);
                // Reset other weapon types as needed
            }
            
            if (_debugCombat)
            {
                Debug.Log("Weapon fully sheathed");
            }
            
            _stateTransitionCoroutine = null;
        }
        
        private void ForceSheatheWeapon()
        {
            // Immediately set to sheathed state without animation
            SetCombatState(CombatState.Sheathed);
            
            // Reset all weapon type booleans
            if (_animationController != null)
            {
                _animationController.SetBool("OneHanded", false);
                _animationController.SetBool("TwoHanded", false);
                // Reset other weapon types as needed
            }
            
            // Update context state
            _context.SetStateValue("IsWeaponDrawn", false);
            
            if (_debugCombat)
            {
                Debug.Log("Weapon forcibly sheathed");
            }
            
            // Stop any ongoing transitions
            if (_stateTransitionCoroutine != null)
            {
                StopCoroutine(_stateTransitionCoroutine);
                _stateTransitionCoroutine = null;
            }
        }
        
        private void SetCombatState(CombatState newState)
        {
            _currentState = newState;
            _context.SetStateValue("CombatState", (int)newState);
            
            if (_debugCombat)
            {
                Debug.Log($"Combat state changed to: {newState}");
            }
        }
    }
}
