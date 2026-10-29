using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// Where each equipment slot sits on the character silhouette, and how big it is, in cells. One place for the
    /// layout, so the tab only draws. Sizes come from the design: head 2x2, torso 2x3, arms 1x2, legs 2x2, feet 1x2.
    /// Main and off hand are 1x1 when empty; a filled hand slot is drawn at the item's own footprint.
    /// Grid coordinates: x is across (0 = left), y is down (0 = top). The silhouette is 5 cells wide.
    /// </summary>
    public static class EquipmentSilhouette
    {
        public readonly struct SlotLayout
        {
            public readonly EquipmentSlot Slot;
            public readonly int X;
            public readonly int Y;
            public readonly int Width;
            public readonly int Height;

            public SlotLayout(EquipmentSlot slot, int x, int y, int width, int height)
            {
                Slot = slot;
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }

        // The silhouette is measured in HALF cells, so a slot can sit on a half-cell line. That lets the head, neck and
        // torso share one centre line even when their widths differ. Sizes below are in half cells too:
        // a 2 x 2 cell head is 4 x 4 here. Board size is 10 x 18 half cells, the same physical size as before.
        public const int SilhouetteWidth = 10;
        public const int SilhouetteHeight = 20;

        // Every centre-aligned piece below has its centre on column 5 (x + width/2 = 5). The head is 2 cells wide, the
        // neck 1 cell wide, and the torso 3 cells wide, so all three line up on the same vertical axis.
        /// <summary>
        /// The stored-grid footprint of an item that fits a slot, in cells. This is a rule by slot type, not a value set
        /// per item: every item that fits the torso is 3 x 3, however it is named or whatever its asset says.
        /// Slots with no footprint here (none at present) fall back to the item's own size.
        /// </summary>
        public static bool TryFootprintFor(EquipmentSlot slot, out int widthCells, out int heightCells)
        {
            switch (slot)
            {
                case EquipmentSlot.Torso:     widthCells = 3; heightCells = 3; return true;
                case EquipmentSlot.Legs:      widthCells = 2; heightCells = 3; return true;
                case EquipmentSlot.Head:      widthCells = 2; heightCells = 2; return true;
                case EquipmentSlot.Feet:      widthCells = 2; heightCells = 1; return true;
                case EquipmentSlot.Gauntlets: widthCells = 1; heightCells = 2; return true;
                case EquipmentSlot.MainHand:  widthCells = 1; heightCells = 2; return true;
                case EquipmentSlot.Offhand:   widthCells = 1; heightCells = 2; return true;
                case EquipmentSlot.Neck:      widthCells = 1; heightCells = 1; return true;
                case EquipmentSlot.RingLeft:  widthCells = 1; heightCells = 1; return true;
                case EquipmentSlot.RingRight: widthCells = 1; heightCells = 1; return true;
                default:                      widthCells = 0; heightCells = 0; return false;
            }
        }

        /// <summary>
        /// Stored footprint of a weapon, by its type and size. The type sets the base shape, and two-handed adds one row.
        /// Weapons are the exception to the slot rule: their equipped silhouette is a slot, but their stored size is set by
        /// what they are, so a long staff stays long in the bag even though it sits in a hand slot.
        /// </summary>
        public static void WeaponFootprint(WeaponType type, WeaponSize size, out int widthCells, out int heightCells)
        {
            switch (type)
            {
                case WeaponType.Blade:  widthCells = 1; heightCells = 2; break;
                case WeaponType.Blunt:  widthCells = 2; heightCells = 2; break;
                case WeaponType.Staff:  widthCells = 1; heightCells = 3; break;
                case WeaponType.Ranged: widthCells = 2; heightCells = 3; break;
                default:                widthCells = 1; heightCells = 2; break;
            }
            if (size == WeaponSize.TwoHanded) heightCells += 1;
        }

        /// <summary>
        /// Stored footprint of an offhand item. A shield, tower shield, focus or torch has a fixed size. A weapon held in the
        /// offhand is not an offhand item: it is sized by its own weapon type (see WeaponFootprint).
        /// </summary>
        public static void OffhandFootprint(OffhandType type, out int widthCells, out int heightCells)
        {
            switch (type)
            {
                case OffhandType.Shield:      widthCells = 2; heightCells = 2; break;
                case OffhandType.TowerShield: widthCells = 2; heightCells = 3; break;
                case OffhandType.Focus:       widthCells = 1; heightCells = 1; break;
                case OffhandType.Torch:       widthCells = 1; heightCells = 2; break;
                default:                      widthCells = 1; heightCells = 1; break;
            }
        }

        public static readonly IReadOnlyList<SlotLayout> Slots = new List<SlotLayout>
        {
            new SlotLayout(EquipmentSlot.Head,      x: 3, y: 0,  width: 4, height: 4),
            new SlotLayout(EquipmentSlot.Neck,      x: 4, y: 4,  width: 2, height: 2),
            new SlotLayout(EquipmentSlot.Torso,     x: 2, y: 6,  width: 6, height: 6),
            new SlotLayout(EquipmentSlot.Gauntlets, x: 0, y: 6,  width: 2, height: 4),
            new SlotLayout(EquipmentSlot.MainHand,  x: 0, y: 10, width: 2, height: 4),
            new SlotLayout(EquipmentSlot.RingLeft,  x: 8, y: 6,  width: 2, height: 2),
            new SlotLayout(EquipmentSlot.RingRight, x: 8, y: 8,  width: 2, height: 2),
            new SlotLayout(EquipmentSlot.Offhand,   x: 8, y: 10, width: 2, height: 4),
            // Legs are 2 cells wide by 3 cells tall (4 x 6 half cells), centred on the same axis as the torso.
            new SlotLayout(EquipmentSlot.Legs,      x: 3, y: 12, width: 4, height: 6),
            // Feet are 2 cells wide (4 half cells), centred under the legs.
            new SlotLayout(EquipmentSlot.Feet,      x: 3, y: 18, width: 4, height: 2),
        };
    }
}
