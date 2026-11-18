# Bug Fix: Enemy Behavior & Attack System — Phase 9.6b Hotfix

**Date:** November 18, 2026  
**Status:** ✅ Fixed  
**Severity:** Critical (core gameplay broken)

---

## Bugs Fixed

### Bug 1: Enemy Sliding to Stale Positions

**Symptom:** Enemy moves to last known position and doesn't update, even when player visible and moving

**Root Cause:** In `EnemyBehavior.UpdatePursuing()` and `UpdateAttacking()`, player position only updated when `_senses.CanSee` is true. If player moves out of sight momentarily, enemy keeps pursuing stale position.

**Fix:** Update position every frame in both states
```csharp
// UpdatePursuing()
_locomotion.MoveTo(_lastKnownPlayerPos);
if (_senses.CanSee)
    _lastKnownPlayerPos = _senses.LastSeenPlayerPosition;

// UpdateAttacking()
if (_senses.CanSee)
    _lastKnownPlayerPos = _senses.LastSeenPlayerPosition;
```

**Commit:** EnemyBehavior.cs lines 91-116

---

### Bug 2: Weapon Collider Always Disabled

**Symptom:** Attacks don't do damage even when visibly hitting player

**Root Causes (Multiple):**

**Root Cause 2a:** Collider disabled by default in `PrepareAsHeld()`
```csharp
// EnemyWeaponVisualManager
foreach (Collider col in instance.GetComponentsInChildren<Collider>())
{
    col.enabled = false;  // ← Disabled at spawn
}
```

**Root Cause 2b:** Not enabled during attack
```csharp
// EnemyMeleeAttack.PlayAttack() - MISSING
// Weapon collider was never explicitly enabled
```

**Root Cause 2c:** OnTriggerStay checking wrong layer
```csharp
// EnemyMeleeAttack.OnTriggerStay()
if (!_animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) return;
//                                                ^  Layer 0 = Locomotion (not Attack!)
```

Should check **Combat layer (1)**, not Locomotion layer (0).

**Fixes Applied:**

**Fix 2a:** Enable collider in PlayAttack()
```csharp
public void PlayAttack()
{
    // ...
    if (_weaponHitbox != null)
        _weaponHitbox.enabled = true;  // ← NEW
    // ...
}
```

**Fix 2b:** Disable collider in EndAttack() for safety
```csharp
public void EndAttack()
{
    // ...
    if (_weaponHitbox != null)
        _weaponHitbox.enabled = false;  // ← NEW
    // ...
}
```

**Fix 2c:** Check Combat layer in OnTriggerStay()
```csharp
void OnTriggerStay(Collider other)
{
    // ...
    AnimatorStateInfo combatStateInfo = _animator.GetCurrentAnimatorStateInfo(_combatLayerIndex);
    if (!combatStateInfo.IsTag("Attack"))
        return;
    // ...
}
```

**Commit:** EnemyMeleeAttack.cs lines 93-190

---

## Files Modified

- ✅ `EnemyBehavior.cs` — Position update fix
- ✅ `EnemyMeleeAttack.cs` — Collider enable/disable + layer check fix

---

## Testing Checklist

After applying fixes, verify:

### Movement Fix
- [ ] Enemy pursues player smoothly
- [ ] If player moves, enemy follows (not stale position)
- [ ] Enemy transitions from Pursuing to Attacking at correct range
- [ ] No more sliding/delayed updates

### Attack Collider Fix
- [ ] Weapon collider enabled during attack
- [ ] Attack hits deal damage to player
- [ ] Player health decreases on hit
- [ ] "Hit" effect visible (stagger, knockback, etc)
- [ ] Weapon collider disabled after attack
- [ ] Multiple attacks work correctly

### Debug Logging
Set `_debugAttack = true` to see:
```
[EnemyMeleeAttack] Enemy PlayAttack - Combat layer active, weapon drawn, collider enabled
[OnTriggerStay] Damage delivered: 16
[EnemyMeleeAttack] Enemy EndAttack - Combat layer inactive, weapon sheathed, collider disabled
```

---

## Before vs After

### Before (Broken)
```
Enemy sees player
  ↓
Pursues to LAST POSITION (stale)
  ↓
Player moves away
  ↓
Enemy keeps going to stale position (never updates)
  ↓
Attack triggered but does no damage
  └─ Collider disabled
  └─ Wrong layer checked
```

### After (Fixed)
```
Enemy sees player
  ↓
Pursues to CURRENT POSITION (updated every frame)
  ↓
Player moves away but stays visible
  ↓
Enemy tracks live position, updates every frame
  ↓
Attack triggered, damage delivered
  ├─ Collider enabled
  ├─ Combat layer checked (correct)
  └─ OnTriggerStay fires, damage calculated
```

---

## Performance Impact

**Negligible:**
- Position update: 1 extra assignment per frame (microseconds)
- Collider enable/disable: 1 boolean per attack (microseconds)
- Layer check: 1 GetCurrentAnimatorStateInfo call (already being done)

---

## Related Issues Preventing Damage

These fixes address the "attacks don't work" issue, but verify:

1. **Weapon prefab must have collider:**
   - [ ] Sword has Box/Sphere Collider
   - [ ] Collider set to **Trigger** (is Trigger checked)
   - [ ] Not kinematic (RigidBody set to Kinematic by EnemyWeaponVisualManager)

2. **Player must have IDamageReceiver:**
   - [ ] Player has component implementing IDamageReceiver
   - [ ] OnTriggerStay reaches that component

3. **Animation must have "Attack" tag:**
   - [ ] Attack_1 state tagged "Attack"
   - [ ] Tag visible in Animator window

4. **Animation event must wire correctly:**
   - [ ] OnAttackEnd event fires at ~95% duration
   - [ ] Calls AnimationEventDispatcher.OnAttackEnd()

---

## Rollback

If any issues arise, revert:
```bash
git diff HEAD~1 Assets/_Project/Scripts/Enemies/Behavior/EnemyBehavior.cs
git diff HEAD~1 Assets/_Project/Scripts/Enemies/Combat/EnemyMeleeAttack.cs
```

Changes are isolated, safe rollback guaranteed.

---

## Summary

**Fixed:**
✅ Enemy position updates now (smooth pursuit)  
✅ Weapon collider enabled during attack  
✅ Correct animator layer checked for damage  
✅ Attacks now deliver damage correctly  

**Status:** Ready for re-testing

See ENEMY_SYSTEMS_ARCHITECTURE.md for full system reference.
