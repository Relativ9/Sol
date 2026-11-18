# Phase 9: Enemies & Basic AI — Implementation Plan

**Date:** November 17, 2026  
**Status:** Plan for MVP encounter-ready enemy

---

## What Exists (Foundation)

Health, damage, and stats systems are complete:
- `TargetHealth`: Takes damage, shows health bar
- `EnemyStats`: Reads `ActorStatSO` (base stats, status immunities)
- `StatusController`: Shared with player, handles bleed/stagger ticks
- `IDamageReceiver`: Works for all targets

Damage sources exist (turrets, triggers). **No enemy prefab or AI yet.**

---

## Design: Minimal Enemy (MVP)

### Components

- **EnemyAI** (NEW): Detects player, walks, attacks on cooldown
- **EnemyMeleeHitbox** (NEW): Trigger, delivers damage during Attack state
- **Animator** (simple): Idle / Walk / Attack
- **EnemyStats**, **TargetHealth**, **StatusController**: Wire up existing

### Behavior

```
Distance > detectionRange → Idle
Distance in range → Walk toward player at moveSpeed
Distance < attackRange → Stop, attack every attackCooldown seconds
```

No pathfinding, no line of sight, no fancy tactics. Simple distance check loop.

---

## Implementation Steps

### Step 1: Create EnemyAI.cs

Using GameObject.FindWithTag("Player"), walk toward player, set animator Speed and Attack.

### Step 2: Create EnemyMeleeHitbox.cs  

Trigger collider, fires once per Attack animation state, delivers damage via DamageProfileSO.

### Step 3: Create Enemy Animator

Simple state machine: Idle <-> Walk (Speed param), Idle -> Attack (trigger, auto-return).
Reuse clips from Kevin Iglesias humanoid pack.

### Step 4: Build Enemy Prefab

Wire: EnemyStats, TargetHealth, StatusController, EnemyAI, Animator, hitbox collider.
Assign Animator controller, DamageProfileSO, ActorStatSO.
Tag: "Enemy", Layer: "Enemies".

---

## Success Criteria (MVP)

- [ ] Enemy prefab in scene
- [ ] Enemy detects & walks to player
- [ ] Enemy attacks on cooldown
- [ ] Player takes damage from enemy
- [ ] Enemy takes damage from player combo
- [ ] Health bars visible
- [ ] Status effects apply
- [ ] Enemy dies at 0 HP

---

## Scope: ~4-5 hours

Two scripts (EnemyAI, EnemyMeleeHitbox), one animator controller, one prefab, testing & tuning.
Enough for one simple encounter.

---

## Known Limits (Fix Later)

1. No pathfinding (straight-line walk)
2. No line of sight (just distance)
3. No death animation (disappears at 0 HP)
4. No loot on death
5. No group behavior
6. No reaction to hits (no flinch)
7. Animation clips reused from pack (not custom)
