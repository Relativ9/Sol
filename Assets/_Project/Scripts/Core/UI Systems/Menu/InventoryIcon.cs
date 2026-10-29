using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Draws an item icon in a UI Toolkit element. The icon is cropped to fill the box, not stretched, so a
    /// non-square icon keeps its proportions and the overflow is clipped. Returns null when there is no icon, so the
    /// caller can fall back to the item's name.
    /// </summary>
    public static class InventoryIcon
    {
        public static Image Create(Sprite icon)
        {
            if (icon == null) return null;

            var image = new Image
            {
                image = icon.texture,
                scaleMode = ScaleMode.ScaleAndCrop,
                pickingMode = PickingMode.Ignore,
            };
            image.style.flexGrow = 1f;
            image.style.flexShrink = 1f;
            return image;
        }
    }
}
