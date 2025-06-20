using Unity.Cinemachine;
using UnityEngine;

namespace Sol
{
    public class SlowFallRing : MonoBehaviour
    {
        [Header("Boost Settings")] [SerializeField]
        private float _slowFallMultiplier = 0.5f;
        
        [SerializeField] private float _duration;
        [SerializeField] private ModifierType _modifierType = ModifierType.Multiplicative;
        [SerializeField] private ModifierCatagory _modifierCatagory = ModifierCatagory.Temporary;
        
        [Header("Source Identification")] [SerializeField]
        private string _sourceId = "";
        
        [SerializeField] private bool _generateUniqueId = false;
        
        [Header("Visual Effects")] [SerializeField]
        private ParticleSystem _activationEffect;
        
        [SerializeField] private AudioClip _floatSound;
        [SerializeField] private float _effectCooldown = 0.5f; // Prevent effect spam

        private float _lastEffectTime = -1f;

        private Transform _playerTransform;
        
        private void Start()
        {
            //Generates a source ID if none is set manually
            if (string.IsNullOrEmpty(_sourceId) || _generateUniqueId)
            {
                _sourceId = $"SlowFallRing_{gameObject.GetInstanceID()}";
            }
            
            Debug.Log($"SlowFallRing initialized with sourceId: {_sourceId}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerTransform =  other.transform;
                bool canPlayEffects = Time.time - _lastEffectTime > _effectCooldown;
                
                IPlayerContext playerContext = other.GetComponent<IPlayerContext>();
                if (playerContext == null) return;
                
                IStatsService statsService = other.GetComponent<IStatsService>();
                if (statsService == null) return;
                
                ApplySlowFall(statsService);

                if (canPlayEffects)
                {
                    PlaySlowFallEffect();
                    _lastEffectTime = Time.time;
                }
                
            }
        }

        private void ApplySlowFall(IStatsService statsService)
        {
            StatModifier gravityModifier = new StatModifier
            {
                value = _slowFallMultiplier,
                type = _modifierType,
                duration = _duration,
            };
            
            string modifierId = ((StatsService)statsService).ApplyOrReplaceModifier("GravityMultiplier", gravityModifier, _modifierCatagory, _sourceId);
            
            Debug.Log(
                $"Applied/refreshed speed boost from {_sourceId}: +{_slowFallMultiplier} for {_duration} seconds. ID: {modifierId}");
        }

        private void PlaySlowFallEffect()
        {
            if (_activationEffect != null)
            {
                _activationEffect.Play();
            }

            if (_floatSound != null)
            {
                AudioSource.PlayClipAtPoint(_floatSound, _playerTransform.position);
            }
        }
    }
}
