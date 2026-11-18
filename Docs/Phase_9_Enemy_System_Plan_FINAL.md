
# Phase 9: Enemy System — REFINED Plan (Systemic Architecture + SOLID Compliance)

**Date:** November 17, 2026
**Status:** Refined plan with architectural compliance verified, ready for phased implementation
**Revisions:** Equipment drop corrected to body-as-container (E to interact); perception stats added; animation cancellation hook clarified; SOLID compliance verified against Architecture.md

---

## Architecture Compliance

### SOLID Principles (verified against §5 of Architecture.md)

**Single Responsibility:**
- `EnemySenses`: Only reads perception (vision, hearing)
- `EnemyBehavior`: Only state machine logic
- `EnemyLocomotion`: Only pathfinding/movement commands
- `EnemyMeleeAttack`: Only weapon hitbox and damage delivery
- `EnemyLootDropper`: Only spawns loot container on death
- No class handles multiple concerns

**Open/Closed:**
- `StatusController` (existing) already supports both player and enemy targets via `IStatusTarget`
- New AI behaviors extend via `BehaviorState` enum (closed to modification if using virtual methods later)
- Perception ranges extend via `ActorStatSO` bonuses (no code changes needed for tuning)
- Loot tables could extend via `ActorStatSO.lootTable` (SO-based design pattern)

**Liskov Substitution:**
- `EnemyLocomotion` implements `IMovement`-like contract (internal use; not global)
- `EnemyMeleeAttack` reuses `WeaponHitbox` pipeline via `IDamageReceiver`
- `TargetHealth` implements `IStatusTarget` identically to player adapter

**Interface Segregation:**
- Interfaces are narrow: `EnemySenses` exposes only `CanSee`, `CanHear`, `IsAware`
- No controller exposes unrelated methods (no `EnemyBehavior.UpdateInventory()`)
- Cross-service calls only via interfaces, not concrete types

**Dependency Inversion:**
- Services depend on interfaces: `EnemyBehavior` queries `IMovement`-like Locomotion, not a concrete class
- `IStatusController` is the abstraction; enemies use the same pipeline as player
- `ActorStatSO` is injected via `EnemyStats`, not hardcoded

### Awake / Start Split (§5, CRITICAL RULE)

**Awake():** Registration only
- Create plain C# services (Senses, Behavior, Locomotion, Attack, LootDropper)
- Register into enemy's local cache (not global ServiceLocator unless needed)
- **Do NOT** call ServiceLocator.Get() in Awake
- **Do NOT** cross-inject between enemy services in Awake

**Start():** Data hydration
- Get player reference (via ServiceLocator or FindWithTag, acceptable in enemy context)
- Call Initialize() on Behavior, passing Senses, Locomotion, Attack
- TargetHealth initializes StatusController with `Initialize(IStatusTarget)`

### SO-Based Design (§5)

**ActorStatSO:** Holds immutable design-time data
- Base stats (health, damage, stagger chance, bleed chance, penetration)
- Per-kind immunities (`immunities`, `StatusKind` flags)
- **NEW:** Perception bonuses (`perceptionRangeBonus` float, default 1.0)
- **NEW:** Equipped items (weapon, armor slots)
- **NEW:** Loot table (optional; items to drop on death)

**EnemyStats:** Copies base stats at Awake, applies bonuses
- Perception range = `_baseVisionRange * (1.0 + actorStatSO.perceptionRangeBonus)`
- Hearing range = `_baseHearingRange * (1.0 + actorStatSO.perceptionRangeBonus)`
- All other stats follow same pattern as player

---

## Architecture: Five-Layer AI System (Revised)

### Layer 1: Perception (Vision + Hearing + ActorStat Bonuses)

**`EnemySenses.cs`** (~150 lines)

- **Vision (Line of Sight):**
  - Raycast from enemy head transform to player head transform
  - Check if Physics.Raycast() hits player before obstacles
  - Cache result per 50ms (per-frame raycasts are cheap but batching is cheaper)
  - `_visionRange` base: 20 m, boosted by `ActorStats.GetStat(Perception)` if exists
  - **Actual range = _visionRange * (1.0 + actorStatBonus)**
  - Result: `bool CanSee { get; }`

- **Hearing (Sound Awareness):**
  - Query player velocity: `_playerMovement.Velocity.magnitude`
  - Idle/walk (< 0.5 m/s): Not heard
  - Walking (0.5–5 m/s): Heard at base range (10 m)
  - Sprinting (> 5 m/s): Heard at 1.5× range (15 m)
  - `_hearingRange` base: 10 m, boosted by `ActorStats` bonus
  - Result: `bool CanHear { get; }`

