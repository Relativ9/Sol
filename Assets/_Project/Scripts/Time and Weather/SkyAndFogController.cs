using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Sol
{
    /// <summary>
    /// Validates and owns the Sky and Fog Volume.
    /// Exposes ApplyRotation so CelestialRotator can drive the space skybox
    /// rotation using the same calculated Quaternion as the sun transform.
    /// </summary>
    public class SkyAndFogController : MonoBehaviour
    {
        [SerializeField] private Volume skyAndFogVolume;

        private PhysicallyBasedSky _physicallyBasedSky;

        private void Start()
        {
            if (skyAndFogVolume == null || skyAndFogVolume.profile == null)
            {
                Debug.LogError("[SkyAndFogController] Sky and Fog Volume or its profile is not assigned.");
                return;
            }

            if (!skyAndFogVolume.profile.TryGet(out _physicallyBasedSky))
            {
                Debug.LogError("[SkyAndFogController] No PhysicallyBasedSky found in the Volume profile.");
            }
        }

        public void ApplyRotation(Quaternion rotation)
        {
            if (_physicallyBasedSky == null)
                return;

            _physicallyBasedSky.spaceRotation.value = rotation.eulerAngles;
        }
    }
}