using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// MonoBehaviour wrapper for VirtualCursor.
    /// 
    /// Responsibilities:
    /// - Provides inspector-configurable values (speed, input asset)
    /// - Wires the lifecycle (Awake/Update/OnDestroy)
    /// - Registers the cursor with ServiceLocator for access by UIManager
    /// 
    /// Keeps VirtualCursor class pure (no Unity lifecycle dependencies) for testability.
    /// </summary>
    public class VirtualCursorController : MonoBehaviour
    {
        [Header("Input Configuration")]
        [SerializeField] private InputActionAsset _inputAsset;
        [Tooltip("Action map name in the Input Action asset (usually 'UI')")]
        [SerializeField] private string _actionMapName = "UI";
    
        [Header("Cursor Settings")]
        [SerializeField] private float _cursorSpeed = 800f;
        [SerializeField] private StyleSheet _cursorStyleSheet;
    
        [Header("Optional Visual")]
        [SerializeField] private Texture2D _cursorTexture; // If null, uses default box

        // The actual cursor implementation
        private VirtualCursor _cursor;
        private CursorInputActionMap _input;

        private void Awake()
        {
            // Create input abstraction
            _input = new CursorInputActionMap(_inputAsset);
            _cursor = new VirtualCursor(_input, _cursorStyleSheet, _cursorSpeed);
    
            Debug.LogError($"[VirtualCursorController] Registering cursor instance: {_cursor.GetHashCode()}");
            ServiceLocator.RegisterService<IVirtualCursor>(_cursor);
        }

        private void Update()
        {
            // Drive the update loop. Using unscaledDeltaTime ensures cursor 
            // remains responsive even when game is paused (timeScale = 0)
            _cursor?.Update(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            // Cleanup to prevent memory leaks and stale service references
            _input?.Dispose();
        }
    }
}