using UnityEngine;
using System.Collections;

namespace Sol
{
    public class Jumping : MonoBehaviour, IPlayerComponent, IBaseJumping
    { 
        [SerializeField] private float _jumpForce = 5f;
        [SerializeField] private float _horizontalVelocityRetention = 1.0f; // How much horizontal velocity to keep
        
        private IPlayerContext _context;
        private Rigidbody _rigidbody;
        private bool _isActive = true;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _rigidbody = GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
            {
                Debug.LogError("No Rigidbody found on the GameObject. Jumping won't work!");
            }
            
            Debug.Log("Jumping component initialized");
        }
        
        public bool CanBeActivated()
        {
            return true; // Always allow jumping component to be active
        }
        
        public void OnActivate()
        {
            _isActive = true;
            Debug.Log("Jumping component activated");
        }
        
        public void OnDeactivate()
        {
            _isActive = false;
            Debug.Log("Jumping component deactivated");
        }
        
        public bool IsActive()
        {
            return _isActive;
        }
        
        public void HandleJumpInput()
        {
            if (!_isActive || _rigidbody == null) return;
            
            bool isGrounded = _context.GetStateValue<bool>("IsGrounded", false);
            
            if (isGrounded)
            {
                // Get current velocity
                Vector3 currentVelocity = _rigidbody.linearVelocity;
                
                // Extract horizontal velocity
                Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                
                // Apply jump force (vertical component)
                _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                
                // If we're moving horizontally, ensure we maintain that momentum
                if (horizontalVelocity.magnitude > 0.1f)
                {
                    // Scale horizontal velocity if needed (e.g., for a forward boost when jumping)
                    Vector3 targetHorizontalVelocity = horizontalVelocity * _horizontalVelocityRetention;
                    
                    // Calculate the force needed to achieve the desired horizontal velocity
                    Vector3 horizontalForce = targetHorizontalVelocity - horizontalVelocity;
                    
                    // Apply the horizontal force
                    _rigidbody.AddForce(horizontalForce, ForceMode.VelocityChange);
                    
                    Debug.Log($"Jump with horizontal velocity: {targetHorizontalVelocity.magnitude}");
                }
                else
                {
                    Debug.Log("Vertical jump (no horizontal velocity)");
                }
            }
            else
            {
                Debug.Log("Jump input received but not grounded");
            }
        }
    }
}
