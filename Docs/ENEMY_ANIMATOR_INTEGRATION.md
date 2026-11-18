# Enemy Animator Integration — Technical Details

**Date:** November 18, 2026  
**Status:** ✅ IMPLEMENTATION COMPLETE

---

## Architecture: 3-Layer System

```
Animator Controller (Enemy)
├─ Layer 0: Locomotion (default)
│  └─ Weight: 1.0 (always on)
│  └─ States: Idle, Walk, Run, Strafe
│
├─ Layer 1: Combat (weapon attacks)
│  └─ Weight: 0.0 → 1.0 → 0.0 (during attack)
│  └─ States: Attack_1 (→ Attack_2, Attack_3 in Phase 9.8)
│
└─ Layer 2: Hands (weapon poses)
   └─ Weight: 1.0 → 0.0 → 1.0 (inverse of Combat)
   └─ States: Idle_Sheathed, Unsheathe, Idle_Unsheathed, Sheathe
```

**Why 3 Layers?**
- Locomotion always plays (movement under attacks)
- Combat separate from Locomotion (synced attacks without movement interruption)
- Hands layer independent (weapon poses in/out smoothly)
- Matches player system (consistent codebase)
- Ready for combos (Phase 9.8)

---

## Code Implementation

### EnemyMeleeAttack: Layer Weight Management

```csharp
public void PlayAttack()
{
    // Switch layers: show Combat, hide Hands
    SetLayerWeights(combatActive: true);
    
    // Draw weapon visually (hip → hand)
    if (_weaponVisuals != null)
        _weaponVisuals.SetDrawn(true);
    
    // Trigger attack animation
    _animator.SetTrigger(_attackTrigger);
}

public void EndAttack()
{
    // Switch layers: hide Combat, show Hands
    SetLayerWeights(combatActive: false);
    
    // Sheathe weapon visually (hand → hip)
    if (_weaponVisuals != null)
        _weaponVisuals.SetDrawn(false);
}

private void SetLayerWeights(bool combatActive)
{
    // Combat layer: 1.0 during attack, 0.0 during idle
    float combatWeight = combatActive ? 1f : 0f;
    _animator.SetLayerWeight(_combatLayerIndex, combatWeight);
    
    // Hands layer: 0.0 during attack, 1.0 during idle
    float handsWeight = combatActive ? 0f : 1f;
    _animator.SetLayerWeight(_handsLayerIndex, handsWeight);
}
```

### Animation Event Flow

**Setup:**
1. `AnimationEventDispatcher` on enemy root receives animator events
2. `EnemyMeleeAttack` implements `IAnimationEventReceiver`
3. On Start(), registers to receive `"OnAttackEnd"` event

**Code:**
```csharp
void Start()
{
    if (_eventDispatcher != null)
    {
        _eventDispatcher.RegisterReceiver(this, "OnAttackEnd");
    }
}

public void OnAnimationEvent(string eventName)
{
    if (eventName == "OnAttackEnd")
    {
        EndAttack();
    }
}
```

---

## Synchronization Points

### Between Animator Layers

- **Combat synced with Locomotion:** Both play simultaneously
- **Hands independent:** Can blend in/out separately
- **Result:** Upper body attacks + lower body movement or idle

### Between Components

| From | To | Method | Purpose |
|------|----|---------||---------|
| PlayAttack() | SetLayerWeights() | Direct call | Activate Combat layer |
| PlayAttack() | SetDrawn(true) | Via _weaponVisuals | Weapon to hand |
| Animation event | EndAttack() | Via OnAnimationEvent() | Deactivate Combat layer |
| EndAttack() | SetDrawn(false) | Via _weaponVisuals | Weapon to hip |

---

## Timeline: Attack Sequence

```
Frame N: PlayAttack() called
├─ SetLayerWeights(true) → Combat=1, Hands=0
├─ SetDrawn(true) → Weapon to hand
└─ SetTrigger("Attack")

Frame N+1 to N+40: Attack animation plays
├─ OnAttackStart fires (frame 0)
├─ Damage window open
└─ OnAttackEnd fires (frame ~38, 95% duration)

Frame N+38: EndAttack() executed
├─ SetLayerWeights(false) → Combat=0, Hands=1
└─ SetDrawn(false) → Weapon to hip

Frame N+40+: Animation ends
├─ Combat layer fades out
├─ Hands layer fades in
└─ Return to Idle
```

---

## Component Dependencies

```
EnemyMeleeAttack
├─ Animator (GetComponentInChildren)
├─ EnemyStats (GetComponentInParent)
├─ EnemyWeaponVisualManager (GetComponent)
└─ AnimationEventDispatcher (GetComponentInChildren)

EnemyBehavior
├─ Calls _attack.PlayAttack() when ready
└─ (Relies on animation events for attack end)

EnemyWeaponVisualManager
├─ Called by PlayAttack() → SetDrawn(true)
├─ Called by EndAttack() → SetDrawn(false)
└─ Reads from WeaponDataSO positions
```

---

## Animator Parameters

**Used Now:**

| Parameter | Type | Set By | Purpose |
|-----------|------|--------|---------|
| `Speed` | Float | EnemyLocomotion | Locomotion speed |
| `Attack` | Trigger | PlayAttack() | Start attack animation |
| `IsSheathed` | Bool | WeaponVisualManager | Control weapon pose |

**Future (Phase 9.8):**

| Parameter | Type | Purpose |
|-----------|------|---------|
| `ComboIndex` | Int | Which attack in combo |
| `CanStringToCombo` | Bool | Can chain to next swing? |

→ **Continue in ENEMY_ANIMATOR_INTEGRATION_PART2.md**
