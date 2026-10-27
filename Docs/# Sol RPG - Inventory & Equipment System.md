# Inventory & Equipment System — Decisions, Design, and Current State

## 1. Status at a Glance

| Aspect | State |
|---|---|
| Equipment ↔ StatsService integration pattern | ✅ Decided (modifiers via `ModifierCategory.Equipment`, `sourceId = itemInstanceId`) |
| Save fields for inventory/equipment | ✅ Present in `PlayerSaveData` (IDs only; needs upgrade for grid — see §4) |
| Grid-based spatial inventory (varying shapes/sizes) | 🟡 Design direction established by you, formalized here for the first time |
| Compactable vs rigid items (cloth tunic vs plate tunic) | 🟡 Design direction established by you, formalized here for the first time |
| `ItemDefinition` / `ItemInstance` domain model | ⬜ Not yet implemented — schema proposed in §3.2 |
| `IInventoryService` / `IEquipmentService` | ⬜ Not yet designed or implemented |
| Item registry for save-ID resolution | ⬜ Not yet designed |
| Current project priority | ⬜ NOT the active task — stat/talent pipeline (Gap 2) comes first |

**Important honesty note for the record:** the grid layout and compactability ideas were *your design vision* (grid with varying item shapes; cloth folds, plate does not). They had not previously been written into the knowledgebase or implemented. This document formalizes them as the agreed design direction so they survive into the next work session.

---

## 2. What Was Decided in the Thread (Equipment Integration)

These decisions are locked in and are the contract the future inventory system must honor.

### 2.1 Equipment bonuses are modifiers, never base stats

Attribute growth (Brawn from level-ups) mutates base values through `StatsService.SetBaseStat()`. Equipment bonuses are **removable effects** and must use:

```csharp
ModifierCategory.Equipment
```

Removing a piece of gear must revert stats exactly, without touching progression allocations or `PlayerStatSO` defaults.

### 2.2 Item-instance ID is the modifier `sourceId`

The identity used for stat modifiers is the **specific instance**, not the item type. Two iron rings are two instances:

```text
IronRing instance A: itemInstanceId = "item-uuid-A"
IronRing instance B: itemInstanceId = "item-uuid-B"
```

Apply on equip:

```csharp
var modifier = new StatModifier(
    type: ModifierType.FlatAdditive,
    category: ModifierCategory.Equipment,
    statType: StatTypeEnum.ArmorValue,
    value: armorBonus,
    sourceId: itemInstanceId,
    duration: -1f
);
_statsService.ApplyOrReplaceModifier(modifier);
```

Remove on unequip (strips *all* modifiers from that item across every stat/category at once):

```csharp
_statsService.RemoveModifiersFromSource(itemInstanceId);
```

### 2.3 Saves store IDs; modifiers are rebuilt on load

`PlayerSaveData` already reserves:

```csharp
public string equippedHeadId;
public string equippedFaceId;
public string equippedNeckId;
public string equippedTorsoId;
public string equippedHandsId;
public string equippedLegsId;
public string equippedFeetId;
public string equippedRingLeftId;
public string equippedRingRightId;
public string equippedMainHandId;
public string equippedOffhandId;

public string[] inventoryInstanceIds; // ← insufficient for a grid; see §4
```

Active modifiers are **never** saved. On load, the system resolves IDs against an item registry, reconstructs state, and re-applies equipment modifiers. This keeps one source of truth (items + slots), not two (items AND modifier lists).

### 2.4 One item may modify several stats

One item can push `ArmorValue`, `FireResistance`, and `MoveSpeed` modifiers under the same `sourceId`. A single `RemoveModifiersFromSource(itemInstanceId)` removes them all.

**Aggregation rule (decided as the initial approach):** an item contributes **at most one modifier per `StatTypeEnum` + category**. `ApplyOrReplaceModifier` replaces by matching `sourceId` within a stat/category list, so two separate same-stat modifiers from one item would silently overwrite each other. Aggregate an item's bonuses per stat before applying. If a future design needs multiple same-stat effects from one item (e.g., enchant + base), revisit the modifier identity model then.

### 2.5 Sequencing in the roadmap

Inventory sits **after** the character-stat pipeline, before the Main Menu:

1. Finish talent effects → `StatsService` (Gap 2 — current task)
2. Validate save/load round trip
3. `DerivedStatResolver` (Brawn → MeleeDamage, etc.)
4. **Inventory & equipment** ← this document's subject
5. Main Menu (New Game / Continue / Load)

---

## 3. The Grid Inventory Design (Formalized Here)

### 3.1 Core model: spatial grid, not slot list

The inventory is a **2D grid** where items occupy rectangular footprints of varying width × height. A dagger might be 1×2, a bow 2×4, a plate cuirass 3×4, a potion 1×1. This is the Diablo/Resident-evil-style model you described, applied to a Skyrim-like frame.

```text
Grid 6 wide × 8 tall (example)

. . . . . .
. . P P . .      P = Potion 1x1
. . P P . .      B = Bow 2x4 (rotated)
. B B . . .
. B B . . .
. B B . . .
. B B . C .
. . . . . .      C = Cloth Tunic, compacted to 2x1
```

Items are **placed, not auto-sorted**. The player drags items into specific cells. An item can only be placed where its entire footprint fits over empty cells.

### 3.2 Domain model: definition vs instance

Follow the same design-time/runtime split used across the whole project:

**`ItemDefinitionSO` — ScriptableObject, design-time, immutable template:**

```csharp
[CreateAssetMenu(menuName = "Sol/Items/Item Definition")]
public class ItemDefinitionSO : ScriptableObject
{
    public string definitionId;        // "IronRing", "ClothTunic", "PlateCuirass"
    public string displayName;
    public Sprite icon;
    public ItemCategory category;      // Weapon, Armor, Consumable, Material, Quest...
    public float weightKg;             // feeds CarryCapacity checks

    [Header("Grid Footprint")]
    public int gridWidth;              // cells occupied horizontally
    public int gridHeight;             // cells occupied vertically
    public bool rotatable;             // allows 90° rotation when placing

    [Header("Compactability")]
    public bool isCompactable;         // cloth folds; plate never does
    public int compactedWidth;         // footprint when compacted (e.g., 2x1)
    public int compactedHeight;

    [Header("Stacking")]
    public bool stackable;             // potions, arrows: yes. weapons/armor: no.
    public int maxStackSize;

    [Header("Equipment")]
    public EquipmentSlotType slotType; // Head, Torso, MainHand, ... (None for non-equippables)
    public bool twoHanded;             // occupies MainHand AND Offhand

    [Header("Stat Bonuses")]
    public ItemStatBonus[] statBonuses; // resolved to Equipment modifiers on equip
}

[System.Serializable]
public class ItemStatBonus
{
    public StatTypeEnum statType;
    public ModifierType modifierType;   // FlatAdditive or PercentAdditive
    public float value;
}
```

**`ItemInstance` — plain C#, runtime, per-physical-item:**

```csharp
public class ItemInstance
{
    public string instanceId;          // GUID — the stat-modifier sourceId
    public string definitionId;        // resolves back to ItemDefinitionSO via registry
    public int stackCount;             // 1 for non-stackables
    public bool isCompacted;           // only meaningful if definition.isCompactable
    public bool isRotated;             // current grid orientation

    // FUTURE, undecided: durability, quality, enchantments, affix rolls, custom names
}
```

Per-instance data that gameplay mutates lives on the instance; shared data lives on the definition. This mirrors `PlayerStatSO` (template) vs `StatsService` (runtime) exactly.

### 3.3 Footprints, rotation, placement

