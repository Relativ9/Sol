using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sol
{
    /// <summary>
/// ICursorInput implementation using Unity's Input System.
/// Maps actions from an InputActionAsset.
/// 
/// Expected Action Names (configurable via consts below):
/// - Navigate: Vector2 for analog stick/d-pad movement
/// - Submit: Button for primary action
/// - Cancel: Button for secondary action
/// </summary>
public class CursorInputActionMap : ICursorInput, IDisposable
{
    // Action name constants - change these to match your Input Action asset
    private const string MOVE_ACTION_NAME = "Navigate";
    private const string PRIMARY_ACTION_NAME = "Submit";
    private const string SECONDARY_ACTION_NAME = "Cancel";
    private const string ACTION_MAP_NAME = "UI";

    private readonly InputAction _moveAction;
    private readonly InputAction _primaryAction;
    private readonly InputAction _secondaryAction;

    /// <summary>
    /// Constructs the input wrapper from an InputActionAsset.
    /// Finds the specified action map and caches the required actions.
    /// </summary>
    public CursorInputActionMap(InputActionAsset asset)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        var map = asset.FindActionMap("UI");
        if (map == null)
            throw new InvalidOperationException($"Action map '{ACTION_MAP_NAME}' not found in {asset.name}");

        _moveAction = map.FindAction("Point");
        _primaryAction = map.FindAction("Left Click");
        _secondaryAction = map.FindAction("Right Click");

        if (_moveAction == null || _primaryAction == null || _secondaryAction == null)
        {
            Debug.LogError($"Missing required actions. Need: {MOVE_ACTION_NAME}, {PRIMARY_ACTION_NAME}, {SECONDARY_ACTION_NAME}");
        }

        Enable();
    }

    public void Enable()
    {
        _moveAction?.Enable();
        _primaryAction?.Enable();
        _secondaryAction?.Enable();
    }

    public void Disable()
    {
        _moveAction?.Disable();
        _primaryAction?.Disable();
        _secondaryAction?.Disable();
    }

    public Vector2 GetMovementDelta()
    {
        if (Mouse.current != null && Mouse.current.delta.ReadValue().magnitude > 0.01f)
        {
            // Mouse gives delta directly - scale it down to match gamepad feel
            return Mouse.current.delta.ReadValue() * 0.1f;
        }
    
        // Gamepad stick gives -1 to 1
        return _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    public bool WasPrimaryPressed()
    {
        return _primaryAction?.WasPressedThisFrame() ?? false;
    }

    public bool WasSecondaryPressed()
    {
        return _secondaryAction?.WasPressedThisFrame() ?? false;
    }

    public bool IsMouseBeingUsed()
    {
        // Check if mouse delta exceeds threshold
        if (Mouse.current == null) return false;
        
        Vector2 delta = Mouse.current.delta.ReadValue();
        return delta.magnitude > 0.01f;
    }

    public void Dispose()
    {
        Disable();
    }
}
}
