// using UnityEngine;
// using UnityEditor;
//
// namespace Sol.Editor
// {
//     /// <summary>
//     /// Quick setup menu items for common Sol operations that actually perform the setup
//     /// </summary>
//     public static class SolQuickSetup
//     {
//         [MenuItem("Tools/Sol/Quick Setup/Minimal Setup", priority = 10)]
//         public static void MinimalSetup()
//         {
//             var config = SolSetupUtilities.GetRecommendedConfig(SolSetupUtilities.ProjectType.Minimal);
//             PerformQuickSetup(config, "Minimal");
//         }
//
//         [MenuItem("Tools/Sol/Quick Setup/Standard Setup", priority = 11)]
//         public static void StandardSetup()
//         {
//             var config = SolSetupUtilities.GetRecommendedConfig(SolSetupUtilities.ProjectType.Standard);
//             PerformQuickSetup(config, "Standard");
//         }
//
//         [MenuItem("Tools/Sol/Quick Setup/Complete Setup", priority = 12)]
//         public static void CompleteSetup()
//         {
//             var config = SolSetupUtilities.GetRecommendedConfig(SolSetupUtilities.ProjectType.Complete);
//             PerformQuickSetup(config, "Complete");
//         }
//
//         [MenuItem("Tools/Sol/Validate Scene Setup", priority = 20)]
//         public static void ValidateSetup()
//         {
//             string validation = SolSetupUtilities.ValidateSceneSetup();
//             EditorUtility.DisplayDialog("Sol Scene Validation", validation, "OK");
//         }
//
//         private static void PerformQuickSetup(SolSetupWizard.SetupConfig config, string setupType)
//         {
//             bool proceed = EditorUtility.DisplayDialog(
//                 $"Sol {setupType} Setup",
//                 $"This will set up a {setupType.ToLower()} Sol configuration in your current scene. Continue?",
//                 "Yes", "Cancel"
//             );
//
//             if (proceed)
//             {
//                 try
//                 {
//                     // Actually perform the setup using the same logic as the wizard
//                     PerformSetupLogic(config);
//                     
//                     EditorUtility.DisplayDialog("Sol Setup Complete", 
//                         $"{setupType} setup completed successfully! Your Sol Time & Celestial System is ready to use.", 
//                         "OK");
//                 }
//                 catch (System.Exception e)
//                 {
//                     EditorUtility.DisplayDialog("Setup Error", 
//                         $"An error occurred during {setupType.ToLower()} setup: {e.Message}", 
//                         "OK");
//                 }
//             }
//         }
//
//         private static void PerformSetupLogic(SolSetupWizard.SetupConfig config)
//         {
//             WorldTimeData worldTimeData = null;
//
//             if (config.createWorldTimeData)
//             {
//                 worldTimeData = CreateWorldTimeData(config);
//             }
//
//             if (config.createTimeManager)
//             {
//                 CreateTimeManager(config, worldTimeData);
//             }
//
//             if (config.createSeasonalData)
//             {
//                 CreateSeasonalData(config);
//             }
//
//             if (config.createDirectionalLight)
//             {
//                 CreateDirectionalLight(config);
//             }
//
//             if (config.createDemoScene)
//             {
//                 CreateDemoScene(config);
//             }
//         }
//
//         // Implementation methods (same logic as wizard but static)
//         private static WorldTimeData CreateWorldTimeData(SolSetupWizard.SetupConfig config)
//         {
//             SolSetupUtilities.EnsureFolderExists(config.dataFolderPath);
//             
//             WorldTimeData worldTimeData = ScriptableObject.CreateInstance<WorldTimeData>();
//             // Configure with defaults...
//             
//             string assetPath = $"{config.dataFolderPath}/DefaultWorldTimeData.asset";
//             AssetDatabase.CreateAsset(worldTimeData, assetPath);
//             AssetDatabase.SaveAssets();
//             
//             Debug.Log($"[Sol Quick Setup] WorldTimeData created at {assetPath}");
//             return worldTimeData;
//         }
//
//         private static void CreateTimeManager(SolSetupWizard.SetupConfig config, WorldTimeData worldTimeData)
//         {
//             TimeManager existingTimeManager = Object.FindObjectOfType<TimeManager>();
//             if (existingTimeManager != null)
//             {
//                 Debug.Log("[Sol Quick Setup] TimeManager already exists in scene.");
//                 return;
//             }
//
//             GameObject timeManagerGO = new GameObject("TimeManager");
//             TimeManager timeManager = timeManagerGO.AddComponent<TimeManager>();
//
//             if (worldTimeData != null)
//             {
//                 // Assign WorldTimeData using reflection
//                 var worldTimeDataField = typeof(TimeManager).GetField("worldTimeData", 
//                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
//                 worldTimeDataField?.SetValue(timeManager, worldTimeData);
//             }
//
//             Debug.Log("[Sol Quick Setup] TimeManager created successfully.");
//         }
//
//         private static void CreateSeasonalData(SolSetupWizard.SetupConfig config)
//         {
//             SolSetupUtilities.EnsureFolderExists(config.dataFolderPath);
//             
//             SeasonalData seasonalData = ScriptableObject.CreateInstance<SeasonalData>();
//             // Configure seasonal data...
//             
//             string assetPath = $"{config.dataFolderPath}/DefaultSeasonalData.asset";
//             AssetDatabase.CreateAsset(seasonalData, assetPath);
//             AssetDatabase.SaveAssets();
//             
//             Debug.Log($"[Sol Quick Setup] Seasonal data created at {assetPath}");
//         }
//
//         private static void CreateDirectionalLight(SolSetupWizard.SetupConfig config)
//         {
//             Light existingLight = Object.FindObjectOfType<Light>();
//             if (existingLight != null && existingLight.type == LightType.Directional)
//             {
//                 if (existingLight.GetComponent<CelestialRotator>() == null)
//                 {
//                     existingLight.gameObject.AddComponent<CelestialRotator>();
//                 }
//                 return;
//             }
//
//             GameObject sunLightGO = new GameObject("Sun Light");
//             Light sunLight = sunLightGO.AddComponent<Light>();
//             sunLight.type = LightType.Directional;
//             sunLight.color = new Color(1f, 0.95f, 0.8f);
//             sunLight.intensity = 1.0f;
//             sunLight.shadows = LightShadows.Soft;
//
//             sunLightGO.AddComponent<CelestialRotator>();
//             
//             Debug.Log("[Sol Quick Setup] Directional light created successfully.");
//         }
//
//         private static void CreateDemoScene(SolSetupWizard.SetupConfig config)
//         {
//             var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
//                 UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, 
//                 UnityEditor.SceneManagement.NewSceneMode.Additive
//             );
//
//             // Create demo content
//             GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
//             ground.name = "Ground";
//             ground.transform.localScale = new Vector3(10, 1, 10);
//
//             GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//             cube.name = "Demo Cube";
//             cube.transform.position = new Vector3(0, 0.5f, 0);
//
//             GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//             sphere.name = "Demo Sphere";
//             sphere.transform.position = new Vector3(3, 0.5f, 0);
//
//             GameObject cameraGO = new GameObject("Demo Camera");
//             Camera demoCamera = cameraGO.AddComponent<Camera>();
//             demoCamera.transform.position = new Vector3(0, 2, -5);
//             demoCamera.transform.LookAt(Vector3.zero);
//             demoCamera.tag = "MainCamera";
//
//             // Save demo scene
//             string demoScenePath = "Assets/Sol/Scenes/SolDemo.unity";
//             SolSetupUtilities.EnsureFolderExists("Assets/Sol/Scenes");
//             UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, demoScenePath);
//
//             Debug.Log($"[Sol Quick Setup] Demo scene created: {demoScenePath}");
//         }
//     }
// }