using System;
using UnityEngine;

namespace Sol
{
    public class GroundChecker : MonoBehaviour, IGroundChecker, IPlayerComponent
    {
        [Header("Ground Check Settings")]
        [SerializeField] private Transform _checkPoint;
        [SerializeField] private float _checkRadius = 0.2f;
        [SerializeField] private float _checkDistance = 0.3f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private bool _useSphereCast = true;
        [SerializeField] private bool _debugDraw = false;
        
        [Header("Advanced Settings")]
        [SerializeField] private float _groundedBufferTime = 0.1f;
        [SerializeField] private float _coyoteTime = 0.15f;
        
        [Header("Slope Look-Ahead")]
        [SerializeField] private float _lookAheadDistance = 0.4f;   // How far ahead to probe
        [SerializeField] private float _lookAheadRayOffset = 0.3f;  // Forward offset of probe ray
        private RaycastHit _lookAheadHit;
        private bool _hasLookAheadHit;
        private Vector3 _lookAheadDirection = Vector3.forward; // Updated externally
        
        private IPlayerContext _context;
        [SerializeField] private bool _isGrounded;
        private bool _wasGrounded;
        private float _lastGroundedTime;
        private float _groundedStateTime; // Time spent in current grounded state
        private RaycastHit _groundHit;
        private float _distanceToGround;
        
        public bool IsGrounded => _isGrounded || Time.time - _lastGroundedTime <= _coyoteTime;
        public float DistanceToGround => _distanceToGround;
        public float GroundNormalSlope => Vector3.Angle(Vector3.up, _groundHit.normal);
        public RaycastHit GroundHit => _groundHit;
        
        public bool HasLookAheadHit => _hasLookAheadHit;
        public RaycastHit LookAheadHit => _lookAheadHit;
        
        public event Action<bool> OnGroundedStateChanged;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            
            // Create check point if not assigned
            if (_checkPoint == null)
            {
                GameObject checkPointObj = new GameObject("GroundCheckPoint");
                _checkPoint = checkPointObj.transform;
                _checkPoint.SetParent(transform);
                
                // Position at the bottom of the collider
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    float bottom = col.bounds.min.y - transform.position.y;
                    _checkPoint.localPosition = new Vector3(0, bottom + 0.3f, 0);
                }
                else
                {
                    _checkPoint.localPosition = new Vector3(0, -0.7f, 0);
                }
            }
            
            // Force initial ground check
            CheckGround();
            
            // Set initial state
            if (_context != null)
            {
                _context.SetStateValue("IsGrounded", IsGrounded);
            }
        }
        
        private void FixedUpdate()
        {
            CheckGround();
            
            // Register state change
            if (_wasGrounded != _isGrounded)
            {
                _wasGrounded = _isGrounded;
                _groundedStateTime = 0f; // Reset time in state
                OnGroundedStateChanged?.Invoke(_isGrounded);
            }
            else
            {
                _groundedStateTime += Time.fixedDeltaTime;
            }
            
            // Update context state
            if (_context != null)
            {
                _context.SetStateValue("IsGrounded", IsGrounded);
                _context.SetStateValue("DistanceToGround", _distanceToGround);
                _context.SetStateValue("GroundNormalSlope", GroundNormalSlope);
            }
        }
        
        private void CheckGround()
        {
            bool hitGround = false;
            _distanceToGround = float.MaxValue;
        
            if (_useSphereCast)
            {
                // Sphere cast for better detection on uneven terrain
                if (Physics.SphereCast(
                        _checkPoint.position,
                        _checkRadius,
                        Vector3.down,
                        out _groundHit,
                        _checkDistance,
                        _groundLayer))
                {
                    hitGround = true;
                    _distanceToGround = _groundHit.distance;
                }
            }
            else
            {
                // Simple raycast
                if (Physics.Raycast(
                        _checkPoint.position,
                        Vector3.down,
                        out _groundHit,
                        _checkDistance,
                        _groundLayer))
                {
                    hitGround = true;
                    _distanceToGround = _groundHit.distance;
                }
            }
        
            if (_debugDraw)
            {
                Debug.Log($"GroundChecker: Hit ground: {hitGround}, Distance: {_distanceToGround}");
            }
        
            // Update grounded state with buffer time
            if (hitGround)
            {
                _lastGroundedTime = Time.time;
                
                // Set grounded immediately or after buffer time
                if (!_isGrounded)
                {
                    if (_groundedBufferTime <= 0f)
                    {
                        _isGrounded = true;
                    }
                    else if (_groundedStateTime >= _groundedBufferTime)
                    {
                        _isGrounded = true;
                    }
                }
            }
            else
            {
                _isGrounded = false;
            }
            
            _hasLookAheadHit = false;
            if (_isGrounded && _lookAheadDirection.sqrMagnitude > 0.01f)
            {
                Vector3 probeOrigin = _checkPoint.position 
                                      + _lookAheadDirection.normalized * _lookAheadRayOffset;
                _hasLookAheadHit = Physics.Raycast(
                    probeOrigin,
                    Vector3.down,
                    out _lookAheadHit,
                    _checkDistance + 0.3f,  // Slightly longer range to catch upcoming drops too
                    _groundLayer
                );
            }
        }
        
        public void SetLookAheadDirection(Vector3 worldDirection)
        {
            _lookAheadDirection = worldDirection;
        }
        
        public Vector3 GetSmoothedGroundNormal()
        {
            if (!_isGrounded) return Vector3.up;
    
            Vector3 currentNormal = _groundHit.normal;
    
            if (_hasLookAheadHit)
            {
                // Blend toward the upcoming surface normal
                return Vector3.Slerp(currentNormal, _lookAheadHit.normal, 0.5f).normalized;
            }
    
            return currentNormal;
        }
        
        public bool IsGroundedStrict => _isGrounded;
        
        private void OnDrawGizmos()
        {
            if (!_debugDraw || _checkPoint == null) return;
            
            if (IsGrounded)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }
            
            if (_useSphereCast)
            {
                // Draw sphere at check point
                Gizmos.DrawWireSphere(_checkPoint.position, _checkRadius);
                
                // Draw sphere at max distance
                Gizmos.DrawWireSphere(
                    _checkPoint.position + Vector3.down * _checkDistance,
                    _checkRadius);
            }
            else
            {
                // Draw ray
                Gizmos.DrawLine(
                    _checkPoint.position,
                    _checkPoint.position + Vector3.down * _checkDistance);
            }
        }
    }

}
