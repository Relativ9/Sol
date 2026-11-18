# Phase 9.6 Integration Checklist

**Enemy Weapon Spawning & Sheathe/Unsheathe**

---

## Pre-Integration

- [x] Code compiles
- [x] SOLID upheld
- [x] Events implemented
- [x] Null checks in place

---

## Step 1: Update Enemy Armature

For **each enemy prefab**:

1. Open prefab in scene
2. Find **right hand bone** (e.g., RightHand)
3. Create child Transform: `RightHandWeaponSpot`
   - Position/Rotation: (0,0,0)
4. Find **hip/spine bone**
5. Create child Transform: `SheathedWeaponSpot`
   - Position: (-0.5, -0.3, 0) [adjust as needed]
   - Rotation: (0, 90, 0) [or adjust]

**Verification:**
- [ ] RightHandWeaponSpot on hand bone
- [ ] SheathedWeaponSpot on hip bone

---

## Step 2: Add Component

For **each enemy root**:

1. Select root (e.g., `Bandit_01`)
2. **Add Component** → `EnemyWeaponVisualManager`
3. Assign:
   - `_rightHandMount` → RightHandWeaponSpot
   - `_sheathedMount` → SheathedWeaponSpot
   - `_drawn` → `false`
4. Save prefab

**Verification:**
- [ ] Component added
- [ ] Both mounts assigned
- [ ] No Console errors

---

## Step 3: Configure WeaponDataSO

Check each weapon asset:

```
Iron_Sword.asset
├─ weaponPrefab: Sword_Model.prefab ✓
├─ weaponDamage: 10
├─ unsheathedPosition: (0, 0, 0.15)
├─ unsheathedRotation: (0, 90, 0)
├─ sheathedPosition: (0, -0.2, 0)
├─ sheathedRotation: (0, 0, 0)
```

**Verification:**
- [ ] weaponPrefab assigned
- [ ] Positions tuned (can adjust in playtest)

---

## Step 4: Verify Unarmed

EnemyMeleeAttack on each prefab:
```
_unarmedDamageProfile: Unarmed_Melee_Damage.asset ✓
```

- [ ] Assigned to all prefabs

---

## Step 5: Create Test Actors

**TestBandit_Unarmed.asset**
- MainHand: (empty)
- Brawn: 12
- Expected damage: 6

**TestBandit_Armed.asset**
- MainHand: Iron_Sword.asset
- Brawn: 12
- Expected damage: 16

**TestBandit_Armored.asset**
- MainHand: Iron_Sword.asset
- Torso: Plate_Chest.asset
- Brawn: 12
- Expected damage: 16 + armor reduction

---

## Step 6: Playtest

### Test 1: Unarmed
- Spawn with TestBandit_Unarmed
- No weapon visible ✓
- Attack → damage ≈ 6 ✓

### Test 2: Armed
- Spawn with TestBandit_Armed
- Sword visible on hip ✓
- Attack → damage ≈ 16 ✓

### Test 3: Armored
- Spawn with TestBandit_Armored
- Sword visible + armor ✓
- Player takes reduced damage ✓

### Test 4: Multiple
- Spawn 5 enemies (mix)
- Each works independently ✓

---

## Troubleshooting

**Weapon not visible:**
- Check mounts assigned
- Check weaponPrefab not empty

**Damage not delivered:**
- Check unarmed profile assigned
- Check weapon has collider

**Weapon in wrong position:**
- Adjust unsheathedPosition/Rotation
- Playtest and iterate

---

## Success Criteria

✅ Unarmed damage ≈ 6  
✅ Armed damage ≈ 16  
✅ Weapon visual correct  
✅ Multiple enemies work  
✅ No errors  
✅ Code compiles
