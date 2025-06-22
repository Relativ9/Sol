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
        
        private Transform _playerTransform;
        private IGravityController _gravityController;
        private bool _playerInZone = false;
        
        private void Start()
        {
            // Initialize zone effects
            if (_zoneEffect != null)
            {
                _zoneEffect.Play();
            }
        }
        
        private void Update()
        {
            if (_playerInZone && _gravityController != null && _playerTransform != null)
            {
                Vector3 gravityDir;
                
                if (_useTransformDirection)
                {
                    // Calculate direction from player to this object's center
                    gravityDir = (transform.position - _playerTransform.position).normalized;
                }
                else
                {
                    // Use the custom direction
                    gravityDir = _customGravityDirection.normalized;
                }
                
                // Apply the gravity direction
                _gravityController.SetCustomGravityDirection(gravityDir);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            _playerTransform = other.transform;
            _playerInZone = true;
            
            // Get the gravity controller
            _gravityController = other.GetComponent<IGravityController>();
            if (_gravityController == null)
            {
                Debug.LogWarning("Player does not have a GravityController component");
                return;
            }
            
            // Initial gravity direction will be set in Update
            
            // Play effects
            PlayEnterEffects();
            
            Debug.Log("Player entered gravity zone.");
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && _gravityController != null)
            {
                _playerInZone = false;
                _playerTransform = null;
                
                // Reset to default gravity
                _gravityController.ResetToDefaultGravity();
                
                // Play exit effects
                PlayExitEffects();
                
                Debug.Log("Player exited gravity zone. Gravity reset to default.");
            }
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
            }
            else
            {
                // Show the custom direction
                Gizmos.DrawRay(transform.position, _customGravityDirection.normalized * 2f);
            }
            
            // Draw the affected zone
            Gizmos.color = new Color(0, 1, 1, 0.2f); // Transparent cyan
            Collider col = GetComponent<Collider>();
            if (col != null && col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
        }
    }
}
