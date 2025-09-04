using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Sol
{
    [System.Serializable]
    public class CelestialBody
    {
        [Header("Basic Configuration")]
        public string name = "Sol";
        public bool active = true;
        
        [Header("Orbital Override Settings")]
        [Tooltip("Override the common orbital angle with individual setting")]
        public bool overrideOrbitalAngle = false;
        
        [Header("Azimuth (Y-Axis) - Continuous Orbit")]
        [Tooltip("Enable continuous orbital motion around the sky")]
        public bool yAxisEnabled = true;
        
        [Tooltip("Orbital speed multiplier (1.0 = one orbit per day)")]
        public float yAxisSpeed = 1f;
        
        [Tooltip("Override to sync with day length from TimeManager")]
        public bool yAxisOverrideSpeed = false;
        
        [Header("Orbital Path Configuration")]
        [Tooltip("Angle of orbital path relative to horizon (0° = flat circle, 45° = angled orbit)")]
        [Range(0f, 89f)]
        public float orbitalAngle = 30f;
        
        [Tooltip("Base elevation when celestial body is at Y=0° (starting point of orbit)")]
        [Range(0f, 360f)]
        public float baseElevation = 180f; // Horizon
        
        [Header("Moon-Specific Settings")]
        [Tooltip("Orbital period in days (creates monthly drift effect)")]
        public float orbitalPeriod = 29.5f;
        
        [Tooltip("Phase offset in degrees from sun position")]
        [Range(0f, 360f)]
        public float phaseOffset = 0f;
    }

    [CreateAssetMenu(fileName = "New Seasonal Data", menuName = "Sol/Seasonal Data")]
    public class SeasonalData : ScriptableObject
    {
        [Header("Celestial Bodies")]
        [SerializeField] private List<CelestialBody> stars = new List<CelestialBody>();
        [SerializeField] private List<CelestialBody> moons = new List<CelestialBody>();
        
        [Header("Common Orbital Settings")]
        [SerializeField] private bool useCommonOrbitalAngle = true;
        [Tooltip("Common orbital angle for all celestial bodies (realistic astronomy - Earth's axial tilt is 23.5°)")]
        [SerializeField] private float commonOrbitalAngle = 23.5f; // Earth's axial tilt


        public List<CelestialBody> Stars => stars;
        public List<CelestialBody> Moons => moons;

        public CelestialBody GetCelestialBodyByName(string name)
        {
            var star = stars.FirstOrDefault(s => s.name == name);
            if (star != null) return star;

            var moon = moons.FirstOrDefault(m => m.name == name);
            return moon;
        }
        
        
        public float GetEffectiveOrbitalAngle(CelestialBody celestialBody)
        {
            return (useCommonOrbitalAngle && !celestialBody.overrideOrbitalAngle) 
                ? commonOrbitalAngle 
                : celestialBody.orbitalAngle;
        }

        public List<CelestialBody> GetAllActiveCelestialBodies()
        {
            var allBodies = new List<CelestialBody>();
            allBodies.AddRange(stars.Where(s => s.active));
            allBodies.AddRange(moons.Where(m => m.active));
            return allBodies;
        }
        

        private void OnValidate()
        {
            // Ensure we have at least one star
            if (stars.Count == 0)
            {
                stars.Add(new CelestialBody { name = "Sol" });
            }
        }
    }
}