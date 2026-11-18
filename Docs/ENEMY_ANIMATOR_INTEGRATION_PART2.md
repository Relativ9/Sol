# Enemy Animator Integration — Part 2: Performance & Extensions

**Date:** November 18, 2026

---

## Physics & Collision

### Weapon Collider Lifecycle

```
Idle (sheathed):
├─ Collider disabled
└─ Rigidbody kinematic

Attack starts (PlayAttack):
├─ Layer weights switched
├─ Weapon repositioned to hand
└─ Physics enabled during damage window

During attack:
├─ OnTriggerStay fires
├─ Damage delivered via EnemyMeleeAttack.OnTriggerStay()
└─ Only hits once per swing (_hitThisSwing flag)

After attack (EndAttack):
├─ Layer weights restored
├─ Weapon repositioned to hip
└─ Collider disabled again
```

**Note:** `EnemyWeaponVisualManager` pre-configures weapon prefab:
- All rigidbodies set to kinematic
- All colliders disabled by default
- EnemyMeleeAttack controls when collider is active

---

## State Transitions & Conditions

### Combat Layer

```
Attack_1 (motion) → Any State
└─ Condition: exit time 100%
```

### Hands Layer

```
Idle_Sheathed ← → Unsheathe
├─ Forward: IsSheathed == false
├─ Backward: IsSheathed == true
└─ Unsheathe → Idle_Unsheathed (100% exit time)

Idle_Unsheathed ← → Sheathe
├─ Forward: IsSheathed == true
├─ Backward: IsSheathed == false
└─ Sheathe → Idle_Sheathed (100% exit time)
```

---

## Performance

### Layer Weight Changes
- **Cost:** Very low (single float per layer per frame)
- **Frequency:** Once per attack start + end
- **Impact:** Negligible

### Animation Event Dispatch
- **Cost:** Dictionary lookup + list iteration
- **Frequency:** Once per attack animation event
- **Impact:** Negligible

### Weapon Repositioning
- **Cost:** Transform parent change + position/rotation
- **Frequency:** Once per attack start + end
- **Impact:** Negligible

**Conclusion:** Even with 10+ enemies attacking, CPU cost is minimal.

---

## Testing Checklist (Code)

- [ ] `EnemyMeleeAttack` implements `IAnimationEventReceiver`
- [ ] `PlayAttack()` calls `SetLayerWeights(true)` before `SetTrigger()`
- [ ] `EndAttack()` calls `SetLayerWeights(false)` and `SetDrawn(false)`
- [ ] `OnAnimationEvent()` handles `"OnAttackEnd"`
- [ ] Layer indices default to 1 and 2
- [ ] References obtained in Awake and Start
- [ ] Registration to events in Start, unregistration in OnDestroy
- [ ] Debug logging available via inspector flag

---

## Extension Points: Phase 9.8 Multi-Attack

### When Ready to Implement Combos

**Changes needed:**

1. **Create 3 attack states** on Combat layer:
   - `Attack_1`, `Attack_2`, `Attack_3`

2. **Create blend tree** by `ComboIndex` parameter

3. **Update PlayAttack()**:
   ```csharp
   public void PlayAttack(int comboIndex = 0)
   {
       SetLayerWeights(true);
       _animator.SetInteger("ComboIndex", comboIndex);
       _animator.SetTrigger("Attack");
   }
   ```

4. **Add `CanStringToCombo` management**:
   ```csharp
   public void QueueNextAttack()
   {
       _animator.SetBool("CanStringToCombo", true);
   }
   ```

5. **No changes to layer weight system** (already flexible)

---

## Files Modified in Phase 9.6b

**Code changes:**
- ✅ `EnemyMeleeAttack.cs` — Added 50+ lines for layer management + events

**Configuration (manual):**
- 📝 Animator Controller — Add 3 layers, states, parameters
- 📝 Animation clips — Add events and assign to states
- 📝 Mount transforms — Create hand and hip weapon positions

**Already existing:**
- ✅ `EnemyWeaponVisualManager.cs` — Weapon repositioning (Phase 9.6)
- ✅ `AnimationEventDispatcher.cs` — Event routing (Player system, reused)

---

## Comparison: Player vs Enemy Animator

| Aspect | Player | Enemy |
|--------|--------|-------|
| Layers | 3 (Locomotion, Combat, Hands) | 3 (same) |
| Attack system | Combo chain (3 swings) | Single attack (ready for combo) |
| Layer weights | Managed by CombatStateController | Managed by EnemyMeleeAttack |
| Animation events | Routed by AnimationEventDispatcher | Routed by AnimationEventDispatcher |
| Weapon draw/sheathe | Separate animations | Separate animations |
| Extensibility | Mature (Phase 9) | Foundation ready (Phase 9.6b) |

**Architecture now fully aligned** between player and enemies.

---

## Debugging Guide

### Enable Debug Logging

In `EnemyMeleeAttack` inspector, set `Debug Attack: true`

### Expected Console Output

```
[EnemyMeleeAttack] Enemy PlayAttack - Combat layer active, weapon drawn
Animation event received: OnAttackEnd on Enemy
[EnemyMeleeAttack] Enemy EndAttack - Combat layer inactive, weapon sheathed
```

### Common Debug Scenarios

**Attack not playing:**
```
[EnemyMeleeAttack] Enemy PlayAttack - Combat layer active, weapon drawn
(no animation)
```
→ Check Attack_1 state has animation clip

**Weapon doesn't move:**
```
[EnemyMeleeAttack] Enemy PlayAttack - Combat layer active, weapon drawn
(weapon visible in Hierarchy but doesn't move)
```
→ Check mount transforms are assigned

**Attack never ends:**
```
[EnemyMeleeAttack] Enemy PlayAttack - Combat layer active, weapon drawn
Animation event received: OnAttackEnd on Enemy
(animation continues playing)
```
→ Check animation event fired at correct time (95% duration)

---

## Summary

**Implemented:**
✅ 3-layer animator architecture (matches player)  
✅ Layer weight management (automatic during attacks)  
✅ Weapon repositioning (hip ↔ hand)  
✅ Animation event routing (OnAttackEnd handling)  
✅ Debug logging (for testing/troubleshooting)  

**Ready to:**
→ Set up animator manually (see ENEMY_ANIMATOR_SETUP_GUIDE.md)  
→ Configure animation clips and events  
→ Create weapon mount transforms  
→ Test attack sequence  

**Next Phase (9.8):**
→ Multi-attack combo system (foundation ready, no architecture changes)  
→ Combo chaining (similar to player system)  
→ Enemy tactics (dodge, retreat, group behavior)  
