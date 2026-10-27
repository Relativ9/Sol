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

The roadmap from scratch to a working character pipeline, in the order it was built.

### Phase 1 — Core Infrastructure (DONE)
- Service Locator, SO-based GameEvent system, `GameInitializer` composition root with strict Awake/Start split
- `SaveService` / `ISaveManager` with lazy (debounced) and immediate save
- `PlayerSaveData` schema (stable, append-only enums; see §12)

### Phase 2 — Physics & Movement (DONE)
- `GravityController` owns vertical force; `Movement` owns horizontal velocity and slope projection
- Slowfall/ledge bug fixed via `GetMovementGroundNormal()` gating

### Phase 3 — Stat System (DONE)
- `StatTypeEnum` for compile-time safety; `StatModifier` struct; `ModifierType` and `ModifierCategory`
- `StandardStatCalculator` (pure), `StatsService` with typed dictionaries, derived cache, reverse lookup, batched event

### Phase 4 — Progression & Attributes (DONE)
- Attribute pool derived from level (`startingAttributePoints + levels × attributePointsPerLevel`); saved value is ignored on load
- Spend, refund, reset, reload; attribute allocations saved as deltas above default

### Phase 5 — Talent → StatsService (DONE)
- `TalentEffectSO` strategy assets (`StatModifierTalentEffectSO`, `PassiveTalentEffectSO` stub) via `ITalentEffectContext`
- Effects applied on allocate, reduced on remove, stripped on reset, replayed on load
- Known limitation: multiple effects on one node share `sourceId = nodeId` and overwrite each other (see §15)

### Phase 6 — Derived Stat Resolver (DONE)
- `DerivedStatResolver` maps attributes to secondary stats with tunable scalars on `PlayerStatSO`
- Reserved `Derived_*` sourceIds; recalculated on spend, refund, reset and load

### Phase 6.5 — Player Stats & Menu (DONE)
- K opens a full-screen menu with a vertical tab bar (`IMenuTab`, `MenuTabBar`)
- Talents tab (wheel, arrows, mouse-wheel tier scroll, tree header, outlines in tree colour)
- Player Stats tab (attributes with spend/refund, vitals, derived stats in a 4-column grid)
- Menu hides the HUD while open

### Phase 7 — Damage & Status (IN PROGRESS: first pass built, not yet fully playtested)
- Damage is typed data: `DamageSO` builds `DamageInfo`, delivered via `IDamageReceiver`
- One shared `DamageTypeTableSO` behind `IDamageTypeTable`, registered by `GameInitializer`, validated at startup
- `DamageCalculator`: deflection, type mitigation, environmental resistance, stagger/bleed chances
- `IChanceRoller`: the single source of randomness for every chance
- Bleed: timed, stackable status, one tick per second of scaled time (pauses with the game)
- Targets: `TargetHealth` with a world-space health bar; `Projectile` and `ProjectileTurret`
- Combat log visibility: player damage always, other damage within 20 m (`LogRules`)
- Sword swings use a `DamageSO` when assigned (animation-driven, untested until animations exist)

### Phase 8 — Inventory & Equipment (PLANNED)
- Equipment sets `RigidWeight` / `FlexWeight` and pushes `Equipment` modifiers
- `sourceId = itemInstanceId`; on unequip, `RemoveModifiersFromSource`

### Phase 9 — Enemies & Actors (PLANNED)
- `ActorStatSO` for NPCs (aggression, hearing, vision, behaviour, personality, profession, interests)
- Until then, targets have zero mitigation
- Enemy AI is a dedicated future task

### Phase 10 — Main Menu & Full Save/Load (PLANNED)
- Separate `MainMenu` scene; `GameInitializer` stays in the persistent scene
- Full round-trip: load a character and verify identical derived stats

---

## 3. Progress Summary — Where We Are

This is a status board for re-orientation after a break.

