using Unity.Cinemachine;
using UnityEngine;

namespace Sol
{
    public class CameraController : MonoBehaviour, ICameraController, IPlayerComponent
    {
        [Header("Camera Settings")]
        [SerializeField] private float _sensitivity = 10.0f;
        [SerializeField] private bool _invertY = false;
        [SerializeField] private bool _lockCursorOnStart = true;
        
        [Header("Cinemachine")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraTarget;
        
        [Header("Debug")]
        [SerializeField] private Vector2 _debugLookInput;
        [SerializeField] private bool _debugMode = false;
        [SerializeField] private bool _isActive = false; // Make this visible in inspector
        
        // Dependencies
        private IPlayerContext _context;
        
        // State
        private float _rotationX;
        private float _rotationY;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            
            // Find the Cinemachine camera if not assigned
            if (_cinemachineCamera == null)
            {
                _cinemachineCamera = FindObjectOfType<CinemachineCamera>();
                if (_cinemachineCamera == null)
                {
                    Debug.LogError("No CinemachineCamera found! Camera control won't work.");
                }
            }
            
            // Create camera target if not assigned
            if (_cameraTarget == null)
            {
                GameObject targetObj = new GameObject("CameraTarget");
                targetObj.transform.position = transform.position + new Vector3(0, 1.6f, 0);
                targetObj.transform.rotation = transform.rotation;
                targetObj.transform.parent = transform;
                _cameraTarget = targetObj.transform;
                
                // Set the Cinemachine camera to follow this target
                if (_cinemachineCamera != null)
                {
                    _cinemachineCamera.Follow = _cameraTarget;
                    Debug.Log("Set Cinemachine camera to follow newly created target");
                }
            }
            
            // Initialize rotation values based on current camera target rotation
            _rotationX = _cameraTarget.eulerAngles.y;
            _rotationY = _cameraTarget.eulerAngles.x;
            
            // Adjust _rotationY to handle the 0-360 to -180 to 180 conversion
            if (_rotationY > 180f)
            {
                _rotationY -= 360f;
            }
            
            // Lock cursor if needed
            if (_lockCursorOnStart)
            {
                LockCursor(true);
            }
            
            // Automatically activate the camera controller
            OnActivate();
            
            Debug.Log("Camera controller initialized with Cinemachine and activated");
        }
        
        public void OnActivate()
        {
            _isActive = true;
            Debug.Log("Camera controller activated");
        }
        
        public void OnDeactivate()
        {
            _isActive = false;
            Debug.Log("Camera controller deactivated");
        }
        
        public bool CanBeActivated()
        {
            return true; // Camera can always be activated
        }
        
        private void Update()
        {
            // Debug the state of the controller
            if (_debugMode && !_isActive)
            {
                Debug.Log("CameraController is not active!");
                // Auto-activate if not active
                OnActivate();
            }
            
            if (!_isActive || _cameraTarget == null) return;
            
            // Get look input from context
            Vector2 lookInput = _context.GetLookInput();
            
            // For debugging
            _debugLookInput = lookInput;
            
            if (_debugMode && lookInput.magnitude > 0)
            {
                Debug.Log($"Look input in CameraController: {lookInput}");
            }
            
            // Apply sensitivity
            Vector2 lookDelta = lookInput * _sensitivity * Time.deltaTime;
            
            // Apply inversion if needed
            if (_invertY)
            {
                lookDelta.y = -lookDelta.y;
            }
            
            // Update rotation values directly - no smoothing for first-person
            _rotationX += lookDelta.x;
            _rotationY -= lookDelta.y; // Subtract to get correct direction
            
            // Clamp vertical rotation (pitch)
            _rotationY = Mathf.Clamp(_rotationY, -80f, 80f);
            
            // Apply rotation to camera target immediately
            _cameraTarget.rotation = Quaternion.Euler(_rotationY, _rotationX, 0);
        }
        
        public void SetSensitivity(float sensitivity)
        {
            _sensitivity = sensitivity;
        }
        
        public void SetInvertY(bool invertY)
        {
            _invertY = invertY;
        }
        
        public void LockCursor(bool locked)
        {
            if (locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        public Transform GetCameraTransform()
        {
            if (_cinemachineCamera != null)
            {
                return _cinemachineCamera.transform;
            }
            return _cameraTarget;
        }
        
        // Add methods to get camera orientation for movement
        public Vector3 GetCameraForward()
        {
            // Get forward direction based on camera's Y rotation only
            return Quaternion.Euler(0, _rotationX, 0) * Vector3.forward;
        }
        
        public Vector3 GetCameraRight()
        {
            // Get right direction based on camera's Y rotation only
            return Quaternion.Euler(0, _rotationX, 0) * Vector3.right;
        }
        
        public float GetCameraYaw()
        {
            return _rotationX;
        }
    }
}
