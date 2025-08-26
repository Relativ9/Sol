using UnityEngine;

namespace Sol
{
    public class WindController : MonoBehaviour
    {
        [Header("Wind Settings")]
        [SerializeField] private WindZone windZone;
        [SerializeField] private float baseWindMain = 1f;
        [SerializeField] private float baseWindTurbulence = 0.1f;
        
        [Header("Time Scale Effects")]
        [SerializeField] private bool affectWindMain = true;
        [SerializeField] private bool affectWindTurbulence = true;
        [SerializeField] private float timeScaleMultiplier = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool enableLogging = false;

        private ITimeManager timeManager;
        private float lastTimeScale = 1f;

        private void Awake()
        {
            timeManager = FindObjectOfType<TimeManager>() as ITimeManager;
            
            if (windZone == null)
            {
                windZone = GetComponent<WindZone>();
            }
        }

        private void Start()
        {
            if (windZone != null)
            {
                baseWindMain = windZone.windMain;
                baseWindTurbulence = windZone.windTurbulence;
            }
        }

        private void Update()
        {
            if (windZone == null || timeManager == null) return;

            float currentTimeScale = timeManager.TimeScale;
            
            // Only update if time scale changed (optimization)
            if (Mathf.Abs(currentTimeScale - lastTimeScale) > 0.01f)
            {
                UpdateWindSettings(currentTimeScale);
                lastTimeScale = currentTimeScale;
            }
        }

        private void UpdateWindSettings(float timeScale)
        {
            float scaledMultiplier = timeScale * timeScaleMultiplier;
            
            if (affectWindMain)
            {
                windZone.windMain = baseWindMain * scaledMultiplier;
            }
            
            if (affectWindTurbulence)
            {
                windZone.windTurbulence = baseWindTurbulence * scaledMultiplier;
            }

            if (enableLogging)
            {
                Debug.Log($"[WindController] Updated wind settings - Main: {windZone.windMain:F2}, Turbulence: {windZone.windTurbulence:F3} (TimeScale: {timeScale:F1}x)");
            }
        }
    }
}