# Animator: Final Answer

**Your Question:** "What should the animator look like and we need to assign it?"

---

## The Answer: 3 Things

### 1. Create Animator Controller

**File:** `Enemy_Bandit_Animator.controller`

**Contents:**
- Parameter: `Attack` (trigger)
- State: `Idle` (default)
- State: `Attack_1` (tag: `"Attack"`)
- Transition: Idle → Attack_1 (trigger: `Attack`)
- Transition: Attack_1 → Idle (exit time: 1.0)

### 2. Assign Animation

On `Attack_1` state:
- Drag your attack animation
- Inspector → Tag: `"Attack"`
- Duration: 1.0-1.5 seconds

### 3. Assign to Prefab

On Armature (child with bones):
- Animator component
- Controller: drag your animator controller

---

## The State Machine

```
Idle (default)
  ↓ Attack trigger
Attack_1 (tagged: "Attack")
  ├─ Plays 1.0-1.5s animation
  ├─ Can deal damage during
  ↓ Animation ends
Idle
```

---

## How It Works

```
EnemyBehavior.UpdateAttacking()
  ↓ Calls PlayAttack()
EnemyMeleeAttack.PlayAttack()
  ↓ SetTrigger("Attack")
Animator receives trigger
  ↓ Transitions Idle → Attack_1
Attack_1 animation plays
  ↓ OnTriggerStay checks collisions
  ↓ If weapon hits: deliver damage
Animation ends (1.0s)
  ↓ Automatic transition to Idle
Ready for next attack
```

---

## Transition Settings

**Idle → Attack_1**
```
Trigger: Attack
Has Exit Time: OFF ← Important!
Exit Time: 0
```

**Attack_1 → Idle**
```
Has Exit Time: ON ← Important!
Exit Time: 1.0
(no trigger needed)
```

---

## Key Points

✅ Only 1 parameter: `"Attack"` (trigger)  
✅ Only 1 tag needed: `"Attack"` (on attack states)  
✅ Case-sensitive: `"Attack"` not `"attack"`  
✅ Animator on Armature, not root  
✅ Animation duration: 1.0-1.5 seconds  

---

## Checklist

- [ ] Animator controller created
- [ ] Parameter `Attack` added
- [ ] Idle state created (default)
- [ ] Attack_1 state created
- [ ] Animation assigned to Attack_1
- [ ] Attack_1 tagged: `"Attack"`
- [ ] Idle → Attack_1 (trigger: Attack)
- [ ] Attack_1 → Idle (exit time: 1.0)
- [ ] Assigned to Armature.Animator
- [ ] Playtest works

---

## References

- ANIMATOR_SETUP_GUIDE.md — Step-by-step creation
- ANIMATOR_STATE_MACHINE_DIAGRAM.md — Visual diagrams
- ANIMATOR_INTEGRATION_SUMMARY.md — Quick reference
- YOUR_ACTION_ITEMS.md — Your tasks
- COMPLETE_INTEGRATION_GUIDE.md — Full overview

---

## That's It!

Simple animator. Simple system. Works great.

You're ready to go! ✅