- Placement validation: every cell under the footprint (accounting for rotation and compaction state) must be empty and in-bounds.
- Rotation is a 90° swap of width/height, only if `definition.rotatable`. Stored as `isRotated` on the instance so the layout survives save/load.
- Recommended placement helpers on the grid model (pure C#, unit-testable):
  - `bool CanPlace(instance, x, y, rotated)`
  - `void Place(instance, x, y, rotated)`
  - `void Remove(instanceId)`
  - `bool TryFindFreeSpot(definition, width, height, out x, out y)` — first-fit, used for "move here if possible" and unequip-into-inventory checks.

### 3.4 Compactable vs rigid items (cloth tunic vs plate tunic)

This is the unique design rule you introduced: **some items collapse into a smaller footprint when stored; others are rigid.**

| Item | Normal footprint | Compacted footprint | Behavior |
|---|---|---|---|
| Cloth tunic | 2×3 | 2×1 | Player (or auto-rule) folds it; soft goods compress |
| Plate cuirass | 3×4 | 3×4 | Rigid. Always occupies full footprint. No folding metal. |

**Proposed rules (confirm these are what you intend):**

1. **Compaction is a per-instance runtime flag** (`instance.isCompacted`), gated by `definition.isCompactable`.
2. **Compaction changes footprint only — never weight.** A folded tunic still weighs the same; `CarryCapacity` (kg) is unaffected. Compactability is about *shape*, weight is about *mass*.
3. **Compaction is a player action** ("Fold/Unfold" context-menu option), not automatic. Auto-compaction on store is possible later as a settings toggle, but manual-first keeps the mental model simple and avoids surprising re-layouts.
4. **Compacted items cannot be equipped.** Unfold before equipping — a validation rule in the equip flow. (Decide explicitly; this keeps "folded" meaningful.)
5. **Compacting into a full grid can fail.** If unfolding would overlap other items, the action fails with UI feedback. Do not auto-shuffle other items.
6. **Equipped items leave the grid entirely** (slot-based, not grid-based). Footprint matters only while the item is stored. Consequence: **unequipping a rigid plate cuirass requires free grid space** — if the 3×4 footprint doesn't fit anywhere, unequip fails. This is a feature (Diablo-style inventory pressure), not a bug. Check `TryFindFreeSpot` before allowing unequip.

### 3.5 Stat integration: InventorySpace and CarryCapacity

Your `StatTypeEnum` already contains both, and `PlayerStatSO` seeds them:

```text
InventorySpace (int)  → base 10
CarryCapacity (float) → base 40 kg
```

**InventorySpace drives the grid size.** Two viable interpretations — pick one:

- **Option A (recommended):** InventorySpace = total usable cells. Grid width is fixed by UI settings (e.g., 6); height = `InventorySpace / width`. A talent or item granting `+6 InventorySpace` grows the grid by one row at runtime — a direct, visible payoff for stat investment.
- **Option B:** InventorySpace = number of rows, width fixed.

Either way: **the grid UI should react to stat changes.** Add a `GameEventListener` on `OnStatsChanged` that queries `GetStat(StatTypeEnum.InventorySpace)` and resizes/redraws the grid. Shrinking (if ever possible via debuff) must validate that no item would be clipped — or simply forbid shrinking below current occupied extents.

**CarryCapacity is weight-based and independent of the grid.** Sum of `weightKg × stackCount` across all instances (grid + equipped) vs `GetStat(StatTypeEnum.CarryCapacity)`.

**Encumbrance integration (recommended, undecided):** when total weight exceeds capacity, push a modifier through the existing pipeline instead of hard-coding a slow walk:

```csharp
// Over-encumbered: heavy armor drag on MoveSpeed
new StatModifier(
    type: ModifierType.PercentAdditive,
    category: ModifierCategory.Temporary,   // or a dedicated decision
    statType: StatTypeEnum.MoveSpeed,
    value: -0.4f,                            // -40% move speed
    sourceId: "Encumbrance",
    duration: -1f
);
```

Removed automatically when weight drops back under the limit. This reuses the exact slowfall/trigger pattern already proven with `GravityMultiplier`.

### 3.6 UI and events

- Grid UI redraws on: item moved/placed/removed, item compacted/unfolded, grid resized.
- Equipment changes raise `OnStatsChanged` automatically (existing pipeline) — equipment UI listens to that event and refreshes.
- Inventory UI changes should raise their own `GameEvent` (e.g., `OnInventoryChanged`) so the grid, weight meter, and any open tooltips refresh. Parameterless, designer-wired — same rules as all events in this project.

---

## 4. Save Format Must Change for the Grid

`string[] inventoryInstanceIds` **cannot** reconstruct a grid layout: it loses position, rotation, compaction state, and stack counts. This is a schema decision to make when inventory is implemented:

```csharp
[System.Serializable]
public class InventorySaveEntry
{
    public string instanceId;     // GUID (also the stat-modifier sourceId)
    public string definitionId;   // resolved against ItemRegistry on load
    public int x;                 // grid cell
    public int y;
    public bool rotated;
    public bool compacted;
    public int stackCount;
}

// In PlayerSaveData:
public InventorySaveEntry[] inventory;   // replaces string[] inventoryInstanceIds
```

Load flow:

1. Resolve each `definitionId` against the item registry (registry is a simple `Dictionary<string, ItemDefinitionSO>` populated in `Awake` from a master list SO, or lazy-resolved).
2. Recreate `ItemInstance` objects.
3. Re-place them into the grid at saved coordinates (validate; if design changed and an item no longer fits, fall back to `TryFindFreeSpot`, and log a warning — the save-migration tolerance pattern already used for talent validation).
4. Re-apply equipment modifiers for whatever is in the equipped slots.

This preserves the established principle: **saves store player state (positions, choices, IDs); everything derivable is rebuilt at load.**

---

## 5. Service Architecture (Proposed — Not Yet Built)

Follow the project's established service patterns: plain C# services, MonoBehaviour hosts, interface-backed, injected via `Initialize` from `GameInitializer`.

| Service | Type | Responsibility |
|---|---|---|
| `ItemRegistry` | Plain C# (hosted on `[Services]`) | `definitionId → ItemDefinitionSO` resolution; populated from a master list SO |
| `IInventoryService` | Plain C# | Owns the grid model, `ItemInstance` collection, placement/compaction/stacking rules, weight sum |
| `IEquipmentService` | Plain C# | Owns slot state, equip/unequip validation (slot compatibility, two-handed, compaction check, grid-space check for unequip), pushes/removes Equipment modifiers via `IStatsService` |
| `InventoryController` | MonoBehaviour (UI scene object) | UI Toolkit grid rendering, drag & drop, context menus (Fold/Unfold, Drop) |

**Why two services instead of one:** SRP. Inventory owns *storage geometry*; equipment owns *loadout state + stat side effects*. They talk to each other on unequip (equipment asks inventory for free space), but neither owns the other's data.

**Proposed `IInventoryService` surface (sketch):**

```csharp
public interface IInventoryService
{
    void Initialize(InventorySaveEntry[] savedEntries);
    bool TryAddItem(string definitionId, int count, out string instanceId); // loot pickup: auto-find spot
    bool CanMoveItem(string instanceId, int x, int y, bool rotated);
    void MoveItem(string instanceId, int x, int y, bool rotated);
    bool TrySetCompacted(string instanceId, bool compacted);
    void RemoveItem(string instanceId);                 // dropped/deleted
    IReadOnlyList<ItemInstance> GetAllItems();
    float GetTotalWeightKg();                           // vs GetStat(CarryCapacity)
    InventorySaveEntry[] CaptureSaveState();            // called by save system
}
```

**Proposed `IEquipmentService` surface (sketch):**

```csharp
public interface IEquipmentService
{
    void Initialize(PlayerSaveData saveData);           // resolve slot IDs, re-apply modifiers
    bool CanEquip(string instanceId);                   // slot match, compaction, two-handed rules
    bool TryEquip(string instanceId);                   // swap logic: displaced item → grid (needs space!)
    bool TryUnequip(EquipmentSlotType slot);            // grid space check first
    ItemInstance GetEquipped(EquipmentSlotType slot);
    IReadOnlyDictionary<EquipmentSlotType, ItemInstance> GetAllEquipped();
    void CaptureSaveState(PlayerSaveData target);
}
```

**Equip flow (full chain):**

```text
TryEquip(instanceId)
  → definition lookup (registry)
  → validations: slotType match, two-handed frees Offhand, not compacted
  → if slot occupied: displaced item must fit back into grid (TryFindFreeSpot) or swap fails
  → move instance out of grid into slot state
  → foreach statBonus: IStatsService.ApplyOrReplaceModifier(Equipment, sourceId: instanceId)
  → OnStatsChanged fires in LateUpdate (existing pipeline) → UI refreshes
```

**Unequip flow:**

```text
TryUnequip(slot)
  → TryFindFreeSpot(definition footprint, honoring compaction state) — fail if no room
  → RemoveModifiersFromSource(instanceId)
  → place instance into grid
  → OnStatsChanged fires
```

---

## 6. Implementation Watch-Outs (Carried Over — Equipment Depends on These)

1. **🔴 `ApplyOrReplaceModifier` replace-path bug (must fix first).** The early `return` in the replace branch skips `_modifierToStatLookup` update and `RefreshDerivedStat`. Equipment *replacement* (reforge an item, swap enchants) and multi-point talents both hit this path. Fix is documented in §15 of the knowledgebase.
2. **🟡 `RemoveModifiersFromSource` leaves stale lookup entries.** It removes modifiers from the category lists but does not purge their IDs from `_modifierToStatLookup`. For equipment this matters: every equip/unequip cycle adds stale entries. Fix alongside the replace-path bug: collect removed modifier IDs and remove them from the lookup before refreshing affected stats.
3. **🟡 Equipment swap uses replace-by-sourceId.** Equipping a different ring into the same hand is NOT a replace — it's a different `sourceId`. You must remove the old instance's modifiers *and* apply the new one. `ApplyOrReplaceModifier` only guards against the same sourceId stacking.
4. **🟡 Two-handed weapons** occupy `MainHand` + `Offhand`. Equipping one must remove/validate the offhand item (and vice versa). Decide: does equipping a two-hander auto-stow the offhand item (if grid space exists) or fail?
5. **🟡 Item registry survival.** If a definition SO is deleted/renamed in a patch, saved `definitionId`s must fail gracefully (log + skip or spawn fallback), exactly like `ValidateLoadedData()` does for orphaned talent nodes.

---

## 7. Still Undecided (Open Questions for the Inventory Design Session)

1. Rotation allowed for all items or only some (`rotatable` flag — proposed yes with flag)?
2. Fixed grid width vs fully dynamic dimensions; what `InventorySpace` exactly controls (cells vs rows).
3. Stacking identity: is a stack one instance with `stackCount`, or N instances? (Proposed: one instance per stack; splitting creates a new instance.)
4. Auto-compaction on store: manual-first (proposed) or automatic toggle?
5. Encumbrance behavior: modifier values, threshold tiers (e.g., 100%–150% = slow, >150% = cannot move), and which `ModifierCategory` it uses.
6. Durability, quality, enchantments, affixes, named items — explicitly deferred; `ItemInstance` has extension room but no fields yet.
7. Containers/bags-in-bag (a backpack item that itself contains a grid) — deferred.
8. Drop-to-world representation (physical prefab spawn vs abstraction) — deferred.
9. Whether `InventorySpace`/grid lives on the persistent scene (persists across levels) or per-level — persistent, matching all other services, but confirm.
10. Context-menu action set (Fold, Drop, Split Stack, Use, Equip) and which are v1.

---

## 8. Recommended Order of Work When Inventory Becomes the Task

1. **Prerequisite check:** confirm the two `StatsService` fixes (§6.1, §6.2) are merged — equipment correctness depends on both.
2. **Domain model first, no UI:** `ItemDefinitionSO` schema, `ItemInstance`, `ItemRegistry`. Decide the open questions in §7 that block schema (1, 2, 3).
3. **Grid model as pure C#:** occupancy, `CanPlace/Place/Remove/TryFindFreeSpot`, compaction rules. Unit-testable, no Unity dependencies — same pattern as `StandardStatCalculator`.
4. **`IInventoryService`** with MonoBehaviour host on `[Services]`, registered in `Awake`, initialized in `Start` — consistent with every other service.
5. **Grid UI** (UI Toolkit), wired to `OnInventoryChanged` GameEvent.
6. **`IEquipmentService`** + modifier push/remove, reusing the exact talent-effect integration pattern (`ApplyOrReplaceModifier` / `RemoveModifiersFromSource` with instanceId).
7. **Save schema upgrade** (`InventorySaveEntry[]`), load flow, round-trip test: equip → save → load → identical stats AND identical grid layout.
8. Encumbrance modifier as the final integration polish.

---

*End of inventory breakdown. Equipment integration contract (§2) is settled; grid + compactability design (§3–§5) is now formalized and awaiting your confirmation on the open questions in §7.*