- **Combined Awareness:**
  - `bool IsAware => CanSee || CanHear`
  - Future: deaf debuffs disable hearing; blindness disables vision

**Integration Points:**
- Requires `IMovement` interface from player (velocity accessible)
- `EnemyBehavior` queries `senses.IsAware` each frame

### Layer 2: Behavior (State Machine)

**`EnemyBehavior.cs`** (~200 lines)

```
Idle
  → (IsAware and CanSee) → Alerted
  → (IsAware but only CanHear) → Alerted (search phase)
  → (Time elapsed?) → Idle (reset)

Alerted
  → (In attack range) → Attacking
  → (Can pursue) → Pursuing
  → (Lost all awareness after timeout) → Idle

Pursuing
  → (Can see player or within reach) → Attacking
  → (Lost all awareness after timeout) → Alerted → Idle
  → (NavMesh path complete, player not found) → Idle

Attacking
  → (Cooldown ready) → PlayAttack() (calls animator trigger)
  → (Player out of range) → Pursuing
  → (Staggered via StatusController) → Forces Idle (animation cancellation)
  → (Spell interrupt) → Forces Idle

Dead
  → (On TargetHealth.IsDead) → SpawnLoot() → Despawn
```

**State Queries:**
- `IsAware` from Senses
- `HasArrived` from Locomotion (NavMesh)
- `InAttackRange` local calculation (distance check)
- `IsDead` from TargetHealth
- `OnStatusApplied` event from StatusController (for stagger interrupt)

**Future Extensions:**
- Insert Aggro states (Neutral, Suspicious, Hostile) before Alerted
- Add Group signals (allies joining, calling for help)
- Add Tactical states (Kiting, Flanking, Retreating)
- Add Boss phase transitions (health thresholds)

### Layer 3: Locomotion (NavMesh Pathfinding)

**`EnemyLocomotion.cs`** (~100 lines)

```csharp
public class EnemyLocomotion : MonoBehaviour
{
    private NavMeshAgent _agent;
    
    public void MoveTo(Vector3 target)        // Set destination
    public void Stop()                        // Clear path
    public bool HasArrived { get; }           // Reached within 0.5 m
    public void TurnTo(Vector3 direction)     // Smooth rotate (Lerp)
}
```

**NavMesh Setup (per-scene, one-time):**
1. Window → AI → Navigation
2. Mark terrain as "Walkable", obstacles as "Not Walkable"
3. Agent radius = humanoid (~0.6 m)
4. Height = humanoid (~1.8 m)
5. Bake

