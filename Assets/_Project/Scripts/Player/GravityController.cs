using UnityEngine;

namespace Sol
{
    public class GravityController : MonoBehaviour, IPlayerComponent, IGravityController
    {
        [Header("Gravity Settings")]
        [SerializeField] private float _defaultGravityMultiplier = 1.0f;
        [SerializeField] private Vector3 _defaultGravityDirection = Vector3.down;
        [SerializeField] private float _defaultGravityStrength = 9.81f;
        [SerializeField] private float _terminalVelocity = -20f;
        
        // Dependencies
        private IPlayerContext _context;
        private IStatsService _statsService;
        private IGroundChecker _groundChecker;
        private Rigidbody _rigidbody;
        
        // State
        private bool _isActive = true;
        private bool _gravityEnabled = true;
        private Vector3 _customGravityDirection;
        private bool _useCustomGravityDirection;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _statsService = context.GetService<IStatsService>();
            _groundChecker = context.GetService<IGroundChecker>();
            _rigidbody = GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            
            // Disable Unity's built-in gravity
            _rigidbody.useGravity = false;
        }
        
        public bool CanBeActivated() => true;
        
        public void OnActivate() => _isActive = true;
        
        public void OnDeactivate() => _isActive = false;
        
        private void FixedUpdate()
        {
            if (_gravityEnabled)
            {
                ProcessGravity();
            }
        }
        
        public void ProcessGravity()
        {
            if (_rigidbody == null || !_gravityEnabled) return;

            float gravityScale = GetCurrentGravityScale();
            
            // Calculate gravity force
            Vector3 gravityForce;
            if (_useCustomGravityDirection)
            {

                gravityForce = _customGravityDirection * gravityScale;
            }
            else
            {
                if (_rigidbody.linearVelocity.y <= 0)
                {
                    gravityForce = _defaultGravityDirection.normalized * (_defaultGravityStrength * gravityScale);
                }
                else
                {
                    gravityForce = _defaultGravityDirection.normalized * (_defaultGravityStrength * _defaultGravityMultiplier);
                }
            }
            
            // Apply gravity force - always the same regardless of velocity
            _rigidbody.AddForce(gravityForce, ForceMode.Acceleration);
            
            // Clamp to terminal velocity
            float currentTerminalVelocity = _statsService != null ? 
                _statsService.GetStat("TerminalVelocity") : _terminalVelocity;
                
            // Only clamp vertical velocity
            if (_rigidbody.linearVelocity.y < currentTerminalVelocity)
            {
                Vector3 clampedVelocity = _rigidbody.linearVelocity;
                clampedVelocity.y = currentTerminalVelocity;
                _rigidbody.linearVelocity = clampedVelocity;
            }
        }
        
        public void SetGravityScale(float scale)
        {
            // Apply as a modifier to the stat
            if (_statsService != null)
            {
                StatModifier scaleModifier = new StatModifier(
                    ModifierType.Multiplicative,
                    ModifierCatagory.Temporary,
                    scale,
                    this,
                    -1f // No duration (will be removed manually)
                );
                
                _statsService.ApplyOrReplaceModifier(
                    "GravityMultiplier", 
                    scaleModifier, 
                    ModifierCatagory.Temporary, 
                    "GravityController"
                );
            }
        }
        
        public void SetCustomGravityDirection(Vector3 gravity)
        {
            _customGravityDirection = gravity;
            _useCustomGravityDirection = true;
        }
        
        public void ResetToDefaultGravity()
        {
            _useCustomGravityDirection = false;
            
            // Remove any gravity scale modifiers
            if (_statsService != null)
            {
                _statsService.RemoveModifiersFromSource("GravityMultiplier", "GravityController");
            }
        }
        
        public float GetCurrentGravityScale()
        {
            return _statsService != null ? 
                _statsService.GetStat("GravityMultiplier") : _defaultGravityMultiplier;
        }
        
        public Vector3 GetCurrentGravity()
        {
            if (_useCustomGravityDirection)
                return _customGravityDirection * GetCurrentGravityScale();
            else
                return _defaultGravityDirection.normalized * _defaultGravityStrength * GetCurrentGravityScale();
        }
        
        public void EnableGravity(bool enable) => _gravityEnabled = enable;
    }
}
