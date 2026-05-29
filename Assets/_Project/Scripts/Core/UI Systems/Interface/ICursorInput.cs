using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Abstracts input source for the virtual cursor.
    /// Allows swapping between Input System, legacy input, touch, or AI-driven input without changing cursor code.
    /// </summary>
    public interface ICursorInput
    {
        /// <summary>
        /// Returns the analog stick or directional input as a Vector2.
        /// Expected range: -1 to 1 on both axes. Will be scaled by cursor speed.
        /// </summary>
        Vector2 GetMovementDelta();

        /// <summary>
        /// true for mouse/touch false for gamepad/joystick/analogs
        /// </summary>
        /// <returns></returns>
        bool IsDeltaPointerDriven();
    
        /// <summary>
        /// Primary action (typically South button/Face Down on controllers, Left Click on mouse).
        /// </summary>
        bool WasPrimaryPressed();
    
        /// <summary>
        /// Secondary action (typically East button/Face Right on controllers, Right Click on mouse).
        /// Used for refunds, context menus, etc.
        /// </summary>
        bool WasSecondaryPressed();
    
        /// <summary>
        /// Detects if a physical mouse is currently being moved.
        /// When true, virtual cursor should hide and let real mouse take over.
        /// </summary>
        bool IsMouseBeingUsed();
    }
}