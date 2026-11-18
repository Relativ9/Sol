# Enemy Animator State Machine Diagram

---

## Recommended Setup

### Simple Single-Attack

```
START (default)
  │
  ├─→ [Idle] ←────────────────────┐
      │                           │
      │ Attack (trigger)          │ (animation end)
      │                           │
      ├─→ [Attack_1] ─────────────┘
          (tagged: "Attack")
```

**Transitions:**
1. Idle → Attack_1
   - Trigger: `Attack`
   - Has Exit Time: OFF
   - Exit Time: 0

2. Attack_1 → Idle
   - Trigger: (none)
   - Has Exit Time: ON
   - Exit Time: 1.0

---

### Multiple Attacks (Combo-Ready)

```
START (default)
  │
  ├─→ [Idle] ←────────────────────┐
      │                           │
      │ Attack (trigger)          │ (animation end)
      │                           │
      ├─→ [Attack_1] ─────────────┤
      │   (tagged: "Attack")      │
      │                           │
      ├─→ [Attack_2] ─────────────┤
      │   (tagged: "Attack")      │
      │                           │
      └─→ [Attack_3] ─────────────┘
          (tagged: "Attack")
```

**All Attack states transition back to Idle with:**
- Has Exit Time: ON
- Exit Time: 1.0

---

## Parameter Configuration

### Single Required Parameter

| Name | Type | Default |
|------|------|---------|
| `Attack` | Trigger | (inactive) |

---

## State Details

### State: Idle

- **Animation:** Idle loop (or static pose)
- **Duration:** Looping
- **Transitions Out:**
  - → Attack_1 (on `Attack` trigger)
- **Purpose:** Default/rest state

### State: Attack_1

- **Animation:** Attack swing (1.0-1.5 sec)
- **Duration:** Fixed length
- **Transitions Out:**
  - → Idle (on animation end)
- **Tag:** `"Attack"` ← REQUIRED!
- **Purpose:** Melee attack with weapon

---

## Animation Event Timing

Within Attack_1 animation:

```
Timeline: 0.0s ────────────────── 1.0s
          │                       │
          Start                   End
          │                       │
    ┌─────┴────────────────────┬──┴─────┐
    │   Can damage window      │        │
    │   (OnTriggerStay active) │ Return │
    └──────────────────────────┴────────┘
          Frame 5-25 of 30 (example)
```

**Important:** Damage can happen ANY time during Attack_1. The system tracks with `_hitThisSwing` to prevent double-hits.

---

## Transition Rules

### Idle → Attack_1

```
┌─────────────────────────────────┐
│ Condition                       │
├─────────────────────────────────┤
│ Attack = true (trigger)         │
├─────────────────────────────────┤
│ Has Exit Time: OFF              │
│ Exit Time: 0                    │
│ Transition Duration: 0.1s       │
│ Can Transition To Self: OFF     │
└─────────────────────────────────┘
```

### Attack_1 → Idle

```
┌─────────────────────────────────┐
│ Condition                       │
├─────────────────────────────────┤
│ (no condition - automatic)      │
├─────────────────────────────────┤
│ Has Exit Time: ON               │
│ Exit Time: 1.0                  │
│ Transition Duration: 0.1s       │
│ Can Transition To Self: OFF     │
└─────────────────────────────────┘
```

---

## Code Integration

### Triggering

```csharp
// From EnemyMeleeAttack.PlayAttack()
_animator.SetTrigger("Attack");
// → Causes: Idle → Attack_1 transition
```

### Checking State

```csharp
// From EnemyMeleeAttack.OnTriggerStay()
AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

if (stateInfo.IsTag("Attack"))
{
    // Attack animation is active
    // Safe to deal damage
}

// From EnemyMeleeAttack.Update()
if (!stateInfo.IsTag("Attack") && _hitThisSwing)
{
    // Attack animation ended
    // Reset hit tracking
    _hitThisSwing = false;
}
```

---

## Setup Checklist

- [ ] Animator controller created
- [ ] Parameter `Attack` (trigger) added
- [ ] Idle state exists (is default)
- [ ] Attack_1 state exists
- [ ] Attack_1 tagged: `"Attack"`
- [ ] Idle → Attack_1 transition (trigger: `Attack`)
- [ ] Attack_1 → Idle transition (exit time: ON, 1.0)
- [ ] Animator assigned to Armature
- [ ] No animator errors in Console

---

## Example Hierarchy

```
Enemy_Root
├── EnemyStats
├── EnemyBehavior
├── EnemyMeleeAttack
├── EnemyLocomotion
├── Armature ← Animator HERE
│   ├── Skeleton...
│   ├── RightHandWeaponSpot
│   ├── SheathedWeaponSpot
│   └── [Animator component assigned]
└── EnemyWeaponVisualManager
```

---

## Performance

- **Animator Cost:** ~1-2% CPU per enemy
- **Optimization:** Use low-poly animations
- **Reuse:** All enemies can share same controller

---

## Variants (Future)

### Dual Attack Types

```
Parameter: AttackType (int or enum)

Idle → Slash (if AttackType == 0)
    → Thrust (if AttackType == 1)
```

### Stagger Response

```
Parameter: Staggered (trigger)

Any State → Stagger
Stagger → Idle
```

### Weapon-Based Animations

```
Parameter: WeaponEquipped (bool)

If WeaponEquipped == true:
  Attack → SlashAttack
Else:
  Attack → PunchAttack
```

For Phase 9.6: Stick with simple single-attack setup.

---

## Quick Test

1. Play Scene
2. Position camera to see enemy
3. Click enemy to select it
4. In Animator window, observe:
   - ```
     Idle (green) → default
     Enemy attacks
     Attack_1 (green) → active
     Animation plays
     Idle (green) → returns
     ```
5. If see this flow: ✅ Correct!
