using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Inventory tab of the K menu. Equipment slots sit on the left, the stored grid on the right. Items show their
    /// real footprint, so a 2-handed sword covers 3 cells and a plate tunic 2 x 3.
    ///
    /// First draft controls: click an item to select it, click an empty cell to move it there, and press Equip on
    /// the selected item. Unequip is a button beside each worn slot. Drag and drop comes later.
    ///
    /// Services are resolved in OnShow, not Build. Build runs when the menu starts, which can be before the
    /// inventory and equipment services are registered.
    /// </summary>
    public class InventoryMenuTab : IMenuTab
    {
        public string Title => "Inventory";
        public string ContentName => "inventory-tab-content";

        // The layout is a serialized asset, handed in by UIManager (the same way the stats tab receives its layout).
        private readonly VisualTreeAsset _layout;

        public InventoryMenuTab(VisualTreeAsset layout)
        {
            _layout = layout;
        }

        // The stored grid's cell. 1.5 times the original 40px, so items are easier to read. The equipment slots below are
        // sized from their own base, so they do not change with this.
        private const float CellSize = 60f;
        private const float CellGap = 3f;

        // Each drawn cell has this margin on every side (the .inventory-cell rule). It is half the gap between cells, so
        // an item block covers its cells exactly and the two stay aligned.
        private const float CellMargin = CellGap / 2f;

        // The equipment silhouette uses the same cell and gap as the stored grid, so both read as one size of slot.
        private const float SlotCellSize = CellSize;
        private const float SlotCellGap = CellGap;

        // Distance from one silhouette cell to the next.
        private const float SlotPitch = SlotCellSize + SlotCellGap;

        // The silhouette is laid out in HALF cells (see EquipmentSilhouette). So one unit is half a full cell's pitch.
        // Using the half pitch keeps the board the same physical size it was, with the finer grid inside it.
        private const float HalfSlotPitch = SlotPitch / 2f;
        private const float HalfSlotCell = SlotCellSize / 2f;
        private const float HalfSlotGap = SlotCellGap / 2f;

        private IInventoryService _inventory;
        private IEquipmentService _equipment;
        private ItemMover _mover;

        private VisualElement _gridRoot;
        private VisualElement _slotRoot;
        private Label _status;
        private Label _weightLabel;

        private bool _subscribed;

        // The layout is authored in InventoryTab.uxml and styled by MenuTheme.uss. Cloning it fills this tab's content
        // element. This method only finds the named containers and fills them; the interaction code is unchanged.
        public void Build(VisualElement content)
        {
            content.Clear();
            _root = content;

            if (_layout == null)
            {
                Debug.LogError($"[{nameof(InventoryMenuTab)}] No layout assigned on UIManager. The inventory tab cannot be built.");
                return;
            }
            _layout.CloneTree(content);

            _weightLabel = content.Q<Label>("inventory-weight");
            _status = content.Q<Label>("inventory-status");
            _slotRoot = content.Q("inventory-slot-board");
            _gridRoot = content.Q("inventory-grid");
            _details = content.Q("inventory-details");

            // The tooltip floats above the tab, so it lives on the tab root rather than inside a column.
            _tooltip = Text("", 12, new Color(1f, 0.95f, 0.8f));
            _tooltip.AddToClassList("menu-tooltip");
            _tooltip.style.display = DisplayStyle.None;
            content.Add(_tooltip);

            UpdateDetails(null);
        }

        // The former pixel-placed layout, kept out of the build path. Not called.
        private void BuildLegacyLayoutUnused(VisualElement content)
        {
            _root = content;

            // Fills the tab area; margins come from the stylesheet rather than absolute offsets.
            var column = new VisualElement { name = "inventory-column" };
            column.AddToClassList("inventory-root");
            content.Add(column);

            _weightLabel = Text("", 14, new Color(0.9f, 0.9f, 0.9f));
            column.Add(_weightLabel);

            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.alignItems = Align.Center;
            column.Add(actionRow);

            _status = Text("", 13, new Color(1f, 0.8f, 0.5f));
            _status.style.minHeight = 20f;
            _status.style.flexGrow = 1f;
            actionRow.Add(_status);


            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            body.style.flexShrink = 1f;
            body.style.flexBasis = 0f;
            body.style.minHeight = 0f;
            column.Add(body);

            // The silhouette sits in its own vertical scroll, so a tall layout never runs past the panel.
            var slotScroll = new ScrollView(ScrollViewMode.Vertical) { name = "inventory-slot-scroll" };
            slotScroll.style.width = EquipmentSilhouette.SilhouetteWidth * HalfSlotPitch + 1 + 16f;
            slotScroll.style.marginRight = 24f;
            slotScroll.style.flexShrink = 0f;
            slotScroll.style.minHeight = 0f;
            body.Add(slotScroll);

            _slotRoot = new VisualElement();
            _slotRoot.style.width = EquipmentSilhouette.SilhouetteWidth * HalfSlotPitch + 1;
            slotScroll.Add(_slotRoot);

            var gridScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal) { name = "inventory-grid-scroll" };
            // The grid takes whatever width is left. minWidth 0 lets it shrink below its content, so it scrolls instead of
            // pushing the details panel off the edge.
            gridScroll.style.flexGrow = 1f;
            gridScroll.style.flexShrink = 1f;
            gridScroll.style.flexBasis = 0f;
            gridScroll.style.minWidth = 0f;
            gridScroll.style.minHeight = 0f;
            body.Add(gridScroll);

            _gridRoot = new VisualElement();
            gridScroll.Add(_gridRoot);

            // The details panel sits on the right, with a fixed width so the grid and silhouette keep their space.
            // A scrolling panel: long descriptions and many stats scroll inside it rather than running past the edge.
            _details = new ScrollView(ScrollViewMode.Vertical) { name = "inventory-details" };
            _details.style.width = 240f;
            _details.style.minWidth = 180f;
            _details.style.marginLeft = 24f;
            _details.style.paddingLeft = 10f;
            _details.style.paddingRight = 10f;
            _details.style.paddingTop = 10f;
            _details.style.flexShrink = 1f;
            _details.style.backgroundColor = new Color(0.1f, 0.08f, 0.06f, 0.9f);
            _details.style.borderTopLeftRadius = 4f;
            _details.style.borderTopRightRadius = 4f;
            _details.style.borderBottomLeftRadius = 4f;
            _details.style.borderBottomRightRadius = 4f;
            body.Add(_details);

            // A floating label that appears over a hovered item. It ignores the pointer so it never steals hover.
            _tooltip = Text("", 12, new Color(1f, 0.95f, 0.8f));
            _tooltip.pickingMode = PickingMode.Ignore;
            _tooltip.style.position = Position.Absolute;
            _tooltip.style.backgroundColor = new Color(0.05f, 0.04f, 0.03f, 0.92f);
            _tooltip.style.paddingLeft = 6f;
            _tooltip.style.paddingRight = 6f;
            _tooltip.style.paddingTop = 2f;
            _tooltip.style.paddingBottom = 2f;
            _tooltip.style.display = DisplayStyle.None;
            content.Add(_tooltip);

            UpdateDetails(null);
        }

        public void OnShow()
        {
            _inventory = ServiceLocator.TryGet<IInventoryService>();
            _equipment = ServiceLocator.TryGet<IEquipmentService>();
            if (_inventory == null || _equipment == null)
            {
                _status.text = "Inventory is not ready.";
                return;
            }

            _mover = new ItemMover(_inventory, _equipment);

            // Redraw whenever the grid or the slots change, even while the tab is open. Without this, items stored
            // from outside the tab (debug tools, pickups) would only appear after switching tabs away and back.
            // Unsubscribed in OnHide so a closed tab does not keep redrawing.
            Unsubscribe();
            _inventory.Changed += Refresh;
            _equipment.Changed += Refresh;
            _subscribed = true;

            Refresh();
        }

        public void OnHide()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_inventory != null) _inventory.Changed -= Refresh;
            if (_equipment != null) _equipment.Changed -= Refresh;
            _subscribed = false;
        }

        private void Refresh()
        {
            if (_inventory == null || _equipment == null) return;

            _selectionOutlines.Clear();
            BuildSlots();
            BuildGrid();
            DropStaleFocus();
            ApplySelectionStyle();

            float stored = _inventory.GetStoredWeightKg();
            float worn = 0f;
            foreach (var item in _equipment.GetAllEquipped().Values)
                worn += item.Weight;
            _weightLabel.text = $"Carried: {stored + worn:0.0} kg (stored {stored:0.0}, worn {worn:0.0})";

        }

        // Equipment slots on the character silhouette. Each slot is a box at its layout position and size. A filled
        // slot shows the item name and an Unequip button; an empty one shows the slot name.
        // The silhouette is a flex grid: one row per silhouette row, and each row holds a cell per silhouette column.
        // Each slot occupies its own cell range, so the grid, not pixel offsets, decides where it sits and how big it is.
        // The silhouette board is a fixed-size box sized from the design's grid. Each slot sits at its grid position in
        // pixels. The board lives in a scroll view, so on a short panel it scrolls rather than overlapping its neighbours.
        private void BuildSlots()
        {
            _slotRoot.Clear();
            _slotRoot.style.width = EquipmentSilhouette.SilhouetteWidth * HalfSlotPitch;
            _slotRoot.style.height = EquipmentSilhouette.SilhouetteHeight * HalfSlotPitch;

            foreach (EquipmentSilhouette.SlotLayout layout in EquipmentSilhouette.Slots)
            {
                VisualElement box = BuildSlotBox(layout);
                box.style.position = Position.Absolute;
                // Position and size are in half-cell units. The gap is taken off each side so neighbours keep a small space.
                box.style.left = layout.X * HalfSlotPitch;
                box.style.top = layout.Y * HalfSlotPitch;
                box.style.width = layout.Width * HalfSlotPitch - HalfSlotGap;
                box.style.height = layout.Height * HalfSlotPitch - HalfSlotGap;
                _slotRoot.Add(box);
            }
        }

        private VisualElement BuildSlotBox(EquipmentSilhouette.SlotLayout layout)
        {
            EquipmentDataSO worn = _equipment.GetEquipped(layout.Slot);

            // A slot takes its silhouette span: its width in columns and its height in rows, as flex shares.
            var box = new VisualElement();
            box.userData = layout.Slot;
            box.AddToClassList("inventory-slot");
            if (worn != null) box.AddToClassList("inventory-slot-filled");

            SetBorder(box, new Color(0.55f, 0.42f, 0.2f));

            if (worn != null)
            {
                AddIconOrName(box, worn);
                TrackItemElement(EquippedInstanceId(layout.Slot), box);
            }
            else
            {
                // An empty slot shows its name, dimmed, so the player can see what goes there.
                var label = Text(SlotName(layout.Slot), 11, new Color(0.55f, 0.5f, 0.42f));
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.flexGrow = 1f;
                box.Add(label);
            }

            if (worn != null)
            {
                // A worn item can be dragged to another slot. Dragging starts on press, like a grid item.
                EquipmentSlot dragSlot = layout.Slot;
                box.RegisterCallback<PointerDownEvent>(evt => BeginSlotPress(evt, dragSlot, worn));
                box.RegisterCallback<PointerEnterEvent>(_ => SetHover(EquippedInstanceId(dragSlot), worn, box));
                box.RegisterCallback<PointerLeaveEvent>(_ => ClearHover());
                // No Unequip button: drag the item out of its slot onto the grid to take it off.
            }
            return box;
        }

        // Empty slots show a friendly name. Hands are labelled as hands, weapons as weapons.
        private static string SlotName(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Gauntlets => "Gauntlets",
                EquipmentSlot.Neck => "Neck",
                EquipmentSlot.RingLeft => "Ring",
                EquipmentSlot.RingRight => "Ring",
                EquipmentSlot.MainHand => "Main hand",
                EquipmentSlot.Offhand => "Off hand",
                _ => slot.ToString(),
            };
        }



        // The grid: one clickable cell per grid square, then each stored item drawn over its footprint.
        private void BuildGrid()
        {
            _gridRoot.Clear();
            int width = _inventory.GridWidth;
            int height = _inventory.GridHeight;

            // Cells are a flex grid of rows. Item overlays are the one thing placed by position, because one overlay
            // spans several cells; they are sized and placed from the same cell pitch, so they line up with the cells.
            var cellRows = new VisualElement[height];
            for (int y = 0; y < height; y++)
            {
                cellRows[y] = new VisualElement();
                cellRows[y].AddToClassList("inventory-grid-row");
                _gridRoot.Add(cellRows[y]);
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new VisualElement();
                    cell.userData = new Vector2Int(x, y);
                    cell.AddToClassList("inventory-cell");
                    SetBorder(cell, new Color(0.35f, 0.27f, 0.15f));
                    cellRows[y].Add(cell);
                }
            }

            foreach (var entry in _inventory.GetAllStored())
            {
                // One block per stack: only the lead is drawn. Its other members share the lead's cell and are not drawn.
                if (_inventory.StackLeadOf(entry.Key) != entry.Key) continue;
                InventoryGrid.Placement? placement = _inventory.GetPlacement(entry.Key);
                if (placement == null) continue;
                _gridRoot.Add(BuildItemBlock(entry.Key, entry.Value, placement.Value));
            }
        }

        // A stored item drawn over its footprint. Clicking it selects it; clicking it again clears the selection.
        private VisualElement BuildItemBlock(string instanceId, ItemDataSO item, InventoryGrid.Placement p)
        {
            var block = new VisualElement();
            block.style.position = Position.Absolute;
            block.pickingMode = PickingMode.Position;
            block.RegisterCallback<PointerDownEvent>(evt => BeginPress(evt, instanceId, item, DragSource.Grid, null));
            block.RegisterCallback<PointerEnterEvent>(_ => SetHover(instanceId, item, block));
            block.RegisterCallback<PointerLeaveEvent>(_ => ClearHover());
            // The cell's own margin (from the .inventory-cell rule) is added on each side, so the block covers exactly the
            // drawn cells of its footprint. Without it the block sits up and left of its cells.
            float margin = CellMargin;
            float pitch = CellSize + 2f * margin;
            block.style.left = p.X * pitch + margin;
            block.style.top = p.Y * pitch + margin;
            block.style.width = p.Width * CellSize + (p.Width - 1) * 2f * margin;
            block.style.height = p.Height * CellSize + (p.Height - 1) * 2f * margin;
            block.style.backgroundColor = new Color(0.3f, 0.22f, 0.12f, 0.95f);
            block.style.overflow = Overflow.Hidden;
            AddIconOrName(block, item);
            // The block is one stack, drawn from its lead, so its label is that stack's own size.
            AddCornerLabels(block, item, _inventory.CountOf(instanceId));
            TrackItemElement(instanceId, block);
            return block;
        }

        // Small corner labels on a stored item: weight bottom-left, and the stack count bottom-right when it is above 1.
        // The count is the number of individuals in this block's stack (see IInventoryService.CountOf).
        private static void AddCornerLabels(VisualElement block, ItemDataSO item, int stackCount)
        {
            var weight = Text($"{item.Weight:0.#}kg", 10, new Color(0.95f, 0.9f, 0.8f));
            weight.AddToClassList("item-corner-label");
            weight.AddToClassList("item-corner-left");
            block.Add(weight);

            if (stackCount > 1)
            {
                var stack = Text($"x{stackCount}", 10, new Color(1f, 0.95f, 0.7f));
                stack.AddToClassList("item-corner-label");
                stack.AddToClassList("item-corner-right");
                block.Add(stack);
            }
        }

        // Shows the item's icon, cropped to fill the box. With no icon, shows the name, so the item is still identifiable.
        private static void AddIconOrName(VisualElement box, ItemDataSO item)
        {
            Image icon = InventoryIcon.Create(item.Icon);
            if (icon != null)
            {
                box.Add(icon);
                return;
            }

            var name = Text(item.DisplayName, 12, new Color(0.95f, 0.95f, 0.95f));
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            name.style.whiteSpace = WhiteSpace.Normal;
            name.style.flexGrow = 1f;
            box.Add(name);
        }

        private static void SetBorder(VisualElement element, Color color)
        {
            element.style.borderTopWidth = 1f;
            element.style.borderBottomWidth = 1f;
            element.style.borderLeftWidth = 1f;
            element.style.borderRightWidth = 1f;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }
        // ---- Drag and drop -------------------------------------------------------------------------------------
        // Press an item to pick it up. While dragging, the target under the pointer is worked out every frame, and
        // the spot it would land on is highlighted. Release commits exactly that highlighted target. If there is no
        // target, the item stays where it was and the reason is shown.
        //
        // Targets:
        //   - An equipment slot the item is valid for: the slot lights up.
        //   - The grid: the pointer's cell is the aim point, and the nearest spot the item fits is used. Its cells
        //     light up. Nothing is changed until release.

        private enum DragSource { Grid, Slot }

        private string _dragId;
        private ItemDataSO _dragItem;
        private DragSource _dragFrom;
        // The slot a dragged item came from, when it was dragged out of an equipment slot.
        private EquipmentSlot? _dragFromSlot;
        private bool _dragRotated;

        // What the player has selected (click) and what the pointer is over. The details panel and the discard key
        // both read these. A hovered item wins for the details panel; the selection is kept when the pointer leaves.
        private string _selectedId;
        private ItemDataSO _selectedItem;
        private string _hoverId;
        private ItemDataSO _hoverItem;
        private VisualElement _details;
        private Label _tooltip;
        private VisualElement _hoverElement;

        // A press that has not become a drag yet. It becomes a drag only once held (see Tick), so a quick click is a
        // selection and nothing else.
        private const float HoldSeconds = 0.15f;
        private const float DragStartDistance = 8f;
        private bool _pressActive;
        private string _pressId;
        private ItemDataSO _pressItem;
        private DragSource _pressFrom;
        private EquipmentSlot? _pressSlot;
        private float _pressTime;
        private Vector2 _pressAt;

        // Where inside the dragged item the press happened. The ghost keeps this point under the pointer.
        private Vector2 _grabOffset;

        // One white outline per selectable item, toggled in place. Selecting never rebuilds the grid under the pointer.
        private readonly Dictionary<string, VisualElement> _selectionOutlines = new();
        private VisualElement _ghost;
        private VisualElement _ghostParent;
        private VisualElement _root;
        private VisualElement _preview;

        // The target the current pointer position would drop to. The preview and the commit on release both read this,
        // so the highlight and the result are the same by construction.
        private struct DropTarget
        {
            public bool Valid;
            public bool IsSlot;
            public EquipmentSlot Slot;
            public int GridX;
            public int GridY;
            public bool Rotated;
            public string Reason;
            public bool IsMerge;
            public string MergeInstanceId;
        }

        // Pressing an item only selects it and remembers what was pressed. Nothing is picked up yet: the drag starts in
        // Tick, once the button has been held or the pointer has moved, so a quick click stays a selection.
        private void BeginPress(PointerDownEvent evt, string instanceId, ItemDataSO item, DragSource from, EquipmentSlot? slot)
        {
            if (evt.button != 0 || _dragId != null) return;

            Select(instanceId, item);

            _pressActive = true;
            _pressId = instanceId;
            _pressItem = item;
            _pressFrom = from;
            _pressSlot = slot;
            _pressTime = Time.unscaledTime;
            _pressAt = evt.position;

            // Remember where inside the item the grab happened, so the item stays under the pointer at that same point.
            // Measured from the pressed element's top-left, in the same space the pointer uses.
            if (evt.target is VisualElement pressed)
            {
                Rect bounds = pressed.worldBound;
                _grabOffset = new Vector2(evt.position.x - bounds.xMin, evt.position.y - bounds.yMin);
            }
            else
            {
                _grabOffset = Vector2.zero;
            }

            // Unregister first, so repeated presses never stack the same callback.
            _root.UnregisterCallback<PointerUpEvent>(OnPressUp);
            _root.RegisterCallback<PointerUpEvent>(OnPressUp);
        }

        // Releasing before the drag starts ends the press. The item stays selected.
        private void OnPressUp(PointerUpEvent evt)
        {
            if (evt.button != 0) return;

            // A press released without becoming a drag is a click. Ctrl held on a stack opens the split popup. Ctrl is
            // read from the keyboard, since the virtual cursor's events do not carry modifiers.
            string id = _pressId;
            ItemDataSO item = _pressItem;
            bool fromGrid = _pressFrom == DragSource.Grid;
            bool wasStack = id != null && _inventory != null && _inventory.CountOf(id) > 1;
            ClearPress();

            if (IsCtrlHeld() && id != null && item != null && wasStack && fromGrid)
                OpenSplitMenu(id, item);
        }

        private void ClearPress()
        {
            _root?.UnregisterCallback<PointerUpEvent>(OnPressUp);
            _pressActive = false;
            _pressId = null;
            _pressItem = null;
            _pressSlot = null;
        }

        // Starts a drag from a press that has been held long enough. The ghost follows the pointer from here on.
        private void StartDrag(Vector2 at, string instanceId, ItemDataSO item, DragSource from, EquipmentSlot? slot)
        {
            if (_dragId != null) return;
            _dragFromSlot = slot;

            _dragId = instanceId;
            _dragItem = item;
            _dragFrom = from;
            _dragRotated = false;

            _ghost = new VisualElement();
            _ghost.pickingMode = PickingMode.Ignore;
            AddIconOrName(_ghost, item);
            _ghost.style.position = Position.Absolute;
            _ghost.style.width = item.GridWidth * CellSize + (item.GridWidth - 1) * CellGap;
            _ghost.style.height = item.GridHeight * CellSize + (item.GridHeight - 1) * CellGap;
            _ghost.style.backgroundColor = new Color(0.6f, 0.45f, 0.2f, 0.7f);
            _ghost.style.opacity = 0.85f;

            // The ghost lives on the panel's top-level element, so its position is in panel space, like the cursor.
            _ghostParent = _root.panel?.visualTree ?? _root;
            _ghostParent.Add(_ghost);

            _root.RegisterCallback<PointerUpEvent>(OnDragUp);

            UpdateDrag(at);
        }


        /// <summary>
        /// Called every frame by the owner (UIManager). Drives an active drag: follows the cursor, reads R, and refreshes
        /// the target. It does not depend on mouse-move events, which the virtual cursor only sends when it moves, so
        /// rotating with the mouse still is possible. Does nothing when no drag is active.
        /// </summary>
        public void Tick(IVirtualCursor cursor)
        {
            HandleDiscardKey();
            if (cursor == null) return;

            // A held press turns into a drag here. The press is cleared first, so the drag owns the release from now on.
            if (_pressActive && _dragId == null && PressIsDrag(cursor.PanelPosition))
            {
                string id = _pressId;
                ItemDataSO item = _pressItem;
                DragSource from = _pressFrom;
                EquipmentSlot? slot = _pressSlot;
                ClearPress();

                // Ctrl held while dragging a stack halves it on the spot. The smaller half (the lower count for an odd stack)
                // is the one carried. Ctrl is read from the keyboard, not the pointer event, since the virtual cursor's events
                // do not carry modifiers.
                if (IsCtrlHeld() && from == DragSource.Grid && _inventory != null && _inventory.CountOf(id) > 1)
                {
                    // The drag carries the whole stack's cell as usual. The split is recorded, not executed. The ghost shows the
                    // part that would move, and the grid does not change until the drop is accepted.
                    if (RecordSplitIntent(id))
                    {
                        _status.text = $"Drop to split {_splitAmount} off the stack. Drop elsewhere to cancel.";
                    }
                    StartDrag(cursor.PanelPosition, id, item, from, slot);
                    return;
                }

                StartDrag(cursor.PanelPosition, id, item, from, slot);
            }

            if (_dragId == null) return;
            UpdateDrag(cursor.PanelPosition);
        }

        // A press becomes a drag when the button has been held for HoldSeconds, or the pointer has moved DragStartDistance.
        private bool PressIsDrag(Vector2 at)
        {
            bool held = Time.unscaledTime - _pressTime >= HoldSeconds;
            bool moved = (at - _pressAt).magnitude >= DragStartDistance;
            return held || moved;
        }

        // Rotation is only available while an item is held: R turns it 90 degrees clockwise, looping. It is read from
        // the keyboard here because it is a drag-only action, not a global binding. Non-rotatable items ignore it.
        private void HandleRotateKey()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || !keyboard.rKey.wasPressedThisFrame) return;
            if (_dragItem == null || !_dragItem.Rotatable) return;

            _dragRotated = !_dragRotated;
        }

        // Runs on every move: follows the cursor with the ghost and refreshes the highlighted target.
        private void UpdateDrag(Vector2 at)
        {
            HandleRotateKey();
            ResizeGhost();
            MoveGhost(at);
            DropTarget target = ResolveTarget(at);
            ShowPreview(target);
            _status.text = target.Valid ? $"Drop: {DescribeTarget(target)}" : target.Reason;
        }

        // Works out what the pointer would drop onto. This one function feeds both the preview and the drop.
        private DropTarget ResolveTarget(Vector2 at)
        {
            // Equipment slots take priority, so a helm dropped on the head does not fall through to the grid.
            if (_slotRoot != null)
            {
                foreach (VisualElement box in _slotRoot.Children())
                {
                    if (box.userData is not EquipmentSlot slot || !box.worldBound.Contains(at)) continue;
                    return ResolveSlot(slot);
                }
            }

            // Grid: the item lands exactly where it is held. The cell is the one under the item's top-left corner, which is
            // the pointer minus the grab offset. No nearest-fit search: if the footprint is blocked, the drop is refused.
            if (_gridRoot != null && _inventory != null && _gridRoot.worldBound.Contains(at))
            {
                (int cellX, int cellY) = GridCellUnder(at);
                (int fw, int fh) = DragFootprint();

                // A grid drop onto a stack of the same item merges into it when the combined count fits. It is checked
                // before the footprint, because a merge needs no room of its own.
                string mergeTarget = MergeTargetAt(cellX, cellY);
                if (mergeTarget != null)
                {
                    return new DropTarget
                    {
                        Valid = true,
                        IsSlot = false,
                        IsMerge = true,
                        MergeInstanceId = mergeTarget,
                        GridX = cellX,
                        GridY = cellY,
                    };
                }

                bool[,] blocked = _inventory.BlockedCells(_dragId, cellX, cellY, fw, fh);
                bool fits = true;
                for (int dx = 0; dx < fw; dx++)
                    for (int dy = 0; dy < fh; dy++)
                        if (blocked[dx, dy]) fits = false;

                return new DropTarget
                {
                    Valid = fits,
                    IsSlot = false,
                    GridX = cellX,
                    GridY = cellY,
                    Rotated = _dragRotated,
                    Reason = fits ? null : "That spot is occupied. Make room or choose another spot.",
                };
            }

            return new DropTarget { Valid = false, Reason = "Drop onto the grid or a matching slot." };
        }

        // The stack a grid drag would merge into at this cell, or null. Only a grid item merges, only into an entry of
        // the same item, and only when the combined count is within the item's stack limit.
        private string MergeTargetAt(int x, int y)
        {
            if (_dragItem == null || _dragFrom != DragSource.Grid || _dragItem.MaxStackSize <= 1) return null;
            string owner = _inventory.OwnerAt(x, y);
            if (owner == null || owner == _dragId) return null;

            ItemDataSO target = _inventory.GetStored(owner);
            if (target == null || target.ItemId != _dragItem.ItemId) return null;

            int combined = _inventory.CountOf(owner) + _inventory.CountOf(_dragId);
            return combined <= _dragItem.MaxStackSize ? owner : null;
        }

        // The grid cell under the dragged item's top-left corner. The pointer minus the grab offset is that corner, so the
        // cell is taken from where the item actually is, not from where the pointer is. Clamped so a partly-off item
        // still maps to an edge cell, and the footprint check decides whether it fits.
        private (int X, int Y) GridCellUnder(Vector2 pointer)
        {
            Rect gridBounds = _gridRoot.worldBound;
            float pitch = CellSize + CellGap;
            Vector2 topLeft = pointer - _grabOffset;
            int cellX = Mathf.FloorToInt((topLeft.x - gridBounds.xMin) / pitch);
            int cellY = Mathf.FloorToInt((topLeft.y - gridBounds.yMin) / pitch);
            return (cellX, cellY);
        }

        // Checks a slot against the dragged item. An equipped item can move to another slot it fits, as long as that
        // slot is empty. Moving onto its own slot is a no-op and is refused.
        private DropTarget ResolveSlot(EquipmentSlot slot)
        {
            if (_dragFrom == DragSource.Slot && _dragFromSlot == slot)
                return new DropTarget { Valid = false, Reason = "It is already in that slot." };
            if (_dragItem is not EquipmentDataSO equipment)
                return new DropTarget { Valid = false, Reason = $"{_dragItem.DisplayName} cannot be worn." };
            if (equipment.validSlots == null || System.Array.IndexOf(equipment.validSlots, slot) < 0)
                return new DropTarget { Valid = false, Reason = $"{_dragItem.DisplayName} does not fit {SlotName(slot)}." };
            if (_equipment.GetEquipped(slot) != null)
                return new DropTarget { Valid = false, Reason = $"{SlotName(slot)} is already occupied." };

            return new DropTarget { Valid = true, IsSlot = true, Slot = slot };
        }

        private string DescribeTarget(DropTarget t)
        {
            if (t.IsMerge) return "merge into the stack";
            if (t.IsSlot) return $"equip to {SlotName(t.Slot)}";
            return $"place at row {t.GridY + 1}, column {t.GridX + 1}";
        }

        // Highlights where the item would land: the lit slot box, or the grid cells the footprint would cover.
        private void ShowPreview(DropTarget target)
        {
            ClearPreview();

            // Grid drops are previewed even when blocked, so the red cells show where the obstruction is. Slot drops with
            // no valid slot have nothing to show.
            if (!target.IsSlot && _inventory == null) return;
            if (target.IsSlot && !target.Valid) return;

            _preview = new VisualElement();
            _preview.pickingMode = PickingMode.Ignore;
            _preview.style.position = Position.Absolute;

            if (target.IsSlot)
            {
                _preview.style.backgroundColor = new Color(0.4f, 0.9f, 0.4f, 0.35f);
                SetBorder(_preview, new Color(0.5f, 1f, 0.5f));

                _preview.style.left = 0;
                _preview.style.top = 0;
                foreach (VisualElement box in _slotRoot.Children())
                {
                    if (box.userData is not EquipmentSlot s || s != target.Slot) continue;
                    _preview.style.left = box.style.left;
                    _preview.style.top = box.style.top;
                    _preview.style.width = box.style.width;
                    _preview.style.height = box.style.height;
                    break;
                }
                _slotRoot.Add(_preview);
            }
            else
            {
                ShowGridPreview(target);
            }
        }

        // Paints the footprint cell by cell: green where the cell is free, red where another item (or the edge) blocks it.
        // The overlay sits over the grid's own cells at the item's current origin, so it lines up under the held item.
        private void ShowGridPreview(DropTarget target)
        {
            if (target.IsMerge)
            {
                ShowMergePreview(target);
                return;
            }

            (int fw, int fh) = DragFootprint();
            bool[,] blocked = _inventory.BlockedCells(_dragId, target.GridX, target.GridY, fw, fh);

            // One container holds all the cell tints, positioned at the origin and sized to the footprint.
            _preview.style.position = Position.Absolute;
            _preview.style.left = target.GridX * (CellSize + CellGap);
            _preview.style.top = target.GridY * (CellSize + CellGap);
            _preview.style.width = fw * CellSize + (fw - 1) * CellGap;
            _preview.style.height = fh * CellSize + (fh - 1) * CellGap;
            _preview.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _preview.style.borderTopWidth = 0;
            _preview.style.borderBottomWidth = 0;
            _preview.style.borderLeftWidth = 0;
            _preview.style.borderRightWidth = 0;

            for (int dy = 0; dy < fh; dy++)
            {
                for (int dx = 0; dx < fw; dx++)
                {
                    var tint = new VisualElement { pickingMode = PickingMode.Ignore };
                    tint.style.position = Position.Absolute;
                    tint.style.left = dx * (CellSize + CellGap);
                    tint.style.top = dy * (CellSize + CellGap);
                    tint.style.width = CellSize;
                    tint.style.height = CellSize;
                    tint.style.backgroundColor = blocked[dx, dy]
                        ? new Color(0.95f, 0.2f, 0.2f, 0.45f)   // Red: occupied or off the grid. The drop is refused.
                        : new Color(0.4f, 0.9f, 0.4f, 0.35f);   // Green: free.
                    _preview.Add(tint);
                }
            }
            _gridRoot.Add(_preview);
        }

        // A merge is shown as one gold outline over the stack it will join, so it reads differently from a blocked or
        // free placement. The outline uses the target's own footprint, not the dragged item's.
        private void ShowMergePreview(DropTarget target)
        {
            ItemDataSO stack = _inventory.GetStored(target.MergeInstanceId);
            if (stack == null) return;
            int fw = stack.GridWidth;
            int fh = stack.GridHeight;

            _preview.style.position = Position.Absolute;
            _preview.style.left = target.GridX * (CellSize + CellGap);
            _preview.style.top = target.GridY * (CellSize + CellGap);
            _preview.style.width = fw * CellSize + (fw - 1) * CellGap;
            _preview.style.height = fh * CellSize + (fh - 1) * CellGap;
            _preview.style.backgroundColor = new Color(0.95f, 0.8f, 0.3f, 0.25f);   // Gold wash: the stack gains items.
            _preview.style.borderTopWidth = 2;
            _preview.style.borderBottomWidth = 2;
            _preview.style.borderLeftWidth = 2;
            _preview.style.borderRightWidth = 2;
            _preview.style.borderTopColor = new Color(0.98f, 0.85f, 0.4f, 0.95f);
            _preview.style.borderBottomColor = new Color(0.98f, 0.85f, 0.4f, 0.95f);
            _preview.style.borderLeftColor = new Color(0.98f, 0.85f, 0.4f, 0.95f);
            _preview.style.borderRightColor = new Color(0.98f, 0.85f, 0.4f, 0.95f);
            _gridRoot.Add(_preview);
        }

        private void ClearPreview()
        {
            _preview?.RemoveFromHierarchy();
            _preview = null;
        }

        // The footprint of the dragged item in its current orientation. Preview, ghost and commit all read this one
        // helper, so they always agree on the size.
        private (int Width, int Height) DragFootprint()
        {
            if (_dragItem == null) return (1, 1);
            return _dragRotated
                ? (_dragItem.GridHeight, _dragItem.GridWidth)
                : (_dragItem.GridWidth, _dragItem.GridHeight);
        }

        // Resizes the ghost to the dragged item's current footprint, so a rotation is visible while held.
        private void ResizeGhost()
        {
            if (_ghost == null) return;
            (int w, int h) = DragFootprint();
            _ghost.style.width = w * CellSize + (w - 1) * CellGap;
            _ghost.style.height = h * CellSize + (h - 1) * CellGap;
        }

        private void MoveGhost(Vector2 at)
        {
            if (_ghost == null) return;
            // The item is placed so the point it was grabbed at stays under the pointer. Everything here is in screen space
            // (the pointer, the grab offset, and the grid bounds), so the ghost's top-left is converted to panel space
            // only at the point of drawing. This keeps the ghost and the drop cell measured in the same space.
            Vector2 topLeftScreen = at - _grabOffset;
            Vector2 topLeftPanel = RuntimePanelUtils.ScreenToPanel(_root.panel, topLeftScreen);
            _ghost.style.left = topLeftPanel.x;
            _ghost.style.top = topLeftPanel.y;
        }

        // Commits the target shown on release. It is recomputed at the release point, so it matches the last preview.
        private void OnDragUp(PointerUpEvent evt)
        {
            if (evt.button != 0 || _dragId == null) return;

            string id = _dragId;
            EquipmentSlot? fromSlot = _dragFrom == DragSource.Slot ? _dragFromSlot : null;
            DropTarget target = ResolveTarget(evt.position);

            // Read the split intent before the drag ends, since ending the drag clears it.
            int splitAmount = _splitAmount;
            string splitSource = _splitSourceId;
            EndDragVisual();

            if (!target.Valid)
            {
                _status.text = $"{target.Reason} The item went back.";
                Refresh();
                return;
            }

            // A Ctrl-drag split is carried out only now, on an accepted grid drop. Anything else cancels it, and the stack
            // stays whole. The split is one call that checks the cell before it changes anything.
            if (splitAmount > 0)
            {
                bool split = !target.IsSlot && !target.IsMerge
                    && _inventory.TrySplitOnto(splitSource, splitAmount, target.GridX, target.GridY, target.Rotated, out _);
                _status.text = split ? $"Split {splitAmount} off the stack." : "The split was not placed. The stack is unchanged.";
                Refresh();
                return;
            }

            bool ok;
            if (target.IsMerge)
                ok = _inventory.TryMerge(id, target.MergeInstanceId);
            else if (target.IsSlot && fromSlot != null)
                ok = _mover.TryMoveBetweenSlots(fromSlot.Value, target.Slot);
            else if (target.IsSlot)
                ok = _mover.TryEquipFromGrid(id, target.Slot);
            else if (fromSlot != null)
                ok = _mover.TryUnequipToGridAt(fromSlot.Value, target.GridX, target.GridY, target.Rotated);
            else
                ok = _inventory.TryMove(id, target.GridX, target.GridY, target.Rotated);

            _status.text = ok ? $"Done: {DescribeTarget(target)}." : "That could not be done. The item went back.";
            if (ok && target.IsMerge)
                _status.text = $"Merged into the stack. It now holds {_inventory.CountOf(target.MergeInstanceId)}.";
            Refresh();
        }

        // ---- Split a stack --------------------------------------------------------------------------------------------

        private VisualElement _splitPanel;

        // Control-press on a stack. A slider takes some of the count off into a new entry. The original keeps the rest,
        // so the split can never leave an empty or over-full stack.
        private void OpenSplitMenu(string instanceId, ItemDataSO item)
        {
            CloseSplitMenu();
            int total = _inventory.CountOf(instanceId);

            _splitPanel = new VisualElement { name = "inventory-split" };
            _splitPanel.AddToClassList("split-panel");

            var title = Text($"Split {item.DisplayName}", 14, new Color(0.98f, 0.92f, 0.8f));
            var readout = Text("", 13, new Color(0.9f, 0.9f, 0.85f));

            var slider = new SliderInt(1, total - 1) { value = 1 };
            slider.AddToClassList("split-slider");
            void UpdateReadout(int split)
            {
                readout.text = $"Split off {split}, keep {total - split}";
            }
            slider.RegisterValueChangedCallback(e => UpdateReadout(e.newValue));
            UpdateReadout(1);

            var splitButton = new Button(() =>
            {
                bool ok = _inventory.TrySplit(instanceId, slider.value, out _);
                _status.text = ok ? $"Split {slider.value} {item.DisplayName} off the stack." : "There is no room to split the stack.";
                CloseSplitMenu();
                Refresh();
            }) { text = "Split" };
            splitButton.AddToClassList("menu-button");

            var cancelButton = new Button(CloseSplitMenu) { text = "Cancel" };
            cancelButton.AddToClassList("menu-button");

            var buttons = new VisualElement();
            buttons.AddToClassList("split-buttons");
            buttons.Add(splitButton);
            buttons.Add(cancelButton);

            _splitPanel.Add(title);
            _splitPanel.Add(slider);
            _splitPanel.Add(readout);
            _splitPanel.Add(buttons);
            _root.Add(_splitPanel);
        }

        // Halves a stack for a Ctrl-drag. The smaller half moves (the lower count for an odd stack), into its own stack.
        // Returns the new stack's lead for the drag to carry, or null if the split could not be made.
        // A Ctrl-drag on a stack records the split it asks for: the smaller half, from this stack. Nothing is changed in the
        // inventory here. The split happens only if the drop is accepted (see OnDragUp). Returns false if there is no
        // valid split, such as a stack of one.
        private bool RecordSplitIntent(string instanceId)
        {
            int total = _inventory.CountOf(instanceId);
            int moving = total / 2;
            if (moving <= 0) return false;

            string lead = _inventory.StackLeadOf(instanceId);
            if (lead == null) return false;

            _splitSourceId = lead;
            _splitAmount = moving;
            return true;
        }

        // The split the current drag will carry out on drop. Zero when the drag is not a split.
        private int _splitAmount;
        private string _splitSourceId;

        private void ClearSplitIntent()
        {
            _splitAmount = 0;
            _splitSourceId = null;
        }

        private void CloseSplitMenu()
        {
            _splitPanel?.RemoveFromHierarchy();
            _splitPanel = null;
        }

        // Clears all drag state and the ghost and preview, whether the drop succeeded or not.
        private void EndDragVisual()
        {
            // The split intent belongs to this drag only. It is cleared here, so no later drag can inherit it.
            ClearSplitIntent();
            _root?.UnregisterCallback<PointerUpEvent>(OnDragUp);
            _ghost?.RemoveFromHierarchy();
            _ghost = null;
            ClearPreview();
            _dragId = null;
            _dragItem = null;
        }

        // ---- Selection, hover and focus ------------------------------------------------------------------------------

        // Pointer enters an item: it becomes the hovered item. The details panel and tooltip follow it.
        private void SetHover(string instanceId, ItemDataSO item, VisualElement element)
        {
            _hoverId = instanceId;
            _hoverItem = item;
            _hoverElement = element;
            UpdateDetailsAndTooltip();
        }

        private void ClearHover()
        {
            _hoverId = null;
            _hoverItem = null;
            _hoverElement = null;
            UpdateDetailsAndTooltip();
        }

        // The instance ID of the item in an equipment slot, or null.
        private string EquippedInstanceId(EquipmentSlot slot)
        {
            return _equipment?.GetEquippedInstanceId(slot);
        }

        // A worn item is pressed from its slot. The slot is remembered, so a move can start from it.
        private void BeginSlotPress(PointerDownEvent evt, EquipmentSlot slot, EquipmentDataSO worn)
        {
            string instanceId = EquippedInstanceId(slot);
            if (string.IsNullOrEmpty(instanceId)) return;
            BeginPress(evt, instanceId, worn, DragSource.Slot, slot);
        }

        // Click selects. Selection is what the details panel and the discard key act on when nothing is hovered.
        private void Select(string instanceId, ItemDataSO item)
        {
            _selectedId = instanceId;
            _selectedItem = item;
            ApplySelectionStyle();
            UpdateDetailsAndTooltip();
        }

        // One line of the details panel, with a comparison when the selected item has the same value.
        // The hovered item's value is shown, then the difference from the selection in brackets: green when this item is
        // higher, red when lower, and "=" when equal. With nothing to compare, the line is shown plainly.
        private void AddComparedLine(string label, float value, float? other, string format, Color plain, string suffix = "")
        {
            string shown = $"{label} {value.ToString(format)}{suffix}";
            if (other == null)
            {
                _details.Add(Text(shown, 12, plain));
                return;
            }

            float delta = value - other.Value;
            if (Mathf.Approximately(delta, 0f))
            {
                _details.Add(Text($"{shown}  (=)", 12, plain));
                return;
            }

            Color colour = delta > 0f ? new Color(0.45f, 0.95f, 0.45f) : new Color(1f, 0.4f, 0.4f);
            string sign = delta > 0f ? "+" : "";
            const string deltaSymbol = "\u0394";
            _details.Add(Text($"{shown}  ({deltaSymbol}{sign}{delta.ToString(format)}{suffix})", 12, colour));
        }

        // The elemental damage an equipment item adds for one damage type, or null if it does not add that type.
        private static float? ElementalOf(EquipmentDataSO item, DamageType type)
        {
            foreach (EquipmentDataSO.ElementalAttack elemental in item.elementalAttacks)
            {
                if (elemental != null && elemental.damageType == type) return elemental.damage;
            }
            return null;
        }

        // The value an item grants for one stat and modifier type, if it grants that stat at all.
        private static bool TryGetModifier(ItemDataSO item, StatTypeEnum stat, ModifierType type, out float value)
        {
            value = 0f;
            if (item is not EquipmentDataSO equipment || equipment.statModifiers == null) return false;
            foreach (EquipmentDataSO.StatModifierDefinition mod in equipment.statModifiers)
            {
                if (mod == null || mod.statName != stat || mod.modifierType != type) continue;
                value = mod.value;
                return true;
            }
            return false;
        }

        // A readable stat name from the enum: "MaxHealth" becomes "Max Health". Keeps the enum as the single source.
        private static string FormatStatName(StatTypeEnum stat)
        {
            string raw = stat.ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i])) sb.Append(' ');
                sb.Append(raw[i]);
            }
            return sb.ToString();
        }

        // Adds a white outline over an item, hidden until that item is selected.
        private void TrackItemElement(string instanceId, VisualElement element)
        {
            if (string.IsNullOrEmpty(instanceId) || element == null) return;

            var outline = new VisualElement { pickingMode = PickingMode.Ignore };
            outline.style.position = Position.Absolute;
            outline.style.left = 0f;
            outline.style.right = 0f;
            outline.style.top = 0f;
            outline.style.bottom = 0f;
            outline.style.borderTopWidth = 2f;
            outline.style.borderBottomWidth = 2f;
            outline.style.borderLeftWidth = 2f;
            outline.style.borderRightWidth = 2f;
            outline.style.borderTopColor = Color.white;
            outline.style.borderBottomColor = Color.white;
            outline.style.borderLeftColor = Color.white;
            outline.style.borderRightColor = Color.white;
            outline.style.display = instanceId == _selectedId ? DisplayStyle.Flex : DisplayStyle.None;

            element.Add(outline);
            _selectionOutlines[instanceId] = outline;
        }

        // Shows the outline on the selected item only. Toggles in place, so nothing is rebuilt.
        private void ApplySelectionStyle()
        {
            foreach (KeyValuePair<string, VisualElement> pair in _selectionOutlines)
                pair.Value.style.display = pair.Key == _selectedId ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // After a change, the selection and hover can point at an item that has moved or gone. Those are cleared, so the
        // details panel never shows an item that is no longer there. An item that moved keeps its selection.
        private void DropStaleFocus()
        {
            if (_selectedId != null && !IsHeld(_selectedId)) { _selectedId = null; _selectedItem = null; }
            if (_hoverId != null && !IsHeld(_hoverId)) { _hoverId = null; _hoverItem = null; _hoverElement = null; }
        }

        // True when an item is stored on the grid or worn in a slot.
        private bool IsHeld(string instanceId)
        {
            if (_inventory != null && _inventory.GetStored(instanceId) != null) return true;
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                if (EquippedInstanceId(slot) == instanceId) return true;
            return false;
        }

        // The focused item: the hovered one, else the selection.
        private (string Id, ItemDataSO Item) Focused()
        {
            if (_hoverId != null) return (_hoverId, _hoverItem);
            return (_selectedId, _selectedItem);
        }

        // Discard destroys the focused item. It is removed from the grid or its slot and is not returned to the world.
        // A drag in progress is not interrupted.
        private void DiscardFocused()
        {
            if (_dragId != null) return;
            (string id, ItemDataSO item) = Focused();
            if (id == null || item == null) return;

            bool ok = DropToFeet(id, item);
            if (ok)
            {
                _status.text = $"Discarded {item.DisplayName}.";
                if (_selectedId == id) { _selectedId = null; _selectedItem = null; }
                if (_hoverId == id) { _hoverId = null; _hoverItem = null; }
            }
            // DropToFeet sets the specific reason (no world model, no room, and so on). Only fall back to a generic message
            // when it did not, so the player sees the real cause.
            Refresh();
        }

        // Discard drops the item at the player's feet. A worn item is taken off into the grid first, because only
        // stored items can be dropped. An item with no world prefab is refused, and nothing changes.
        private bool DropToFeet(string instanceId, ItemDataSO item)
        {
            if (item.WorldPrefab == null)
            {
                _status.text = $"{item.DisplayName} has no world model set, so it cannot be dropped.";
                return false;
            }

            IPlayerPickup pickup = ServiceLocator.TryGet<IPlayerPickup>();
            if (pickup == null)
            {
                _status.text = "The player is not ready to drop items.";
                return false;
            }

            if (_inventory.GetStored(instanceId) == null)
            {
                EquipmentSlot? slot = FindWornSlot(instanceId);
                if (slot == null || !_mover.TryUnequipToGrid(slot.Value))
                {
                    _status.text = "There is no room in the grid to take it off first.";
                    return false;
                }
            }

            // A stack drops every individual in it. Each one lands as its own pile with its own instance ID. The member list
            // is copied first, because each drop removes a member from the live stack.
            var individuals = new System.Collections.Generic.List<string>(_inventory.MembersOf(instanceId));
            if (individuals.Count == 0) return pickup.TryDrop(instanceId);

            bool allDropped = true;
            foreach (string individual in individuals)
                allDropped &= pickup.TryDrop(individual);
            return allDropped;
        }

        private EquipmentSlot? FindWornSlot(string instanceId)
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (EquippedInstanceId(slot) == instanceId) return slot;
            }
            return null;
        }

        // The discard key, read each frame while the tab is open, the same way rotation is read.
        // True while either Control key is held. Read from the keyboard directly, so it works with the virtual cursor.
        private static bool IsCtrlHeld()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return false;
            return keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        }

        private void HandleDiscardKey()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.xKey.wasPressedThisFrame) DiscardFocused();
        }

        // Updates the details panel and the "X to discard" tooltip for whatever is focused.
        private void UpdateDetailsAndTooltip()
        {
            (string _, ItemDataSO item) = Focused();
            // Comparison is only when hovering a different item than the selection. Hovering the selection itself, or
            // having no selection, shows its stats plainly.
            ItemDataSO compareAgainst = (_hoverId != null && _selectedItem != null && _hoverId != _selectedId) ? _selectedItem : null;
            UpdateDetails(item, compareAgainst);

            if (_tooltip == null) return;
            bool show = _hoverId != null && _hoverItem != null && _dragId == null;
            _tooltip.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;

            _tooltip.text = $"{_hoverItem.DisplayName}  -  X to discard";
            // Sit just above the hovered item, in the tab's own space, so it reads as attached to the item.
            if (_hoverElement != null && _root != null)
            {
                Rect bounds = _hoverElement.worldBound;
                Vector2 local = _root.WorldToLocal(new Vector2(bounds.xMin, bounds.yMin));

                // Keep the tooltip inside the tab, so it never runs off the edge when the item is near one.
                float maxX = Mathf.Max(0f, _root.resolvedStyle.width - _tooltip.resolvedStyle.width);
                float maxY = Mathf.Max(0f, _root.resolvedStyle.height - _tooltip.resolvedStyle.height);
                _tooltip.style.left = Mathf.Clamp(local.x, 0f, maxX);
                _tooltip.style.top = Mathf.Clamp(local.y - 22f, 0f, maxY);
            }
        }

        // The right-hand details panel: name, material, footprint, fit, weight, armour and description.
        private void UpdateDetails(ItemDataSO item, ItemDataSO compareAgainst = null)
        {
            if (_details == null) return;
            _details.Clear();

            if (item == null)
            {
                _details.Add(Text("Select or hover an item to see its details.", 13, new Color(0.6f, 0.55f, 0.45f)));
                return;
            }

            var light = new Color(0.85f, 0.85f, 0.85f);
            _details.Add(Text(item.DisplayName, 16, new Color(0.98f, 0.92f, 0.8f)));
            _details.Add(Text($"Material: {item.Material} (efficiency {item.MaterialEfficiency:0.0})", 12, light));
            _details.Add(Text($"Weight: {item.Weight:0.0} kg", 12, light));
            _details.Add(Text($"Footprint: {item.GridWidth} x {item.GridHeight}", 12, light));

            if (item is EquipmentDataSO equipment && equipment.validSlots != null && equipment.validSlots.Length > 0)
            {
                var slots = new System.Collections.Generic.List<string>();
                foreach (EquipmentSlot s in equipment.validSlots) slots.Add(SlotName(s));
                _details.Add(Text($"Fits: {string.Join(", ", slots)}", 12, light));
            }

            if (item is WearableDataSO wearable && wearable._wearableCategory == WearableCategory.Armor)
            {
                float armour = EquipmentService.ArmorValueFor(wearable);
                float? armourOther = compareAgainst is WearableDataSO otherArmour && otherArmour._wearableCategory == WearableCategory.Armor
                    ? EquipmentService.ArmorValueFor(otherArmour)
                    : (float?)null;
                AddComparedLine("Armour value", armour, armourOther, "0", new Color(0.95f, 0.8f, 0.5f));
            }

            // Attack: the flat physical damage, then each elemental part. Compared line by line with the selection, so a
            // weapon that adds fire damage is compared on its fire as well as its physical damage.
            if (item is EquipmentDataSO attacker && (attacker.physicalAttack > 0f || attacker.elementalAttacks.Length > 0))
            {
                EquipmentDataSO other = compareAgainst as EquipmentDataSO;
                AddComparedLine("Attack", attacker.physicalAttack, other != null ? other.physicalAttack : (float?)null,
                    "0.##", new Color(0.95f, 0.85f, 0.7f));
                foreach (EquipmentDataSO.ElementalAttack elemental in attacker.elementalAttacks)
                {
                    if (elemental == null) continue;
                    float? otherElemental = other != null ? ElementalOf(other, elemental.damageType) : (float?)null;
                    AddComparedLine($"  {elemental.damageType}", elemental.damage, otherElemental, "0.##",
                        new Color(0.85f, 0.9f, 0.95f));
                }
            }

            // Stat modifiers the item grants when worn. Shown as "Stat +value", with "%" for percent modifiers.
            if (item is EquipmentDataSO withStats && withStats.statModifiers != null && withStats.statModifiers.Length > 0)
            {
                var header = Text("Grants", 12, new Color(0.9f, 0.75f, 0.45f));
                header.style.marginTop = 6f;
                _details.Add(header);
                foreach (EquipmentDataSO.StatModifierDefinition mod in withStats.statModifiers)
                {
                    if (mod == null) continue;
                    string suffix = mod.modifierType == ModifierType.PercentAdditive ? "%" : "";

                    // A stat the selected item also grants is compared with the same rule as every other value.
                    float? other = compareAgainst != null && TryGetModifier(compareAgainst, mod.statName, mod.modifierType, out float o)
                        ? o : (float?)null;

                    // The label is the stat name alone. AddComparedLine prints the value once, with its sign and suffix.
                    AddComparedLine($"  {FormatStatName(mod.statName)}", mod.value, other, "+0.##;-0.##;0", new Color(0.85f, 0.9f, 0.85f), suffix);
                }
            }

            if (!string.IsNullOrEmpty(item.Description))
            {
                var description = Text(item.Description, 12, new Color(0.75f, 0.72f, 0.65f));
                description.style.whiteSpace = WhiteSpace.Normal;
                description.style.marginTop = 6f;
                _details.Add(description);
            }
        }

        private void OnUnequip(EquipmentSlot slot)
        {
            bool ok = _mover.TryUnequipToGrid(slot);
            _status.text = ok ? "Unequipped to the grid." : "No room in the grid for that item.";
            Refresh();
        }

        private static Label Text(string text, int size, Color color)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = size;
            label.style.color = color;
            return label;
        }

        private static void StyleSmallButton(Button button)
        {
            button.style.height = 24f;
            button.style.marginLeft = 4f;
            button.style.marginRight = 4f;
        }
    }
}