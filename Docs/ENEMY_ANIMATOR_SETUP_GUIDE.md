# Enemy Animator Setup Guide — 3-Layer Architecture

**Date:** November 18, 2026  
**Status:** ✅ IMPLEMENTATION READY  
**Version:** 9.6b — Aligned with Player Architecture

---

## Overview

Enemies now use the **same 3-layer animator architecture as the player** for consistency and extensibility:

| Layer | Purpose | Weight Schedule |
|-------|---------|-----------------|
| **0: Locomotion** | Movement (idle, walk, run, strafe) | Always 1.0 |
| **1: Combat** | Attack animations | 0.0 idle → 1.0 attack → 0.0 end |
| **2: Hands** | Sheathe/unsheathe weapon poses | 1.0 idle → 0.0 attack → 1.0 end |

**Layer switching is automatic** via `EnemyMeleeAttack.SetLayerWeights()` when:
- `PlayAttack()` called → Combat layer active, Hands layer hidden
- `EndAttack()` called (or animation event fires) → Combat layer hidden, Hands layer visible

**Weapon repositioning is automatic** via `EnemyWeaponVisualManager.SetDrawn()`:
- During attack: Weapon moves from hip mount to hand mount
- After attack: Weapon moves back to hip mount

---

## Step 1: Animator Controller Setup

### Create 3 Layers

In your enemy's Animator Controller:

1. **Layer 0: "Locomotion"** (default, weight 1.0)
   - Add states: `Idle`, `Walk`, `Run`, `Strafe`
   - Use directional blend tree by `Speed` parameter
   - **Do NOT sync with other layers**

2. **Layer 1: "Combat"** (starts at weight 0.0)
   - Add states: `Attack_1`
   - Currently: Single attack state
   - **Future (Phase 9.8):** 3-state blend tree (Attack_1, Attack_2, Attack_3) by `ComboIndex` parameter
   - **Sync with Locomotion layer** (checkbox: "Sync Layers")

3. **Layer 2: "Hands"** (starts at weight 1.0)
   - Add states: `Idle_Sheathed`, `Unsheathe`, `Idle_Unsheathed`, `Sheathe`
   - Use `IsSheathed` (bool parameter) to switch between them
   - **Do NOT sync with other layers**

### Configure Parameters

Add these Animator parameters:

| Parameter | Type | Purpose | Default |
|-----------|------|---------|---------|
| `Speed` | Float | Locomotion speed (0 = idle, 1 = walk, 2 = run) | 0 |
| `Attack` | Trigger | Start attacking | — |
| `IsSheathed` | Bool | Is weapon sheathed? | true |

---

## Step 2: Animation Clips

### Assign Attack Animation

Assign your attack animation clip to the `Attack_1` state:

```
Attack_1 (state)
├─ Motion: YourAttackClip.fbx
├─ Speed: 1.0
└─ Animation Events:
   ├─ OnAttackStart (frame 0)
   ├─ OnAttackEnd (frame ~95% of duration)
   └─ [Optional] AttackRecovery (frame ~70% for future combo hold)
```

**Event locations:**
- **OnAttackStart:** At the very beginning of the swing (frame 0)
- **OnAttackEnd:** Near the end, after damage window closes (~95% of clip duration)
- **AttackRecovery:** If implementing future combo hold, set to ~70% of clip duration

### Sheathe/Unsheathe Animations

Assign weapon transition animations to the Hands layer:

```
Idle_Sheathed (default)
├─ Motion: Idle_Weapon_Sheathed.fbx
└─ Transition to Unsheathe (on IsSheathed == false)

Unsheathe
├─ Motion: Draw_Weapon.fbx
└─ Transition to Idle_Unsheathed (at 100% exit time)

Idle_Unsheathed
├─ Motion: Idle_Weapon_Drawn.fbx
└─ Transition to Sheathe (on IsSheathed == true)

Sheathe
├─ Motion: Sheathe_Weapon.fbx
└─ Transition to Idle_Sheathed (at 100% exit time)
```

---

## Step 3: Mount Transforms

### Create Weapon Mounts

On your enemy's armature, create two transforms for weapon positioning:

**1. RightHandWeaponSpot**
- Parent: Right hand bone (e.g., `Armature | Skeleton | Hand_R`)
- Position: (0, 0, 0.15) — Adjust so weapon sits naturally in hand
- Rotation: (0, 0, 0) — Adjust to match weapon grip angle
- Used when weapon is drawn

**2. SheathedWeaponSpot**
- Parent: Hip bone (e.g., `Armature | Skeleton | Hip`)
- Position: (0, 0, 0) — Adjust so weapon sits on hip
- Rotation: (0, 0, 0) — Adjust to match sheathed position
- Used when weapon is at rest

**Verify in Scene View:**
- Draw weapon in inspector (set `_drawn = true` on `EnemyWeaponVisualManager`)
- Weapon should appear in hand
- Sheathe weapon (`_drawn = false`)
- Weapon should appear on hip

---

## Step 4: Component Configuration

### EnemyMeleeAttack Inspector

Set these fields:

```
Combat Layer Index: 1
Hands Layer Index: 2
Attack Trigger: "Attack"
Debug Attack: true (for testing, disable later)
```

### EnemyWeaponVisualManager Inspector

Set these fields:

```
Right Hand Mount: Drag RightHandWeaponSpot here
Sheathed Mount: Drag SheathedWeaponSpot here
Drawn: false (starts sheathed)
```

### AnimationEventDispatcher (NEW)

**On the enemy root or model GameObject, add `AnimationEventDispatcher` component** (if not already present):

This component receives animation events from the animator and routes them to `EnemyMeleeAttack.OnAnimationEvent()`.

**Wire up animation events in Animator:**
- Select the enemy model/animator
- In Animation window, add events to clips:
  - `OnAttackStart` → calls `AnimationEventDispatcher.OnAttackStart()`
  - `OnAttackEnd` → calls `AnimationEventDispatcher.OnAttackEnd()`

---

## Step 5: Weapon Setup

### Configure Weapon Prefab

In your weapon prefab:

```
Sword (root)
├─ Mesh (visual)
├─ Collider (hitbox, child of Sword)
├─ Rigidbody (set to Kinematic when held)
└─ (no physics constraints needed)
```

The `EnemyWeaponVisualManager` automatically:
- Makes rigidbodies kinematic
- Disables colliders until attack happens
- Enables collider during attack window
- Positions weapon on correct mount

### Configure WeaponDataSO

In your weapon data asset:

```
WeaponDataSO (e.g., "Iron Sword")
├─ Weapon Name: "Iron Sword"
├─ Weapon Damage: 10
├─ Weapon Prefab: [Your sword prefab]
├─ Unsheathed Position: (0, 0, 0.15)
├─ Unsheathed Rotation: (0, 0, 0)
├─ Sheathed Position: (0, 0, 0)
├─ Sheathed Rotation: (0, 0, 0)
└─ [Other fields as needed]
```

**→ Continue in ENEMY_ANIMATOR_SETUP_PART2.md**
