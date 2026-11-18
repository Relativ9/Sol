
# Phase 9: Enemy System — Revised Plan (Systemic Architecture)

**Date:** November 17, 2026
**Status:** Plan for encounter-ready enemy with systemic architecture

---

## Updated Design Requirements

Your feedback emphasizes **testability** and **systemic expansion**:

1. **Pathfinding + Vision + Hearing** — Required for future stealth, ranged, debuff, climbing systems
2. **Animation Cancelling** — For stagger/spell effects and recoil feedback
3. **Weapon-Mounted Hitbox** — Visual feedback on enemy reach
4. **Loot on Death** — Lootable containers for equipment/inventory

All deferred features (aggro states, group AI, boss behaviors, etc.) must layer cleanly on top.

---

## Architecture: Layered AI System

### Layer 1: Perception (Vision + Hearing)

**`EnemySenses.cs`** (NEW)
- **Line of Sight (Vision):**
  - Raycast from enemy head to player head
  - Check if `Physics.Raycast()` hits player before hitting obstacles
  - Cache result per frame (don't raycast every Update)
  - "Can See" = true if LOS + within `_visionRange`

- **Sound Awareness (Hearing):**
  - Enemies "hear" when player is moving above a speed threshold
  - Walking = heard at `_hearingRange` (default 10 m)
  - Sprinting = heard at `_hearingRange * 1.5` (default 15 m)
  - Idle/crouching = silent
  - Query player velocity; if `speed > 0.5`, fire a spherecast at player to check if any enemy is in range
  - Result: `bool CanHear { get; }`

- **Combined Awareness:**
  - `bool IsAware => CanSee || CanHear`
  - Different states react differently (see Layer 2)

### Layer 2: State Machine (Behavior)

**`EnemyBehavior.cs`** (NEW)
- Simple finite state machine:
  ```
  Idle
    → (Sees or hears player?) → Alerted
    → (Attack ready?) → Attacking
  
  Alerted
    → (Lost sight & hearing?) → Idle (after search timeout)
    → (In attack range?) → Attacking
    → (Not in range) → Pursuing
  
  Pursuing
    → (Uses NavMesh to path to last known player pos)
    → (Lost LOS & hearing?) → Alerted → Idle
    → (Player in attack range?) → Attacking
  
  Attacking
    → (Attack cooldown ready?) → Deliver hit
    → (Player out of range?) → Pursuing
    → (Staggered?) → Idle (animation cancellation)
  
  Dead
    → Spawn loot container
    → Despawn (or ragdoll fade)
  ```

### Layer 3: Movement (Locomotion + Pathfinding)

**`EnemyLocomotion.cs`** (NEW)
- NavMesh Agent wrapper:
  - `MoveTo(Vector3 target)` — Path toward target
  - `Stop()` — Hold position
  - `TurnTo(Vector3 direction)` — Rotate smoothly
  - Query `remainingDistance` to detect "arrived"
  - Handles obstacle avoidance automatically (NavMesh)

### Layer 4: Combat (Attack + Reaction)

**`EnemyMeleeAttack.cs`** (revised from simple hitbox)
- **Weapon Hitbox Attachment:**
  - Hitbox collider is a child of the weapon mesh (not body)
  - Position/scale defined in weapon prefab itself
  - Visual feedback: player sees exactly how far reach goes
  - On attack: `hitbox.BeginSwing()` (similar to `WeaponHitbox.cs`)

- **Animation Cancellation:**
  - Animator parameter `CanCancelAttack` (bool)
  - When staggered or spell interrupts, `CombatStateController` calls `CancelSwing()`
  - Enemy Animator transitions to Stagger immediately (no finish-swing)
  - Applies to enemies exactly as it applies to player

- **Recoil/Flinch on Hit:**
  - When hit, `StatusController.Apply()` fires `AnimatorTrigger` (e.g., "Stagger")
  - Stagger animation plays (recoil)
  - Clears `_attackQueued` and resets `_nextAttackTime`
  - Player sees visual feedback (enemy jerks back)

### Layer 5: Loot Management

**`EnemyLootDropper.cs`** (NEW)
- On death (`TargetHealth.IsDead`):
  - Spawn a loot container prefab at enemy position
  - Container holds `InventoryGrid` (like player inventory, but drop-only)
  - Copy enemy equipment into it (iterate `ActorStatSO.equippedItems`?)
  - Copy enemy inventory (if enemies will have inventory)
  - Container persists until player loots it or leaves the area
  - On pickup: items move to player inventory via `IPlayerPickup`

---

## Implementation (Systemic Order)

### Phase 9.1: Perception (Vision + Hearing) — ~3 hours

**`EnemySenses.cs`**
```csharp
public class EnemySenses : MonoBehaviour
{
    [SerializeField] private float _visionRange = 20f;
    [SerializeField] private float _hearingRange = 10f;
    [SerializeField] private float _sprintHearingMultiplier = 1.5f;
    [SerializeField] private float _sprintThreshold = 5f;  // m/s
    
    private Transform _playerTransform;
    private IMovement _playerMovement;  // To read velocity
    private Transform _head;  // Eye position (for raycast)
    private bool _canSeeCached;
    private float _cacheTime;
    
    public bool CanSee { get; private set; }
    public bool CanHear { get; private set; }
    public bool IsAware => CanSee || CanHear;
    
    void Update()
    {
        // Raycast cache per frame
        if (Time.time > _cacheTime + 0.05f)  // Update every ~50ms
        {
            UpdateVision();
            UpdateHearing();
            _cacheTime = Time.time;
        }
    }
    
    void UpdateVision()
    {
        if (_playerTransform == null) { CanSee = false; return; }
        
        float dist = Vector3.Distance(_head.position, _playerTransform.position);
        if (dist > _visionRange) { CanSee = false; return; }
        
        Vector3 toPlayer = (_playerTransform.position - _head.position).normalized;
        if (Physics.Raycast(_head.position, toPlayer, dist))
            CanSee = false;  // Hit something before player
        else
            CanSee = true;
    }
    
    void UpdateHearing()
    {
        if (_playerMovement == null) { CanHear = false; return; }
        
        float playerSpeed = _playerMovement.Velocity.magnitude;
        if (playerSpeed < 0.5f) { CanHear = false; return; }
        
        float hearRange = playerSpeed > _sprintThreshold
            ? _hearingRange * _sprintHearingMultiplier
            : _hearingRange;
        
        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        CanHear = dist < hearRange;
    }
}
```

**Wiring:**
- Enemy gets `EnemySenses` component
- `OnEnable`, find player and cache `IMovement`
- `EnemyBehavior` queries `senses.IsAware`

**Testing:**
- Walk around enemy at distance: not seen or heard
- Sprint toward enemy: heard first, then seen
- Hide behind wall: not seen (raycast blocked)
- Stop moving: enemy forgets you (after timeout)

---

### Phase 9.2: Behavior State Machine — ~3 hours

**`EnemyBehavior.cs`**
```csharp
enum BehaviorState { Idle, Alerted, Pursuing, Attacking, Dead }

public class EnemyBehavior : MonoBehaviour
{
    private EnemySenses _senses;
    private EnemyLocomotion _locomotion;
    private EnemyMeleeAttack _attack;
    private TargetHealth _health;
    
    private BehaviorState _state = BehaviorState.Idle;
    private Vector3 _lastKnownPlayerPos;
    private float _alertTimeout = 5f;
    private float _alertedAt;
    
    void Update()
    {
        if (_health.IsDead)
        {
            _state = BehaviorState.Dead;
            return;
        }
        
        switch (_state)
        {
            case BehaviorState.Idle:
                if (_senses.IsAware)
                {
                    _state = BehaviorState.Alerted;
                    _lastKnownPlayerPos = _senses.PlayerPosition;  // Cache it
                    _alertedAt = Time.time;
                }
                break;
            
            case BehaviorState.Alerted:
                if (Vector3.Distance(transform.position, _senses.PlayerPosition) < _attackRange)
                    _state = BehaviorState.Attacking;
                else if (_senses.IsAware)
                    _state = BehaviorState.Pursuing;
                else if (Time.time > _alertedAt + _alertTimeout)
                    _state = BehaviorState.Idle;
                break;
            
            case BehaviorState.Pursuing:
                _locomotion.MoveTo(_lastKnownPlayerPos);
                if (_senses.CanSee) _lastKnownPlayerPos = _senses.PlayerPosition;
                
                if (!_senses.IsAware && _locomotion.HasArrived)
                    _state = BehaviorState.Idle;
                else if (Vector3.Distance(transform.position, _senses.PlayerPosition) < _attackRange)
                    _state = BehaviorState.Attacking;
                break;
            
            case BehaviorState.Attacking:
                _locomotion.Stop();
                if (Time.time > _nextAttackTime)
                    _attack.PlayAttack();
                if (Vector3.Distance(transform.position, _senses.PlayerPosition) > _attackRange)
                    _state = BehaviorState.Pursuing;
                break;
        }
    }
}
```

**Wiring:**
- `EnemyBehavior` queries `EnemySenses`, commands `EnemyLocomotion` and `EnemyMeleeAttack`
- On stagger (`StatusController.Apply(Stagger)`), call `behavior.ForceStateChange(Idle)` to drop animation-based state

---

### Phase 9.3: Locomotion (NavMesh) — ~2 hours

**`EnemyLocomotion.cs`**
```csharp
public class EnemyLocomotion : MonoBehaviour
{
    private NavMeshAgent _agent;
    
    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();
    }
    
    public void MoveTo(Vector3 target)
    {
        _agent.SetDestination(target);
    }
    
    public void Stop()
    {
        _agent.ResetPath();
    }
    
    public bool HasArrived => !_agent.hasPath || _agent.remainingDistance < 0.5f;
    
    public void TurnTo(Vector3 direction)
    {
        Quaternion lookDir = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookDir, Time.deltaTime * 5f);
    }
}
```

**NavMesh Setup:**
- Bake NavMesh in the scene (Window → AI → Navigation)
- Mark terrain as "Walkable"
- Mark obstacles as "Not Walkable"
- Agent size = humanoid (~0.6 m radius)

---

### Phase 9.4: Combat (Weapon Hitbox + Animation Cancellation) — ~3 hours

**`EnemyMeleeAttack.cs`** (revised)
```csharp
public class EnemyMeleeAttack : MonoBehaviour
{
    [SerializeField] private Collider _weaponHitbox;  // Child of weapon
    [SerializeField] private DamageProfileSO _damage;
    [SerializeField] private float _attackCooldown = 2f;
    
    private Animator _animator;
    private EnemyStats _stats;
    private float _nextAttackTime;
    private bool _hitThisSwing;
    
    public void PlayAttack()
    {
        _animator.SetTrigger("Attack");
        _nextAttackTime = Time.time + _attackCooldown;
    }
    
    public void CancelSwing()
    {
        // Stagger or spell interrupts the swing
        _animator.SetTrigger("Stagger");  // Animator handles the cancel
        _hitThisSwing = false;
    }
    
    void Update()
    {
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        bool isAttacking = info.IsTag("Attack");
        if (!isAttacking && _hitThisSwing)
            _hitThisSwing = false;  // Swing ended, reset for next
    }
    
    void OnTriggerStay(Collider other)
    {
        if (_hitThisSwing) return;
        if (!_animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) return;
        
        IDamageReceiver receiver = other.GetComponentInParent<IDamageReceiver>();
        if (receiver == null) return;
        
        _hitThisSwing = true;
        DamageInfo hit = _damage.Build(..., _stats.Stats.GetStat(StatTypeEnum.StaggerChance), ...);
        receiver.ReceiveHit(hit);
    }
}
```

**Animator Changes:**
- Add `Staggetrigger, no exit time)`
- `Stagger → Idle (exit time 1.0, auto-return)`
- Stagger state plays a recoil animation

**StatusController Integration:**
- When `StaggerEffectSO.Apply()` fires on an enemy, it calls `CombatLocked` which blocks new attacks
- `EnemyMeleeAttack.CancelSwing()` is called by behavior state machine or directly by status

---

### Phase 9.5: Loot System — ~2 hours

**`EnemyLootDropper.cs`** (NEW)
```csharp
public class EnemyLootDropper : MonoBehaviour
{
    [SerializeField] private GameObject _lootContainerPrefab;  // Has WorldItem + InventoryGrid
    [SerializeField] private Transform _dropPoint;  // Position to spawn loot
    
    private TargetHealth _health;
    private EnemyStats _stats;
    
    void Start()
    {
        _health.OnDeath += DropLoot;
    }
    
    void DropLoot()
    {
        GameObject container = Instantiate(_lootContainerPrefab, _dropPoint.position, Quaternion.identity);
        InventoryGrid grid = container.GetComponent<InventoryGrid>();
        
        // Copy enemy equipment
        // TODO: Implement enemy equipment assignment (ActorStatSO → equipped items)
        // For now, add a few test items
        
        // Copy enemy inventory
        // TODO: Implement if enemies will have inventory
    }
}
```

**Loot Container Prefab:**
- Empty GameObject, tag "Loot"
- Child: Capsule or chest mesh (visual)
- Child: Trigger collider (for pickup detection)
- Components:
  - `WorldItem`: Assigned at runtime with a dummy ItemDataSO
  - `InventoryGrid`: Empty grid, ~10x5 cells
  - `InventoryUIDisplay`: Shows when player looks at it
  - `OnTriggerEnter`: Call `IPlayerPickup.OnPickup()`

**Equipment Export:**
- `ActorStatSO` needs an `equippedItems` array (Weapon, Armor, Accessories)
- On death, copy those to loot container grid

---

## Revised Success Criteria (MVP + Systemic Foundation)

- [ ] Enemy detects player by sight (raycast) or sound (sprint)
- [ ] Enemy state machine: Idle → Alerted → Pursuing → Attacking
- [ ] Enemy uses NavMesh to navigate around obstacles
- [ ] Weapon hitbox is visually part of the weapon mesh
- [ ] Player can stagger enemy mid-swing (animation cancellation)
- [ ] Enemy plays recoil animation on stagger
- [ ] Enemy attacks reset cooldown on stagger
- [ ] Player takes damage from enemy attack
- [ ] Enemy dies at 0 HP and spawns loot container
- [ ] Loot container holds enemy equipment
- [ ] Player can pick up and equip enemy items

---

## Scope Revised: ~14–16 hours

| Component | Hours | Depends On |
|-----------|-------|------------|
| `EnemySenses` (raycast + hearing) | 3 | IMovement API |
| `EnemyBehavior` (state machine) | 3 | EnemySenses, Locomotion, Attack |
| `EnemyLocomotion` (NavMesh) | 2 | NavMesh setup, terrain baking |
| `EnemyMeleeAttack` (weapon hitbox + cancel) | 3 | Animator states for Stagger |
| `EnemyLootDropper` (loot on death) | 2 | Inventory/equipment system |
| Animator setup (5+ states, transitions) | 1.5 | |
| NavMesh baking + test scene | 1 | |
| Testing & tuning | 1 | |
| **Total** | **16.5 hours** | |

---

## Systemic Foundation Achieved

After this MVP:
- ✅ Vision + hearing foundation (future: blind debuff, sound-based tactics)
- ✅ Behavior state machine (future: aggro states, group coordination, boss phases)
- ✅ NavMesh pathfinding (future: stealth areas, high-ground tactics, formations)
- ✅ Animation cancellation (future: interrupts from spells, knockback effects)
- ✅ Weapon-mounted reach (future: reach-based combat, visual feedback)
- ✅ Loot containers (future: equipment progression, NPC trade)

---

## Deferred (Phase 9.5+)

1. **Aggro states:** Neutral → Hostile (based on reputation, faction, first-hit)
2. **Group AI:** Enemies coordinate, call for help, form groups
3. **Boss enemies:** Multi-phase, special attacks, arena mechanics
4. **Enemy types:** Variant behaviors, spell access, special loot
5. **AI reactions:** Dodge, kite, retreat, taunt
6. **Persistence:** Save enemy state, respawns
7. **Faction/reputation:** Enemies remember player, guards pursue
