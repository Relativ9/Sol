using UnityEngine;
using System.Collections;

namespace Sol
{
    public class Jumping : MonoBehaviour, IPlayerComponent, IJumping
    { 
                [SerializeField] private float _defaultJumpForce = 5f;
        [SerializeField] private float _defaultJumpDirectionBoost = 1.0f; // How much horizontal velocity to boost
        [SerializeField] private float _defaultDoubleJumpCount = 1;
        [SerializeField] private bool _debugJump = true;
        [SerializeField] private float _jumpCooldown = 0.1f; // Prevent accidental double-taps
        
        private IPlayerContext _context;
        private Rigidbody _rigidbody;
        private IStatsService _statsService;
        private IGroundChecker _groundChecker;
        private bool _isActive = true;
        private float _currentDoubleJumpCount = 0;
        private float _lastJumpTime = -1f;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _statsService = context.GetService<IStatsService>();
            _groundChecker = context.GetService<IGroundChecker>();
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
            if (!CanProcessJumpInput()) return;
            
            var jumpParams = GetJumpParameters();
            bool isGrounded = GetIsGrounded();
            
            // Begin jump priority window early to block stickiness
            BeginJumpPriorityWindow();
            
            if (isGrounded && TryExecuteGroundedJump(jumpParams))
            {
                RecordJumpTime();
            }
            else if (TryExecuteDoubleJump(jumpParams))
            {
                RecordJumpTime();
            }
            else
            {
                // Failed to jump, clear priority immediately
                EndJumpPriority();
                if (_debugJump) Debug.Log("Cannot jump: not grounded and out of double jumps");
            }
        }
        // --- Validation ---
        private bool CanProcessJumpInput()
        {
            if (!_isActive || _rigidbody == null) return false;
            
            if (Time.time - _lastJumpTime < _jumpCooldown)
            {
                if (_debugJump) Debug.Log("Jump ignored due to cooldown");
                return false;
            }
            
            return true;
        }
        // --- State Management ---
        private void BeginJumpPriorityWindow()
        {
            _context.SetStateValue("JumpPriority", true);
            StartCoroutine(ClearJumpPriorityAfterDelay(0.15f));
        }
        private void EndJumpPriority()
        {
            _context.SetStateValue("JumpPriority", false);
        }
        private void RecordJumpTime()
        {
            _lastJumpTime = Time.time;
        }
        // --- Data Retrieval ---
        private bool GetIsGrounded()
        {
            return _groundChecker != null 
                ? _groundChecker.IsGrounded 
                : _context.GetStateValue<bool>("IsGrounded", false);
        }
        private (float force, float directionBoost, float maxDoubleJump) GetJumpParameters()
        {
            float force = _statsService?.GetStat(StatTypeEnum.JumpForce) ?? _defaultJumpForce;
            float boost = _statsService?.GetStat(StatTypeEnum.JumpDirectionBoost) ?? _defaultJumpDirectionBoost;
            float maxDouble = _statsService?.GetStat(StatTypeEnum.MaxDoubleJump)?? _defaultDoubleJumpCount;
            return (force, boost, maxDouble);
        }
        // --- Jump Execution ---
        private bool TryExecuteGroundedJump((float force, float directionBoost, float maxDoubleJump) jumpParams)
        {
            _currentDoubleJumpCount = 0;
            
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            
            // Calculate boosted horizontal velocity
            Vector3 boostedHorizontal = horizontalVelocity;
            if (horizontalVelocity.magnitude > 0.1f && jumpParams.directionBoost > 1.0f)
            {
                boostedHorizontal = horizontalVelocity * jumpParams.directionBoost;
            }
            
            // CRITICAL: Clear sticky downward velocity before applying jump
            _rigidbody.linearVelocity = new Vector3(
                boostedHorizontal.x,
                Mathf.Max(0f, currentVelocity.y), // Kill any sticky down-force
                boostedHorizontal.z
            );
            
            ApplyJumpImpulse(jumpParams.force);
            
            if (_debugJump) Debug.Log($"Ground jump with force: {jumpParams.force}");
            
            TriggerAnimation("JumpTriggered");
            return true;
        }
        private bool TryExecuteDoubleJump((float force, float directionBoost, float maxDoubleJump) jumpParams)
        {
            if (_currentDoubleJumpCount >= jumpParams.maxDoubleJump) return false;
            
            _currentDoubleJumpCount++;
            
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            
            // Double jumps reset negative velocity for consistency
            if (currentVelocity.y < 0)
            {
                _rigidbody.linearVelocity = new Vector3(
                    currentVelocity.x,
                    0,
                    currentVelocity.z
                );
            }
            
            ApplyJumpImpulse(jumpParams.force);
            
            if (_debugJump) Debug.Log($"Double jump {_currentDoubleJumpCount}/{jumpParams.maxDoubleJump}");
            
            TriggerAnimation("DoubleJumpTriggered");
            return true;
        }
        private void ApplyJumpImpulse(float force)
        {
            _rigidbody.AddForce(Vector3.up * force, ForceMode.Impulse);
        }
        // --- Animation Helpers ---
        private void TriggerAnimation(string triggerKey)
        {
            _context.SetStateValue(triggerKey, true);
            
            // Use a local coroutine wrapper to avoid per-call method proliferation
            StartCoroutine(ResetTriggerAfterDelay(triggerKey, 0.1f));
        }
        private IEnumerator ResetTriggerAfterDelay(string key, float delay)
        {
            yield return new WaitForSeconds(delay);
            _context.SetStateValue(key, false);
        }
        private IEnumerator ClearJumpPriorityAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            EndJumpPriority();
            if (_debugJump) Debug.Log("Jump priority cleared");
        }
        
        private IEnumerator ClearJumpPriority(float delay)
        {
            yield return new WaitForSeconds(delay);
            _context.SetStateValue("JumpPriority", false);
            if (_debugJump) Debug.Log("Jump priority cleared");
        }
        
        private IEnumerator ResetJumpTrigger()
        {
            yield return new WaitForSeconds(0.1f);
            _context.SetStateValue("JumpTriggered", false);
        }
        
        private IEnumerator ResetDoubleJumpTrigger()
        {
            yield return new WaitForSeconds(0.1f);
            _context.SetStateValue("DoubleJumpTriggered", false);
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            // Reset double jump count when landing on something
            if (collision.contacts.Length > 0)
            {
                foreach (ContactPoint contact in collision.contacts)
                {
                    // Check if the contact normal is pointing upward (we're standing on something)
                    if (contact.normal.y > 0.7f)
                    {
                        _currentDoubleJumpCount = 0;
                        if (_debugJump) Debug.Log("Double jump count reset due to landing");
                        break;
                    }
                }
            }
        }
    }
}
