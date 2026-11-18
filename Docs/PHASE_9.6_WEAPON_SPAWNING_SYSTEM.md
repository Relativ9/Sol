# Phase 9.6: Enemy Weapon Spawning & Sheathe/Unsheathe System

**Date:** November 18, 2026  
**Status:** ✅ IMPLEMENTED

---

## Overview

Enemies now automatically spawn their equipped weapons with proper positioning and hitbox linking.

**Pattern:** Mirrors player's `WeaponVisualManager` for consistency.

---

## New Components

### 1. `EnemyWeaponVisualManager.cs` (207 lines)

**Spawns and positions weapon based on equipment and drawn state.**

```csharp
public void OnEquipmentChanged(EquipmentSlot slot, EquipmentDataSO newItem)
  // Called when MainHand changes
  
public void SetDrawn(bool drawn)
  // Reposition: drawn=hand, !drawn=hip
```

**Lifecycle:**
1. Awake → Find parent EnemyStats, spawn weapon if equipped
2. Subscribe to `EnemyStats.EquipmentChanged`
3. On change → respawn weapon
4. OnDestroy → cleanup

---

### 2. Modified `EnemyStats.cs` (+14 lines)

**New Event:**
```csharp
public event Action<EquipmentSlot, EquipmentDataSO> EquipmentChanged;
```

**Fires during initialization** for each slot with equipped item.

---

### 3. Modified `EnemyMeleeAttack.cs` (+10 lines)

**New Method:**
```csharp
public void SetWeaponHitbox(Collider hitbox)
  // Called by weapon manager
```

**Hitbox Check:**
```csharp
if (_weaponHitbox != null && other != _weaponHitbox)
    return; // Only weapon collider deals damage
```

---

## Setup: 4 Steps

### Step 1: Add Mounts to Armature

Required transforms:
- `RightHandWeaponSpot` (child of right hand bone)
- `SheathedWeaponSpot` (child of hip bone)

### Step 2: Add Component

Add `EnemyWeaponVisualManager` to enemy root:
- `_rightHandMount` → RightHandWeaponSpot
- `_sheathedMount` → SheathedWeaponSpot

### Step 3: Configure ActorStatSO

```
MainHand = Iron_Sword.asset
├── weaponPrefab = Sword_Model.prefab
├── unsheathedPosition = (0, 0, 0.15)
├── sheathedPosition = (0, 0, 0)
```

### Step 4: Playtest

Enemy spawns with weapon on hip, attacks with weapon, damage delivered via weapon collider.

---

## Architecture

| Check | Status |
|-------|--------|
| SOLID | 5/5 ✅ |
| Awake/Start | ✅ |
| No SO Mutation | ✅ |
| Events | ✅ |
| Type Safety | ✅ |
| Null Safety | ✅ |

---

## Files Modified

1. **EnemyWeaponVisualManager.cs** (NEW) — 207 lines
2. **EnemyStats.cs** — +14 lines (event + firing)
3. **EnemyMeleeAttack.cs** — +10 lines (hitbox setter)

---

## Testing Checklist

- [ ] Enemy spawns with visible weapon
- [ ] Weapon is positioned on hip (sheathed)
- [ ] Attack delivers damage via weapon
- [ ] Damage = stat-based + weapon bonus (16 = 6 + 10)
- [ ] Unarmed enemies work (no weapon)
- [ ] Multiple enemies work
- [ ] No errors on compile
