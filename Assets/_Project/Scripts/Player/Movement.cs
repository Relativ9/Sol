using UnityEngine;

namespace Sol
{
    public class Movement : MonoBehaviour, IPlayerComponent, IBaseMovement
    {
        private IStatsService _statsService;
        private IPlayerContext _context;
        private IGroundChecker _groundChecker;
        
        private Transform _cameraTransform;
        
        [SerializeField] private float _rotationSpeed = 10f;

        private Vector3 _currentVelocity;
        private bool _isActive;

        // Update is called once per frame
        void Update()
        {
        
        }
        

        public void Initialize(IPlayerContext context)
        {
           _context = context;
           _statsService = context.GetService<IStatsService>();
           _groundChecker = context.GetService<IGroundChecker>();
           
           _cameraTransform = Camera.main.transform;
        }

        public bool CanBeActivated()
        {
            // Can walk if:
            // 1. On the ground (using ground checker)
            // 2. Not in water or other special zones
            // 3. Not in a state that prevents walking (stunned, etc.)
            bool isGrounded = _groundChecker != null ? _groundChecker.IsGrounded : 
                _context.GetStateValue<bool>("IsGrounded", false);
            bool canMove = !_context.GetStateValue<bool>("IsStunned", false);
            bool isInWater = _context.GetStateValue<bool>("IsInWater", false);
        
            return isGrounded && canMove && !isInWater;
        }

        public void ProcessMovement()
        {
            if (!_isActive) return;

            Vector2 input = _context.GetMovementInput();
            bool hasInput = input.magnitude > 0.01f;

            float currentSpeed = _statsService.GetStat("moveSpeed");

            Vector3 moveDirection = CalculateMoveDirection(input);

            if (hasInput)
            {
                RotateTowardsMoveDirection(moveDirection);

                _currentVelocity = moveDirection * currentSpeed;
            }
            else //Look into a better method of breaking, this might not be ideal depending on how our state system ends up working
            {
                _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, Time.deltaTime * 10f);
            }
            
            Vector3 finalVelocity = new Vector3(_currentVelocity.x, _context.GetCurrentVeolocity().y, _currentVelocity.z);
            _context.ApplyMovement(finalVelocity); //makes sure dependency inversion is respected and actually apply the forces in the controller.
            
        }
        
        private Vector3 CalculateMoveDirection(Vector2 input)
        {
            // Convert input to world space direction based on camera orientation
            Vector3 forward = _cameraTransform.forward;
            Vector3 right = _cameraTransform.right;
        
            // Project vectors onto the horizontal plane
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
        
            // Combine directions based on input
            return (forward * input.y + right * input.x).normalized;
        }
        
        private void RotateTowardsMoveDirection(Vector3 moveDirection)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                // Calculate target rotation
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            
                // Smoothly rotate towards the target rotation
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }
        }

        public void OnActivate()
        {
            _isActive = true;
            Debug.Log("Movement state activated");
        }

        public void OnDeactivate()
        {
            _isActive = false;
            Debug.Log("Movement state deactivated");
        }
    }
}
