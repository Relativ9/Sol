using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "New Seasonal Data", menuName = "Sol/Seasonal Data")]
    public class SeasonalData : ScriptableObject
    {
        [System.Serializable]
        public class StarSettings
        {
            public bool enabled = false;
            public CelestialRotationMode rotationMode = CelestialRotationMode.Oscillate;
        
            // Oscillation settings
            public float oscillationSpeed = 1.0f;
            public bool syncWithAxis = false;
            public int syncAxisIndex = 0; // 0=X, 1=Y
            public float minRange = -30.0f;
            public float maxRange = 30.0f;
        
            // Continuous rotation settings (rotations per hour)
            public float rotationSpeed = 0.0f;
        }

        public StarSettings xAxis; // Elevation
        public StarSettings yAxis; // Azimuth (planet rotation)
    }
}
