using UnityEngine;

namespace Sol
{
    public class Modifier_GravityDirectionZone : MonoBehaviour
    {
        [Header("Gravity Settings")]
        [SerializeField] private bool _useTransformDirection = true;
        [SerializeField] private Vector3 _customGravityDirection = Vector3.down;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _zoneEffect;
        [SerializeField] private AudioClip _enterSound;
        [SerializeField] private AudioClip _exitSound;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogs = true;
        
        private Transform _playerTransform;
        private IGravityController _gravityController;
        private bool _playerInZone = false;
        private string _zoneId;
        
        private void Start()
        {
            // Generate unique zone ID for debugging
            _zoneId = $"GravityZone_{gameObject.name}_{gameObject.GetInstanceID()}";
            
            // Initialize zone effects
            if (_zoneEffect != null)
            {
                _zoneEffect.Play();
            }
            
            if (_enableDebugLogs)
                Debug.Log($"Gravity Direction Zone {_zoneId} initialized.");
        }
        
        private void Update()
        {
            // Only update gravity direction if player is in zone and we have valid references
            if (_playerInZone && _gravityController != null && _playerTransform != null)
            {
                Vector3 gravityDir = CalculateGravityDirection();
                
                // ONLY set direction, preserve magnitude/strength
                _gravityController.SetCustomGravityDirection(gravityDir);
                
                if (_enableDebugLogs && Time.frameCount % 60 == 0) // Log every second
                {
                    Debug.Log($"[{_zoneId}] Setting gravity direction: {gravityDir}");
                }
            }
        }
        
        private Vector3 CalculateGravityDirection()
        {
            if (_useTransformDirection)
            {
                // Calculate direction from player to this object's center
                Vector3 direction = (transform.position - _playerTransform.position).normalized;
                return direction;
            }
            else
            {
                // Use the custom direction
                return _customGravityDirection.normalized;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            
            if (_enableDebugLogs)
                Debug.Log($"[{_zoneId}] Player entering gravity zone");
            
            _playerTransform = other.transform;
            
            // Get the gravity controller
            _gravityController = other.GetComponent<IGravityController>();
            if (_gravityController == null)
            {
                Debug.LogWarning($"[{_zoneId}] Player does not have a GravityController component");
                return;
            }
            
            _playerInZone = true;
            
            // Play effects
            PlayEnterEffects();
            
            if (_enableDebugLogs)
                Debug.Log($"[{_zoneId}] Player entered gravity zone. Will apply custom gravity direction only.");
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player") || !_playerInZone) return;
            
            if (_enableDebugLogs)
                Debug.Log($"[{_zoneId}] Player exiting gravity zone");
            
            // CRITICAL: Set player in zone to false FIRST to stop Update() from continuing to set direction
            _playerInZone = false;
            
            if (_gravityController != null)
            {
                // ONLY reset direction to default, do NOT touch gravity strength/magnitude
                _gravityController.ResetToDefaultGravity();
                
                if (_enableDebugLogs)
                    Debug.Log($"[{_zoneId}] Reset gravity direction only (preserving strength)");
            }
            
            // Play exit effects
            PlayExitEffects();
            
            // Clear references AFTER we've handled the gravity reset
            CleanupReferences();
            
            if (_enableDebugLogs)
                Debug.Log($"[{_zoneId}] Player exited gravity zone. Direction reset, strength preserved.");
        }
        
        private void OnDisable()
        {
            // Emergency cleanup if the zone is disabled while player is inside
            if (_playerInZone)
            {
                if (_enableDebugLogs)
                    Debug.Log($"[{_zoneId}] Zone disabled while player inside - emergency cleanup");
                
                _playerInZone = false;
                
                if (_gravityController != null)
                {
                    _gravityController.ResetToDefaultGravity();
                }
                
                CleanupReferences();
            }
        }
        
        private void OnDestroy()
        {
            // Final cleanup - only reset direction
            if (_playerInZone && _gravityController != null)
            {
                _gravityController.ResetToDefaultGravity();
            }
        }
        
        private void CleanupReferences()
        {
            _playerTransform = null;
            _gravityController = null;
        }
        
        private void PlayEnterEffects()
        {
            if (_enterSound != null && _playerTransform != null)
            {
                AudioSource.PlayClipAtPoint(_enterSound, _playerTransform.position);
            }
        }
        
        private void PlayExitEffects()
        {
            if (_exitSound != null && _playerTransform != null)
            {
                AudioSource.PlayClipAtPoint(_exitSound, _playerTransform.position);
            }
        }
        
        // Optional: Visualize the gravity direction in the editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            
            // Show the direction that would be used
            if (_useTransformDirection)
            {
                // Show a sphere to represent center-directed gravity
                Gizmos.DrawWireSphere(transform.position, 1f);
                
                // If we can find a player in the scene, show the direction
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    Vector3 dir = (transform.position - player.transform.position).normalized;
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(player.transform.position, dir * 2f);
                    
                    // Show text with direction info
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(transform.position + Vector3.up, $"Dir: {dir:F2}");
                    #endif
                }
            }
            else
            {
                // Show the custom direction
                Gizmos.DrawRay(transform.position, _customGravityDirection.normalized * 2f);
            }
            
            // Draw the affected zone
            Gizmos.color = new Color(0, 1, 1, 0.2f); // Transparent cyan
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
            }
        }
    }
}