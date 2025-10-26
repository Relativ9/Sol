using UnityEngine;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sol
{
    
    [ExecuteAlways]
    [RequireComponent(typeof(Light))]
    public class OverrideCelestialBodyShading : MonoBehaviour
    {
        private Light lightComponent;
        private HDAdditionalLightData hdLightData;

        private void OnEnable()
        {
            InitializeComponents();
            ApplyOverride();
        }

        private void OnValidate()
        {
            // Called when script is loaded or a value is changed in inspector
            InitializeComponents();
            ApplyOverride();
        }

        private void Update()
        {
            // Continuously enforce the override in editor and runtime
            ApplyOverride();
        }

        private void InitializeComponents()
        {
            if (lightComponent == null)
            {
                lightComponent = GetComponent<Light>();
            }

            if (hdLightData == null && lightComponent != null)
            {
                hdLightData = lightComponent.GetComponent<HDAdditionalLightData>();
            }
        }

        private void ApplyOverride()
        {
            if (lightComponent == null || hdLightData == null)
            {
                InitializeComponents();
            }

            if (lightComponent != null && 
                lightComponent.type == LightType.Directional && 
                hdLightData != null)
            {
                if (hdLightData.celestialBodyShadingSource != HDAdditionalLightData.CelestialBodyShadingSource.ReflectSunLight)
                {
                    hdLightData.celestialBodyShadingSource = HDAdditionalLightData.CelestialBodyShadingSource.ReflectSunLight;
                
#if UNITY_EDITOR
                    // Mark the scene as dirty so Unity knows changes were made
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(hdLightData);
                        EditorUtility.SetDirty(gameObject);
                    }
#endif
                }
            }
        }
    }

}
