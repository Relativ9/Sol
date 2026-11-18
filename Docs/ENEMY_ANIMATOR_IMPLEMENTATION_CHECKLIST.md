# Enemy Animator Implementation Checklist — Phase 9.6b (PART 1)

**Date:** November 18, 2026  
**Status:** Ready for implementation  
**Scope:** Enemy 3-layer animator setup

---

## Pre-Implementation (Code Review)

- [ ] Read `EnemyMeleeAttack.cs` changes:
  - [ ] New fields: `_combatLayerIndex`, `_handsLayerIndex`, `_debugAttack`
  - [ ] New method: `SetLayerWeights(bool combatActive)`
  - [ ] New method: `EndAttack()`
  - [ ] Updated `PlayAttack()` with layer switching
  - [ ] Implements `IAnimationEventReceiver`
  - [ ] Registers to `"OnAttackEnd"` event in Start()

- [ ] Verify dependencies:
  - [ ] `EnemyWeaponVisualManager` exists and has `SetDrawn(bool)`
  - [ ] `AnimationEventDispatcher` exists and has event registration
  - [ ] `EnemyBehavior` calls `_attack.PlayAttack()`

- [ ] Compile check:
  - [ ] No compilation errors
  - [ ] IntelliSense shows new methods

---

## Animator Setup (Manual in Unity)

### Step 1: Create Layers

- [ ] Open Animator window (Window → Animation → Animator)
- [ ] Select enemy's Animator Controller

**Layer 0: "Locomotion"** (already exists)
- [ ] Default layer
- [ ] Weight: 1.0

**Layer 1: "Combat"** (new)
- [ ] Right-click "Layers" → "Create Layer"
- [ ] Name: "Combat"
- [ ] Weight: 0.0 (starts off)
- [ ] Sync Layers: ☑ Locomotion

**Layer 2: "Hands"** (new)
- [ ] Right-click "Layers" → "Create Layer"
- [ ] Name: "Hands"
- [ ] Weight: 1.0 (starts on)
- [ ] Sync Layers: ☐ (unchecked)

### Step 2: Create States on Locomotion Layer

- [ ] Create states: `Idle`, `Walk`, `Run`, `Strafe`
- [ ] Assign animation clips to each
- [ ] Create transitions by `Speed` parameter:
  - [ ] Idle → Walk (Speed > 0.5)
  - [ ] Walk → Run (Speed > 1.5)
  - [ ] Run → Walk (Speed < 1.5)
  - [ ] Walk → Idle (Speed < 0.1)
  - [ ] Run → Idle (Speed < 0.1)

### Step 3: Create States on Combat Layer

- [ ] Click "Combat" layer tab
- [ ] Create state: `Attack_1` (default)
- [ ] Assign attack animation clip
- [ ] Create transition: `Attack_1` → Any State (100% exit time)

### Step 4: Create States on Hands Layer

- [ ] Click "Hands" layer tab
- [ ] Create states:
  - [ ] `Idle_Sheathed` (default)
  - [ ] `Unsheathe`
  - [ ] `Idle_Unsheathed`
  - [ ] `Sheathe`

- [ ] Assign clips (can be empty if no anims exist):
  - [ ] `Idle_Sheathed`: Idle weapon sheathed (optional)
  - [ ] `Unsheathe`: Draw weapon
  - [ ] `Idle_Unsheathed`: Idle weapon drawn (optional)
  - [ ] `Sheathe`: Put weapon away

- [ ] Create transitions:
  - [ ] `Idle_Sheathed` → `Unsheathe` (IsSheathed == false)
  - [ ] `Unsheathe` → `Idle_Unsheathed` (100% exit time)
  - [ ] `Idle_Unsheathed` → `Sheathe` (IsSheathed == true)
  - [ ] `Sheathe` → `Idle_Sheathed` (100% exit time)

### Step 5: Create Parameters

- [ ] `Speed` (Float) — default 0
- [ ] `Attack` (Trigger)
- [ ] `IsSheathed` (Bool) — default true

---

## Animation Setup

### Step 6: Add Animation Events to Attack Clip

- [ ] Select attack animation clip in Assets
- [ ] In Inspector, click "Animation Events"
- [ ] Add event at frame 0:
  - [ ] Function: `AnimationEventDispatcher.OnAttackStart()`

- [ ] Add event at ~95% duration:
  - [ ] Function: `AnimationEventDispatcher.OnAttackEnd()`

### Step 7: Verify Event Names

All clips should have these events where applicable:
- [ ] OnAttackStart at frame 0
- [ ] OnAttackEnd at ~95%

---

## Hierarchy & Mounts

### Step 8: Create Weapon Mount Transforms

In Hierarchy, expand enemy armature and create:

**RightHandWeaponSpot**
- [ ] Parent: Right hand bone (Armature | Skeleton | Hand_R)
- [ ] Position: (0, 0, 0.15) — adjust so weapon in hand
- [ ] Rotation: (0, 0, 0)

**SheathedWeaponSpot**
- [ ] Parent: Hip bone (Armature | Skeleton | Hip)
- [ ] Position: (0, 0, 0) — adjust so weapon on hip
- [ ] Rotation: (0, 0, 0)

### Step 9: Verify Mounts Work

- [ ] Enter Play mode
- [ ] Select enemy
- [ ] Select `EnemyWeaponVisualManager`
- [ ] Toggle `_drawn` in Inspector:
  - [ ] true → weapon appears in hand
  - [ ] false → weapon appears on hip
- [ ] Exit Play mode

---

## Component Configuration

### Step 10: Configure EnemyMeleeAttack

Inspector fields:
- [ ] Combat Layer Index: **1**
- [ ] Hands Layer Index: **2**
- [ ] Attack Trigger: **"Attack"**
- [ ] Debug Attack: **☑ true**

### Step 11: Configure EnemyWeaponVisualManager

Inspector fields:
- [ ] Right Hand Mount: Drag `RightHandWeaponSpot`
- [ ] Sheathed Mount: Drag `SheathedWeaponSpot`
- [ ] Drawn: **☐ false**

### Step 12: Add AnimationEventDispatcher

- [ ] If missing from enemy root:
  - [ ] Add Component → `AnimationEventDispatcher`

### Step 13: Configure Weapon

Check WeaponDataSO:
- [ ] `unsheathedPosition`: (0, 0, 0.15)
- [ ] `unsheathedRotation`: Match grip
- [ ] `sheathedPosition`: (0, 0, 0)
- [ ] `sheathedRotation`: Match sheathed

**→ Continue in ENEMY_ANIMATOR_IMPLEMENTATION_CHECKLIST_PART2.md**