**Future Extensions:**
- Formation movement (group around leader)
- High-ground tactics (pathfind to elevated positions)
- Terrain modifiers (slowed by mud, can't cross water)

### Layer 4: Combat (Weapon Hitbox + Animation Cancellation + Recoil)

**`EnemyMeleeAttack.cs`** (~150 lines)

- **Weapon Hitbox Attachment:**
  - Collider is a **child of the weapon mesh**, not the body
  - Position and scale inherited from weapon prefab (e.g., axe extends 0.8 m forward)
  - Visually evident: player sees exactly how far the weapon reaches
  - On `PlayAttack()`: calls `hitbox.BeginSwing()` (identical to player's `WeaponHitbox.cs`)
  - Works with existing damage pipeline: `IDamageReceiver.ReceiveHit(DamageInfo)`

- **Animation Cancellation (Stagger Interrupt):**
  - When `StatusController.Apply(StaggerEffectSO)` fires on enemy:
    - StaggerEffectSO has `AnimatorTrigger = "Stagger"` (SO field)
    - StatusController calls `IStatusTarget.SetAnimatorTrigger("Stagger")`
    - TargetHealth implements this: `_animator.SetTrigger("Stagger")`
    - Animator transitions to Stagger state **immediately** (no finish-swing)
  - `EnemyBehavior.OnStatusApplied()` event fires:
    - Clears `_attackQueued`
    - Resets `_nextAttackTime`
    - Returns state to Idle
  - Result: Enemy recoils, swing interrupted, animation cancels

- **Recoil Feedback:**
  - Knockback applied by StatusController (existing: `TryConsumeImpulse()`)
  - TargetHealth's movement system (if any) consumes impulse
  - Animation + physics = clear visual feedback

**Future Extensions:**
- Parry system: Player weapon hits enemy weapon → blocks enemy attack
- Knockback effects: Knock enemy back on heavy hits
- Interrupt spells: Interrupt enemy casting

### Layer 5: Loot Management (Body-as-Container)

**`EnemyLootContainer.cs` (NEW)** (~150 lines)

**Important Clarification:** The enemy's body IS the loot container. No dropped item prefab.

On death (`TargetHealth.IsDead`):
1. Enemy body remains in place
2. Add `EnemyLootContainer` component to body (or it's already there, dormant)
3. Container holds `InventoryGrid` (6×6 cells, like player inventory)
4. Copy enemy equipment from `ActorStatSO.equippedItems[]` into grid:
   - Iterate `[Weapon, Armor slots]` from ActorStatSO
   - Add each item to InventoryGrid at default position
5. Player can interact: **Walk up to body, press E**
6. Opens a modal window:
   - **Left side:** Player inventory grid (existing)
   - **Right side:** Loot container grid (6×6)
   - **Center:** "Transfer All" button
   - **Drag:** Move items between grids
7. On close or all items transferred: Container despawns, body stays (or fades)

**`EnemyLootContainer.cs` Design:**
```csharp
public class EnemyLootContainer : MonoBehaviour, ILootable
{
    private InventoryGrid _grid;
    private TargetHealth _health;
    
    void Start()
    {
        // Don't open container until dead
        _grid.gameObject.SetActive(false);
        _health.OnDeath += EnableLoot;
    }
    
    void EnableLoot()
    {
        _grid.gameObject.SetActive(true);
        // Copy equipment from ActorStatSO into grid
        foreach (var item in _stats.ActorStatSO.equippedItems)
            if (item != null) _grid.AddItem(item);
    }
    
    public void OnInteract()
    {
        // Open loot UI window (calls existing InventoryUI with container grid)
        ServiceLocator.Get<ILootUI>().Open(_grid, playerInventory);
    }
}
```

**UI Window (`LootContainerWindow.cs`, NEW):**
- **Left:** Player inventory grid (reference existing)
- **Right:** Container inventory grid (reference _grid)
- **Center buttons:** "Transfer All", "Close"
- **Drag behavior:** Existing ItemMover handles item transfers
- Identical to existing inventory UI, but two-way transfer

**Future Extensions:**
- Enemy loot tables: ActorStatSO.lootTable[] (SO-based drops)
- Quest drops: Specific items only if quest active
- Faction loot: Different equipment per faction
- Enemy corpse decay: Fade over time if not looted

---

## Perception Stat Bonus System

**ActorStatSO (new field):**
```csharp
[Range(0, 1)]
[SerializeField] private float _perceptionRangeBonus = 0f;  // 0 = base range, 0.5 = +50%
public float PerceptionRangeBonus => _perceptionRangeBonus;
```

**EnemySenses (apply bonus):**
```csharp
void UpdateVision()
{
    float bonusMultiplier = 1.0f + _stats.ActorStatSO.PerceptionRangeBonus;
    float effectiveRange = _baseVisionRange * bonusMultiplier;
    if (distance > effectiveRange) { CanSee = false; return; }
    // ... raycast logic
}
```

**Result:** All enemy types tunable via ActorStatSO, no code changes needed.

---

## Animation Cancellation + Parry (Future)

### Current (Phase 9.1–9.5)

**Stagger interrupts mid-swing:**
- StatusController fires `SetAnimatorTrigger("Stagger")`
- Animator transitions to Stagger state immediately
- Behavior state resets to Idle

### Future (Phase 10+)

**Parry system:**
- Player weapon collides with enemy weapon at exact time
- Calls `EnemyMeleeAttack.OnParried()`
- Enemy cannot attack for 0.5 s (recovery)
- Combat log shows "Parried"
- No damage dealt

**Spell interrupts:**
- Similar to stagger, but different animation
- Can hat

- [x] **Single Responsibility:** Each class has one reason to change
- [x] **Open/Closed:** New behaviors layer via SO data, not code modification
- [x] **Liskov Substitution:** Enemies use same interfaces as player (IStatusTarget, IDamageReceiver)
- [x] **Interface Segregation:** Narrow interfaces (Senses, Behavior, Locomotion, etc.)
- [x] **Dependency Inversion:** All dependencies injected or via interfaces
- [x] **Awake/Start Split:** Awake for creation, Start for hydration
- [x] **SO-Based Design:** All tuning via ActorStatSO, no hardcoded values
- [x] **Service Locator:** Used sparingly (player lookup acceptable; enemy services stay local)

---

## Phased Implementation (Recommended Order)

### Phase 9.0: Setup + Testing Infrastructure (~1 hour)

**Deliverables:**
- `ActorStatSO` fields: `perceptionRangeBonus`, `equippedItems[]`, `lootTableSO` (optional)
- Test enemy prefab (debug sphere, no animations yet)
- Test scene with player + enemy + NavMesh baked
- Unit tests for perception ranges (RaycastMock?)

**Why first:** Ensures all dependent layers have the data they need

### Phase 9.1: Perception (Vision + Hearing) (~3 hours)

**Deliverables:**
- `EnemySenses.cs` (raycast LOS, speed-based hearing)
- Integration with `IMovement` for player velocity
- Test: Walk around enemy, sprint triggers hearing
- Perception range boosted by ActorStatSO bonus

**Why:** Foundation for all future AI sensing (stealth, ranged, debuffs)
**Depends on:** Phase 9.0 (ActorStatSO fields)
**Enables:** Phase 9.2 (Behavior uses Senses)

### Phase 9.2: Behavior State Machine (~3 hours)

**Deliverables:**
- `EnemyBehavior.cs` (Idle → Alerted → Pursuing → Attacking → Dead)
- State transitions, timeouts, event callbacks
- Integration with Senses, Locomotion, Attack
- Test: Trigger detection, state transitions

**Why:** All behavior flows through here; easy to extend with new states
**Depends on:** Phase 9.1 (Senses), Phase 9.3 (Locomotion)
**Enables:** Phase 9.3, 9.4, 9.5

### Phase 9.3: Locomotion (NavMesh) (~2 hours)

**Deliverables:**
- `EnemyLocomotion.cs` (NavMesh wrapper)
- NavMesh setup per test scene
- Test: Enemy navigates around obstacles

**Why:** Movement independent of behavior; can be used by future tactics
**Depends on:** Nothing new (only Unity NavMesh setup)
**Enables:** Phase 9.2 (Behavior uses Locomotion)

### Phase 9.4: Combat (Weapon Hitbox + Animation Cancel) (~3 hours)

**Deliverables:**
- `EnemyMeleeAttack.cs` (weapon-mounted hitbox, animation cancel)
- Animator: Idle, Walk, Attack, Stagger, Dead states + transitions
- Integration with StatusController for interrupt
- Test: Attack hits player, stagger cancels mid-swing

**Why:** Tests stagger system end-to-end; same pipeline as player
**Depends on:** Phase 9.2 (Behavior calls PlayAttack)
**Enables:** Phase 9.5 (Loot on death)

### Phase 9.5: Loot System (Body-as-Container) (~2.5 hours)

**Deliverables:**
- `EnemyLootContainer.cs` (body interaction, grid UI)
- `LootContainerWindow.cs` (two-way inventory UI)
- Integration with InventoryGrid and ItemMover
- ActorStatSO: `equippedItems[]` populated
- Test: Kill enemy, walk up, press E, see loot, drag items to inventory

**Why:** Completes the MVP loop; testable end-to-end
**Depends on:** Phase 9.4 (death event), existing inventory system
**Enables:** Equipment progression, NPC drops (Phase 9.6+)

### Phase 9.6: Animator Setup + Integration (~2 hours)

**Deliverables:**
- Enemy Animator: Idle ↔ Walk (Speed param), Idle → Attack (trigger), Attack → Stagger (trigger), Stagger → Idle, Idle → Dead
- Reuse clips from Kevin Iglesias humanoid pack
- Tuning: Speed curves, transition durations
- Test: All state transitions smooth, no popping

**Why:** Can layer in anytime after Phase 9.4; not on critical path
**Depends on:** Phase 9.2 (Behavior state machine ready)
**Enables:** Phase 9.7 (full playtesting)

### Phase 9.7: Full Integration Test + Tuning (~2 hours)

**Deliverables:**
- Multiple enemies in scene
- Player vs. enemies full combat loop
- Perception ranges tuned (see at X distance, hear sprint at Y)
- Attack feel (cooldown, reach, damage)
- Loot pickup workflow
- Combat log verification

**Why:** MVP complete; ready for extended play
**Depends on:** Phases 9.0–9.6
**Enables:** Phase 9.8+ (extended AI)

---

## Total Scope: ~17 Hours (Phases 9.0–9.7)

| Phase | Component | Hours | Status |
|-------|-----------|-------|--------|
| 9.0 | Setup + Data | 1 | Foundation |
| 9.1 | Perception | 3 | MVP critical |
| 9.2 | Behavior | 3 | MVP critical |
| 9.3 | Locomotion | 2 | MVP critical |
| 9.4 | Combat | 3 | MVP critical |
| 9.5 | Loot | 2.5 | MVP critical |
| 9.6 | Animator | 2 | Polish |
| 9.7 | Integration | 2 | Polish |
| **Total** | | **17.5** | **MVP Complete** |

---

## Success Criteria (MVP Complete)

- [ ] Enemy detects player by sight (raycast LOS, obstacles block)
- [ ] Enemy detects player by hearing (sprinting louder than walking)
- [ ] Perception ranges boosted by ActorStatSO bonus (different enemy types, different ranges)
- [ ] State machine transitions smooth (Idle → Alerted → Pursuing → Attacking)
- [ ] Enemy navigates around obstacles via NavMesh (no clipping walls)
- [ ] Weapon reach matches weapon mesh (child collider, visible reach)
- [ ] Stagger mid-swing cancels animation (player sees recoil, swing stops)
- [ ] Stagger resets attack cooldown (no spam after interrupt)
- [ ] Player and enemy both take damage
- [ ] Status effects (bleed, stagger) apply identically
- [ ] Enemy at 0 HP stays in place (corpse visible)
- [ ] Body interactive (press E when close and looking)
- [ ] Loot windoory, right: enemy loot, center: transfer all)
- [ ] Can drag items between inventories
- [ ] Can equip enemy equipment
- [ ] Multiple enemies simultaneous (no serialization)
- [ ] Combat log shows all interactions

---

## Systemic Extensions (Phase 9.8+, Not in MVP)

Architecture supports all without redesign:

1. **Aggro States** (3–5 h): Insert Neutral/Suspicious before Alerted
2. **Group Behavior** (5–7 h): Allies join fight, coordinate
3. **Boss Phases** (5–10 h): Multi-phase on health thresholds
4. **Enemy Types** (3–5 h): Variants with spells, immunities, special loot
5. **Tactics** (4–6 h): Dodge, kite, retreat, terrain
6. **Persistence** (2–3 h): Save state, respawns
7. **Reputation** (2–3 h): Faction interactions

---

## Architecture Diagrams

### Dependency Flow

```
EnemyBehavior (orchestrator)
├─ Queries: EnemySenses (IsAware)
├─ Commands: EnemyLocomotion (MoveTo, Stop)
├─ Commands: EnemyMeleeAttack (PlayAttack)
├─ Listens: StatusController (OnStatusApplied event)
└─ Reads: TargetHealth (IsDead)

EnemySenses
├─ Reads: Player IMovement (Velocity)
├─ Raycasts: Physics layer
└─ Applies: ActorStatSO.PerceptionRangeBonus

EnemyMeleeAttack
├─ Owns: Weapon hitbox collider (child of weapon mesh)
├─ Fires: OnTriggerStay → IDamageReceiver.ReceiveHit
├─ Listens: StatusController (Stagger event → CancelSwing)
└─ Calls: DamageProfileSO.Build (damage calculation)

EnemyLootContainer
├─ Listens: TargetHealth.OnDeath
├─ Creates: InventoryGrid (6×6)
├─ Owns: IInteractable interface (E to open)
└─ Shows: LootContainerWindow (two-way transfer)
```

### Class Hierarchy

```
EnemyController (MonoBehaviour, composition root)
├─ Awake: Create and register services
├─ Start: Hydrate services
└─ Services:
    ├─ EnemySenses (MonoBehaviour)
    ├─ EnemyBehavior (MonoBehaviour)
    ├─ EnemyLocomotion (MonoBehaviour, wraps NavMeshAgent)
    ├─ EnemyMeleeAttack (MonoBehaviour)
    ├─ EnemyLootContainer (MonoBehaviour)
    ├─ TargetHealth (MonoBehaviour, existing)
    ├─ StatusController (MonoBehaviour, existing)
    └─ EnemyStats (MonoBehaviour, existing)
```

---

## Next Steps (Your Approval)

1. **Does this phased breakdown feel right?** (9.0 setup, 9.1–9.5 MVP critical, 9.6–9.7 polish)
2. **Any adjustments to perception ranges or bonuses?**
3. **Equipment container window:** Should it show item details (durability, mods, etc.) on hover?
4. **Ready to switch to Act mode and implement Phase 9.0 first?**

---

**Documentation:** This file is the final spec. Code templates in each phase section. Ready to build.
