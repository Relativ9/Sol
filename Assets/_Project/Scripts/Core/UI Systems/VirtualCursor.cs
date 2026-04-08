using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
/// Virtual cursor that translates analog stick input into pointer events.
/// 
/// Architecture Notes:
/// - Implements IVirtualCursor for dependency injection and testing
/// - Depends on ICursorInput (not Input System directly) for flexibility
/// - Uses Pointer events (not Mouse events) because they're input-agnostic
///   and properly abstract mouse, gamepad, and touch inputs
/// - Synthesizes events that UI Toolkit elements receive exactly like real mouse events
/// </summary>
public class VirtualCursor : IVirtualCursor
{
    // Dependencies injected via constructor
    private readonly ICursorInput _input;
    private readonly float _speed;
    
    // Document management
    private readonly List<UIDocument> _registeredDocuments = new();
    private UIDocument _activeDocument;
    
    // Cursor state
    private VisualElement _cursorElement;
    private Vector2 _currentPosition; // Screen space (0,0 bottom-left)
    private VisualElement _lastHoveredElement; // For tracking enter/leave
    private bool _isEnabled = true;
    private StyleSheet _cursorStyleSheet;
    
    // Constants
    private const float CURSOR_SIZE = 32f;
    private const float CURSOR_OFFSET = 16f; // Half size to center on point
    private const float MOUSE_DETECTION_THRESHOLD = 0.1f;

    /// <summary>
    /// Constructs a VirtualCursor with its dependencies.
    /// Uses constructor injection for testability - pass a mock ICursorInput in tests.
    /// </summary>
    /// <param name="input">Input abstraction (Input System, touch, or mock)</param>
    /// <param name="speed">Movement speed in pixels per second</param>
    public VirtualCursor(ICursorInput input, StyleSheet cursorStyleSheet, float speed = 800f)
    {
        _input = input;
        _speed = speed;
        _cursorStyleSheet = cursorStyleSheet;
        _currentPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    /// <inheritdoc />
    public void RegisterDocument(UIDocument document)
    {
        if (document == null || _registeredDocuments.Contains(document))
            return;

        _registeredDocuments.Add(document);
        
        // Ensure cursor visual exists in this document
        // This allows the cursor to persist when switching between documents
        EnsureCursorInDocument(document);
    }

    /// <inheritdoc />
    public void UnregisterDocument(UIDocument document)
    {
        if (document == null) return;
        
        _registeredDocuments.Remove(document);
        
        // If we're removing the active document, clear it
        if (_activeDocument == document)
        {
            _activeDocument = null;
            HideCursor();
        }
    }

    /// <inheritdoc />
    public void SetActiveDocument(UIDocument document)
    {
        Debug.Log($"[VirtualCursor] SetActiveDocument called with: {document?.name ?? "NULL"}");
    
        if (document == null)
        {
            _activeDocument = null;
            HideCursor();
            return;
        }
        _activeDocument = document;
        Debug.Log($"[VirtualCursor] _activeDocument set to: {_activeDocument.name}");
    
        if (!_registeredDocuments.Contains(document))
        {
            _registeredDocuments.Add(document);
            Debug.Log($"[VirtualCursor] Document added to registered list");
        }
    
        EnsureCursorInDocument(document);
        Debug.Log($"[VirtualCursor] EnsureCursorInDocument complete, cursorElement null={_cursorElement == null}");
    
        _cursorElement.BringToFront();
        ShowCursor();
        Debug.Log($"[VirtualCursor] Cursor should now be visible");
    }

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (!enabled)
        {
            // Clear hover state when disabled to prevent stuck tooltips
            if (_lastHoveredElement != null)
            {
                SendPointerLeave(_lastHoveredElement, GetPanelPosition());
                _lastHoveredElement = null;
            }
            HideCursor();
        }
    }

    /// <summary>
    /// Main update loop. Called by VirtualCursorController each frame.
    /// Separated from MonoBehaviour to keep this class testable and free of Unity lifecycle dependencies.
    /// </summary>
    /// <param name="deltaTime">Unscaled delta time for consistent cursor speed regardless of game timescale</param>
    public void Update(float deltaTime)
    {
        if (!_isEnabled || _activeDocument == null)
        {
            Debug.Log($"[VirtualCursor] Skipping - enabled={_isEnabled} doc={_activeDocument?.name}");
            return;
        }
        // // Auto-detect physical mouse usage
        // if (_input.IsMouseBeingUsed())
        // {
        //     HandleMouseTakeover();
        //     return;
        // }
        //
        // // We have gamepad input - ensure cursor is active
        // if (_input.GetMovementDelta().magnitude > MOUSE_DETECTION_THRESHOLD)
        // {
        //     ShowCursor();
        // }

        ShowCursor();
        // Update position based on analog input
        UpdatePosition(deltaTime);
        
        // Handle raycasting and event synthesis
        HandleRaycastAndEvents();
        HandleClicks();
    }

