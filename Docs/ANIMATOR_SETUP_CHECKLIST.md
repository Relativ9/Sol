# Enemy Animator Setup Checklist — Phase 9.6b

**Reference:** ENEMY_SYSTEMS_ARCHITECTURE.md  
**Date:** November 18, 2026  
**Time:** 4-5 hours implementation

---

## Overview

**What's done:** Code + layer management + animation events (Phase 9.6b)  
**What you'll do:** Animator setup + testing (Phase 9.6 completion)

---

## Part 1: Animator Layers (Steps 1-3)

**Layer 0: Locomotion** (exists)
- [ ] Weight 1.0, leave as-is

**Layer 1: Combat** (new)
- [ ] Right-click "Layers" → "Create Layer"
- [ ] Name: "Combat"
- [ ] Weight: 0.0
- [ ] Sync: ☑ Locomotion

**Layer 2: Hands** (new)
- [ ] Right-click "Layers" → "Create Layer"
- [ ] Name: "Hands"
- [ ] Weight: 1.0
- [ ] Sync: ☐ unchecked

---

## Part 2: States (Steps 4-7)

### Locomotion Layer
- [ ] States: Idle, Walk, Run, Strafe
- [ ] Transitions by Speed parameter

### Combat Layer
- [ ] State: Attack_1 (default)
- [ ] Assign attack clip
- [ ] Transition: Attack_1 → Any State (100%)

### Hands Layer
- [ ] States: Idle_Sheathed, Unsheathe, Idle_Unsheathed, Sheathe
- [ ] Transitions via IsSheathed bool

---

## Part 3: Parameters (Step 8)

- [ ] Speed (Float)
- [ ] Attack (Trigger)
- [ ] IsSheathed (Bool, default true)

---

## Part 4: Animation Events (Steps 9-10)

For attack clip:
- [ ] OnAttackStart at frame 0
  - [ ] Function: AnimationEventDispatcher.OnAttackStart()
- [ ] OnAttackEnd at ~95%
  - [ ] Function: AnimationEventDispatcher.OnAttackEnd()

---

## Part 5: Weapon Mounts (Steps 11-12)

**RightHandWeaponSpot**
- [ ] Parent: hand bone
- [ ] Position: (0, 0, 0.15)
- [ ] Rotation: (0, 0, 0)

**SheathedWeaponSpot**
- [ ] Parent: hip bone
- [ ] Position: (0, 0, 0)
- [ ] Rotation: (0, 0, 0)

---

## Part 6: Components (Steps 13-16)

**EnemyMeleeAttack:**
- [ ] Combat Layer Index: 1
- [ ] Hands Layer Index: 2
- [ ] Attack Trigger: "Attack"
- [ ] Debug Attack: ☑ true

**EnemyWeaponVisualManager:**
- [ ] Right Hand Mount: Assign RightHandWeaponSpot
- [ ] Sheathed Mount: Assign SheathedWeaponSpot
- [ ] Drawn: ☐ false

**AnimationEventDispatcher:**
- [ ] Add if missing

**WeaponDataSO:**
- [ ] unsheathedPosition: (0, 0, 0.15)
- [ ] sheathedPosition: (0, 0, 0)

---

## Part 7: Pre-Play (Step 17)

- [ ] 3 layers created
- [ ] States assigned
- [ ] Events wired
- [ ] Mounts created
- [ ] Components configured
- [ ] Debug enabled
- [ ] No compile errors

---

## Part 8: Play Mode (Steps 18-23)

**Startup:**
- [ ] Weapon on hip
- [ ] No errors

**Attack:**
- [ ] Weapon to hand
- [ ] Combat layer ON
- [ ] Attack plays
- [ ] Damage delivered

**End:**
- [ ] Combat layer OFF
- [ ] Weapon to hip
- [ ] No errors

**Multiple Attacks:**
- [ ] 2-3 attacks work
- [ ] Cooldown enforced

---

## Part 9: Cleanup (Steps 24-25)

- [ ] Disable debug logging
- [ ] Save scene

---

## Success

✅ Weapon on hip at start  
✅ Attack: weapon draws, layers switch  
✅ End: weapon sheathes, layers restore  
✅ Multiple attacks work  
✅ No errors  

---

**Full guide:** See ENEMY_SYSTEMS_ARCHITECTURE.md (Section 4)  
**Status report:** See PHASE_9_6B_COMPLETION_REPORT.md  
**Code overview:** EnemyMeleeAttack.cs (244 lines)
