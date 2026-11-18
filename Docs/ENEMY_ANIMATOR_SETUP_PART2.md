# Enemy Animator Setup — Part 2: Verification & Troubleshooting

**Date:** November 18, 2026

---

## Step 6: Verify Integration

### Pre-Play Checklist

- [ ] Animator has 3 layers: Locomotion, Combat, Hands
- [ ] Locomotion layer has idle/walk/run/strafe states
- [ ] Combat layer has Attack_1 state with animation clip
- [ ] Hands layer has Idle_Sheathed, Unsheathe, Idle_Unsheathed, Sheathe states
- [ ] RightHandWeaponSpot created under right hand bone
- [ ] SheathedWeaponSpot created under hip bone
- [ ] EnemyMeleeAttack configured with layer indices (1, 2)
- [ ] EnemyWeaponVisualManager configured with mount transforms
- [ ] AnimationEventDispatcher added to enemy root
- [ ] Animation events wired: OnAttackStart, OnAttackEnd
- [ ] WeaponDataSO has correct positions for drawn/sheathed
- [ ] Enemy has weapon equipped via ActorStatSO.MainHand

### Test Sequence in Play Mode

**1. Startup** — Enemy should idle with weapon on hip
   - [ ] Weapon visible on hip (not in hand)
   - [ ] Enemy standing still
   - [ ] Console shows no errors

**2. Combat triggered** 
   - [ ] Hands layer fades out (unsheathe anim plays, if clips exist)
   - [ ] Combat layer fades in
   - [ ] Weapon moves to hand
   - [ ] Attack animation plays
   - [ ] Damage is delivered when player hit

**3. Attack ends**
   - [ ] Combat layer fades out
   - [ ] Hands layer fades in (sheathe anim plays)
   - [ ] Weapon moves to hip
   - [ ] Enemy returns to idle
   - [ ] No console errors

---

## Common Issues & Fixes

### Issue: Weapon doesn't move between mounts

**Cause:** Mounts not assigned, or hierarchy wrong

**Fix:**
1. Check `EnemyWeaponVisualManager` has both mounts assigned
2. Verify mounts are children of correct bones (hand vs hip)
3. In Play mode, toggle `_drawn` and watch weapon in Scene view

### Issue: Attack animation doesn't play

**Cause:** Combat layer weight is 0 when attack triggers

**Fix:**
1. Check `_combatLayerIndex = 1` in `EnemyMeleeAttack`
2. Enable `_debugAttack` to see layer weight logs
3. Verify `Attack_1` state has animation clip assigned

### Issue: Animation events not firing

**Cause:** AnimationEventDispatcher not on enemy, or events not wired

**Fix:**
1. Add `AnimationEventDispatcher` to enemy root if missing
2. In Animator, add event to attack clip at ~95% duration
3. Set event to call `AnimationEventDispatcher.OnAttackEnd()`

### Issue: Multiple attacks fire in succession

**Cause:** Attack cooldown not enforced, or PlayAttack called twice

**Fix:**
1. Check `EnemyBehavior._attackCooldownSeconds` is set
2. Verify `EndAttack()` called via animation event only
3. Check `_nextAttackTime` is updated

---

## Debug Mode

Enable in `EnemyMeleeAttack` inspector:
- `Debug Attack: true`

Console will show:
```
[EnemyMeleeAttack] Enemy PlayAttack - Combat layer active
[EnemyMeleeAttack] Enemy EndAttack - Combat layer inactive
Animation event received: OnAttackEnd on Enemy
```

---

## Architecture Summary

```
Idle (Combat=0, Hands=1)
   ↓
PlayAttack() → SetLayerWeights(true) → Combat=1, Hands=0
   ↓
[Attack animation plays]
   ↓
OnAttackEnd event fires
   ↓
EndAttack() → SetLayerWeights(false) → Combat=0, Hands=1
   ↓
Idle (Combat=0, Hands=1)
```

**Files Modified:**
- ✅ `EnemyMeleeAttack.cs` — Added layer weights + animation events
- 📝 Animator Controller — Manual setup required
- 📝 Animation clips — Manual setup required
- 📝 Mount transforms — Manual setup required