    /// <summary>
    /// Creates the cursor visual element if it doesn't exist.
    /// The cursor is a simple VisualElement that follows the virtual position.
    /// </summary>
    private VisualElement CreateCursorVisual()
    {
        return new VisualElement
        {
            name = "virtual-cursor",
            pickingMode = PickingMode.Ignore // Still set in code - it's behaviour not appearance
        };
    }

    /// <summary>
    /// Ensures the cursor visual exists in the specified document.
    /// Called when registering documents to prepare them for cursor interaction.
    /// </summary>
    private void EnsureCursorInDocument(UIDocument document)
    {
        if (_cursorElement == null)
        {
            _cursorElement = CreateCursorVisual();
        }
    
        if (_cursorElement.parent != document.rootVisualElement)
        {
            // Only add stylesheet if one was provided
            if (_cursorStyleSheet != null)
                document.rootVisualElement.styleSheets.Add(_cursorStyleSheet);
            
            document.rootVisualElement.Add(_cursorElement);
            _cursorElement.BringToFront();
        }
    }

    /// <summary>
    /// Moves the cursor based on analog stick input.
    /// Operates in screen space, then converts to panel space for rendering.
    /// </summary>
    private void UpdatePosition(float deltaTime)
    {
        Vector2 input = _input.GetMovementDelta();
        
        // Scale input by speed and frame time for consistent movement
        Vector2 delta = input * _speed * deltaTime;
        _currentPosition += delta;

        // Clamp to screen bounds
        _currentPosition.x = Mathf.Clamp(_currentPosition.x, 0, Screen.width);
        _currentPosition.y = Mathf.Clamp(_currentPosition.y, 0, Screen.height);

        // Convert screen position to panel space for UI Toolkit
        Vector2 panelPosition = GetPanelPosition();

        // Update visual element position (centered on the point)
        _cursorElement.style.left = panelPosition.x - CURSOR_OFFSET;
        _cursorElement.style.top = panelPosition.y - CURSOR_OFFSET;
    }

    /// <summary>
    /// Converts current screen position to UI Toolkit panel coordinates.
    /// Accounts for PanelSettings scale mode (Constant Pixel Size vs Scale With Screen Size).
    /// </summary>
    /// <returns>Position in panel space (0,0 at top-left of document)</returns>
    private Vector2 GetPanelPosition()
    {
        if (_activeDocument?.panelSettings == null)
            return _currentPosition;
        float scale = _activeDocument.panelSettings.scale;
    
        // Flip Y: Screen space is bottom-up, UI Toolkit panel space is top-down
        float flippedY = Screen.height - _currentPosition.y;
    
        return new Vector2(_currentPosition.x / scale, flippedY / scale);
    }

    /// <summary>
    /// Handles raycasting into the UI and synthesizing hover events.
    /// Uses manual hierarchy traversal since panel.Pick() isn't publicly accessible.
    /// 
    /// Event Synthesis Strategy:
    /// - PointerEnterEvent: Fired when cursor moves over a new element
    /// - PointerLeaveEvent: Fired when cursor leaves an element
    /// - PointerMoveEvent: Fired continuously while over an element (for tooltip position updates)
    /// </summary>
    private void HandleRaycastAndEvents()
    {
        Vector2 panelPos = GetPanelPosition();
        VisualElement hoveredElement = PickElement(_activeDocument.rootVisualElement, panelPos);

        // Handle enter/leave transitions
        if (hoveredElement != _lastHoveredElement)
        {
            // Leave previous element
            if (_lastHoveredElement != null)
            {
                SendPointerLeave(_lastHoveredElement, panelPos);
            }

            // Enter new element
            if (hoveredElement != null)
            {
                SendPointerEnter(hoveredElement, panelPos);
            }

            _lastHoveredElement = hoveredElement;
        }

        // Continuous move events for tracking (used by tooltips for position updates)
        if (hoveredElement != null)
        {
            SendPointerMove(hoveredElement, panelPos);
        }
    }

    /// <summary>
    /// Handles click detection and synthesizes PointerDown/PointerUp/Click events.
    /// Separates primary (0) and secondary (1) button actions.
    /// </summary>
    private void HandleClicks()
    {
        Vector2 panelPos = GetPanelPosition();
        VisualElement target = PickElement(_activeDocument.rootVisualElement, panelPos);

        if (target == null) return;

        // Primary action (typically Allocate Point in your talent tree)
        if (_input.WasPrimaryPressed())
        {
            SynthesizeClick(target, panelPos, 0);
        }
        // Secondary action (typically Refund Point)
        else if (_input.WasSecondaryPressed())
        {
            SynthesizeClick(target, panelPos, 1);
        }
    }

