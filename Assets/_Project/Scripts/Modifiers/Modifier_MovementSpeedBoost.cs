using UnityEngine;

namespace Sol
{
    public class Modifier_MovementSpeedBoost : MonoBehaviour
    {
        [Header("Boost Settings")] [SerializeField]
        private float _speedBoostAmount = 20f;

        [SerializeField] private float _boostDuration = 3f;
        [SerializeField] private ModifierType _modifierType = ModifierType.Additive;
        [SerializeField] private ModifierCatagory _modifierCategory = ModifierCatagory.Temporary;

        [Header("Source Identification")] [SerializeField]
        private string _sourceId = "";

        [SerializeField] private bool _generateUniqueId = false;

        [Header("Visual Effects")] [SerializeField]
        private ParticleSystem _activationEffect;

        [SerializeField] private AudioClip _boostSound;
        [SerializeField] private float _effectCooldown = 0.5f; // Prevent effect spam

        private float _lastEffectTime = -1f;

        private void Start()
        {
            // Generate a source ID if needed
            if (string.IsNullOrEmpty(_sourceId) || _generateUniqueId)
            {
                _sourceId = $"SpeedBoostPad_{gameObject.GetInstanceID()}";
            }

            Debug.Log($"SpeedBoostPad initialized with sourceId: {_sourceId}");
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if it's the player
            if (other.CompareTag("Player"))
            {
                // Check if we can play effects (to prevent spam)
                bool canPlayEffects = Time.time - _lastEffectTime > _effectCooldown;

                // Try to get the player context
                IPlayerContext playerContext = other.GetComponent<IPlayerContext>();
                if (playerContext == null) return;

                // Get the stats service
                IStatsService statsService = playerContext.GetService<IStatsService>();
                if (statsService == null) return;

                // Apply or replace the speed boost
                ApplySpeedBoost(statsService);

                // Play effects if not in cooldown
                if (canPlayEffects)
                {
                    PlayBoostEffects();
                    _lastEffectTime = Time.time;
                }
            }
        }

        private void ApplySpeedBoost(IStatsService statsService)
        {
            // Create the modifier
            StatModifier speedModifier = new StatModifier
            {
                value = _speedBoostAmount,
                type = _modifierType,
                duration = _boostDuration
            };

            // Apply or replace the modifier using our sourceId
            string modifierId = ((StatsService)statsService).ApplyOrReplaceModifier(
                "moveSpeed", speedModifier, _modifierCategory, _sourceId);

            Debug.Log(
                $"Applied/refreshed speed boost from {_sourceId}: +{_speedBoostAmount} for {_boostDuration} seconds. ID: {modifierId}");
        }

        private void PlayBoostEffects()
        {
            // Play particle effect
            if (_activationEffect != null)
            {
                _activationEffect.Play();
            }

            // Play sound
            if (_boostSound != null)
            {
                AudioSource.PlayClipAtPoint(_boostSound, transform.position);
            }
        }
    }
}
