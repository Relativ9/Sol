using Unity.Cinemachine;
using UnityEngine;

namespace Sol
{
    public class Modifier_GravityMultiplierTrigger : MonoBehaviour
    {
                [Header("Gravity Modifier Settings")]
        [SerializeField] private float _gravityMultiplier = 0.5f;
        [SerializeField] private bool _persistWhileInTrigger = false;
        [SerializeField] private float _duration = 3.0f;
        [SerializeField] private ModifierType _modifierType = ModifierType.Multiplicative;
        [SerializeField] private ModifierCatagory _modifierCatagory = ModifierCatagory.Temporary;
        
        [Header("Source Identification")]
        [SerializeField] private string _sourceId = "";
        [SerializeField] private bool _generateUniqueId = false;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _activationEffect;
        [SerializeField] private AudioClip _effectSound;
        [SerializeField] private float _effectCooldown = 0.5f; // Prevent effect spam

        private float _lastEffectTime = -1f;
        private Transform _playerTransform;
        private IStatsService _playerStatsService;
        private bool _playerInTrigger = false;
        
        private void Start()
        {
            // Generate a source ID if none is set manually
            if (string.IsNullOrEmpty(_sourceId) || _generateUniqueId)
            {
                _sourceId = $"GravityModifier_{gameObject.GetInstanceID()}";
            }
            
            Debug.Log($"Gravity Modifier initialized with sourceId: {_sourceId}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerTransform = other.transform;
                _playerInTrigger = true;
                
                IPlayerContext playerContext = other.GetComponent<IPlayerContext>();
                if (playerContext == null) return;
                
                _playerStatsService = other.GetComponent<IStatsService>();
                if (_playerStatsService == null) return;
                
                // Apply the gravity modifier
                ApplyGravityModifier();
                
                // Play effects if cooldown allows
                bool canPlayEffects = Time.time - _lastEffectTime > _effectCooldown;
                if (canPlayEffects)
                {
                    PlayEffects();
                    _lastEffectTime = Time.time;
                }
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && _playerStatsService != null)
            {
                _playerInTrigger = false;
                
                // If we're using persist mode, remove the modifier when player exits
                if (_persistWhileInTrigger)
                {
                    RemoveGravityModifier();
                }
                
                // Clear references
                _playerStatsService = null;
                _playerTransform = null;
            }
        }
        
        private void ApplyGravityModifier()
        {
            if (_playerStatsService == null) return;
            
            // Create the modifier with appropriate duration
            float modifierDuration = _persistWhileInTrigger ? -1f : _duration;
            
            StatModifier gravityModifier = new StatModifier
            {
                value = _gravityMultiplier,
                type = _modifierType,
                duration = modifierDuration,
            };
            
            // Apply the modifier
            string modifierId = ((StatsService)_playerStatsService).ApplyOrReplaceModifier(
                "GravityMultiplier", 
                gravityModifier, 
                _modifierCatagory, 
                _sourceId
            );
            
            string durationText = _persistWhileInTrigger ? "while in trigger" : $"for {_duration} seconds";
            Debug.Log($"Applied gravity modifier from {_sourceId}: {_gravityMultiplier} {durationText}. ID: {modifierId}");
        }
        
        private void RemoveGravityModifier()
        {
            if (_playerStatsService == null) return;
            
            _playerStatsService.RemoveModifiersFromSource("GravityMultiplier", _sourceId);
            Debug.Log($"Removed gravity modifier from {_sourceId}");
        }

        private void PlayEffects()
        {
            if (_activationEffect != null)
            {
                _activationEffect.Play();
            }

            if (_effectSound != null && _playerTransform != null)
            {
                AudioSource.PlayClipAtPoint(_effectSound, _playerTransform.position);
            }
        }
        
        // [Header("Boost Settings")] [SerializeField]
        // private float _slowFallMultiplier = 0.5f;
        //
        // [SerializeField] private float _duration;
        // [SerializeField] private ModifierType _modifierType = ModifierType.Multiplicative;
        // [SerializeField] private ModifierCatagory _modifierCatagory = ModifierCatagory.Temporary;
        //
        // [Header("Source Identification")] [SerializeField]
        // private string _sourceId = "";
        //
        // [SerializeField] private bool _generateUniqueId = false;
        //
        // [Header("Visual Effects")] [SerializeField]
        // private ParticleSystem _activationEffect;
        //
        // [SerializeField] private AudioClip _floatSound;
        // [SerializeField] private float _effectCooldown = 0.5f; // Prevent effect spam
        //
        // private float _lastEffectTime = -1f;
        //
        // private Transform _playerTransform;
        //
        // private void Start()
        // {
        //     //Generates a source ID if none is set manually
        //     if (string.IsNullOrEmpty(_sourceId) || _generateUniqueId)
        //     {
        //         _sourceId = $"SlowFallRing_{gameObject.GetInstanceID()}";
        //     }
        //     
        //     Debug.Log($"SlowFallRing initialized with sourceId: {_sourceId}");
        // }
        //
        // private void OnTriggerEnter(Collider other)
        // {
        //     if (other.CompareTag("Player"))
        //     {
        //         _playerTransform =  other.transform;
        //         bool canPlayEffects = Time.time - _lastEffectTime > _effectCooldown;
        //         
        //         IPlayerContext playerContext = other.GetComponent<IPlayerContext>();
        //         if (playerContext == null) return;
        //         
        //         IStatsService statsService = other.GetComponent<IStatsService>();
        //         if (statsService == null) return;
        //         
        //         ApplySlowFall(statsService);
        //
        //         if (canPlayEffects)
        //         {
        //             PlaySlowFallEffect();
        //             _lastEffectTime = Time.time;
        //         }
        //         
        //     }
        // }
        //
        // private void ApplySlowFall(IStatsService statsService)
        // {
        //     StatModifier gravityModifier = new StatModifier
        //     {
        //         value = _slowFallMultiplier,
        //         type = _modifierType,
        //         duration = _duration,
        //     };
        //     
        //     string modifierId = ((StatsService)statsService).ApplyOrReplaceModifier("GravityMultiplier", gravityModifier, _modifierCatagory, _sourceId);
        //     
        //     Debug.Log(
        //         $"Applied/refreshed speed boost from {_sourceId}: +{_slowFallMultiplier} for {_duration} seconds. ID: {modifierId}");
        // }
        //
        // private void PlaySlowFallEffect()
        // {
        //     if (_activationEffect != null)
        //     {
        //         _activationEffect.Play();
        //     }
        //
        //     if (_floatSound != null)
        //     {
        //         AudioSource.PlayClipAtPoint(_floatSound, _playerTransform.position);
        //     }
        // }
    }
}
