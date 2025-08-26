using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Scriptable Object that defines weather patterns and behavior
    /// Can be shared across multiple seasons or used for special events
    /// </summary>
    [CreateAssetMenu(fileName = "New Weather Data", menuName = "Sol/Weather Data")]
    public class WeatherData : ScriptableObject
    {
                [Header("Weather Identity")]
        [SerializeField] private string weatherName = "Default Weather";
        
        [Header("Weather Pattern")]
        [SerializeField, Range(0f, 1f), Tooltip("Probability of snow occurring (0 = never, 1 = always)")]
        private float snowChance = 0.3f;
        
        [Header("Duration Settings (Hours)")]
        [SerializeField, Tooltip("Minimum duration for snow periods in game hours")]
        private float minSnowDuration = 1f;
        
        [SerializeField, Tooltip("Maximum duration for snow periods in game hours")]
        private float maxSnowDuration = 4f;
        
        [SerializeField, Tooltip("Minimum duration for clear weather periods in game hours")]
        private float minClearDuration = 2f;
        
        [SerializeField, Tooltip("Maximum duration for clear weather periods in game hours")]
        private float maxClearDuration = 8f;
        
        [Header("Visual Effects")]
        [SerializeField, Tooltip("Minimum snow particle emission rate (particles per second)")]
        private float minSnowEmissionRate = 50f;
        
        [SerializeField, Tooltip("Maximum snow particle emission rate (particles per second)")]
        private float maxSnowEmissionRate = 200f;
        
        [SerializeField, Tooltip("Color tint applied to snow particles")]
        private Color snowTint = Color.white;
        
        [Header("Audio")]
        [SerializeField, Tooltip("Wind sound effect played during snow")]
        private AudioClip windSound;
        
        [SerializeField, Range(0f, 1f), Tooltip("Volume level for wind sound (0 = silent, 1 = full volume)")]
        private float windVolume = 0.5f;

        // Properties
        public string WeatherName => weatherName;
        public float SnowChance => snowChance;
        public float MinSnowDuration => minSnowDuration;
        public float MaxSnowDuration => maxSnowDuration;
        public float MinClearDuration => minClearDuration;
        public float MaxClearDuration => maxClearDuration;
        public float MinSnowEmissionRate => minSnowEmissionRate;
        public float MaxSnowEmissionRate => maxSnowEmissionRate;
        public Color SnowTint => snowTint;
        public AudioClip WindSound => windSound;
        public float WindVolume => windVolume;

        // Utility method to get random emission rate
        public float GetRandomEmissionRate()
        {
            return Random.Range(minSnowEmissionRate, maxSnowEmissionRate);
        }

        private void OnValidate()
        {
            minSnowDuration = Mathf.Max(0.1f, minSnowDuration);
            maxSnowDuration = Mathf.Max(minSnowDuration, maxSnowDuration);
            minClearDuration = Mathf.Max(0.1f, minClearDuration);
            maxClearDuration = Mathf.Max(minClearDuration, maxClearDuration);
            minSnowEmissionRate = Mathf.Max(0f, minSnowEmissionRate);
            maxSnowEmissionRate = Mathf.Max(minSnowEmissionRate, maxSnowEmissionRate);
        }
    }
}