# Post-Hotfix Verification Checklist

**Date:** November 18, 2026  
**Phase:** 9.6b Testing & Validation  

---

## Pre-Test Setup

- [ ] Open enemy scene in Unity
- [ ] Select enemy → EnemyMeleeAttack component
- [ ] Set `_debugAttack = true` (for logging)
- [ ] Save scene
- [ ] Compile (should have no errors)

---

## Test 1: Movement Tracking

**Objective:** Enemy follows player smoothly with live position updates

### Steps
1. [ ] Play game
2. [ ] Stand still near enemy
3. [ ] Let enemy start pursuing
4. [ ] **Move left** — Enemy should adjust course smoothly
5. [ ] **Move right** — Enemy should adjust immediately
6. [ ] **Move around in circle** — Enemy should track smoothly

### Success Criteria
- ✅ Enemy follows your actual position (not stale)
- ✅ Adjusts course smoothly when you move
- ✅ No sliding to old positions
- ✅ Movement fluid and responsive

### If Fails
- Check NavMesh is set up in scene
- Check EnemyLocomotion has NavMeshAgent component
- Check _lastKnownPlayerPos is being updated in EnemyBehavior

---

## Test 2: Attack Damage Delivery

**Objective:** Attacks hit player and deal damage

### Setup
- [ ] Player health visible (check UI or inspector)
- [ ] Player at full health

### Steps
1. [ ] Play game
2. [ ] Let enemy attack player
3. [ ] Watch Console for debug logs:
   ```
   [EnemyMeleeAttack] Enemy PlayAttack - collider enabled
   [OnTriggerStay] Damage delivered
   [EnemyMeleeAttack] Enemy EndAttack - collider disabled
   ```
4. [ ] Check player health decreased

### Success Criteria
- ✅ Console shows "collider enabled"
- ✅ Console shows damage delivered
- ✅ Player health decreased
- ✅ Stagger/effect visible
- ✅ Console shows "collider disabled"

### If No Damage
- [ ] Check weapon prefab has Collider
- [ ] Check Collider is set to "Is Trigger"
- [ ] Check player has IDamageReceiver
- [ ] Enable debug and watch console

---

## Test 3: Repeated Attacks

**Objective:** Multiple attacks work correctly

### Steps
1. [ ] Let enemy attack 3-4 times
2. [ ] Each attack should:
   - [ ] Enable collider
   - [ ] Play attack animation
   - [ ] Deal damage
   - [ ] Disable collider
   - [ ] Reset to idle

### Success Criteria
- ✅ All 3-4 attacks work
- ✅ Weapon collider toggles each time
- ✅ Damage delivered each time
- ✅ No stuck states

---

## Test 4: State Transitions

**Objective:** Verify state machine works correctly

### Steps
1. [ ] Start far from enemy (Idle state)
2. [ ] Move close (should go to Pursuing)
3. [ ] Get within attack range (should go to Attacking)
4. [ ] Move away (should go to Pursuing again)
5. [ ] Move very far away (should go to Idle)

### Console Output
```
[EnemyBehavior] Enemy: Idle → Alerted
[EnemyBehavior] Enemy: Alerted → Pursuing
[EnemyBehavior] Enemy: Pursuing → Attacking
[EnemyBehavior] Enemy: Attacking → Pursuing
[EnemyBehavior] Enemy: Pursuing → Alerted
```

### Success Criteria
- ✅ All state transitions occur
- ✅ Logged in console
- ✅ Behaviors match state (movement, attacking, etc)

---

## Test 5: Edge Cases

### Stagger During Attack
- [ ] Let enemy attack
- [ ] Apply stagger effect (use debug or game mechanic)
- [ ] Expected: Attack cancels, cooldown resets, state → Idle
- [ ] Result: ✅ Passes / ❌ Fails

### Player Out of Sight
- [ ] Let enemy pursue
- [ ] Hide behind wall
- [ ] Expected: Enemy goes to last known position
- [ ] Result: ✅ Passes / ❌ Fails

### Multiple Enemies
- [ ] Spawn 2-3 enemies
- [ ] Have player attacked by all
- [ ] Expected: All attack independently, all deal damage
- [ ] Result: ✅ Passes / ❌ Fails

---

## Console Log Analysis

### Expected Logs During Attack

```
[EnemyBehavior] Enemy: Idle → Alerted → Pursuing → Attacking
[EnemyMeleeAttack] Enemy PlayAttack - Combat layer active, weapon drawn, collider enabled
[OnTriggerStay] Damage delivered: 16
[EnemyMeleeAttack] Enemy EndAttack - Combat layer inactive, weapon sheathed, collider disabled
[EnemyBehavior] Enemy: Attacking → Pursuing (if player out of range)
```

### Red Flags

❌ "collider enabled" never appears → Fix not applied  
❌ "collider disabled" never appears → EndAttack not called  
❌ "Damage delivered" never appears → OnTriggerStay not firing  
❌ State doesn't change → EnemyBehavior not updating  

---

## Performance Check

While attacking:
- [ ] FPS stable (no frame drops)
- [ ] No lag or stuttering
- [ ] Smooth animations
- [ ] Weapon repositions smoothly

---

## Final Verification

After all tests:

| Test | Pass | Notes |
|------|------|-------|
| Movement Tracking | ☐ | |
| Attack Damage | ☐ | |
| Repeated Attacks | ☐ | |
| State Transitions | ☐ | |
| Edge Cases | ☐ | |
| Console Logs | ☐ | |
| Performance | ☐ | |

---

## If Any Test Fails

1. **Note the issue specifically**
2. **Enable debug logging** (`_debugAttack = true`)
3. **Reproduce and capture console output**
4. **Check BUGFIX_Enemy_Behavior_Attacks.md for diagnosis**

---

## When All Tests Pass

✅ **Hotfix validated and working**

Next steps:
1. Disable debug logging (`_debugAttack = false`)
2. Save scene
3. Commit changes
4. Continue to Phase 9.8 (multi-attack combos) OR
5. Phase 9.7 (polish) as needed

---

## Reference

- **Changes Made:** BUGFIX_Enemy_Behavior_Attacks.md
- **System Architecture:** ENEMY_SYSTEMS_ARCHITECTURE.md
- **Setup Guide:** ANIMATOR_SETUP_CHECKLIST.md

---

**Date Tested:** ___________  
**Tester:** ___________  
**Status:** ☐ All Pass / ☐ Some Failures / ☐ Critical Failures  
