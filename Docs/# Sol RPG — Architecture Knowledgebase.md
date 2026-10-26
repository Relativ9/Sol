# Sol RPG — Architecture Knowledgebase

**Last Updated:** October 2026
**Engine:** Unity (C#)
**Namespace:** `Sol`
**Project Type:** Skyrim-like open-world action RPG with unique design deviations

---

## Table of Contents
1. [Project Vision & Design Goals](#1-project-vision--design-goals)
2. [The Overall Plan](#2-the-overall-plan)
3. [Progress Summary — Where We Are](#3-progress-summary--where-we-are)
4. [Active Task — What We Are Doing Right Now](#4-active-task--what-we-are-doing-right-now)
5. [Global Architecture Rules](#5-global-architecture-rules)
6. [Scene & Initialization Architecture](#6-scene--initialization-architecture)
7. [Service Locator Pattern](#7-service-locator-pattern)
8. [SO-Based GameEvent System](#8-so-based-gameevent-system)
9. [Stat System](#9-stat-system)
10. [Progression System](#10-progression-system)
11. [Talent System](#11-talent-system)
12. [Save & Persistence System](#12-save--persistence-system)
13. [Physics & Movement System](#13-physics--movement-system)
14. [Respec System](#14-respec-system)
15. [Known Bugs & Watch-Outs](#15-known-bugs--watch-outs)
16. [Completed Work Checklist](#16-completed-work-checklist)
17. [Remaining Roadmap & Next Steps](#17-remaining-roadmap--next-steps)

---

## 1. Project Vision & Design Goals

Sol is a first-person open-world action RPG in the vein of The Elder Scrolls (Skyrim), but with deliberate design departures. Key distinctions:

- **Energy as universal resource:** Health and a single Energy pool govern everything — sprinting, spells, attacks, jumping, blocking, channelling. No stamina/mana split.
- **Focus-based play:** `Focus` attribute increases parry/block timing windows, crit chance, projectile range, and highlights hidden loot. Rewards attentive players.
- **Hybrid talent nodes:** Talent tree has cross-tree "synergy" nodes that require investment in two different trees.
- **Shrine/collectible permanent boosts:** Permanent modifiers can come from world exploration (e.g., praying at a shrine gives +0.1 FireResistance permanently).
- **Rare respec:** Respecs exist (e.g., potion/shrine) but are intentionally uncommon. They trigger a full "relevel" process for attributes.
- **Crit design:** Unarmored weakpoints are guaranteed crits. Armored weakpoints have a `critChance` roll. AOE and indirect spells use `0.5x critChance`. `CritMultiplier` is capped at `3f`.

---

## 2. The Overall Plan

This section captures the full high-level development plan agreed upon over the course of the project so far, ordered from earliest to latest. This is the roadmap from scratch to a functional character pipeline.

### Phase 1 — Core Infrastructure (DONE)
- Service Locator pattern
- SO-based GameEvent system
- `GameInitializer` as composition root with strict Awake/Start split
- `SaveService` / `ISaveManager` with lazy (debounced) and immediate save
- `PlayerSaveData` schema design (stable, migration-safe)

### Phase 2 — Physics & Movement (DONE)
- `GravityController` owns vertical force
- `Movement` owns horizontal velocity and slope projection
- `GroundChecker` exposes `HasLookAheadHit` and `LookAheadHit`
- Slowfall/ledge interaction bug fixed via `GetMovementGroundNormal()` gating

### Phase 3 — Stat System Architecture (DONE)
- Migrated from string-based stat keys to `StatTypeEnum` for compile-time safety
- `StatModifier` struct with GUID id, sourceId, type, category, value, duration
- `ModifierType` collapsed to `FlatAdditive` / `PercentAdditive`
- `ModifierCategory`: Base, Permanent, Equipment, Temporary, Environmental
- `IStatCalculator` interface and `StandardStatCalculator` (pure, stateless, plain C#)
- `StatsService` refactored: typed dictionaries, derived cache, reverse lookup, batched event
- `PercentAdditive` math fixed: additive summation `(1 + sum)`, not compounding per modifier
- `SetBaseStat()` added to `IStatsService`

### Phase 4 — Progression & Attribute System (DONE)
- `ProgressionService` extended with attribute point pool and allocation tracking
- `TrySpendAttributePoint`, `TryRefundAttributePoint`, `ResetAllAttributes`, `ReloadAllBaseStats`
- `PlayerSaveData` migrated to `AttributeAllocationEntry[]` (removed individual allocation ints)
- Save stores deltas (points above default), not final values
- `GameInitializer.Start()` wired to call `ReloadAllBaseStats()` after progression init (Gap 1 fix)

### Phase 5 — Talent → StatsService Wiring (DONE, pending in-editor verification)
- `TalentStateService` receives `IStatsService` via `Initialize(saveData, statsService)`
- `ResetAll()` strips Permanent modifiers before clearing allocations
- Gap 2 implemented: `TalentEffectSO`, `StatModifierTalentEffectSO`, `PassiveTalentEffectSO` (stub), `ITalentEffectContext`
- `TalentNodeDataSO.effects` added; `TryAllocatePoint` / `TryRemovePoint` / `Initialize` / `ResetAll` wired
- Verified in Unity: talent allocation and stacking work
- Known limitation: multiple effects on one node share `sourceId = nodeId` and overwrite each other

### Phase 6 — Derived Stat Resolver (NEXT AFTER GAP 2)
- Plain C# `DerivedStatResolver`: Brawn → MeleeDamage, Vigor → MaxHealth, etc.
- Formula floats stored in `PlayerStatSO` as designer-tunable fields
- Pushes `Permanent` modifiers with reserved sourceIds like `"Derived_Brawn"`
- Conditional reactive effects (e.g., armor-based health bonus) also live here

### Phase 7 — Inventory & Equipment System (PLANNED)
- Equipment pushes `ModifierCategory.Equipment` modifiers into `StatsService`
- `sourceId = itemInstanceId` (unique per item instance)
- On equip: `ApplyOrReplaceModifier` per stat bonus
- On unequip: `RemoveModifiersFromSource(itemInstanceId)`

### Phase 8 — Main Menu & Full Save/Load (PLANNED)
- Separate lightweight `MainMenu` scene
- New Game / Continue / Load Game buttons
- `GameInitializer` lives only in `PersistentGameScene`, never in Main Menu
- Full save round-trip validation (load character → verify identical derived stats)

---

## 3. Progress Summary — Where We Are

This is a concise status board for re-orientation after a break.

| System | Status | Notes |
|---|---|---|
| Service Locator | ✅ Complete | |
| GameEvent SO system | ✅ Complete | All events use this, no C# Action events |
| GameInitializer Awake/Start split | ✅ Complete | |
| SaveService / ISaveManager | ✅ Complete | Lazy (2s debounce) + Immediate |
| PlayerSaveData schema | ✅ Complete | Stable. Migration history documented in §12 |
| Physics ledge/slowfall fix | ✅ Complete | `GetMovementGroundNormal()` gating |
| StatTypeEnum | ✅ Complete | |
| StatModifier struct | ✅ Complete | GUID id, sourceId, type, category, duration |
| ModifierType / ModifierCategory | ✅ Complete | |
| StandardStatCalculator | ✅ Complete | Pure, stateless, plain C# |
| StatsService refactor | ✅ Complete | Cache, reverse lookup, batched event |
| PercentAdditive math fix | ✅ Complete | Additive sum, not compounding |
| SetBaseStat() | ✅ Complete | |
| ProgressionService attribute layer | ✅ Complete | Spend, refund, reset, reload |
| ReloadAllBaseStats() in GameInitializer | ✅ Complete | Gap 1 fixed |
| TalentStateService constructor/init split | ✅ Complete | Race-condition-safe |
| TalentStateService receives IStatsService | ✅ Complete | Via Initialize(), not constructor |
| TalentStateService.ResetAll() strips modifiers | ✅ Complete | Code is there, no modifiers pushed yet |
| ApplyOrReplaceModifier replace-path bug | ✅ Fixed | Early return removed, shared tail runs for replace and add |
| Gap 2: Talent modifiers → StatsService | ✅ Implemented | Verified in Unity: allocation and stacking. Save round-trip not yet verified |
| Save file validation + repair prompt | ✅ Implemented | `SaveValidator`, `ModalDialog` via `IUIManager.ShowModal`, backup on new game |
| Virtual cursor owns OS cursor | ✅ Implemented | `IsOsCursorOwned`; camera `LockCursor` is blocked while active |
| Vitals HUD + environmental triggers + combat log | ✅ Implemented | Not yet verified in-editor (regen, triggers, log) |
| DerivedStatResolver | ⬜ Not started | After Gap 2 |
| Inventory / Equipment modifiers | ⬜ Not started | After DerivedStatResolver |
| Main Menu scene | ⬜ Not started | After full save round-trip works |

---

## 4. Active Task — What We Are Doing Right Now

**We are implementing Gap 2: making talent node allocations push `Permanent` modifiers into `StatsService`.**

Until this is done, the talent tree is cosmetic. Allocating a node stores a point count internally in `TalentStateService` but has zero effect on any stat. Loading a save correctly restores allocation counts but the stat bonuses are not replayed.

### Why this was blocked
The original `TalentStateService` had no reference to `IStatsService`. That has since been resolved — `IStatsService` is now injected via `Initialize(saveData, statsService)` in `GameInitializer.Start()`, which is guaranteed to run after all Awake registrations.

### Step-by-step implementation plan for Gap 2

**Step 1 — Fix the ApplyOrReplaceModifier bug FIRST (see §15)**
The replace path has an early `return` that skips `_modifierToStatLookup` update and `RefreshDerivedStat`. Multi-point talent nodes will silently fail to update stats if this is not fixed first.

**Step 2 — Create the TalentEffectSO strategy architecture (3 new files)**

`ITalentEffectContext.cs` (plain C# interface):
```csharp
public interface ITalentEffectContext
{
    IStatsService StatsService { get; }
    // Future: IAbilityService AbilityService { get; }
}
```

`TalentEffectSO.cs` (abstract ScriptableObject):
```csharp
public abstract class TalentEffectSO : ScriptableObject
{
    public abstract void Apply(ITalentEffectContext context, string nodeId, int allocatedPoints);
    public abstract void Remove(ITalentEffectContext context, string nodeId);
}
```

`StatModifierTalentEffectSO.cs` (concrete, inspector-configurable):
```csharp
[CreateAssetMenu(menuName = "Sol/Talent System/Effects/Stat Modifier")]
public class StatModifierTalentEffectSO : TalentEffectSO
{
    public StatTypeEnum targetStat;
    public ModifierType modifierType;
    public ModifierCategory category = ModifierCategory.Permanent;
    public float valuePerPoint;

    public override void Apply(ITalentEffectContext context, string nodeId, int points)
    {
        var mod = new StatModifier(
            type: modifierType,
            category: category,
            statType: targetStat,
            value: valuePerPoint * points,
            sourceId: nodeId,
            duration: -1f
        );
        context.StatsService.ApplyOrReplaceModifier(mod);
    }

    public override void Remove(ITalentEffectContext context, string nodeId)
    {
        context.StatsService.RemoveModifiersFromSource(nodeId);
    }
}
```

`PassiveTalentEffectSO.cs` (stub for conditional/reactive effects — e.g., "2 health per 10 armor"):
```csharp
[CreateAssetMenu(menuName = "Sol/Talent System/Effects/Passive (Stub)")]
public class PassiveTalentEffectSO : TalentEffectSO
{
    public override void Apply(ITalentEffectContext ctx, string nodeId, int pts)
        => Debug.Log($"[PassiveTalentEffectSO] Node {nodeId}: TODO register reactive rule.");
    public override void Remove(ITalentEffectContext ctx, string nodeId)
        => Debug.Log($"[PassiveTalentEffectSO] Node {nodeId}: TODO unregister reactive rule.");
}
```

**Why `PassiveTalentEffectSO` is a stub and NOT a static modifier:**
Conditional effects like *"Second Skin — gain 2 health per 10 armor value"* are reactive snapshots. If armor changes (equip/unequip), the health bonus must recalculate. Baking a static value at allocation time would go stale. These belong in `DerivedStatResolver` (Phase 6). The talent node marks the rule as active; the resolver owns the math.

**Step 3 — Add `effects` list to TalentNodeDataSO**
```csharp
[Header("Effects")]
public List<TalentEffectSO> effects;
```

**Step 4 — Wire TalentStateService**

Add fields:
```csharp
private ITalentEffectContext _effectContext;
```

Extend `Initialize`:
```csharp
public void Initialize(PlayerSaveData saveData, IStatsService statsService)
{
    _statsService = statsService;
    _effectContext = new TalentEffectContext { StatsService = statsService };
    _allocatedPoints.Clear();

    if (saveData?.talentAllocations != null)
        foreach (var entry in saveData.talentAllocations)
            if (entry.allocatedPoints > 0)
                _allocatedPoints[entry.nodeId] = entry.allocatedPoints;

    ValidateLoadedData();

    // Replay Permanent modifiers for all loaded allocations
    foreach (var nodeId in _allocatedPoints.Keys)
        ApplyNodeEffects(nodeId);
}
```

Update `TryAllocatePoint`:
```csharp
IncrementPointAllocation(nodeId);
ApplyNodeEffects(nodeId);  // ADD
RaiseStateChanged();
```

Update `TryRemovePoint`:
```csharp
DecrementPointAllocation(nodeId);
if (GetAllocatedPoints(nodeId) <= 0)
    RemoveNodeEffects(nodeId);   // fully strip
else
    ApplyNodeEffects(nodeId);    // re-apply scaled down value via ApplyOrReplace
RaiseStateChanged();
```

Update `ResetAll`:
```csharp
foreach (var nodeId in _allocatedPoints.Keys.ToArray())
    RemoveNodeEffects(nodeId);   // strip before clearing
_allocatedPoints.Clear();
RaiseStateChanged();
```

Add private helpers:
```csharp
void ApplyNodeEffects(string nodeId)
{
    var data = _treeGenerator.GetNodeData(nodeId);
    int points = GetAllocatedPoints(nodeId);
    if (data?.effects == null) return;
    foreach (var effect in data.effects)
        effect?.Apply(_effectContext, nodeId, points);
}

void RemoveNodeEffects(string nodeId)
{
    var data = _treeGenerator.GetNodeData(nodeId);
    if (data?.effects == null) return;
    foreach (var effect in data.effects)
        effect?.Remove(_effectContext, nodeId);
}

private class TalentEffectContext : ITalentEffectContext
{
    public IStatsService StatsService { get; set; }
}
```

**Step 5 — Verify the full chain**
1. Create a test `StatModifierTalentEffectSO` asset: +5 SpellPower per point.
2. Assign it to a test node's `effects` list.
3. Allocate 1 point → `GetStat(SpellPower)` = base + 5.
4. Allocate 2nd point → `GetStat(SpellPower)` = base + 10. *(Requires bug fix from Step 1.)*
5. Remove 1 point → `GetStat(SpellPower)` = base + 5.
6. Remove last point → `GetStat(SpellPower)` = base.
7. Save → quit → load → `GetStat(SpellPower)` = base + 10. *(Round-trip test.)*

Once round-trip passes, Gap 2 is complete and the save/load pipeline is validated end-to-end.

---

## 5. Global Architecture Rules

### SOLID Compliance
- **Single Responsibility:** Each class/service owns one concern. `StatsService` stores and retrieves stats; `StandardStatCalculator` does the math; `ProgressionService` tracks point pools and pushes base values.
- **Open/Closed:** Systems extend via interface and composition, not modification. New stat effects subclass `TalentEffectSO`.
- **Liskov Substitution:** All services are accessed through interfaces outside of `GameInitializer`.
- **Interface Segregation:** Interfaces are narrow. `ITalentStateService` does not expose `Initialize()` to prevent double-init from non-authoritative callers.
- **Dependency Inversion:** High-level modules (e.g., `Movement`) depend on `IStatsService`, not `StatsService`.

### Plain C# vs MonoBehaviour
- **Services needing Unity lifecycle:** `MonoBehaviour` on the `[Services]` GameObject.
- **Pure logic services:** Plain C# classes, factory-created in a host `MonoBehaviour`'s `Awake()`.
- **ScriptableObjects:** Design-time data and GameEvent system only. Never runtime mutable state.

### Awake / Start Split — CRITICAL RULE
```
Awake() = Registration only.
          Create plain-C# services.
          Register into ServiceLocator.
          NEVER call ServiceLocator.Get<T>() in Awake.
          NEVER cross-inject between services in Awake.

Start()  = Data hydration only.
           Get services from ServiceLocator.
           Call Initialize() methods.
           Pass cross-service dependencies.
```

This rule exists because Unity does NOT guarantee `Start()` execution order across GameObjects, but ALL `Awake()` calls complete before any `Start()` runs within the same scene. Any violation risks null-reference on service retrieval.

### ServiceLocator Usage Rule
- `ServiceLocator.RegisterService<T>(instance)` — only in `Awake()`.
- `ServiceLocator.Get<T>()` — only in `Start()` or later.

### Initialization Ownership Rule
`GameInitializer` is the **sole composition root**. It is the only class permitted to:
- Call `Initialize()` on services.
- Cast to concrete types for initialization sequencing (e.g., `as TalentStateService`).
- Pass cross-service dependencies (e.g., `IStatsService` into `TalentStateService.Initialize`).

The cast to concrete is documented and intentional. `ITalentStateService` intentionally omits `Initialize()` so no other system can accidentally re-initialize talent state.

### GameEvent Rule
All cross-system events must use the SO `GameEvent` system. C# `Action<T>` is not used for cross-system notifications. Events are parameterless — listeners pull data themselves via `GetStat()` / service reads.

---

## 6. Scene & Initialization Architecture

### Scene Layout
```
MainMenu (scene)            — Standalone. No player services. Planned, not built yet.
PersistentGameScene (scene) — Loaded once per session. Never unloaded between levels.
    [Services] GameObject
        StatsService         (MonoBehaviour)
        TalentTreeGenerator  (MonoBehaviour)
        SaveManager          (MonoBehaviour — planned)
    GameInitializer          (MonoBehaviour)
Level_X (scene)             — Additively loaded on top of PersistentGameScene.
```

### Awake Registration Order (PersistentGameScene)
1. `StatsService.Awake()` — creates `StandardStatCalculator`, registers `IStatsService`.
2. `TalentTreeGenerator.Awake()` — registers `ITalentTreeGenerator`, factory-creates `TalentStateService(this, _gameStateChangedEvent)`, registers `ITalentStateService`.
3. `GameInitializer.Awake()` — creates `SaveService`, registers `ISaveService`. Creates `ProgressionService`, registers `IProgressionService`.

### Start Hydration Order (GameInitializer.Start)
```csharp
void Start()
{
    // 1. Load or create save data
    PlayerSaveData saveData = _forceNewGame || !_saveService.SaveExists("default")
        ? PlayerSaveData.CreateNewGame(playerProgressionSO)
        : _saveService.Load("default");

    // 2. Seed raw SO defaults into StatsService
    _statsService.Initialize(playerStatSO, saveData);

    // 3. Load attribute allocations, overwrite bases with player's deltas
    _progressionService.Initialize(playerProgressionSO, playerStatSO, _statsService,
        saveData.level, saveData.totalTalentPoints, saveData.totalAttributePoints,
        saveData.attributeAllocations);
    _progressionService.ReloadAllBaseStats(); // Gap 1 fix — must stay here

    // 4. Calculate talent tree node positions (no UI)
    _talentTreeGenerator = ServiceLocator.Get<ITalentTreeGenerator>();
    _talentTreeGenerator?.LoadData();

    // 5. Hydrate talent allocations and inject StatsService
    var talentState = ServiceLocator.Get<ITalentStateService>() as TalentStateService;
    talentState?.Initialize(saveData, _statsService);
    // Gap 2: Initialize will also replay Permanent modifiers once implemented

    // 6. Sync talent point counter from loaded allocations
    int derivedSpent = talentState?.GetTotalAllocatedPoints() ?? 0;
    _progressionService.SetSpentTalentPoints(derivedSpent);

    // Future step 7: DerivedStatResolver.RecalculateAll()
    // Future step 8: Equipment system loads and pushes Equipment modifiers
}
```

### Why GameInitializer casts to concrete TalentStateService
`ITalentStateService` intentionally does not expose `Initialize()`. The concrete cast is the only way to call it. This is the documented exception to the interface-only rule — `GameInitializer` is the composition root and is allowed this knowledge.

### PersistentGameScene and Start() running once
`GameInitializer.Start()` runs exactly once per session because the scene is loaded once and never unloaded. It is safe to call `ReloadAllBaseStats()` here — it does not run every frame, only at session start.

---

## 7. Service Locator Pattern

```csharp
// Registration — Awake only
ServiceLocator.RegisterService<IStatsService>(this);

// Retrieval — Start or later
var stats = ServiceLocator.Get<IStatsService>();
```

All consuming systems hold interface references, never concrete. `GameInitializer` is the only exception (for sequencing).

---

## 8. SO-Based GameEvent System

```csharp
[CreateAssetMenu(fileName = "New Game Event", menuName = "Sol/Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private List<GameEventListener> _listeners = new();

    public void Raise()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            if (_listeners[i] != null)
                _listeners[i].OnEventRaised();
    }

    public void RegisterListener(GameEventListener listener) { ... }
    public void UnregisterListener(GameEventListener listener) { ... }
}
```

### Rules
- All events are parameterless. Listeners pull their own data via service reads.
- Designer-wired via `GameEventListener` MonoBehaviour components in the scene inspector.
- Gameplay systems that need values every frame (Movement, GravityController) **poll** `GetStat()` — they do NOT subscribe to events.
- Reactive systems that only need updates on change (UI health bar, buff icons, HealthController clamping) **subscribe** via `GameEventListener`.

### Current Named Events
| Asset Name | Raised By | Purpose |
|---|---|---|
| `OnStatsChanged` | `StatsService.LateUpdate()` when `_statsDirty` | Notifies UI to refresh stat displays |
| `_talentStateChangedEvent` | `TalentStateService.RaiseStateChanged()` | Notifies talent UI, triggers auto-save |

---

## 9. Stat System

### Data Flow
```
PlayerStatSO (design-time, immutable template)
    ↓ seeds raw defaults via Initialize()
StatsService._baseValues (runtime base storage, per StatTypeEnum)
    ↓ overwritten by ProgressionService.ReloadAllBaseStats()
    ↓ stacked by TalentStateService → ApplyOrReplaceModifier(Permanent) [Gap 2]
    ↓ stacked by Equipment system → ApplyOrReplaceModifier(Equipment) [Future]
    ↓ stacked by triggers/spells → ApplyOrReplaceModifier(Temporary/Environmental) [Runtime]
    ↓ calculated by StandardStatCalculator.Calculate(base, modifiers[])
    ↓ result cached in StatsService._derivedCache
    ↓ read via GetStat(StatTypeEnum) — O(1) dictionary lookup
```

### Ownership Table
| Data | Owner | Method |
|---|---|---|
| Default stat template | `PlayerStatSO` | `StatsService.Initialize()` seeds bases |
| Level-up attribute deltas | `ProgressionService` | `StatsService.SetBaseStat()` |
| Talent permanent bonuses | `TalentStateService` | `StatsService.ApplyOrReplaceModifier(Permanent)` |
| Equipment bonuses | Equipment system (future) | `StatsService.ApplyOrReplaceModifier(Equipment)` |
| Temporary effects | Spells/triggers | `StatsService.ApplyOrReplaceModifier(Temporary)` |
| Final derived values | `StatsService._derivedCache` | `GetStat()` |

### StatTypeEnum
```csharp
public enum StatTypeEnum
{
    // Core Attributes
    Brawn, Finesse, Insight, Focus, Vigor, Willpower,

    // Movement
    MoveSpeed, RunMultiplier, Deceleration, CrouchSpeed,
    JumpForce, JumpCooldown, JumpDirectionBoost, MaxDoubleJump,
    FallDamageMultiplier, GravityMultiplier, TerminalVelocity,

    // Resources
    MaxHealth, MaxEnergy, HealthRegen, EnergyRegen,
    CarryCapacity, InventorySpace, LootMultiplier, Hunger, Thirst,

    // Combat
    MeleeDamage, ProjectileDamage, ProjectileRange, SpellPower,
    ArmorValue, CritChance, CritMultiplier, DeflectionChance,
    MagicResistance, FireResistance, IceResistance, VoidResistance,
    SonicResistance, SoulResistance, ReflectChance,
    EnvironmentalResistance, ParryAndBlockTime, ClueRange,
    CharmChance, KnowledgeChance, DeceptionChance, IntimidationChance,
}
```

### ModifierType
```csharp
public enum ModifierType
{
    FlatAdditive,    // Raw value added to base. e.g., +5 MaxHealth.
    PercentAdditive, // 0.1f = +10%. All percent modifiers SUM before multiplying. Never compound.
}
```

**PercentAdditive Rule — historical bug fixed:**
```csharp
// WRONG — old compounding per modifier (caused 10% + 10% = 21%):
multiplier *= (1 + mod.value);

// CORRECT — additive summation (10% + 10% = 20%):
float percentSum = 0f;
foreach (var mod in ...) percentSum += mod.value;
finalValue *= (1f + percentSum);
```

### ModifierCategory
```csharp
public enum ModifierCategory
{
    Base,           // Reserved. Never pushed by external systems. Base mutations go through SetBaseStat().
    Permanent,      // Talents, shrine blessings, achievement rewards. Persist in save.
    Equipment,      // From equipped items. Removed on unequip.
    Temporary,      // Spell effects, buffs, debuffs. Have duration > 0f. Tick down in Update().
    Environmental,  // Hazards, zone effects. Removed on trigger exit.
}
```

### StatModifier Struct
```csharp
public struct StatModifier
{
    public string id;           // GUID. Auto-generated if empty.
    public string sourceId;     // Logical owner. Used by RemoveModifiersFromSource().
    public ModifierType type;
    public ModifierCategory category;
    public StatTypeEnum statType;
    public float value;
    public float duration;      // -1f = infinite. > 0f = timed.

    public StatModifier(ModifierType type, ModifierCategory category, StatTypeEnum statType,
                        float value, string sourceId = null, float duration = -1f)
    {
        this.id = Guid.NewGuid().ToString();
        this.sourceId = sourceId;
        this.type = type; this.statType = statType;
        this.value = value; this.duration = duration;
        this.category = category;
    }
}
```

### IStatCalculator / StandardStatCalculator
Pure stateless plain C# — extracted from `StatsService` to satisfy SRP. `StatsService` delegates math here and does not know the formula.

```csharp
public interface IStatCalculator
{
    float Calculate(StatTypeEnum statType, float baseValue, StatModifier[] modifiers);
}

public class StandardStatCalculator : IStatCalculator
{
    public float Calculate(StatTypeEnum statType, float baseValue, StatModifier[] modifiers)
    {
        float flatSum = 0f;
        float percentSum = 0f;
        foreach (var mod in modifiers)
        {
            if (mod.category == ModifierCategory.Base) continue;
            if (mod.type == ModifierType.FlatAdditive)      flatSum     += mod.value;
            else if (mod.type == ModifierType.PercentAdditive) percentSum += mod.value;
        }
        return (baseValue + flatSum) * (1f + percentSum);
    }
}
```

If you ever want category-compounding (e.g., Equipment and Talent percents multiply against each other), create a `CategoryCompoundingCalculator` that implements `IStatCalculator` and swap it in `StatsService.Awake()`. `StatsService` will never know the difference.

### StatsService — Internal Storage Fields
```csharp
private Dictionary<StatTypeEnum, float> _baseValues;
private Dictionary<StatTypeEnum, float> _derivedCache;
private Dictionary<StatTypeEnum, Dictionary<ModifierCategory, List<StatModifier>>> _modifiersByCategory;
private Dictionary<string, StatTypeEnum> _modifierToStatLookup; // O(1) reverse lookup
private bool _statsDirty;
private IStatCalculator _calculator;
[SerializeField] private GameEvent _onStatsChangedEvent;
```

### StatsService — Cache & Dirty Pattern
- `GetStat()` reads `_derivedCache` (O(1)). Falls back to `RefreshDerivedStat` only on first access before any modifier is applied.
- Every mutation calls `RefreshDerivedStat(statType)` immediately after the change.
- `RefreshDerivedStat` uses `Mathf.Approximately` — only updates cache if value actually changed. Sets `_statsDirty = true` if changed.
- `LateUpdate()` fires `_onStatsChangedEvent.Raise()` once per frame if dirty, then resets flag. Multiple stat changes in one frame produce one event.
- `UpdateModifierDurations()` runs in `Update()` (NOT `LateUpdate`) so gameplay systems see expired values in the same frame they expire.
- Expiry uses reverse iteration (`Count - 1` down) and `HashSet<StatTypeEnum>` to refresh each affected stat exactly once.

### IStatsService — Full Public API
```csharp
float  GetBaseStat(StatTypeEnum statType);
float  GetStat(StatTypeEnum statType);
void   SetBaseStat(StatTypeEnum statType, float value);
string ApplyOrReplaceModifier(StatModifier modifier);
string ApplyModifier(StatModifier modifier);
void   RemoveModifier(string modifierId);
void   RemoveModifiersFromSource(string sourceId);
void   RemoveAllModifiersOfType(StatTypeEnum statType, ModifierCategory category);
float  GetSpeedMultiplier();
```

### PlayerStatSO — Design-time Template
Immutable SO. Stores defaults for every stat. Core attributes are `int`; all others `float`. All are stored internally as `float` in `StatsService`. Presentation layer handles rounding.

```
Core (int, default 10 each): brawn, finesse, insight, focus, vigor, willpower
Movement: baseMoveSpeed=3, baseRunMultiplier=1.6, baseGravityMultiplier=2.5,
          baseTerminalVelocity=-20, baseJumpForce=5, ...
Resources: baseMaxHealth=100, baseMaxEnergy=100, baseHealthRegen=1, ...
Combat:    baseMeleeDamage=1, baseCritMultiplier=1.2, baseMagicResistance=0, ...
```

Planned addition for Phase 6:
```csharp
[Header("Attribute Derivations")]
public float brawnToMeleeDamage = 0.5f;
public float brawnToCarryCapacity = 2f;
public float vigorToMaxHealth = 5f;
public float insightToSpellPower = 1f;
// etc.
```

---

## 10. Progression System

### Two Separate Point Pools
| Pool | Field | Earned By | Spent On |
|---|---|---|---|
| Talent Points | `_totalTalentPoints` | Level up | Talent tree nodes |
| Attribute Points | `_totalAttributePoints` | Level up (+3/level, 3 at start) | Core 6 attributes |

Both pools use **"total earned minus sum of allocations"** for available count. No separate "spent" counter that can drift out of sync.

### Core Attributes (Allocatable)
Only these six can receive attribute points: `Brawn`, `Finesse`, `Insight`, `Focus`, `Vigor`, `Willpower`. `IsCoreAttribute(StatTypeEnum)` guards `TrySpendAttributePoint` against invalid inputs.

### Attribute Spend Flow
```
UI calls: _progressionService.TrySpendAttributePoint(StatTypeEnum.Brawn)
  → Validate: IsCoreAttribute(Brawn) = true
  → Validate: GetTotalSpentAttributePoints() < _totalAttributePoints
  → _attributeAllocations[Brawn]++
  → RebuildBaseStat(Brawn):
       float defaultValue = GetDefaultAttributeValue(Brawn) = PlayerStatSO.brawn = 10
       int   allocated    = _attributeAllocations[Brawn] = 3
       _statsService.SetBaseStat(Brawn, 13) → triggers cache refresh
  → returns true
```

### Save Delta Rule
Saves store allocation **deltas above default**, not final values. If `PlayerStatSO.brawn` changes from 10 to 8 in a balance patch, old saves load correctly as `8 + 3 = 11` rather than a stale `13`.

### GetDefaultAttributeValue — Compiler-Verified Switch
```csharp
private float GetDefaultAttributeValue(StatTypeEnum statType)
{
    return statType switch
    {
        StatTypeEnum.Brawn     => _defaultStats.brawn,
        StatTypeEnum.Finesse   => _defaultStats.finesse,
        StatTypeEnum.Insight   => _defaultStats.insight,
        StatTypeEnum.Focus     => _defaultStats.focus,
        StatTypeEnum.Vigor     => _defaultStats.vigor,
        StatTypeEnum.Willpower => _defaultStats.willpower,
        _                      => LogMissingDefaultAndReturnZero(statType)
    };
}
```

If a new core attribute is added to the enum and the switch is not updated, the `_` arm logs a warning and returns 0.

### IProgressionService — Full Public API
```csharp
void  Initialize(PlayerProgressionSO, PlayerStatSO, IStatsService, int level,
                 int totalTalentPoints, int totalAttributePoints, AttributeAllocationEntry[]);
int   GetLevel();
int   GetTotalTalentPoints();
int   GetAvailableTalentPoints();
int   GetAvailableAttributePoints();
float GetDefaultAttributeValue(StatTypeEnum);
void  RebuildBaseStat(StatTypeEnum);         // default + allocated → SetBaseStat
void  ReloadAllBaseStats();                  // iterate allocations, rebuild each — called on load
bool  TrySpendAttributePoint(StatTypeEnum);  // validates, increments, rebuilds
bool  TryRefundAttributePoint(StatTypeEnum);
void  ResetAllAttributes();                  // clears dict, pushes defaults for all 6 core stats
bool  SpendTalentPoint();
void  RefundTalentPoint();
void  ResetAllTalentPoints();
void  SetSpentTalentPoints(int);             // syncs counter post-talent-load in GameInitializer
```

---

## 11. Talent System

### Architecture Overview
```
TalentTreeGenerator (MonoBehaviour on [Services])
    Awake: Registers ITalentTreeGenerator
           Factory-creates TalentStateService(this, _gameStateChangedEvent)
           Registers ITalentStateService
    LoadData(): Calculates node positions. Called by GameInitializer.Start().
    Generate(root, template): Creates VisualElements. Called by UI Manager on panel open.
    ClearUI(): Releases element refs on panel close.

TalentStateService (plain C#, created by TalentTreeGenerator.Awake)
    Registered as ITalentStateService
    IStatsService injected via Initialize(), NOT constructor
    _allocatedPoints: Dictionary<string nodeId, int points>
```

### Constructor vs Initialize Split — Why
- **Constructor** takes only `ITalentTreeGenerator` and `GameEvent` — both available in `Awake`.
- **`Initialize(PlayerSaveData, IStatsService)`** is called by `GameInitializer.Start()` where `IStatsService` is guaranteed registered.
- Passing `IStatsService` to the constructor would require factory creation in `Start()`, which has no guaranteed execution order across GameObjects. This was explicitly rejected.

### TalentStateService — Full Public API
```csharp
void     Initialize(PlayerSaveData saveData, IStatsService statsService);
int      GetTotalAllocatedPoints();
int      GetAllocatedPoints(string nodeId);
string[] GetAllocatedNodeIds();
bool     HasAllocatedPoints(string nodeId);
bool     TryAllocatePoint(string nodeId);    // validates maxPoints, prerequisites
bool     TryRemovePoint(string nodeId);      // validates dependents
void     ResetAll();                         // strips Permanent modifiers, clears dict, raises event
```

### TalentNodeDataSO — Current
```csharp
public string nodeId;           // e.g., "HA_Sacred_Plate" — must be globally unique
public string displayName;
public Sprite icon;
public int maxPoints;           // 1–3 for most nodes
public string description;
public bool isActiveSkill;
public bool isHybrid;           // Synergy node between two trees
public float tier;              // 0–12
public float offset;            // -2 to 2
public TreeLayoutSO parentTree;
public List<TalentNodeDataSO> prerequisites;

// PENDING — add this for Gap 2:
public List<TalentEffectSO> effects;
```

### Prerequisite Logic
Currently: ANY single prerequisite met = node is unlocked. Review and potentially change to ALL prerequisites before building out the actual talent tree content.

### TalentEffectSO Strategy Pattern (Gap 2 — see §4 for implementation)
Each node holds a `List<TalentEffectSO>`. Each concrete `TalentEffectSO` knows how to `Apply` and `Remove` itself. `TalentStateService` calls these without knowing what the effect does. New effect types (ability unlocks, passive rules) are added by creating a new subclass and a new asset — no service code changes.

### Conditional / Reactive Effects
Effects like *"Second Skin — gain 2 health per 10 armor value"* are NOT handled by `StatModifierTalentEffectSO`. They belong in `DerivedStatResolver` (Phase 6). The talent node uses `PassiveTalentEffectSO` (stub) to mark the rule as active. The resolver owns the math and recalculates when `ArmorValue` changes.

---

## 12. Save & Persistence System

### Infrastructure Status
Save infrastructure is complete. The schema is stable. Main Menu UI (New Game / Continue / Load) is not yet built.

### PlayerSaveData — Current Schema
```csharp
[Serializable]
public class PlayerSaveData
{
    public static PlayerSaveData CreateNewGame(PlayerProgressionSO progression)
    {
        return new PlayerSaveData
        {
            level                = progression.startingLevel,
            totalTalentPoints    = progression.startingTalentPoints,
            totalAttributePoints = progression.startingAttributePoints, // starts at 3
            talentAllocations    = Array.Empty<TalentAllocationEntry>(),
            attributeAllocations = Array.Empty<AttributeAllocationEntry>(),
        };
    }

    // Progression
    public int level;
    public int totalTalentPoints;      // lifetime total earned, not "available"
    public int totalAttributePoints;   // lifetime total earned
    public int prestigePoints;

    // Allocations — deltas, not final values
    public TalentAllocationEntry[]    talentAllocations;
    public AttributeAllocationEntry[] attributeAllocations;

    // Mutable resources (current snapshot, not derived max)
    public float currentHealth;
    public float currentEnergy;

    // Equipment (resolved against item registry on load — future)
    public string equippedHeadId, equippedFaceId, equippedNeckId,
                  equippedTorsoId, equippedHandsId, equippedLegsId,
                  equippedFeetId, equippedRingLeftId, equippedRingRightId,
                  equippedMainHandId, equippedOffhandId;

    // Inventory (future)
    public string[] inventoryInstanceIds;
}

[Serializable]
public struct TalentAllocationEntry
{
    public string nodeId;
    public int allocatedPoints;
}

[Serializable]
public class AttributeAllocationEntry
{
    public StatTypeEnum statType;
    public int pointsAllocated; // delta above PlayerStatSO default
}
```

### Schema Migration History
| Version | Change | Reason |
|---|---|---|
| v1 | `string[] selectedTalentIds` | Initial implementation |
| v2 | → `TalentAllocationEntry[] talentAllocations` | Multi-point nodes were truncated by the string array |
| v2 | Removed `spentTalentPoints` from save | Caused negative available points; now derived by summing allocations |
| v3 | Added `totalAttributePoints`, `AttributeAllocationEntry[]` | Attribute system built |
| v3 | Removed `brawnAllocation`, `finesseAllocation` etc. individual ints | Redundant with the array; two sources of truth = bug |

### What Is and Is Not Saved
| Saved | Not Saved |
|---|---|
| `level`, `totalTalentPoints`, `totalAttributePoints` | Active temporary/environmental modifiers |
| `talentAllocations[]` (nodeId + points) | Derived stat values (recalculated on load) |
| `attributeAllocations[]` (statType + delta) | `_spentTalentPoints` counter (derived from sum) |
| `currentHealth`, `currentEnergy` | |
| Equipment slot IDs (future) | |

### ISaveManager
```csharp
public interface ISaveManager
{
    void RequestLazySave();      // Debounced 2s. For frequent changes (talent allocation).
    void RequestImmediateSave(); // Bypasses debounce. For critical moments (respec, level-up, quit).
}
```

### Load Round-Trip Order
```
1. StatsService.Initialize(playerStatSO, saveData)     → seeds raw SO defaults
2. ProgressionService.Initialize(...)                  → loads allocation dictionaries
3. ProgressionService.ReloadAllBaseStats()             → pushes attribute deltas via SetBaseStat
4. [Future] DerivedStatResolver.RecalculateAll()       → Brawn→MeleeDamage etc.
5. TalentStateService.Initialize(saveData, stats)      → loads allocations
   + ApplyNodeEffects for each (Gap 2)                 → replays Permanent modifiers
6. [Future] Equipment system loads items               → pushes Equipment modifiers

After step 6: GetStat() for all stats must be identical to pre-save values.
```

---

## 13. Physics & Movement System

### Separation of Concerns (Design Rule)
| Concern | Owner |
|---|---|
| Vertical force (gravity, jump) | `GravityController` |
| Horizontal velocity, slope projection | `Movement` |
| `GravityMultiplier` stat value | `StatsService` |
| Reading `GravityMultiplier` at runtime | `GravityController` polls `GetStat(GravityMultiplier)` |
| Surface normal smoothing and lookahead | `GroundChecker` |

### Slowfall / Ledge Bug — FIXED
**Problem:** `Movement` used `GetSmoothedGroundNormal()` unconditionally. At a ledge, the smoothed normal blends flat ground with the steep cliff face below, producing a phantom downward ramp that overrides slowfall gravity.

**Fix Applied:**
1. `GroundChecker` now exposes `HasLookAheadHit` (bool) and `LookAheadHit` (RaycastHit).
2. `Movement.GetMovementGroundNormal()` logic:
   - No lookahead hit → return raw `GroundHit.normal` (ledge/void case)
   - Lookahead angle > `_maxWalkableSlopeAngle` → return raw `GroundHit.normal` (cliff face)
   - Lookahead confirms walkable ramp → return `GetSmoothedGroundNormal()`
3. `ApplyGroundedVelocity` retains original slope-following including negative Y projection for real downhill ramps.

### Environmental Modifier Example
```csharp
// On slowfall trigger enter:
var mod = new StatModifier(
    type: ModifierType.PercentAdditive,
    category: ModifierCategory.Environmental,
    statType: StatTypeEnum.GravityMultiplier,
    value: -0.5f,               // -50% gravity
    sourceId: "SlowfallZone_1",
    duration: -1f
);
_statsService.ApplyOrReplaceModifier(mod);

// On trigger exit:
_statsService.RemoveModifiersFromSource("SlowfallZone_1");
```

---

## 14. Respec System

### Full Respec — Must Coordinate Three Services in Order
```csharp
public class RespecController : MonoBehaviour
{
    public void PerformFullRespec()
    {
        // 1. Strip talent Permanent modifiers + clear allocations
        ServiceLocator.Get<ITalentStateService>()?.ResetAll();

        // 2. Refund talent point counter
        var prog = ServiceLocator.Get<IProgressionService>();
        prog?.ResetAllTalentPoints();

        // 3. Reset core attributes to SO defaults
        prog?.ResetAllAttributes();

        // 4. Save immediately
        ServiceLocator.Get<ISaveManager>()?.RequestImmediateSave();
    }
}
```

**Order matters:** Strip modifiers before zeroing counters. Attributes revert before save captures the state.

### ResetAllAttributes — Implementation Detail
```csharp
public void ResetAllAttributes()
{
    _attributeAllocations.Clear(); // wipe delta dictionary
    foreach (StatTypeEnum coreStat in GetCoreAttributes())
    {
        float defaultValue = GetDefaultAttributeValue(coreStat);
        _statsService.SetBaseStat(coreStat, defaultValue); // explicitly push each default
    }
}
```

Does NOT call `ReloadAllBaseStats()` (which iterates the now-empty dictionary and does nothing). Must explicitly push defaults.

### Single-Point Refund
```csharp
_progressionService.TryRefundAttributePoint(StatTypeEnum.Brawn);
// Validates: IsCoreAttribute, current > 0
// Decrements, calls RebuildBaseStat → SetBaseStat
```

---

## 15. Known Bugs & Watch-Outs

### 🔴 BUG: ApplyOrReplaceModifier — Replace Path Skips Cache Refresh
**File:** `StatsService.cs`
**Impact:** Allocating a 2nd or 3rd point to a multi-point talent node silently fails to update the stat. The cache stays at the 1-point value forever until a different mutation triggers a refresh.
**Root Cause:** Early `return modToStore.id` inside the replace branch skips both `_modifierToStatLookup[modToStore.id] = statType` and `RefreshDerivedStat(statType)`.

```csharp
// CURRENT (BROKEN) — replace branch early returns before shared tail:
if (existingIndex >= 0)
{
    modToStore.id = modifiers[existingIndex].id;
    modifiers[existingIndex] = modToStore;
    Debug.Log(...);
    return modToStore.id; // ← BUG: skips lookup + cache refresh
}

// FIX — remove early return, set wasReplaced flag, fall through to shared tail:
if (existingIndex >= 0)
{
    modToStore.id = modifiers[existingIndex].id;
    modifiers[existingIndex] = modToStore;
    wasReplaced = true;
    Debug.Log(...);
}
if (!wasReplaced)
{
    modifiers.Add(modToStore);
    Debug.Log(...);
}
// SHARED — runs for both replace AND add:
_modifierToStatLookup[modToStore.id] = statType;
RefreshDerivedStat(statType);
return modToStore.id;
```

**This must be fixed before Gap 2. It is the first thing to do on resume.**

### 🟡 WATCH-OUT: ReloadAllBaseStats on empty dict (new game)
On a fresh new game, `attributeAllocations` is empty. `ReloadAllBaseStats()` iterates nothing. This is **correct** — `StatsService.Initialize()` already seeded raw SO defaults. Do NOT remove `ReloadAllBaseStats()` from the init sequence thinking it's redundant; it is essential on load game.

### 🟡 WATCH-OUT: Partial respec / key at zero
If you refund an attribute down to 0 allocations, do NOT remove the key from `_attributeAllocations`. Keep the key with value 0. If the key is deleted, `ReloadAllBaseStats()` skips that stat, leaving `StatsService` stuck at the previously-raised base. Always call `RebuildBaseStat(statType)` explicitly after decrement instead of relying on the load-time iteration.

### 🟡 WATCH-OUT: TalentStateService.ResetAll strips nothing until Gap 2
The strip loop in `ResetAll()` is correct but has nothing to strip yet because no Permanent modifiers have ever been pushed. Once Gap 2 is complete and `ApplyNodeEffects` is running, this becomes meaningful and critical.

### 🟡 WATCH-OUT: Prerequisite logic is ANY, not ALL
`PrerequisitesMet()` returns `true` if ANY single prerequisite node is allocated. This may be too permissive. Review before building final talent tree content and change to ALL if the design requires it.

### 🟡 WATCH-OUT: PercentAdditive is global, not per-category
All percent modifiers across all categories sum into one pool before multiplication. There is no per-category compounding. This is intentional. If you ever want category-level compounding, use a new `IStatCalculator` implementation — do not change `StandardStatCalculator`.

### 🟡 WATCH-OUT: DontDestroyOnLoad / duplicate scene load
If a bug causes `PersistentGameScene` to load additively a second time, `GameInitializer.Start()` fires again and double-registers services. Add a static `_isInitialized` guard to `GameInitializer` if you suspect scene-loading bugs.

---

## 16. Completed Work Checklist

| System | Task | Status |
|---|---|---|
| Infrastructure | Service Locator | ✅ |
| Infrastructure | SO GameEvent system | ✅ |
| Infrastructure | Awake/Start split enforced | ✅ |
| Infrastructure | `ISaveManager` (lazy + immediate) | ✅ |
| Infrastructure | `PlayerSaveData` schema (stable) | ✅ |
| Physics | Ledge/slowfall `HasLookAheadHit`, `GetMovementGroundNormal()` | ✅ |
| Physics | `GravityController` reads `GravityMultiplier` from `StatsService` | ✅ |
| Stat System | `StatTypeEnum` compile-time keys | ✅ |
| Stat System | `StatModifier` struct (GUID, sourceId, duration) | ✅ |
| Stat System | `ModifierType` (FlatAdditive / PercentAdditive) | ✅ |
| Stat System | `ModifierCategory` (5 categories) | ✅ |
| Stat System | `IStatCalculator` interface | ✅ |
| Stat System | `StandardStatCalculator` (pure, stateless) | ✅ |
| Stat System | `PercentAdditive` math fixed (additive sum) | ✅ |
| Stat System | Derived stat cache `_derivedCache` | ✅ |
| Stat System | Reverse modifier lookup `_modifierToStatLookup` | ✅ |
| Stat System | Batched dirty event `_statsDirty` + `LateUpdate` | ✅ |
| Stat System | `SetBaseStat()` on `IStatsService` | ✅ |
| Stat System | `UpdateModifierDurations` — in-place, HashSet batch | ✅ |
| Stat System | `RemoveModifiersFromSource` — HashSet deduplication | ✅ |
| Stat System | `RemoveModifier` — O(1) via reverse lookup | ✅ |
| Progression | Attribute point pool + `_totalAttributePoints` | ✅ |
| Progression | `TrySpendAttributePoint` with `IsCoreAttribute` guard | ✅ |
| Progression | `TryRefundAttributePoint` | ✅ |
| Progression | `RebuildBaseStat` single-stat | ✅ |
| Progression | `ReloadAllBaseStats` on load | ✅ |
| Progression | `ResetAllAttributes` (clears + pushes defaults) | ✅ |
| Progression | `ResetAllTalentPoints` | ✅ |
| Progression | `GetDefaultAttributeValue` switch | ✅ |
| Save System | `TalentAllocationEntry[]` (migrated from `string[]`) | ✅ |
| Save System | `AttributeAllocationEntry[]` (migrated from individual ints) | ✅ |
| Save System | `totalTalentPoints` / `totalAttributePoints` (not spent counters) | ✅ |
| Talent System | `TalentTreeGenerator` / `TalentStateService` architecture | ✅ |
| Talent System | Constructor/Initialize split (race-condition-safe) | ✅ |
| Talent System | `TalentStateService` receives `IStatsService` via Initialize | ✅ |
| Talent System | `ResetAll()` strips Permanent modifiers from StatsService | ✅ |
| Talent System | `ValidateLoadedData()` removes corrupt allocations on load | ✅ |
| Init | `GameInitializer` full Awake/Start orchestration | ✅ |
| Init | Gap 1: `ReloadAllBaseStats()` after Progression.Initialize | ✅ |

---

## 17. Remaining Roadmap & Next Steps

### Immediate — Do These in Order

#### Step 1: Fix ApplyOrReplaceModifier bug (🔴 must do first)
See §15 for exact fix. One method, remove the early return, add `wasReplaced` flag. No other changes needed. Verify that calling `ApplyOrReplaceModifier` twice with the same `sourceId` updates the cache correctly.

#### Step 2: Implement Gap 2 — Talent modifiers push into StatsService
Full implementation plan in §4. Summary:
1. Create `ITalentEffectContext`, `TalentEffectSO`, `StatModifierTalentEffectSO`, `PassiveTalentEffectSO`
2. Add `List<TalentEffectSO> effects` to `TalentNodeDataSO`
3. Wire `ApplyNodeEffects` / `RemoveNodeEffects` in `TalentStateService` at all mutation points
4. Test one node with a static stat bonus end-to-end including save round-trip

#### Step 3: Save Round-Trip Validation
After Gap 2, run the full test:
- Spend 3 attribute points in Brawn
- Allocate 2 points to a SpellPower talent node
- Verify: `GetStat(Brawn)` = 13, `GetStat(SpellPower)` = base + 10
- Save, quit, load
- Verify: identical stat values post-load

This validates the entire character pipeline end-to-end and unblocks the Main Menu.

### Next Major Systems (In Order)

#### Phase 6: DerivedStatResolver
Plain C# class. Reads attribute base values and pushes `Permanent` modifiers with reserved sourceIds.

```
Brawn     → MeleeDamage    (+0.5 per point above default 10)
Brawn     → CarryCapacity  (+2kg per point)
Vigor     → MaxHealth      (+5 per point)
Insight   → SpellPower     (+1 per point)
Finesse   → CritMultiplier (+0.02 per point)
Focus     → CritChance     (+0.005 per point)
Willpower → MaxEnergy      (+5 per point)
```

Formula scalars stored in `PlayerStatSO` as designer-tunable floats. `RecalculateAll()` called:
- After `ReloadAllBaseStats()` in `GameInitializer.Start()`
- After `TrySpendAttributePoint()`
- After `ResetAllAttributes()`

Conditional talent effects (e.g., armor-based health bonus) also registered here, unlocked by `PassiveTalentEffectSO` marking the rule as active.

#### Phase 7: Inventory & Equipment System
Equipment uses the same `ApplyOrReplaceModifier` pattern:
- `ModifierCategory.Equipment`
- `sourceId = itemInstanceId` (per-instance, not per-type)
- On equip → `ApplyOrReplaceModifier` per stat bonus
- On unequip → `RemoveModifiersFromSource(itemInstanceId)`
- On upgrade → `ApplyOrReplaceModifier` with same sourceId (replaces old)

#### Phase 8: Main Menu Scene
```
MainMenu (scene, standalone, no player services)
  MainMenuController
    New Game  → SceneManager.LoadScene("PersistentGameScene", LoadSceneMode.Single)
    Continue  → SaveService.SaveExists("default") check, then load scene
    Load Game → (future: slot-based save browser)
    Options   → settings panel
    Quit      → Application.Quit()

PersistentGameScene — loads on New Game / Continue
  GameInitializer.Start() runs once, initializes the full pipeline
```

`GameInitializer` never lives in `MainMenu`. The Main Menu only needs a lightweight `SaveService` instance to check save existence for the Continue button. No stats, no talents, no progression in the Main Menu.