| System | Status | Notes |
|---|---|---|
| Service Locator | ✅ Complete | `TryGet` for per-frame lookups that must not log |
| GameEvent SO system | ✅ Complete | |
| GameInitializer Awake/Start split | ✅ Complete | Validates the damage table at startup |
| SaveService / ISaveManager | ✅ Complete | Lazy (2 s debounce) and immediate; skipped while the repair prompt is open |
| PlayerSaveData schema | ✅ Complete | Attribute allocations now saved |
| Save validation + repair prompt | ✅ Complete | Repairs broken talent pools; backup on new game |
| Stat system | ✅ Complete | Append-only `StatTypeEnum` |
| Progression / attributes | ✅ Complete | Pool derived from level |
| Talent tree (data, effects, UI) | ✅ Complete | Verified in Unity: allocation, stacking, tree focus |
| DerivedStatResolver | ✅ Complete | Verified in Unity: spend and persistence |
| K menu + tab system | ✅ Complete | Talents and Player Stats tabs |
| Virtual cursor owns OS cursor | ✅ Complete | Camera cannot unlock it while active |
| Damage pipeline | 🟡 First pass | Built, compiled and tested on dummy and player; needs fuller playtest |
| Stagger / bleed | 🟡 First pass | Bleed verified in play; stagger logs only (no interrupt yet) |
| Target health + world bar | 🟡 First pass | Verified on the dummy |
| Projectiles | 🟡 First pass | Verified against the player and the target |
| Sword damage (DamageSO) | ⬜ Blocked | Needs attack animations |
| Weight stats → armour | ⬜ Not started | Equipment sets the values |
| Enemy stats (ActorStatSO) | ⬜ Not started | Targets have zero mitigation until then |
| Inventory / equipment | ⬜ Not started | |
| Main Menu scene | ⬜ Not started | |

---

## 4. Active Task — What We Are Doing Right Now

**Current focus: Phase 7, the damage and status pass, now committed.** The first pass is built and has been tested on the training dummy, the projectile turret, and the player.

Open items on this phase, in rough order:
1. Stagger has no effect yet beyond the log line (interrupt and energy drain are not built).
2. Clamp of tree scrolling and counter-rotated labels are done; no further talent-screen work is open.
3. Sword damage through `AttackEventHandler`'s `DamageSO` path needs attack animations to test.
4. Weight stats are wired but stay at zero until equipment exists.

Everything above the damage pass is complete. Design sections (§5 to §14) describe the rules as currently understood, and are placeholders until the design bible is written.

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
    _progressionService.ReloadAllBaseStats(); // Must stay here: needed on load, see §15

    // 4. Calculate talent tree node positions (no UI)
    _talentTreeGenerator = ServiceLocator.Get<ITalentTreeGenerator>();
    _talentTreeGenerator?.LoadData();

    // 5. Hydrate talent allocations and inject StatsService
    var talentState = ServiceLocator.Get<ITalentStateService>() as TalentStateService;
    talentState?.Initialize(saveData, _statsService);
    // Initialize also replays talent effects (Permanent modifiers) for loaded allocations

    // 6. Sync talent point counter from loaded allocations
    int derivedSpent = talentState?.GetTotalAllocatedPoints() ?? 0;
    _progressionService.SetSpentTalentPoints(derivedSpent);

    // Step 7: DerivedStatResolver.RecalculateAll() (implemented, called after each attribute change)
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
    ↓ stacked by TalentStateService → ApplyOrReplaceModifier(Permanent) [implemented]
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

