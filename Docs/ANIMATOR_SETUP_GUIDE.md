# Enemy Animator Setup Guide

**Location:** Animator on Armature child (not root)  
**Status:** REQUIRED FOR COMBAT

---

## What the Animator Needs

### 1. One Parameter: `Attack` (Trigger)

That's it! Just one trigger.

### 2. State Tag: `Attack`

All attack animations must be tagged `"Attack"`.

### 3. State Machine

```
Idle (default)
  ↓ Attack (trigger)
Attack_1 (tagged: "Attack")
  ↓ (animation ends)
Idle
```

---

## Step-by-Step Setup

### Step 1: Create Animator Controller

1. Right-click in Assets → Create → Animator Controller
2. Name: `Enemy_Bandit_Animator`

### Step 2: Add Parameter

1. Double-click to open Animator
2. Parameters panel (left):
   - Click `+` → Trigger
   - Name: `Attack`
3. Save

### Step 3: Create States

**Create Idle:**
1. Right-click in grid → Create State → Empty
2. Name: `Idle`
3. Right-click → Set as Layer Default State

**Create Attack_1:**
1. Right-click → Create State → Empty
2. Name: `Attack_1`
3. Drag your attack animation here
4. Inspector → Tag: type `Attack` (must match exactly!)

### Step 4: Add Transitions

**Idle → Attack_1:**
1. Right-click `Idle` → Make Transition → drag to `Attack_1`
2. Click transition arrow
3. Inspector:
   - Conditions: `Attack` = True (trigger)
   - Has Exit Time: OFF
   - Exit Time: 0

**Attack_1 → Idle:**
1. Right-click `Attack_1` → Make Transition → drag to `Idle`
2. Click transition arrow
3. Inspector:
   - Conditions: (empty)
   - Has Exit Time: ON
   - Exit Time: 1.0

### Step 5: Assign to Enemy

1. Select Armature (child of enemy root)
2. In Inspector → Animator component:
   - Controller: drag `Enemy_Bandit_Animator` here
3. Save

---

## Animation Requirements

**Attack Animation:**
- Duration: 1.0-1.5 seconds
- Hitbox can collide any time during
- System only hits once per swing

**Idle Animation:**
- Looping preferred
- Any length

---

## How It Works

```
EnemyBehavior → _attack.PlayAttack()
  ↓
EnemyMeleeAttack.PlayAttack()
  ↓
_animator.SetTrigger("Attack")
  ↓
Animator: Idle → Attack_1
  ↓
During animation: OnTriggerStay checks collisions
  ↓
Animation ends: Idle
  ↓
Complete!
```

---

## Checking It Works

1. Open Animator (double-click controller)
2. Play mode in Scene
3. Click on enemy to select it
4. In Animator window, should see:
   - Idle state highlighted initially
   - When enemy attacks: Attack_1 highlighted
   - Animation plays
   - Returns to Idle

---

## Common Issues

**Attack doesn't play:**
- Check trigger name is exactly `"Attack"` (case-sensitive)
- Check transition has `Attack` condition

**No damage:**
- Check attack animation tagged: `"Attack"`
- Tag must match exactly (case-sensitive)

**Animation loops:**
- Check return transition:
  - Has Exit Time: ON ✓
  - Exit Time: 1.0
  - No trigger required

**Animator not found:**
- Check Animator component on Armature (not root)
- Check Controller is assigned

---

## Multiple Attacks (Optional)

For later phases, you can add:

```
Idle
  ↓ Attack
  ├→ Attack_1 (random)
  ├→ Attack_2 (random)
  └→ Attack_3 (random)
    ↓
    Idle
```

For now: Single attack works fine.

---

## Reference

**EnemyMeleeAttack expects:**
```csharp
_animator.SetTrigger("Attack"); // Triggers state change

AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
if (state.IsTag("Attack")) // Checks during animation
{
    // Can damage now
}
```

**See scripts:**
- `/Assets/_Project/Scripts/Enemies/Combat/EnemyMeleeAttack.cs`
- `/Assets/_Project/Scripts/Enemies/Behavior/EnemyBehavior.cs`
