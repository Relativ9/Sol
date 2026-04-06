using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

namespace Sol
{
    public class SkyAndFogController : MonoBehaviour
    {
        public Volume skyAndFogVolume; // Reference to the Sky and Fog Volume
        public float rotationSpeed = 10f; // Speed of rotation along the Y-axis (degrees per second)
        private PhysicallyBasedSky physicallyBasedSky;
        void Start()
        {
            // Ensure the Volume and the Physically Based Sky are correctly set up
            if (skyAndFogVolume != null && skyAndFogVolume.profile != null)
            {
                if (skyAndFogVolume.profile.TryGet(out physicallyBasedSky))
                {
                    // Successfully obtained the Physically Based Sky component
                    Debug.Log("Physically Based Sky found in the Sky and Fog Volume.");
                }
                else
                {
                    Debug.LogError("No Physically Based Sky found in the Sky and Fog Volume profile. Please assign a Physically Based Sky in the Volume.");
                }
            }
            else
            {
                Debug.LogError("Sky and Fog Volume or its profile is not assigned.");
            }
        }
        void Update()
        {
            if (physicallyBasedSky != null)
            {
                // Get the current space rotation
                Vector3 currentRotation = physicallyBasedSky.spaceRotation.value;
                // Increment the Y (yaw) rotation value over time
                currentRotation.y += rotationSpeed * Time.deltaTime;
                // Optional: Wrap the value between 0 and 360 degrees
                if (currentRotation.y >= 360f)
                    currentRotation.y -= 360f;
                else if (currentRotation.y < 0f)
                    currentRotation.y += 360f;
                // Apply the updated rotation back to the Physically Based Sky
                physicallyBasedSky.spaceRotation.value = currentRotation;
            }
        }
    }
}