// Implemented: talent effects are applied and replayed on load
public List<TalentEffectSO> effects;
```

### Prerequisite Logic
Currently: ANY single prerequisite met = node is unlocked. Review and potentially change to ALL prerequisites before building out the actual talent tree content.

### TalentEffectSO Strategy Pattern
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
4. DerivedStatResolver.RecalculateAll()                → Brawn→MeleeDamage etc. (implemented)
5. TalentStateService.Initialize(saveData, stats)      → loads allocations
   + ApplyNodeEffects for each                         → replays Permanent modifiers
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

### ✅ FIXED: ApplyOrReplaceModifier — replace path skipped cache refresh
The early return in the replace branch skipped the reverse lookup and cache refresh, so a second talent point never updated the stat. Fixed by setting `wasReplaced` and letting both branches reach a shared tail. Kept here as a warning: any future early return in that method will reintroduce it.

### 🟡 WATCH-OUT: Partial respec and keys at zero
When refunding an attribute to 0, keep the key in `_attributeAllocations` with value 0. Deleting the key makes `ReloadAllBaseStats()` skip that stat, so `StatsService` keeps the old raised base. Call `RebuildBaseStat(statType)` explicitly after a decrement.

### 🟡 WATCH-OUT: ReloadAllBaseStats must stay in the init sequence
On a new game it iterates nothing and looks redundant, but on load it is essential. `ResolveDerived` is also called from it, so removing it breaks derived stats on load.

### 🟡 WATCH-OUT: Talent effects sharing a sourceId
Every effect on one node uses `sourceId = nodeId`. Two effects on the same node that hit the same stat and category overwrite each other. Single-effect nodes are fine. Fix (per-effect key, e.g. `nodeId + index`) before authoring multi-effect nodes.

### 🟡 WATCH-OUT: Prerequisite logic is ANY, not ALL
`PrerequisitesMet()` passes if any one prerequisite is allocated. Confirm with the design bible before final tree content; change to ALL if required.

### 🟡 WATCH-OUT: Saved enums are stored as integers
Confirmed by test: `JsonUtility` writes enums as their integer value (`"statType":4`). New members go only at the end of `StatTypeEnum`, `DamageType` and `DamageTag`. Never insert, reorder, or rename.

### 🟡 WATCH-OUT: Trigger messages need a Rigidbody
Unity routes `OnTriggerEnter/Stay/Exit` to the object that owns the collider's Rigidbody, or to the collider's own object if there is none. A trigger on a child, or a moving projectile, needs a Rigidbody (kinematic) on the object that should receive the message. `Projectile` adds its own in `Awake`.

### 🟡 WATCH-OUT: Only one PlayerVitals
`GameInitializer` finds the `PlayerVitals` on the player. It must not create one itself, or services will register the wrong instance.

### 🟡 WATCH-OUT: Health bar positioning uses Screen.height
`WorldHealthBar` converts a world point to a screen point and positions it in UI Toolkit pixels. Verify it stays aligned if the UI scales.

### 🟡 WATCH-OUT: DontDestroyOnLoad / duplicate scene load
If `PersistentGameScene` ever loads additively a second time, `GameInitializer.Start()` fires again and double-registers services. Add a static `_isInitialized` guard if you suspect scene-loading bugs.

---

## 16. Completed Work Checklist

| System | Task | Status |
|---|---|---|
| Infrastructure | Service Locator (`Get`, and `TryGet` for per-frame lookups) | ✅ |
| Infrastructure | SO GameEvent system | ✅ |
| Infrastructure | Awake/Start split enforced | ✅ |
| Infrastructure | `ISaveManager` (lazy + immediate, skipped during repair prompt) | ✅ |
| Infrastructure | `PlayerSaveData` schema (append-only enums) | ✅ |
| Physics | Ledge/slowfall `HasLookAheadHit`, `GetMovementGroundNormal()` | ✅ |
| Physics | `GravityController` reads `GravityMultiplier` from `StatsService` | ✅ |
| Stat System | `StatTypeEnum`, `StatModifier`, `ModifierType`, `ModifierCategory` | ✅ |
| Stat System | `StandardStatCalculator`, derived cache, reverse lookup, batched event | ✅ |
| Stat System | `RigidWeight`, `FlexWeight`, `CovertMovement`, `Tinker`, stagger/bleed resistances | ✅ |
| Progression | Attribute pool derived from level (saved value ignored on load) | ✅ |
| Progression | Spend, refund, reset, reload, and `DerivedStatResolver` | ✅ |
| Save System | Attribute allocations saved | ✅ |
| Save System | `SaveValidator` + repair prompt for broken talent pools | ✅ |
| Save System | Backup on new game (`save_default.backup.json`) | ✅ |
| Talent System | `TalentEffectSO` strategy assets (stat modifier, passive stub) | ✅ |
| Talent System | Effects applied, reduced, stripped on reset, replayed on load | ✅ |
| Talent System | Tree focus (arrows, mouse-wheel tier scroll, eased rotation) | ✅ |
| Talent System | Tree header, outlines in tree colour, counter-rotated labels | ✅ |
| UI | K menu: full-screen, vertical tabs (`IMenuTab`, `MenuTabBar`) | ✅ |
| UI | Player Stats tab (attributes, vitals, 4-column derived grid) | ✅ |
| UI | HUD (vitals bars) and combat log, hidden while the menu is open | ✅ |
| UI | Virtual cursor owns OS cursor; camera cannot unlock it while active | ✅ |
| UI | Modal dialog via `IUIManager.ShowModal` | ✅ |
| Damage | `DamageType` (append-only), `DamageTag`, `DamageSO`, `DamageInfo` | ✅ |
| Damage | `IDamageReceiver` implemented by player, targets | ✅ |
| Damage | `DamageTypeTableSO` as one shared table behind `IDamageTypeTable` | ✅ |
| Damage | Startup validation of the table (missing and duplicate rows) | ✅ |
| Damage | `DamageCalculator`: deflection, mitigation, environmental, status chances | ✅ |
| Damage | `IChanceRoller`: single source of randomness | ✅ |
| Damage | `StatusChanceCalculator` (subtractive armour rules, 5% floor) | ✅ |
| Damage | Bleed: stackable, one tick per second of scaled time | ✅ |
| Damage | Stagger: logged (no interrupt yet) | 🟡 |
| Combat | `LogRules`: player always shown, others within 20 m | ✅ |
| Combat | `TargetHealth` + `WorldHealthBar` on the training dummy | ✅ |
| Combat | `Projectile` (adds its own kinematic Rigidbody) and `ProjectileTurret` | ✅ |
| Combat | Sword swings use `DamageSO` when assigned | 🟡 (needs animations) |
| Init | `GameInitializer` orchestration; `ReloadAllBaseStats` after Progression init | ✅ |

---

## 17. Remaining Roadmap & Next Steps

### Where we are
The character pipeline is working end to end: attributes, talents, derived stats, saves, the menu, and the first damage pass. What's left before the pipeline is complete is **Phase 7 (damage and status, finishing)** and **Phase 8 (equipment)**. Phases 9 and 10 come after.

### Phase 7 — Damage & Status: open items
1. **Stagger effect.** Currently a log line only. Needs the interrupt (0.5 s), the knockback, and the 15 energy drain, per the armour doc.
2. **Sword damage.** `AttackEventHandler` already uses a `DamageSO` when one is assigned. It needs attack animations to test, so it's blocked until they exist.
3. **Remove temporary harnesses** once animations exist. The swing path itself stays.

### Phase 8 — Inventory & Equipment
1. Equipment sets `RigidWeight` and `FlexWeight` from the worn pieces, which already feed the stagger and bleed maths.
2. Armour value, type multiplier and material efficiency feed `ArmorValue`, per the armour doc.
3. `ModifierCategory.Equipment` modifiers, with `sourceId = itemInstanceId`, and removal on unequip.
4. Inventory grid and item packing, per the design doc.

### After Phase 8
- **Phase 9: Enemies & Actors.** `ActorStatSO` for NPCs, so targets get real mitigation. Enemy AI is a separate task.
- **Phase 10: Main Menu.** Separate scene, New Game / Continue / Load, and a full character round-trip check.

### Deferred to the design bible
- Enemy AI, aggression, hearing and vision.
- Trap and environmental damage tuning, and which traps use which types.
- The "current effects" screen for active statuses.
- Prerequisite rule (ANY vs ALL).

---

