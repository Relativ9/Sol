# Hotfix Summary — Enemy Behavior & Attack Issues

**Date:** November 18, 2026  
**Status:** ✅ Applied & Ready for Testing

---

## What Was Broken

1. **Enemy movement stale** — Doesn't track player position, slides to old locations
2. **Attacks don't damage** — Weapon collider disabled, wrong layer checked

---

## What Was Fixed

### Fix #1: Enemy Position Tracking
**File:** EnemyBehavior.cs  
**Location:** UpdateAlerted() + UpdateAttacking()

**Change:** Update player position every frame while aware
```csharp
// Before: Only updated when _senses.CanSee
// After: Updates every frame while _senses.IsAware
```

**Result:** Enemy smoothly follows player, no stale positions

---

### Fix #2: Weapon Collider Management
**File:** EnemyMeleeAttack.cs

**Changes:**
1. Enable collider in PlayAttack()
2. Disable collider in EndAttack()
3. Check Combat layer (not Locomotion) in OnTriggerStay()

**Result:** Attacks now deliver damage correctly

---

## Testing Checklist

- [ ] Enemy pursues player smoothly
- [ ] Player moves → enemy follows (not stale)
- [ ] Attack hits deal damage
- [ ] Weapon collider enabled during attack
- [ ] Weapon collider disabled after attack
- [ ] Multiple attacks work
- [ ] No errors in Console

---

## Files Modified

- ✅ EnemyBehavior.cs (position tracking)
- ✅ EnemyMeleeAttack.cs (collider + layer check)

**Status:** Compiled and ready

---

## Debug Logging

Enable in inspector: `EnemyMeleeAttack._debugAttack = true`

Should see:
```
[EnemyMeleeAttack] Enemy PlayAttack - collider enabled
[EnemyMeleeAttack] Enemy EndAttack - collider disabled
```

---

See **BUGFIX_Enemy_Behavior_Attacks.md** for technical details.
