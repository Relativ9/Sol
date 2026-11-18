# Animator Integration Summary

**What:** Create Animator controller and assign to Armature  
**Time:** ~10 minutes  
**Location:** Animator on Armature (bones child)

---

## What You Need to Create

### 1. Animator Controller

- Right-click Assets → Create → Animator Controller
- Name: `Enemy_Bandit_Animator`

### 2. Configure

**Parameter:**
- Add one trigger: `Attack`

**States:**
- Create `Idle` (set as default)
- Create `Attack_1` 
  - Assign attack animation
  - Tag it: `"Attack"`

**Transitions:**
- Idle → Attack_1 (trigger: `Attack`, exit time: OFF)
- Attack_1 → Idle (exit time: ON, 1.0)

### 3. Assign to Prefab

1. Select Armature (child with bones)
2. In Animator component: drag controller
3. Save

---

## The Flow

```
EnemyBehavior sees player
  ↓
Time for attack? (cooldown ready)
  ↓
PlayAttack()
  ↓
SetTrigger("Attack")
  ↓
Animator: Idle → Attack_1
  ↓
Animation plays (1.0-1.5s)
  ↓
During animation: OnTriggerStay() checks collisions
  ↓
Animation ends → Idle
  ↓
Repeat
```

---

## Transition Settings

**Idle → Attack_1**
- Trigger: `Attack`
- Has Exit Time: OFF ← Important!
- Exit Time: 0

**Attack_1 → Idle**
- Trigger: (none)
- Has Exit Time: ON ← Important!
- Exit Time: 1.0

---

## Checklist

- [ ] Animator controller created
- [ ] Parameter `Attack` added
- [ ] Idle state (default) created
- [ ] Attack_1 state created
- [ ] Attack animation assigned
- [ ] Attack_1 tagged: `"Attack"`
- [ ] Transitions configured
- [ ] Animator assigned to Armature
- [ ] Prefab saved
- [ ] Playtest: see animation play

---

## Common Issues

**Animation doesn't trigger:**
- Parameter name wrong (must be `"Attack"`)
- Transition missing `Attack` trigger

**No damage:**
- Attack state NOT tagged `"Attack"`
- Tag is case-sensitive!

**Loops forever:**
- Return transition: Has Exit Time should be ON
- Exit Time should be 1.0

**Can't find animator:**
- Must be on Armature (child), not root
- Must have controller assigned

---

## Code Expectations

```csharp
// Setting trigger
_animator.SetTrigger("Attack");

// Checking state
if (_animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
    // Can damage now
```

**See:**
- EnemyMeleeAttack.cs (line 28, 52, 76)
- EnemyBehavior.cs (line 145)

---

## That's It!

One trigger, two states, two transitions.  
Simple and effective.
