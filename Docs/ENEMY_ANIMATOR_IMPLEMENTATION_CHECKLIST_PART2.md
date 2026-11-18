# Enemy Animator Implementation Checklist — PART 2

**Continuation from ENEMY_ANIMATOR_IMPLEMENTATION_CHECKLIST.md**

---

## Wiring Animation Events

### Step 14: Wire Events in Animator

- [ ] In Hierarchy, select enemy's model/Animator
- [ ] Open Animation window (Window → Animation → Animation)
- [ ] Select attack animation clip
- [ ] For OnAttackStart event (frame 0):
  - [ ] Function: `AnimationEventDispatcher.OnAttackStart()`
- [ ] For OnAttackEnd event (frame ~95%):
  - [ ] Function: `AnimationEventDispatcher.OnAttackEnd()`

---

## Pre-Play Testing

### Step 15: Final Checks Before Play

- [ ] Compile successful (no red errors)
- [ ] Animator setup complete:
  - [ ] 3 layers visible
  - [ ] Combat layer has Attack_1 with clip
  - [ ] Hands layer has 4 states
  - [ ] All parameters created
- [ ] Mounts created:
  - [ ] RightHandWeaponSpot under right hand
  - [ ] SheathedWeaponSpot under hip
- [ ] Events wired on clips
- [ ] Components configured:
  - [ ] EnemyMeleeAttack: layer indices (1, 2)
  - [ ] EnemyWeaponVisualManager: mounts assigned
  - [ ] AnimationEventDispatcher: present
  - [ ] WeaponDataSO: positions set
- [ ] Debug enabled: `_debugAttack = true`

---

## Play Mode Testing

### Step 16: Test Startup (Idle)

- [ ] Press Play in Unity
- [ ] Observe enemy:
  - [ ] ☐ Enemy visible
  - [ ] ☐ Weapon on hip (not hand)
  - [ ] ☐ No Console errors
- [ ] In Animator window:
  - [ ] ☐ Locomotion weight = 1.0
  - [ ] ☐ Combat weight = 0.0
  - [ ] ☐ Hands weight = 1.0
  - [ ] ☐ Playing `Idle_Sheathed`

### Step 17: Test Attack Trigger

- [ ] Get close to trigger combat
- [ ] Observe during attack:
  - [ ] ☐ Weapon moves to hand
  - [ ] ☐ Combat layer weight = 1.0
  - [ ] ☐ Hands layer weight = 0.0
  - [ ] ☐ Attack animation plays
  - [ ] ☐ Console shows: `PlayAttack - Combat layer active`
  - [ ] ☐ Damage delivered when hit

### Step 18: Test Attack End

- [ ] After attack completes:
  - [ ] ☐ Combat layer weight = 0.0
  - [ ] ☐ Hands layer weight = 1.0
  - [ ] ☐ Weapon moves to hip
  - [ ] ☐ Console shows: `EndAttack - Combat layer inactive`
  - [ ] ☐ Console shows: `Animation event received: OnAttackEnd`
  - [ ] ☐ No errors

### Step 19: Test Multiple Attacks

- [ ] Trigger 2-3 attacks:
  - [ ] ☐ Each plays correctly
  - [ ] ☐ Weapon repositions each time
  - [ ] ☐ Layer weights reset
  - [ ] ☐ Cooldown enforced
  - [ ] ☐ No errors

---

## Post-Play Testing

### Step 20: Disable Debug & Clean Up

- [ ] Set `EnemyMeleeAttack._debugAttack = false`
- [ ] Stop Play mode
- [ ] Save scene

### Step 21: Test Without Debug Logging

- [ ] Press Play again
- [ ] Verify attack sequence works
- [ ] Check no Console errors

### Step 22: Test Edge Cases

- [ ] Player moves away during attack:
  - [ ] ☐ Attack finishes normally
  - [ ] ☐ Enemy transitions to Pursuing

- [ ] Enemy staggered during attack:
  - [ ] ☐ Attack canceled (if implemented)
  - [ ] ☐ Stagger animation plays

- [ ] Multiple enemies attack:
  - [ ] ☐ All attacks correct
  - [ ] ☐ No interference
  - [ ] ☐ All weapons reposition

---

## Documentation & Completion

### Step 23: Commit Changes

Git commit:
```
[Phase 9.6b] Enemy Animator: 3-layer system with layer weights

- Added PlayAttack/EndAttack layer management
- Integrated AnimationEventDispatcher
- Weapon repositions (hip ↔ hand)
- Matches player CombatStateController architecture
- Ready for Phase 9.8 combos
```

### Step 24: Update Project Docs

- [ ] Link to ENEMY_ANIMATOR_SETUP_GUIDE.md in main Architecture docs
- [ ] Link to ENEMY_ANIMATOR_INTEGRATION.md in tech docs

---

## Success Checklist (Final)

If all boxes checked, Phase 9.6b is **COMPLETE**:

- ✅ Code compiled without errors
- ✅ 3 layers created in Animator
- ✅ Attack animation plays on Combat layer
- ✅ Weapon mount transforms created
- ✅ Components configured correctly
- ✅ Animation events wired
- ✅ Startup: enemy idles with weapon on hip
- ✅ Attack: layers switch, weapon draws, animation plays
- ✅ Attack end: layers restore, weapon sheathes
- ✅ Multiple attacks work correctly
- ✅ No Console errors
- ✅ Changes committed to git

---

## Next Phases

**Phase 9.7 (Polish):**
- Fine-tune weapon mount positions
- Adjust animation timings
- Sound effects for draw/sheathe

**Phase 9.8 (Multi-Attack):**
- Create Attack_2 and Attack_3 states
- Implement combo chaining
- Add ComboIndex blend tree

**Phase 9.9 (Tactics):**
- Enemy dodge/retreat
- Group behavior
- Boss phases

---

## Troubleshooting Quick Reference

**Attack doesn't play:** Check Attack_1 state has clip  
**Weapon doesn't move:** Check mounts assigned  
**Events don't fire:** Check events wired in animation clip  
**Layer weights wrong:** Check indices (1 and 2)  
**Multiple attacks fire:** Check cooldown is enforced  

See ENEMY_ANIMATOR_SETUP_PART2.md for detailed fixes.

---

**Implementation complete! Ready to test with your enemy models.**
