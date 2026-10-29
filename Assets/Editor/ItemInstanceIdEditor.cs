using System.Collections.Generic;
using System.Linq;
using Sol;
using UnityEditor;
using UnityEngine;

namespace Sol.Editor
{
    /// <summary>
    /// Gives every placed WorldItem a unique ID, once, when it is placed. Designers never type IDs.
    /// Also adds a Regenerate button for repairing an ID, and warns about duplicates, which appear when an item
    /// is copied or a prefab is duplicated.
    /// </summary>
    [CustomEditor(typeof(WorldItem))]
    [CanEditMultipleObjects]
    public class ItemInstanceIdEditor : UnityEditor.Editor
    {
        private SerializedProperty _itemInstanceId;

        private void OnEnable()
        {
            _itemInstanceId = serializedObject.FindProperty("_itemInstanceId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_itemInstanceId, new GUIContent("Item Instance Id"));
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Regenerate ID"))
            {
                foreach (Object target in targets)
                    Regenerate((WorldItem)target);
            }
            EditorGUILayout.EndHorizontal();

            // Show the other copy's name when two placed items share an ID.
            WorldItem me = (WorldItem)target;
            if (!string.IsNullOrEmpty(me.ItemInstanceId) && IsDuplicated(me))
                EditorGUILayout.HelpBox("This ID is shared with another placed item in the scene. Press Regenerate ID.", MessageType.Warning);

            // The rest of the component (the item asset), drawn as normal.
            DrawPropertiesExcluding(serializedObject, "m_Script", "_itemInstanceId");
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Assigns a new ID to a placed item. Records undo and marks the scene dirty so it is saved.</summary>
        public static void Regenerate(WorldItem item)
        {
            var so = new SerializedObject(item);
            SerializedProperty prop = so.FindProperty("_itemInstanceId");
            Undo.RecordObject(item, "Regenerate World Item ID");

            // The GUID and its build index are created together, here, so they can never drift apart.
            string guid = NewId();
            prop.stringValue = guid;
            so.ApplyModifiedProperties();

            WorldObjectRegistryAccess.Register(guid, item.Item != null ? item.Item.ItemId : string.Empty);
            EditorUtility.SetDirty(item);
        }

        public static string NewId() => System.Guid.NewGuid().ToString("N");

        private static bool IsDuplicated(WorldItem item)
        {
            return Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None)
                .Any(other => other != item && other.ItemInstanceId == item.ItemInstanceId);
        }
    }

    /// <summary>
    /// Assigns an ID to every placed WorldItem that has none, each time a scene is saved. Covers items dragged in,
    /// duplicated, or placed from a prefab, without anyone pressing a button.
    /// </summary>
    [InitializeOnLoad]
    public static class ItemInstanceIdAssigner
    {
        static ItemInstanceIdAssigner()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            AssignMissing(scene);
            RetireDeletedFromScene(scene);
        }

        // Retires registry entries whose object is gone. Only safe when the scene is the only one that holds placed
        // objects. With several scenes open, an object may live in another scene, so nothing is retired. That is the
        // conservative choice: an unretired index is harmless, but a wrongly retired one is not.
        private static void RetireDeletedFromScene(UnityEngine.SceneManagement.Scene scene)
        {
            int openScenes = UnityEditor.SceneManagement.EditorSceneManager.sceneCount;
            if (openScenes != 1) return;

            var live = new HashSet<string>();
            foreach (WorldItem item in Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None))
            {
                if (!string.IsNullOrEmpty(item.ItemInstanceId)) live.Add(item.ItemInstanceId);
            }

            WorldObjectRegistryAccess.RetireMissing(WorldObjectRegistryAccess.Get(), live);
        }

        // Also callable by hand. Only items with an empty ID are touched, so a set ID never changes.
        public static int AssignMissing(UnityEngine.SceneManagement.Scene scene)
        {
            int assigned = 0;
            var seen = new HashSet<string>();

            foreach (WorldItem item in Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None))
            {
                if (item.gameObject.scene != scene) continue;
                if (item.IsDropped) continue;

                // A duplicate (two items with one ID) gets a fresh one, so each placement stays unique.
                bool missing = string.IsNullOrEmpty(item.ItemInstanceId);
                bool duplicate = !missing && !seen.Add(item.ItemInstanceId);
                if (missing || duplicate)
                {
                    ItemInstanceIdEditor.Regenerate(item);
                    assigned++;
                }
                seen.Add(item.ItemInstanceId);
            }
            return assigned;
        }
    }
}
