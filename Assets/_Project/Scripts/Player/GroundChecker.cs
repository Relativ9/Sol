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
    
    private IPlayerContext _context;
    private bool _isGrounded;
    private bool _wasGrounded;
    private float _lastGroundedTime;
    private RaycastHit _groundHit;
    private float _distanceToGround;
    private IGroundChecker m_GroundCheckerImplementation;

    public bool IsGrounded => _isGrounded || Time.time - _lastGroundedTime <= _coyoteTime;
    public float DistanceToGround => _distanceToGround;
    public float GroundNormalSlope => m_GroundCheckerImplementation.GroundNormalSlope;

    public RaycastHit GroundHit => _groundHit;
    event Action<bool> IGroundChecker.OnGroundedStateChanged
    {
        add => m_GroundCheckerImplementation.OnGroundedStateChanged += value;
        remove => m_GroundCheckerImplementation.OnGroundedStateChanged -= value;
    }

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
            _checkPoint.localPosition = new Vector3(0, -0.9f, 0);
        }
    }
    
    private void Update()
    {
        CheckGround();
        
        // Register state change
        if (_wasGrounded != _isGrounded)
        {
            _wasGrounded = _isGrounded;
            OnGroundedStateChanged?.Invoke(_isGrounded);
        }
        
        // Update context state
        if (_context != null)
        {
            _context.SetStateValue("IsGrounded", IsGrounded);
            _context.SetStateValue("DistanceToGround", _distanceToGround);
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
        
        // Update grounded state with buffer time
        if (hitGround)
        {
            _lastGroundedTime = Time.time;
            
            // Only set grounded after consistent detection
            if (!_isGrounded && Time.time - _lastGroundedTime >= _groundedBufferTime)
            {
                _isGrounded = true;
            }
        }
        else
        {
            _isGrounded = false;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!_debugDraw || _checkPoint == null) return;
        
        if (_isGrounded)
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
