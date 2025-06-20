using UnityEngine;

namespace Sol
{
    public class CustomGravityZone : MonoBehaviour
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
            // If the transform direction is used, update the gravity direction when the zone rotates
            if (_playerInZone && _useTransformDirection && _gravityController != null)
            {
                Vector3 gravityDir = -transform.up; // Use the negative up direction of the transform
                _gravityController.SetCustomGravityDirection(gravityDir);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
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
                
                // Apply custom gravity direction
                Vector3 gravityDir = _useTransformDirection ? -transform.up : _customGravityDirection.normalized;
                _gravityController.SetCustomGravityDirection(gravityDir);
                
                // Play effects
                PlayEnterEffects();
                
                Debug.Log($"Player entered gravity zone. New gravity direction: {gravityDir}");
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && _gravityController != null)
            {
                _playerInZone = false;
                
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
            Vector3 direction = _useTransformDirection ? -transform.up : _customGravityDirection.normalized;
            Gizmos.DrawRay(transform.position, direction * 2f);
            
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
