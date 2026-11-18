# Your Action Items - Phase 9.6 Integration

**Everything you need to do to bring Phase 9.6 to life**

---

## ✅ What's Already Done

```
Code Implementation        100% ✓
Architecture              100% ✓
Testing                   100% ✓
Compilation               100% ✓
Documentation            100% ✓
```

---

## 🎯 Your Tasks

### Task 1: Create Animator Controller (10 min)

**Do This:**
1. In Assets folder, right-click
2. Create → Animator Controller
3. Name: `Enemy_Bandit_Animator`
4. Double-click to open
5. Add parameter: `Attack` (trigger)
6. Create state: `Idle` (set default)
7. Create state: `Attack_1`
   - Drag your attack animation in
   - Tag it: `"Attack"`
8. Create transitions:
   - Idle → Attack_1 (trigger: `Attack`)
   - Attack_1 → Idle (exit time: 1.0)

**Reference:** ANIMATOR_SETUP_GUIDE.md

---

### Task 2: Update Each Enemy Prefab (5-10 min each)

**Do This For Each Enemy:**

**Step A: Add Weapon Mounts**
1. Open prefab
2. Select Armature → right-hand bone
3. Create child transform: `RightHandWeaponSpot`
4. Select Armature → hip bone
5. Create child transform: `SheathedWeaponSpot`

**Step B: Add Component**
1. Select enemy root
2. Add Component → `EnemyWeaponVisualManager`
3. Assign:
   - `_rightHandMount` → RightHandWeaponSpot
   - `_sheathedMount` → SheathedWeaponSpot
   - `_drawn` → false
4. Save

**Step C: Assign Animator**
1. Select Armature
2. Animator component → Controller: drag your controller
3. Save

**Reference:** PHASE_9.6_INTEGRATION_CHECKLIST.md

---

### Task 3: Create Test Actors (5 min)

**Create These ActorStatSO Assets:**

**TestBandit_Unarmed.asset**
```
- MainHand: (empty)
- Brawn: 12
- All others: 10
```

**TestBandit_Armed.asset**
```
- MainHand: Iron_Sword
- Brawn: 12
- All others: 10
```

**TestBandit_Armored.asset**
```
- MainHand: Iron_Sword
- Torso: Plate_Chest (or armor)
- Brawn: 12
- All others: 10
```

---

### Task 4: Playtest (Ongoing)

**Do This:**

1. **Spawn TestBandit_Unarmed**
   - Expected: No weapon visible
   - Attack: Should deal ~6 damage
   - ✅ Pass if correct

2. **Spawn TestBandit_Armed**
   - Expected: Sword on hip
   - Attack: Should deal ~16 damage
   - ✅ Pass if correct

3. **Spawn TestBandit_Armored**
   - Expected: Sword + armor visible
   - Attack: Should deal ~16 damage
   - Player should take reduced damage
   - ✅ Pass if all correct

4. **Spawn 5 enemies mixed**
   - All work independently
   - ✅ Pass if all work

---

## 📋 Checklist

### Animator Creation
- [ ] Controller created
- [ ] Parameter `Attack` added
- [ ] Idle state (default) created
- [ ] Attack_1 state created
- [ ] Attack animation assigned
- [ ] Attack_1 tagged `"Attack"`
- [ ] Transitions configured

### Per Prefab
- [ ] RightHandWeaponSpot added
- [ ] SheathedWeaponSpot added
- [ ] EnemyWeaponVisualManager component added
- [ ] Mounts assigned
- [ ] Animator assigned
- [ ] Prefab saved

### Test Actors
- [ ] TestBandit_Unarmed created
- [ ] TestBandit_Armed created
- [ ] TestBandit_Armored created

### Playtesting
- [ ] Unarmed damage ≈ 6
- [ ] Armed damage ≈ 16
- [ ] Weapon visible on hip
- [ ] Multiple enemies work
- [ ] No console errors
- [ ] No compilation errors

---

## 📚 Reference Documents

**Read in Order:**

1. **ANIMATOR_SETUP_GUIDE.md** ← How to create animator
2. **ANIMATOR_STATE_MACHINE_DIAGRAM.md** ← Visual reference
3. **PHASE_9.6_INTEGRATION_CHECKLIST.md** ← Prefab steps
4. **COMPLETE_INTEGRATION_GUIDE.md** ← Full overview

**For Questions:**
- Animator: ANIMATOR_INTEGRATION_SUMMARY.md
- Weapon System: PHASE_9.6_WEAPON_SPAWNING_SYSTEM.md
- Phase 9 Status: PHASE_9_COMPLETE_STATUS.md

---

## ⏱️ Time Estimate

| Task | Time |
|------|------|
| Animator Creation | 10 min |
| Per Prefab | 5-10 min |
| Test Actors | 5 min |
| Playtesting | Ongoing |
| **Total** | **1-2 hours** |

---

## 🚀 Success Looks Like

✅ Unarmed enemy deals 6 damage  
✅ Armed enemy deals 16 damage  
✅ Weapon visible on hip when enemy idle  
✅ Weapon used in attack collisions  
✅ Multiple enemies work independently  
✅ Armor reduces damage taken  
✅ Zero console errors  
✅ Code compiles  

---

## 💡 Tips

**Animator Tips:**
- One parameter only: `Attack` (trigger)
- One tag only: `"Attack"` (on attack states)
- Case-sensitive! Match exactly!

**Prefab Tips:**
- Armature = bones hierarchy (child of root)
- Animator goes on Armature, not root
- Weapon mounts = children of bones

**Testing Tips:**
- Unarmed = stat-based only
- Armed = stat-based + 10 weapon bonus
- Damage = Brawn × 0.5 + weapon bonus

---

## 📞 Common Questions

**Q: Where should the animator be?**
A: On the Armature (child with bones), not the root.

**Q: What parameter do I need?**
A: Just one: `Attack` (trigger).

**Q: What states do I need?**
A: Idle and Attack_1 (minimum).

**Q: Does the animator need a tag?**
A: Yes, Attack_1 must be tagged `"Attack"`.

**Q: Can I have multiple attacks?**
A: Yes, but for now one is fine.

---

## 🎓 Learning Resources

**In your project:**
- EnemyMeleeAttack.cs (lines 25-65)
- EnemyBehavior.cs (lines 143-147)
- EnemyWeaponVisualManager.cs (whole file)

**Key methods:**
```csharp
_animator.SetTrigger("Attack"); // Trigger animation
_animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"); // Check if attacking
```

---

## ✨ You're Almost There!

All code is done. You're just connecting the pieces:
1. Create animator
2. Add mounts to prefabs
3. Add component to prefabs
4. Create test actors
5. Playtest

That's it! 🎉

---

## 📊 Progress

```
Phase 9.5 (Equipment):    ✅ 100% Complete
Phase 9.6 (Weapons):      ✅ 100% Complete
Animator Setup:           ⏳ You are here
Integration:              ⏳ Next
Playtesting:              ⏳ Next
```

Ready to integrate? Start with Task 1! 🚀
