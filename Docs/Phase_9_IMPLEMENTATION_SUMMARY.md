# Phase 9: Enemy System - Implementation Summary

**Date:** November 18, 2026
**Status:** Phases 9.0-9.4 COMPLETE ✅ | Ready for Phase 9.5
**Code Added:** ~620 lines | 6 new files | 4 files modified

---

## What Was Built

### Phase 9.0: Setup Infrastructure
- ActorStatSO: Added perception bonuses, equipment, loot fields
- ActorStats: Exposed base SO for perception queries
- PlayerController: Implements IMovement (velocity queries)
- StatusController: Added StatusApplied event for AI reactions

### Phase 9.1: Perception System ✅
- **EnemySenses.cs**: Vision (raycast LOS) + Hearing (velocity-based)
  - Vision: 20m base (boosted by ActorStatSO.perceptionRangeBonus)
  - Hearing: 10m walk, 15m sprint (1.5x boost when moving fast)
  - Raycast every 0.1s (cached, efficient)
  - Gizmo visualization for debugging

### Phase 9.2: Behavior State Machine ✅
- **EnemyBehavior.cs**: 5-state AI orchestrator
  - States: Idle → Alerted → Pursuing → Attacking → Dead
  - Queries EnemySenses, commands EnemyLocomotion, triggers EnemyMeleeAttack
  - Reacts to stagger via StatusController.StatusApplied event
  - Clear transitions, easy to extend with new states

### Phase 9.3: Locomotion System ✅
- **EnemyLocomotion.cs**: NavMesh wrapper
  - MoveTo(target), Stop(), HasArrived
  - Syncs Speed animator parameter
  - Obstacle avoidance built-in

### Phase 9.4: Combat System ✅
- **EnemyMeleeAttack.cs**: Weapon hitbox + animation cancellation
  - Weapon hitbox is child of weapon mesh (visual clarity)
  - OnTriggerStay delivers damage (IDamageReceiver pipeline)
  - Status procs automatic (bleed, stagger)
  - Stagger interrupts mid-swing, plays recoil animation

---

## New Files (6)
1. ISenses.cs - Perception interface
2. IMovement.cs - Movement interface
3. EnemySenses.cs - Vision + hearing perception
4. EnemyBehavior.cs - State machine AI
5. EnemyLocomotion.cs - NavMesh movement
6. EnemyMeleeAttack.cs - Melee combat

---

## Modified Files (4)
1. ActorStatSO.cs - Perception/loot fields
2. ActorStats.cs - Exposed ActorStatSO property
3. PlayerController.cs - IMovement interface
4. StatusController.cs - StatusApplied event

---

## How It Works (Example)

**Scenario:** Player walks toward enemy

1. **EnemySenses.Update()** (every frame):
   - Check distance: 15m (within vision range)
   - If Time >= nextRaycastTime, do raycast (every 0.1s)
   - Raycast hits: CanSee = true
   - Cache LastSeenPlayerPosition

2. **EnemyBehavior.Update()** (every frame):
   - If state == Idle and IsAware: SetState(Alerted)

3. **EnemyBehavior.UpdateAlerted()** (every frame):
   - If CanSee: Update LastKnownPos
   - If distance <= 2m: SetState(Attacking)

4. **EnemyBehavior.UpdateAttacking()** (every frame):
   - Stop movement (locomotion.Stop())
   - If Time >= nextAttackTime: attack.PlayAttack()
   - PlayAttack() sets animator "Attack" trigger

5. **Animator** (animation system):
   - Plays attack animation
   - Animation reaches swing frame

6. **WeaponHitbox.OnTriggerStay()** (when weapon touches player):
   - IDamageReceiver.ReceiveHit(DamageInfo)
   - Damage delivered, status procs calculated

7. **Player gets hit** (stagger proc):
   - StatusController.Apply(StaggerEffect)
   - IStatusTarget.SetAnimatorTrigger("Stagger")
   - StatusApplied?.Invoke(effect)

8. **EnemyBehavior.OnStatusApplied()** (event fired):
   - if (effect.LocksCombat): SetState(Idle)
   - Reset cooldown

9. **Animator** (state machine):
   - "Stagger" trigger fires
   - Transitions to Stagger animation immediately
   - Plays recoil animation
   - Returns to Idle at end

---

## Architecture Highlights

### Modular Design
- Each component has one job (SOLID compliance)
- Systems communicate via interfaces (ISenses, IMovement)
- Events for cross-system reactions (StatusApplied)
- Easy to extend (new states, new behaviors)

### SO-Based Tuning
- All ranges tunable per enemy type via ActorStatSO
- No code changes needed for different enemy variants
- Example: Goblin +0.5 perception = 50% stronger senses

### Performance
- Raycasts cached (10/sec per enemy, not 60)
- Distance checks every frame (cheap)
- NavMesh optimization (Unity built-in)
- 10 enemies = ~200 raycasts/sec (negligible)

### Integration
- Uses existing damage pipeline (DamageProfileSO, IDamageReceiver)
- Uses existing status system (StatusController, StatusEffectSO)
- Uses existing animation system (Animator, animator parameters)
- Works with player inventory (Phase 9.5)

---

## Next: Phase 9.5

Need to implement:
1. **EnemyLootContainer.cs** - Body as interactive container
2. **Loot UI** - Two-pane inventory transfer window
3. **Animator Setup** - 5 states: Idle, Walk, Attack, Stagger, Dead
4. **Scene Test** - NavMesh baked, enemies spawn, test combat
5. **Tuning** - Ranges, cooldowns, reach, balance

---

## Testing Checklist

- [ ] Enemy detects player by sight (raycast works)
- [ ] Enemy detects player by hearing (sprint louder)
- [ ] Perception ranges boosted by ActorStatSO bonus
- [ ] State transitions smooth (Idle → Alerted → Pursuing → Attacking)
- [ ] Enemy navigates around obstacles (NavMesh works)
- [ ] Weapon reach matches mesh (child collider visible)
- [ ] Stagger cancels mid-swing (animation plays immediately)
- [ ] Attack cooldown resets on stagger
- [ ] Both player and enemy take damage
- [ ] Status effects (bleed, stagger) apply identically
- [ ] Corpse stays in place (ready for loot Phase 9.5)
- [ ] Multiple enemies simultaneous (no conflicts)

---

**MVP Foundation Complete. Ready for final integration phase (9.5-9.7).**
