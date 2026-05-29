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
        
        // NEW: Local physics multiplier. Replaces the old stat-modifier hack.
        private float _currentInternalScale;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _statsService = ServiceLocator.Get<IStatsService>();
            _groundChecker = context.GetService<IGroundChecker>();
            _rigidbody = GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            
            _rigidbody.useGravity = false;
            
            // NEW: Start at default
            _currentInternalScale = _defaultGravityMultiplier;
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

            // Read EXTERNAL modifiers (zones, gear, spells) once per frame
            float externalScale = _statsService != null 
                ? _statsService.GetStat(StatTypeEnum.GravityMultiplier) 
                : 1.0f;
            
            Vector3 gravityForce;
            
            if (_useCustomGravityDirection)
            {
                // CHANGED: apply external scale, but no internal falling logic here
                gravityForce = _customGravityDirection * externalScale;
            }
            else
            {
                // CHANGED: internal scale depends on rising vs falling.
                // StatsService is NOT involved in this decision.
                float internalScale = _rigidbody.linearVelocity.y > 0 
                    ? _defaultGravityMultiplier 
                    : _currentInternalScale;
                    
                float finalScale = internalScale * externalScale;
                
                gravityForce = _defaultGravityDirection.normalized * (_defaultGravityStrength * finalScale);
            }
            
            _rigidbody.AddForce(gravityForce, ForceMode.Acceleration);
            
            float currentTerminalVelocity = _statsService != null ? 
                _statsService.GetStat(StatTypeEnum.TerminalVelocity) : _terminalVelocity;
                
            if (_rigidbody.linearVelocity.y < currentTerminalVelocity)
            {
                Vector3 clampedVelocity = _rigidbody.linearVelocity;
                clampedVelocity.y = currentTerminalVelocity;
                _rigidbody.linearVelocity = clampedVelocity;
            }
        }
        
        public void SetGravityScale(float scale)
        {
            // CHANGED: Local state only. Do NOT touch StatsService.
            _currentInternalScale = scale;
        }
        
        public void SetCustomGravityDirection(Vector3 gravity)
        {
            _customGravityDirection = gravity;
            _useCustomGravityDirection = true;
        }
        
        public void ResetToDefaultGravity()
        {
            _useCustomGravityDirection = false;
            
            // CHANGED: Reset local state only.
            _currentInternalScale = _defaultGravityMultiplier;
            
            // REMOVED: _statsService.RemoveModifiersFromSource(...)
            // If an external zone is still active, its stat modifier lives on
            // because the zone owns that lifespan, not this controller.
        }
        
        public float GetCurrentGravityScale()
        {
            // CHANGED: Return the combined multiplier as it would apply right now
            float externalScale = _statsService != null 
                ? _statsService.GetStat(StatTypeEnum.GravityMultiplier) 
                : 1.0f;
                
            if (_useCustomGravityDirection)
                return externalScale;
                
            float internalScale = (_rigidbody != null && _rigidbody.linearVelocity.y > 0)
                ? _defaultGravityMultiplier
                : _currentInternalScale;
                
            return internalScale * externalScale;
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
