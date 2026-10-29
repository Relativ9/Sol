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
    private readonly float _mouseSensitivity;
    
    // Document management
    private readonly List<UIDocument> _registeredDocuments = new();
    private UIDocument _activeDocument;
    
    // Cursor state
    private VisualElement _cursorElement;
    private Vector2 _currentPosition; // Screen space (0,0 bottom-left)
    private VisualElement _lastHoveredElement; // For tracking enter/leave
    private bool _isEnabled = true;
    private StyleSheet _cursorStyleSheet;
    private Texture2D _cursorTexture; // Set in the controller's Inspector. Null means the default box.
    
    // Constants
    private const float CURSOR_SIZE = 32f;
    // The pointer is the image's top-left corner, so the image is placed with no offset.
    private const float CURSOR_OFFSET = 0f;
    private const float MOUSE_DETECTION_THRESHOLD = 0.1f;

    /// <summary>
    /// Constructs a VirtualCursor with its dependencies.
    /// Uses constructor injection for testability - pass a mock ICursorInput in tests.
    /// </summary>
    /// <param name="input">Input abstraction (Input System, touch, or mock)</param>
    /// <param name="speed">Movement speed in pixels per second</param>
    public VirtualCursor(ICursorInput input, StyleSheet cursorStyleSheet, Texture2D cursorTexture,
        float speed = 800f, float mouseSensitivity = 1f)
    {
        _input = input;
        _speed = speed;
        _mouseSensitivity = mouseSensitivity;
        _cursorStyleSheet = cursorStyleSheet;
        _cursorTexture = cursorTexture;
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
            ReleaseOsCursor();
        }
    }

    /// <inheritdoc />
    public int GetScrollNotches() => _input.GetScrollNotches();

    /// <inheritdoc />
    public bool IsOsCursorOwned { get; private set; }

    // The virtual cursor owns the OS cursor while it's active: hidden and locked, so the two can't both show.
    private void ClaimOsCursor()
    {
        IsOsCursorOwned = true;
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    private void ReleaseOsCursor()
    {
        IsOsCursorOwned = false;
    }

    /// <inheritdoc />
    public void SetActiveDocument(UIDocument document)
    {
        Debug.Log($"[VirtualCursor] SetActiveDocument called with: {document?.name ?? "NULL"}");
    
        if (document == null)
        {
            _cursorElement?.RemoveFromHierarchy();
            _activeDocument = null;
            ReleaseOsCursor();
            return;
        }

        // Claim the OS cursor immediately, so there's no frame where it shows before Update runs.
        ClaimOsCursor();

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
            // Debug.Log($"[VirtualCursor] Skipping - enabled={_isEnabled} doc={_activeDocument?.name}");
            return;
        }

        ShowCursor();
        // Re-assert every frame, so nothing else (camera, Cinemachine, the OS) can leave the OS cursor visible.
        ClaimOsCursor();
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
        var cursor = new VisualElement
        {
            name = "virtual-cursor",
            pickingMode = PickingMode.Ignore // Still set in code - it's behaviour not appearance
        };

        // The image comes from the controller's Inspector field, applied here. Null keeps the stylesheet's default box.
        if (_cursorTexture != null)
            cursor.style.backgroundImage = new StyleBackground(_cursorTexture);

        return cursor;
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

    private void UpdatePosition(float deltaTime)
    {

        Vector2 input = _input.GetMovementDelta();

        Vector2 delta = _input.IsDeltaPointerDriven() ? input * _mouseSensitivity : input * _speed * deltaTime;
        
        
        
        // Vector2 input = _input.GetMovementDelta();
        //
        // Vector2 delta = input * _speed * deltaTime;
        //
        // if (delta.magnitude > 100f && input.magnitude < 5f) Debug.LogError($"SHIT'S WRONG YO! input = {input}, deltatime={deltaTime}. delta = {delta}");
        //
         _currentPosition.x += delta.x;
         _currentPosition.y -= delta.y; // Flip Y: mouse delta is bottom-up, screen position tracks top-down
        
        _currentPosition.x = Mathf.Clamp(_currentPosition.x, 0, Screen.width);
         _currentPosition.y = Mathf.Clamp(_currentPosition.y, 0, Screen.height);
         Vector2 panelPosition = GetPanelPosition();
         _cursorElement.style.left = panelPosition.x - CURSOR_OFFSET;
         _cursorElement.style.top = panelPosition.y - CURSOR_OFFSET;
    }

    /// <summary>
    /// Converts current screen position to UI Toolkit panel coordinates.
    /// Accounts for PanelSettings scale mode (Constant Pixel Size vs Scale With Screen Size).
    /// </summary>
    /// <returns>Position in panel space (0,0 at top-left of document)</returns>
    /// <inheritdoc />
    public Vector2 PanelPosition => GetPanelPosition();

    private Vector2 GetPanelPosition()
    {
        if (_activeDocument?.rootVisualElement?.panel == null)
            return _currentPosition;
        // RuntimePanelUtils handles all coordinate space conversions:
        // - Screen resolution vs panel resolution
        // - PanelSettings scale modes
        // - Panel offset and padding
        return RuntimePanelUtils.ScreenToPanel(
            _activeDocument.rootVisualElement.panel,
            _currentPosition
        );

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
        if (hoveredElement != _lastHoveredElement)
        {
            // DO NOT send PointerEnterEvent or PointerLeaveEvent - they crash
            // due to PreDispatch accessing panel.cursorManager with unset pointerId
            // USS :hover handles visual feedback automatically
            _lastHoveredElement = hoveredElement;
        }
        // Move events are safe - only send those
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

        // Primary button: a real press, hold and release, so drag can work.
        // Press sends PointerDown to the element under the cursor and remembers it. While held, PointerMove
        // goes to the press element every frame. Release sends PointerUp to the element under the cursor. UI
        // Toolkit raises ClickEvent when the press and release land on the same element, so plain clicks work
        // as before (talent tree, buttons).
        if (_input.WasPrimaryPressed())
        {
            VisualElement pressTarget = PickElement(_activeDocument.rootVisualElement, panelPos);
            if (pressTarget != null)
            {
                _pressedElement = pressTarget;
                SendPointerEvent(pressTarget, panelPos, EventType.MouseDown, 0, isDown: true);
            }
        }
        else if (_pressedElement != null && _input.IsPrimaryHeld())
        {
            SendPointerMove(_pressedElement, panelPos);
        }
        else if (_pressedElement != null && _input.WasPrimaryReleased())
        {
            VisualElement releaseTarget = PickElement(_activeDocument.rootVisualElement, panelPos) ?? _pressedElement;
            SendPointerEvent(releaseTarget, panelPos, EventType.MouseUp, 0, isDown: false);
            _pressedElement = null;
        }

        // Secondary action (typically Refund Point). Still a single click; drag is primary only.
        if (_input.WasSecondaryPressed())
        {
            VisualElement target = PickElement(_activeDocument.rootVisualElement, panelPos);
            if (target != null) SynthesizeClick(target, panelPos, 1);
        }
    }

    // The element the primary button was pressed on. Drag events go here until release.
    private VisualElement _pressedElement;

    // Sends a pointer down or up event with the given button. Pooled events are disposed after sending.
    private void SendPointerEvent(VisualElement target, Vector2 position, EventType type, int button, bool isDown)
    {
        var raw = new Event { type = type, mousePosition = position, button = button };
        if (isDown)
        {
            var evt = PointerDownEvent.GetPooled(raw);
            target.SendEvent(evt);
            evt.Dispose();
        }
        else
        {
            var evt = PointerUpEvent.GetPooled(raw);
            target.SendEvent(evt);
            evt.Dispose();
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
