# Rigify Animation Integration Checklist

## ✅ Code Implementation Complete

### Issue 1: Layer Weights (FIXED)
- [x] Added `SetLayerWeight()` method to `PlayerAnimationController`
- [x] Added `GetLayerWeight()` method to `PlayerAnimationController`
- [x] Added layer index fields to `CombatStateController`:
  - [x] `_locomotionLayerIndex = 0`
  - [x] `_comboLayerIndex = 1` (was hardcoded)
  - [x] `_handsLayerIndex = 2`
- [x] Store animator reference as `_animator` field in `CombatStateController`
- [x] Initialize layer weights in `Initialize()`:
  - Combat layer: 0 (off)
  - Hands layer: 1 (on)
- [x] Set layer weights in `UnsheatheWeapon()`:
  - Combat layer: 1 (on)
  - Hands layer: 0 (off)
- [x] Restore layer weights in `SheatheWeapon()`:
  - Combat layer: 0 (off)
  - Hands layer: 1 (on)

### Issue 2: Swing Duration per Attack (FIXED)
- [x] Added `GetCurrentSwingDuration()` method
- [x] Reads animator state info from Combat layer
- [x] Accounts for animator speed multiplier
- [x] Falls back to `_swingDuration` (1.0s) if read fails
- [x] Updated comments in code to reflect new behavior

## ⚙️ Setup Required (Before Testing)

### In Animator Controller
- [ ] Verify three layers exist:
  - [ ] Layer 0: "Locomotion"
  - [ ] Layer 1: "Combat" 
  - [ ] Layer 2: "Hands"
- [ ] Verify Combat layer has three states:
  - [ ] "Melee Combo Attack 1"
  - [ ] "Melee Combo Attack 2"
  - [ ] "Melee Combo Attack 3"
- [ ] Assign your Rigify attack animations to each state
- [ ] Verify idle state exists on Locomotion layer

### In CombatStateController Inspector
- [ ] Set `_locomotionLayerIndex = 0`
- [ ] Set `_comboLayerIndex = 1`
- [ ] Set `_handsLayerIndex = 2`
- [ ] Verify `_comboStates` array = { "Melee Combo Attack 1", "Melee Combo Attack 2", "Melee Combo Attack 3" }
- [ ] Verify `_swingDuration = 1.0` (fallback default)
- [ ] Enable `_debugCombat = true` for testing

### Animation Clips (Rigify Exports)
- [ ] Create animation events in each attack clip:
  - [ ] `OnAttackStart` at frame 0 (start of swing)
  - [ ] `AttackRecovery` at ~68% duration (blade returning)
  - [ ] `OnAttackEnd` at end of clip
- [ ] Note the actual clip lengths for documentation

### Player Scene Setup
- [ ] Ensure Player has correct Avatar (Rigify mesh)
- [ ] Ensure Animator controller is assigned
- [ ] Verify Animator has all expected layers visible

## 🧪 Testing Phase 1: Layer Weights

**Goal:** Verify animations display correctly (no layer conflicts)

### Prerequisites
- [ ] Start game in Play mode
- [ ] Draw weapon (press E or toggle key)

### Check Points
1. **Weapon Drawn Animation**
   - [ ] Weapon animation plays smoothly
   - [ ] No z-fighting or ghosting
   - [ ] Player model visible and not glitching

2. **Inspector Layer Verification**
   - [ ] Open Animator window (Window → Animation → Animator)
   - [ ] Click on the animated character in scene
   - [ ] Expand layer list and verify weights:
     - [ ] Locomotion: 1.0
     - [ ] Combat: 1.0
     - [ ] Hands: 0.0

3. **Console Logs**
   - [ ] Should see debug logs about layer setup

## 🧪 Testing Phase 2: Attack Animation

**Goal:** Verify first attack animation plays correctly

### Prerequisites
- [ ] Weapon drawn
- [ ] Player standing still

### Check Points
1. **Press Attack Key**
   - [ ] One attack animation plays
   - [ ] Animation length matches your Rigify clip
   - [ ] Animation completes fully

2. **Animation Flow** - Console should show:
   ```
   Starting attack
   [Combo] Swing duration from animator: X.XXs
   Attack completed
   ```

3. **Visual Check**
   - [ ] Weapon swings smoothly
   - [ ] No clipping through body
   - [ ] Arm movement looks natural
   - [ ] Animation loops back to idle correctly

## 🧪 Testing Phase 3: Combo Chaining

**Goal:** Verify pressing during hold chains to next attack

### Prerequisites
- [ ] Attack animation plays and completes
- [ ] Hold window is active (slowed animation)

### Check Points
1. **Press During Hold**
   - [ ] Second attack animation plays
   - [ ] Combo chains smoothly (no pause)

2. **Third Attack**
   - [ ] Can press again during second hold
   - [ ] Third attack plays

3. **Combo End**
   - [ ] After third attack, no more chains available
   - [ ] Returns to idle

## 📋 Finalization

After all tests pass:
- [ ] Disable `_debugCombat` (set to false)
- [ ] Test with actual gameplay (moving, camera control)
- [ ] Check combat responsiveness feels good
- [ ] Commit changes
