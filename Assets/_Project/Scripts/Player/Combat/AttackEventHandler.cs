using UnityEngine;
using System.Collections;

namespace Sol
{
    public class AttackEventHandler : MonoBehaviour, IPlayerComponent, IAnimationEventReceiver
    {
                [Header("Event Names")]
        [SerializeField] private string _attackStartEvent = "OnAttackStart";
        [SerializeField] private string _attackImpactEvent = "OnAttackImpact";
        [SerializeField] private string _attackEndEvent = "OnAttackEnd";
        
        [Header("Effects")]
        [SerializeField] private AudioClip _swooshSound;
        [SerializeField] private AudioClip _impactSound;
        [SerializeField] private GameObject _slashEffectPrefab;
        [SerializeField] private Transform _slashEffectSpawn;
        [SerializeField] private float _attackRadius = 1.5f;
        [SerializeField] private float _attackDamage = 25f;
        [SerializeField] private bool _debugAttacks = true;
        
        // Dependencies
        private IPlayerContext _context;
        private AnimationEventDispatcher _eventDispatcher;
        private AudioSource _audioSource;
        private ICombatController _combatController;
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _combatController = context.GetService<ICombatController>();
            
            // Find the animator component
            Animator animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("No Animator found in children! Animation events won't work.");
                return;
            }
            
            // Find or create the animation event dispatcher
            _eventDispatcher = animator.gameObject.GetComponent<AnimationEventDispatcher>();
            if (_eventDispatcher == null)
            {
                _eventDispatcher = animator.gameObject.AddComponent<AnimationEventDispatcher>();
                _eventDispatcher.Initialize(context);
            }
            
            // Register for animation events
            _eventDispatcher.RegisterReceiver(this, _attackStartEvent);
            _eventDispatcher.RegisterReceiver(this, _attackImpactEvent);
            _eventDispatcher.RegisterReceiver(this, _attackEndEvent);
            
            // Get audio source
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1.0f; // 3D sound
            }
            
            Debug.Log("Attack Event Handler initialized");
        }
        
        // IAnimationEventReceiver implementation
        public void OnAnimationEvent(string eventName)
        {
            // Only process attack events if weapon is drawn
            if (_combatController == null || !_combatController.IsWeaponDrawn())
                return;
                
            if (eventName == _attackStartEvent)
            {
                OnAttackStart();
            }
            else if (eventName == _attackImpactEvent)
            {
                OnAttackImpact();
            }
            else if (eventName == _attackEndEvent)
            {
                OnAttackEnd();
            }
        }
        
        private void OnAttackStart()
        {
            // Play swoosh sound
            if (_audioSource != null && _swooshSound != null)
            {
                _audioSource.PlayOneShot(_swooshSound);
            }
            
            if (_debugAttacks)
            {
                Debug.Log("Attack started");
            }
        }
        
        private void OnAttackImpact()
        {
            // Spawn slash effect
            if (_slashEffectPrefab != null && _slashEffectSpawn != null)
            {
                Instantiate(_slashEffectPrefab, _slashEffectSpawn.position, _slashEffectSpawn.rotation);
            }
            
            // Check for hits
            CheckForHits();
            
            if (_debugAttacks)
            {
                Debug.Log("Attack impact");
            }
        }
        
        private void OnAttackEnd()
        {
            if (_debugAttacks)
            {
                Debug.Log("Attack ended");
            }
        }
        
        private void CheckForHits()
        {
            // Simple sphere cast to detect hits
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward, _attackRadius);
            
            bool hitSomething = false;
            
            foreach (var hit in hits)
            {
                // Skip self
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;
                    
                // Check if it's an enemy
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Apply damage
                    damageable.TakeDamage(_attackDamage);
                    
                    // Play impact sound
                    if (_audioSource != null && _impactSound != null)
                    {
                        _audioSource.PlayOneShot(_impactSound);
                    }
                    
                    hitSomething = true;
                    
                    if (_debugAttacks)
                    {
                        Debug.Log($"Hit enemy: {hit.name} for {_attackDamage} damage");
                    }
                }
            }
            
            // Visual debug
            if (_debugAttacks)
            {
                // Draw debug sphere to visualize attack range
                Debug.DrawRay(transform.position, transform.forward * _attackRadius, hitSomething ? Color.red : Color.yellow, 1.0f);
                StartCoroutine(DrawDebugSphere(transform.position + transform.forward, _attackRadius, hitSomething ? Color.red : Color.yellow, 1.0f));
            }
        }
        
        private IEnumerator DrawDebugSphere(Vector3 center, float radius, Color color, float duration)
        {
            float startTime = Time.time;
            float endTime = startTime + duration;
            
            while (Time.time < endTime)
            {
                Debug.DrawRay(center, Vector3.up * radius, color);
                Debug.DrawRay(center, Vector3.down * radius, color);
                Debug.DrawRay(center, Vector3.left * radius, color);
                Debug.DrawRay(center, Vector3.right * radius, color);
                Debug.DrawRay(center, Vector3.forward * radius, color);
                Debug.DrawRay(center, Vector3.back * radius, color);
                
                yield return null;
            }
        }
        
        // IPlayerComponent implementation
        public bool CanBeActivated()
        {
            return true;
        }
        
        public void OnActivate()
        {
            // Nothing special needed
        }
        
        public void OnDeactivate()
        {
            // Unregister from animation events
            if (_eventDispatcher != null)
            {
                _eventDispatcher.UnregisterReceiver(this, _attackStartEvent);
                _eventDispatcher.UnregisterReceiver(this, _attackImpactEvent);
                _eventDispatcher.UnregisterReceiver(this, _attackEndEvent);
            }
        }
    }
}
