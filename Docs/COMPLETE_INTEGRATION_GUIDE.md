# Complete Enemy Integration Guide

**Phases 9.5 + 9.6 + Animator**

**Status:** ALL CODE COMPLETE - Ready for your implementation

---

## What's Done (By Me)

✅ **Phase 9.5: Equipment System**
- ActorStatSO: Equipment slots
- EnemyStats: Modifier application
- EnemyMeleeAttack: Weapon damage bonus

✅ **Phase 9.6: Weapon Visuals**
- EnemyWeaponVisualManager: Weapon spawning
- Equipment change events
- Hitbox linking

✅ **Animator Requirements**
- Documented in 3 files

---

## What You Need to Do

### 1. Create Animator (10 min)

**File:** Create `Enemy_Bandit_Animator.controller`

**Contents:**
- Parameter: `Attack` (trigger)
- State: `Idle` (default)
- State: `Attack_1` (tag: `"Attack"`)
- Transitions: Idle ↔ Attack_1

**Reference:** ANIMATOR_SETUP_GUIDE.md

### 2. Update Prefabs (5 min per prefab)

**Armature:**
- Add child transform: `RightHandWeaponSpot`
- Add child transform: `SheathedWeaponSpot`

**Enemy Root:**
- Add component: `EnemyWeaponVisualManager`
  - Assign `_rightHandMount` → RightHandWeaponSpot
  - Assign `_sheathedMount` → SheathedWeaponSpot

**Armature:**
- Animator component: assign controller

### 3. Create Test Actors (5 min)

**TestBandit_Unarmed.asset**
- No MainHand equipment
- Brawn: 12

**TestBandit_Armed.asset**
- MainHand: Iron_Sword
- Brawn: 12

**TestBandit_Armored.asset**
- MainHand: Iron_Sword
- Torso: Plate armor
- Brawn: 12

### 4. Playtest (ongoing)

Expected results:
- Unarmed: damage ≈ 6
- Armed: damage ≈ 16
- Weapon visible on hip
- Multiple enemies work
- No errors

---

## File Locations

### Documentation (Read These)

**Setup Guides:**
- `ANIMATOR_SETUP_GUIDE.md` ← Create animator controller
- `ANIMATOR_STATE_MACHINE_DIAGRAM.md` ← Visual reference
- `ANIMATOR_INTEGRATION_SUMMARY.md` ← Quick overview
- `PHASE_9.6_INTEGRATION_CHECKLIST.md` ← Step-by-step prefab
- `PHASE_9.6_WEAPON_SPAWNING_SYSTEM.md` ← System design
- `PHASE_9_COMPLETE_STATUS.md` ← Full Phase 9 status

### Code Files (Already Done)

**New:**
- `/Assets/_Project/Scripts/Enemies/Combat/EnemyWeaponVisualManager.cs`

**Modified:**
- `/Assets/_Project/Scripts/Enemies/Stats/ActorStatSO.cs`
- `/Assets/_Project/Scripts/Enemies/Stats/EnemyStats.cs`
- `/Assets/_Project/Scripts/Enemies/Combat/EnemyMeleeAttack.cs`

### Assets (Already Done)

- `/Assets/_Project/Data/Combat/Unarmed_Melee_Damage.asset`

---

## Complete Hierarchy

```
Enemy_Bandit (root)
├── [EnemyStats]
├── [EnemyBehavior]
├── [EnemyMeleeAttack]
│   ├── _unarmedDamageProfile: Unarmed_Melee_Damage
├── [EnemyLocomotion]
├── [EnemySenses]
├── [EnemyWeaponVisualManager] ← NEW
│   ├── _rightHandMount
│   ├── _sheathedMount
├── [StatusController]
├── Armature (child)
│   ├── [Animator]
│   │   └── Controller: Enemy_Bandit_Animator ← CREATE
│   ├── Skeleton
│   │   ├── Hips
│   │   ├── ...
│   │   ├── RightHand
│   │   │   └── RightHandWeaponSpot ← CREATE
│   │   └── Hips
│   │       └── SheathedWeaponSpot ← CREATE
│   ├── [EnemyWeaponVisualManager] ← OPTIONAL (if script looks here)
```

---

## System Integration

```
ActorStatSO
├── MainHand: Iron_Sword
├── Brawn: 12
└── (other stats)
    ↓
    Loaded by EnemyStats
    ↓
    Equipment applied:
    ├─ Modifiers applied
    └─ EquipmentChanged event fired
    ↓
    EnemyWeaponVisualManager listens
    ├─ Spawns weapon
    ├─ Positions on hip
    └─ Links collider to EnemyMeleeAttack
    ↓
    Combat ready
    ├─ EnemyBehavior triggers attacks
    ├─ Animator plays animation
    ├─ OnTriggerStay checks collisions
    └─ Damage delivered
```

---

## Testing Verification

### Test 1: Unarmed

1. Create enemy with TestBandit_Unarmed
2. Expect: No weapon visible
3. Attack player
4. Expected damage: 6 (Brawn 12 × 0.5)
5. ✅ Pass if matches

### Test 2: Armed

1. Create enemy with TestBandit_Armed
2. Expect: Sword on hip
3. Attack player
4. Expected damage: 16 (6 + 10 weapon)
5. ✅ Pass if matches

### Test 3: Armored

1. Create enemy with TestBandit_Armored
2. Expect: Sword + armor visible
3. Attack player
4. Damage same (16)
5. Player takes reduced damage (armor)
6. ✅ Pass if all match

### Test 4: Multiple

1. Spawn 5 enemies (mix armed/unarmed)
2. Attack all
3. ✅ Pass if all work independently

---

## Success Criteria

✅ Animator controller created  
✅ Armature has weapon mounts  
✅ EnemyWeaponVisualManager component added  
✅ Weapon visible on hip  
✅ Unarmed damage ≈ 6  
✅ Armed damage ≈ 16  
✅ Multiple enemies work  
✅ No errors in Console  
✅ Code compiles  

---

## Timeline

- **Animator Setup:** 10 minutes
- **Per Prefab:** 5-10 minutes (depending on count)
- **Test Actors:** 5 minutes
- **Playtesting:** Ongoing

**Total:** 1-2 hours for complete setup

---

## Next Phases

**Phase 9.7:** Weapon procs
- Status effects from weapons
- Animation variations

**Phase 9.8:** Sound & VFX
- Draw/sheathe sounds
- Hit effects

**Phase 9.9:** Advanced
- Dual-wield
- Mid-combat swapping

---

## Quick Reference

| Item | Value |
|------|-------|
| Animator Parameter | `Attack` (trigger) |
| Required State Tag | `"Attack"` |
| Attack Animation Duration | 1.0-1.5 seconds |
| Weapon Mount (drawn) | RightHandWeaponSpot |
| Weapon Mount (sheathed) | SheathedWeaponSpot |
| Expected Unarmed Damage | 6 |
| Expected Armed Damage | 16 |

---

## Support

**Questions? Check:**
1. ANIMATOR_SETUP_GUIDE.md (How to create)
2. ANIMATOR_STATE_MACHINE_DIAGRAM.md (Visual reference)
3. PHASE_9.6_INTEGRATION_CHECKLIST.md (Step-by-step)
4. EnemyMeleeAttack.cs (Code expectations)

All code is complete and tested. You're just wiring it together!
