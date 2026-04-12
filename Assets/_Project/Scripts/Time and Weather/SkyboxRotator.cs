using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Drives space skybox rotation by mirroring the rotation of a nominated
    /// celestial body transform — typically the sun. Keeps the skybox in sync
    /// without duplicating any celestial calculation logic.
    /// </summary>
    public class SkyboxRotator : MonoBehaviour
    {
        [SerializeField] private Transform celestialTarget;

        private SkyAndFogController _skyAndFogController;

        private void Start()
        {
            _skyAndFogController = GetComponent<SkyAndFogController>();

            if (_skyAndFogController == null)
                Debug.LogError("[SkyboxRotator] No SkyAndFogController found on this GameObject.");

            if (celestialTarget == null)
                Debug.LogError("[SkyboxRotator] No celestial target assigned.");
        }

        private void Update()
        {
            if (_skyAndFogController == null || celestialTarget == null)
                return;

            _skyAndFogController.ApplyRotation(celestialTarget.rotation);
        }
    }
}