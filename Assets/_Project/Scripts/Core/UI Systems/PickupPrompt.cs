using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Shows the name of the item the player can pick up, just above the virtual cursor. It is a child of the document
    /// root and placed in panel space, the same as the cursor, and brought to front so the menu and HUD do not hide it.
    /// It reads only the current target and the cursor position. It does not know how pickup works.
    /// </summary>
    public class PickupPrompt
    {
        private const float LiftAbovePointer = 28f;

        private readonly VisualElement _root;
        private readonly Label _label;

        public PickupPrompt(VisualElement documentRoot)
        {
            _root = documentRoot;

            _label = new Label
            {
                name = "pickup-prompt",
                pickingMode = PickingMode.Ignore,
            };
            _label.style.position = Position.Absolute;
            _label.style.display = DisplayStyle.None;
            _label.style.paddingLeft = 6f;
            _label.style.paddingRight = 6f;
            _label.style.paddingTop = 2f;
            _label.style.paddingBottom = 2f;
            _label.style.fontSize = 13f;
            _label.style.color = new Color(1f, 0.95f, 0.8f);
            _label.style.backgroundColor = new Color(0.08f, 0.06f, 0.04f, 0.85f);
            _label.style.borderTopLeftRadius = 4f;
            _label.style.borderTopRightRadius = 4f;
            _label.style.borderBottomLeftRadius = 4f;
            _label.style.borderBottomRightRadius = 4f;
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _label.style.whiteSpace = WhiteSpace.NoWrap;

            _root.Add(_label);
        }

        /// <summary>Shows the item name above the cursor, or hides the prompt when there is no target.</summary>
        public void Show(string itemName, Vector2 cursorPanelPosition)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                _label.style.display = DisplayStyle.None;
                return;
            }

            _label.text = itemName;
            _label.style.display = DisplayStyle.Flex;

            // Centre the label horizontally on the pointer and lift it above the cursor visual.
            _label.style.left = cursorPanelPosition.x - _label.resolvedStyle.width * 0.5f;
            _label.style.top = cursorPanelPosition.y - LiftAbovePointer;
            _label.BringToFront();
        }

        public void Dispose()
        {
            _label.RemoveFromHierarchy();
        }
    }
}
