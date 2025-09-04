// using UnityEngine;
//
// namespace Sol
// {
//     public class SnowCoverController : MonoBehaviour
//     {
//         [Header("Material Settings")]
//         [SerializeField] private Material snowMaterial;
//         [SerializeField] private string alphaPropertyName = "_Alpha";
//         
//         [Header("Season Alpha Values")]
//         [SerializeField] private float summerAlpha = 0f;
//         [SerializeField] private float transitionToLongNightAlpha = 0.5f;
//         [SerializeField] private float longNightAlpha = 1f;
//         [SerializeField] private float transitionFromLongNightAlpha = 0.5f;
//         
//         [Header("Transition Settings")]
//         [SerializeField] private float transitionSpeed = 0.1f;
//         [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
//         
//         [Header("Debug")]
//         [SerializeField] private bool enableLogging = true;
//
//         private ITimeManager timeManager;
//         private float targetAlpha;
//         private float currentAlpha;
//         private Season lastSeason;
//
//         private void Awake()
//         {
//             timeManager = FindObjectOfType<TimeManager>() as ITimeManager;
//         }
//
//         private void Start()
//         {
//             if (snowMaterial != null)
//             {
//                 currentAlpha = snowMaterial.GetFloat(alphaPropertyName);
//             }
//             
//             if (timeManager != null)
//             {
//                 lastSeason = timeManager.CurrentSeason;
//                 UpdateTargetAlpha();
//             }
//         }
//
//         private void Update()
//         {
//             if (snowMaterial == null || timeManager == null) return;
//
//             // Check if season changed
//             if (timeManager.CurrentSeason != lastSeason)
//             {
//                 lastSeason = timeManager.CurrentSeason;
//                 UpdateTargetAlpha();
//                 
//                 if (enableLogging)
//                 {
//                     Debug.Log($"[SnowCoverController] Season changed to {lastSeason}, target alpha: {targetAlpha:F2}");
//                 }
//             }
//
//             // Smoothly transition to target alpha
//             if (Mathf.Abs(currentAlpha - targetAlpha) > 0.001f)
//             {
//                 currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, transitionSpeed * Time.deltaTime);
//                 snowMaterial.SetFloat(alphaPropertyName, currentAlpha);
//             }
//         }
//
//         private void UpdateTargetAlpha()
//         {
//             switch (timeManager.CurrentSeason)
//             {
//                 case Season.Lansomr:
//                     targetAlpha = summerAlpha;
//                     break;
//                 case Season.Svik:
//                     targetAlpha = transitionToLongNightAlpha;
//                     break;
//                 case Season.Evinotr:
//                     targetAlpha = longNightAlpha;
//                     break;
//                 case Season.Gro:
//                     targetAlpha = transitionFromLongNightAlpha;
//                     break;
//                 default:
//                     targetAlpha = 0f;
//                     break;
//             }
//         }
//
//         [ContextMenu("Log Current Status")]
//         public void LogCurrentStatus()
//         {
//             if (timeManager == null || snowMaterial == null) return;
//             
//             Debug.Log($"=== Snow Cover Status ===");
//             Debug.Log($"Current Season: {timeManager.CurrentSeason}");
//             Debug.Log($"Current Alpha: {currentAlpha:F3}");
//             Debug.Log($"Target Alpha: {targetAlpha:F3}");
//             Debug.Log($"Material Property: {alphaPropertyName}");
//             Debug.Log($"========================");
//         }
//     }
// }