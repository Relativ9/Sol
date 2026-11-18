# Enemy Systems: Complete Architecture & Implementation

**Date:** November 18, 2026  
**Status:** ✅ Phase 9.6b Complete  
**Scope:** Consolidated guide for AI, Combat, Equipment, Animations

---

## Overview

### Main Components

```
Enemy Root
├─ EnemyStats (config from ActorStatSO)
├─ EnemySenses (vision + hearing perception)
├─ EnemyBehavior (Idle→Alerted→Pursuing→Attacking state machine)
├─ EnemyLocomotion (NavMesh movement)
├─ EnemyMeleeAttack (combat delivery + layer management)
├─ EnemyWeaponVisualManager (weapon spawning + positioning)
├─ TargetHealth (health + death)
├─ StatusController (status effects: bleed, stagger)
├─ AnimationEventDispatcher (routes animation events)
└─ Animator (3-layer controller: Locomotion, Combat, Hands)
```

### Design: SOLID + Event-Driven + SO-Based

- Single responsibility per component
- Event publishing (loose coupling)
- Immutable configuration (ActorStatSO)
- Aligned with player architecture

---

## 1. AI System

### Perception (EnemySenses)

**Vision:** Raycast line-of-sight, range boosted by `ActorStatSO.perceptionRangeBonus`  
**Hearing:** Distance-based detection, same bonus applied  
**API:** `CanSee`, `CanHear`, `IsAware`, `LastSeenPlayerPosition`, `DistanceToPlayer`

### Behavior (EnemyBehavior)

**States:** Idle → Alerted → Pursuing → Attacking → Dead

**Transitions:** Based on perception, distance, and cooldown

**Attack Cooldown:** 1.5 seconds (reset on stagger)

---

## 2. Combat System

### Attack Flow

```
EnemyBehavior.PlayAttack()
  ├─ SetLayerWeights(true) [NEW 9.6b]
  ├─ SetDrawn(true) [weapon to hand]
  └─ SetTrigger("Attack")
      ↓
  Animator plays Attack_1
  ├─ OnAttackStart event
  ├─ Damage window: OnTriggerStay enabled
  └─ OnAttackEnd event (~95%)
      ↓
  AnimationEventDispatcher routes to EnemyMeleeAttack
      ↓
  EndAttack() executed
  ├─ SetLayerWeights(false)
  └─ SetDrawn(false) [weapon to hip]
```

### Damage

```
Base Damage (DamageProfileSO)
+ Weapon Bonus (WeaponDataSO.weaponDamage)
+ RNG (stagger%, bleed%, penetration from EnemyStats)
```

---

## 3. Equipment System

### Setup

**ActorStatSO defines:**
- MainHand weapon
- OffHand item
- Armor

**EnemyStats fires event:** `EquipmentChanged(slot, newItem)`

**EnemyWeaponVisualManager listens:** Spawns/destroys weapon

**EnemyMeleeAttack receives:** Hitbox reference for damage delivery

### Weapon Mounts

- **RightHandWeaponSpot** — Child of hand bone (drawn position)
- **SheathedWeaponSpot** — Child of hip bone (resting position)

Auto-positioned by `SetDrawn(true/false)`

---

## 4. Animator System (NEW Phase 9.6b)

### 3-Layer Structure

| Layer | Purpose | Weight |
|-------|---------|--------|
| 0: Locomotion | Movement (idle, walk, run) | Always 1.0 |
| 1: Combat | Attacks | 0.0 idle → 1.0 attack → 0.0 |
| 2: Hands | Weapon poses (sheathe/unsheathe) | Inverse of Combat |

### Layers in Action

**Idle state:**
- Locomotion: 1 (movement animation plays)
- Combat: 0 (no attack)
- Hands: 1 (weapon sheathed pose visible)

**Attack state:**
- Locomotion: 1 (movement still active)
- Combat: 1 (attack animation plays)
- Hands: 0 (weapon pose hidden)

### Parameters

**Used now:** `Speed`, `Attack` (trigger), `IsSheathed`  
**Future (9.8):** `ComboIndex`, `CanStringToCombo`

### Animation Events

| Event | When | Handler |
|-------|------|---------|
| OnAttackStart | Frame 0 | Optional (logging) |
| **OnAttackEnd** | ~95% duration | **Calls EndAttack()** |

---

## 5. Integration Points

### Key Communication Patterns

| From | To | Trigger | Purpose |
|------|----|---------||---------|
| EnemyBehavior | EnemyMeleeAttack | PlayAttack() | Start attack |
| EnemyMeleeAttack | EnemyWeaponVisualManager | SetDrawn() | Weapon position |
| AnimationEventDispatcher | EnemyMeleeAttack | OnAnimationEvent() | Attack end |
| StatusController | EnemyBehavior | StatusApplied event | Interrupt + reset |

---

## 6. Implementation Phases

### ✅ Phase 9.6b: Architecture Complete
- [x] Code: Layer weight management implemented
- [x] Code: Animation events integrated
- [x] Docs: Full architecture documented

### 📋 Phase 9.6: Setup (Next, 4-5 hours)
- [ ] Create 3-layer animator controller
- [ ] Assign animation clips to states
- [ ] Wire animation events
- [ ] Create weapon mounts (RightHand, Sheathed)
- [ ] Test with actual enemy models

### 🔮 Future Phases
- Phase 9.7: Polish (sound, effects, 1 week)
- Phase 9.8: Multi-attack combos (2 weeks)
- Phase 9.9+: Dodge, boss phases, group AI

---

## Quick Reference

### Common Tasks

**Adjust attack damage:**
1. ActorStatSO.BaseDamage OR
2. WeaponDataSO.weaponDamage

**Adjust perception:**
1. ActorStatSO.perceptionRangeBonus

**Add enemy type:**
1. Create ActorStatSO
2. Assign MainHand weapon
3. Use existing prefab (all components reusable)

**Debug attack issue:**
1. Set `_debugAttack = true` in EnemyMeleeAttack
2. Check console for layer transitions
3. Verify animator has Attack_1 state with clip

---

## Files & References

**Consolidated in this document:**
- AI system (perception + behavior)
- Combat system (attacks + damage)
- Equipment system (weapons + visuals)
- Animator system (3-layer management)

**For detailed setup steps:**
→ See **ANIMATOR_SETUP_CHECKLIST.md** (24-step guide)

**For Phase 9 planning context:**
→ See original **Phase_9_Enemy_System_Plan_FINAL.md**

**For Phase 9.6b status:**
→ See **PHASE_9_6B_COMPLETION_REPORT.md**

---

## Success Criteria

After Phase 9.6 setup completion:

✅ Enemy idles with weapon on hip  
✅ Attack: weapon draws, Combat layer visible, attack plays  
✅ End: weapon sheathes, layers restore  
✅ Multiple attacks work seamlessly  
✅ Damage delivered correctly  
✅ No console errors  
✅ Ready for Phase 9.7 (polish)  

---

**Status: Architecture complete. Setup guide ready. See ANIMATOR_SETUP_CHECKLIST.md for next steps.**