    /// <summary>
    /// Manually traverses the visual tree to find the topmost element at the given position.
    /// Required because UI Toolkit doesn't expose a public raycast method.
    /// 
    /// Traversal order: Reverse child order (topmost rendered element first).
    /// Respects visibility and pickingMode settings.
    /// </summary>
    private VisualElement PickElement(VisualElement root, Vector2 position)
    {
        // Traverse children in reverse (top-most first in UI Toolkit rendering)
        for (int i = root.hierarchy.childCount - 1; i >= 0; i--)
        {
            var child = root.hierarchy[i];
            
            // Skip invisible or non-interactive elements
            if (!child.visible || child.pickingMode == PickingMode.Ignore)
                continue;

            // Check if position is within child's world bounds
            if (child.worldBound.Contains(position))
            {
                // Recurse into children to find the most specific element
                var result = PickElement(child, position);
                
                // Return deepest match, or this child if no deeper match found
                return result ?? child;
            }
        }
        return null;
    }
    

    /// <summary>
    /// Synthesizes a complete click gesture: PointerDown, (optional Click), PointerUp.
    /// Uses Pointer events for unified input abstraction across mouse/gamepad/touch.
    /// </summary>
    /// <param name="target">Element to receive the event</param>
    /// <param name="position">Panel-space position of the cursor</param>
    /// <param name="button">0 for primary, 1 for secondary (standard Pointer event button mapping)</param>
    private void SynthesizeClick(VisualElement target, Vector2 position, int button)
    {
        // UI Toolkit provides this factory method for synthetic events
        var downEvent = PointerDownEvent.GetPooled(
            new Event()
            {
                type = EventType.MouseDown,
                mousePosition = position,
                button = button
            }
        );
        target.SendEvent(downEvent);
        downEvent.Dispose();

        var upEvent = PointerUpEvent.GetPooled(
            new Event()
            {
                type = EventType.MouseUp,
                mousePosition = position,
                button = button
            }
        );
        target.SendEvent(upEvent);
        upEvent.Dispose();
    }
    
    private void SendPointerEnter(VisualElement element, Vector2 position)
    {
        if (element?.panel == null) return; // Guard against null panel
    
        var evt = PointerEnterEvent.GetPooled(new Event()
        {
            type = EventType.MouseMove,
            mousePosition = position
        });
        element.SendEvent(evt);
        evt.Dispose();
    }

    private void SendPointerLeave(VisualElement element, Vector2 position)
    {
        if (element?.panel == null) return;
    
        var evt = PointerLeaveEvent.GetPooled(new Event()
        {
            type = EventType.MouseMove,
            mousePosition = position
        });
        element.SendEvent(evt);
        evt.Dispose();
    }

    private void SendPointerMove(VisualElement element, Vector2 position)
    {
        if (element?.panel == null) return;
    
        var evt = PointerMoveEvent.GetPooled(new Event()
        {
            type = EventType.MouseMove,
            mousePosition = position
        });
        element.SendEvent(evt);
        evt.Dispose();
    }



    // /// <summary>
    // /// Sends a PointerEnterEvent to the specified element.
    // /// Used for hover states and triggering MouseEnterEvent handlers.
    // /// </summary>
    // private void SendPointerEnter(VisualElement element, Vector2 position)
    // {
    //     var evt = PointerEnterEvent.GetPooled(
    //         new Event()
    //         {
    //             type = EventType.MouseMove,
    //             mousePosition = position
    //         }
    //     );
    //     element.SendEvent(evt);
    //     evt.Dispose();
    // }
    //
    //
    // /// <summary>
    // /// Sends a PointerLeaveEvent to the specified element.
    // /// Used for clearing hover states and hiding tooltips.
    // /// </summary>
    // private void SendPointerLeave(VisualElement element, Vector2 position)
    // {
    //     var evt = PointerLeaveEvent.GetPooled(new Event()
    //     {
    //         type = EventType.MouseMove,
    //         mousePosition = position
    //     });
    //     element.SendEvent(evt);
    //     evt.Dispose();
    // }
    //
    //
    // /// <summary>
    // /// Sends a PointerMoveEvent to the specified element.
    // /// Sent continuously while hovering. Used by tooltips to track cursor position.
    // /// </summary>
    // private void SendPointerMove(VisualElement element, Vector2 position)
    // {
    //     var evt = PointerMoveEvent.GetPooled(new Event()
    //     {
    //         type = EventType.MouseMove,
    //         mousePosition = position
    //     });
    //     element.SendEvent(evt);
    //     evt.Dispose();
    // }

    
    /// <summary>
    /// Called when physical mouse movement is detected.
    /// Hides virtual cursor and clears hover states to prevent stuck tooltips.
    /// </summary>
    private void HandleMouseTakeover()
    {
        HideCursor();
        
        // Clear hover state so tooltips don't get stuck
        if (_lastHoveredElement != null)
        {
            SendPointerLeave(_lastHoveredElement, GetPanelPosition());
            _lastHoveredElement = null;
        }
    }

    private void ShowCursor() => _cursorElement.style.display = DisplayStyle.Flex;
    private void HideCursor() => _cursorElement.style.display = DisplayStyle.None;
}

}
