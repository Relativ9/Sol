# Sol Combat System Refactor — November 2026

## Summary
Removed Animation Rigging procedural reach system (`SwingReach`, `WeaponRigController`, IK constraints) and simplified combat to 100% animation-driven motion using Blender-authored clips. The combo system remains and chains three attack animations on input timing.

## Files Deleted
- `Assets/_Project/Scripts/Player/Combat/SwingReach.cs` — Procedural IK reach driver
- `Assets/_Project/Scripts/Player/Combat/WeaponRigController.cs` — Hand IK rig weight manager
- `Assets/_Project/Scripts/Player/PlayerInterfaces/ISwingReach.cs` — Interface for reach service

## Files Modified
- **`PlayerController.cs`** — Removed `SwingReach` service registration (line 58)
- **`CombatStateController.cs`** — Removed all `_swingReach?.Begin()` and `.End()` calls (5 locations)
- **`WeaponVisualManager.cs`** — Removed `_swingReach?.SetWeapon(_hitbox)` call (1 location)
- **`# Sol RPG — Architecture.md`** — Updated Phase 8 status and roadmap; marked swing system complete
- **`FLOWS.md`** — Removed `WeaponRigController` from "not covered" section
- **`Problems and Pitfalls.md`** — Added section 7: decision log for IK removal and clip-driven motion

## What Still Works
- **Combo system:** Three attack animations chain on input timing (hold window ~1 second)
- **Weapon visuals:** Shows/hides and mounts on hand or back
- **Hitbox detection:** Blade collision triggers damage from animated position
- **Combat state:** Unsheathed, sheathed, attacking, unsheathing, sheathing states unchanged
- **Input system:** Attack input maps to combo progression via `CombatStateController`
- **Animation layers:** Weapon layer (attacks, draw/sheathe) and hands layer (idle grip) still active

## Scene Cleanup Required (Manual in Unity)
1. Delete `OneHandedRigs` hierarchy (contains all Animation Rigging constraints: `Right Hand IK`, `SwingRig`, `Swing Hand IK`, `DrawnWeaponRig`)
2. On Player object, remove components:
   - `SwingReach`
   - `WeaponRigController`
3. On `RigBuilder` component: Remove all Rig layers. If none remain, the component can be deleted.

## Why This Change
- Procedural IK reach was stiff and unnatural, no matter how tuned.
- Animation Rigging is built for static poses, not dynamic motion with artistic intent.
- Blender-authored animations give full control over feel, timing, and motion path.
- Faster to iterate: change the clip, not the solver parameters.

## Trade-offs
- **Gain:** Natural motion, artistic control, simpler code, faster iteration
- **Loss:** No mid-swing procedural adjustments (e.g., real-time aim following). The full swing is authored.

## Next Steps
1. In Unity: Delete scene objects and components as listed above.
2. In Blender: Create three attack animation clips (or import from motion capture).
3. Assign clips to `_comboStates` array on `CombatStateController`.
4. Test: Press attack to swing combo 1 → 2 → 3 on subsequent inputs within hold window.

## Code Balance Check
All code files compile. No orphaned references to `SwingReach` or `WeaponRigController` remain in the project.

---
**Date:** November 4, 2026  
**Author:** Cline (automated refactor)  
**Status:** Ready for scene cleanup and animation import

---

# Melee Combo System Rebuild — November 16, 2026

## Summary
The three-swing combo is complete and playtested. `CombatStateController` now reads the combo from the Animator state instead of from animation events. It queues one press per swing and opens a hold at `AttackRecovery`, which slows the rest of the swing. The Animator owns every transition, and code only sets parameters. Rules: Architecture §5b. History: Problems and Pitfalls §15.

## Files Deleted
- `Assets/_Project/Scripts/Player/Combat/CombatStateMachine.cs` (and `.meta`): hold timing moved into the controller.

## Files Modified
- **`CombatStateController.cs`**
  - Added `TrackComboFromAnimator`: each frame, the Combat layer's current or next state sets the swing. Leaving the `MeleeCombo` states ends the combo.
  - Presses during a combo queue the next swing (one per swing) and set `ComboIndex` to it.
  - `AttackRecovery` opens the hold and sets `CanStringToCombo` if a press is queued.
  - The hold slows the attack state through the `ComboSpeed` parameter, capped at 1.
  - The idle press keeps `ComboIndex` at 0 so the entry transition can fire. It is dropped after 0.5 s if the Animator never takes it.
  - Removed: `_swingDuration` (swing length now comes from the clip), the hold start delay, the hold enable flag, the stuck-gate watchdog, and the timing logs.
  - Added inspector fields `_holdEndNormalizedTime`, `_comboSpeedParam`, `_comboTag` and `_comboLayerIndex`. Added an on-screen combo debug line.
- **`Player.controller`** (edited in Unity)
  - Tag `MeleeCombo` on the three attack states. New parameters `ComboIndex` (int) and `ComboSpeed` (float).
  - Chains: `ComboIndex` = next swing plus `CanStringToCombo` true, exit time off, 0.1 duration.
  - Entry requires `ComboIndex` = 0. Idle exits are unchanged.
- **Attack 2 and 3 clips** (`1HandedAttack_2_r.fbx`, `1HandedAttack_3_r.fbx`) added, with `OnAttackStart`, `AttackRecovery` and `OnAttackEnd`.
- **`WeaponVisualManager.cs`** (user change): the trail is now a visual effect.
- **Docs:** Architecture §4, §5b, §17; `FLOWS.md` 3.4; `DECISION_TREES.md` 4 and 4b; `Animation_Integration_Notes.md`; Problems and Pitfalls §4c and §11 (marked superseded) and §15 (new).

## Still To Do
- Tune `_comboHoldSeconds` for the slow-down feel. It needs to be above about 0.59 s to slow the current clips.
- Check trail and hitbox timing on Attacks 2 and 3.
- Test stagger cancel and forced sheathe mid-combo.

**Status:** Core combo complete.

---

# Phase 9: Enemies & Basic AI — Planning & Scope Assessment — November 17, 2026

## Summary

Foundation for enemies is solid: health, damage, status effects, and stats all work. No behavior system exists yet. Created Phase 9 implementation plan for an MVP enemy that can detect the player, walk toward them, and attack on cooldown. Estimated 4–5 hours to implementation.

## Assessment of Existing Systems

**What Works:**
- `TargetHealth`: Takes damage, shows world health bar, triggers `Changed` event
- `EnemyStats`: Reads per-enemy-type `ActorStatSO` (base stats, status immunities)
- `StatusController`: Shared with player, processes bleed/stagger ticks on targets
- `IDamageReceiver` + `ReceiveHit`: Identical path as player (deflection, mitigation, status procs)
- Damage sources: `EnemyDamageSource` (trigger + cooldown), `Projectile`, `ProjectileTurret`
- `WorldHealthBar`: Positioned each frame, hidden at 0 HP or 4 s after last damage

**What Doesn't Exist:**
- Enemy animation controllers (no Animator)
- Behavior tree or state machine (no decision-making)
- Enemy prefabs (no instantiable unit)
- AI perception (no line of sight, hearing, or aggression)
- Aggro system (no detection or threat tracking)

## MVP Design (Encounter-Ready Enemy)

**Components:** EnemyAI (NEW), EnemyMeleeHitbox (NEW), Animator (simple), existing stats/health/status.

**Behavior:** Distance check loop. Too far → idle. In range → walk at player. Close → stop, attack on cooldown.

**Animator:** Idle ↔ Walk (Speed param), Idle → Attack (trigger, auto-return).

**Attack:** Trigger collider in front, fires once per Attack animation state, delivers damage via `DamageProfileSO` + `EnemyStats` bonuses.

## Files & Scope

**New files:** 2 components (EnemyAI.cs, EnemyMeleeHitbox.cs), 1 animator controller, documentation.

**Effort:** ~4–5 hours (scripts, animator wiring, prefab assembly, testing & tuning).

**Result:** One enemy prefab that can encounter the player, walk, attack, take damage, and die.

## Known Limitations (Deferred)

1. No pathfinding (straight-line walk, obstacles block)
2. No line of sight (just distance detection)
3. No death animation (disappears at 0 HP)
4. No loot on death
5. No group behavior or formations
6. No reaction to hits (no flinch or knockback effects)
7. Animation clips reused from pack (not custom enemy attacks)

## Roadmap After MVP

1. NavMesh pathfinding (avoid obstacles)
2. Vision + hearing (smarter detection)
3. Behavior variety (attack patterns, dodge, kite)
4. Enemy groups and formations
5. Boss enemies (multi-phase attacks)
6. Loot on death (items, currency, XP)
7. Save/load persistence (enemy state, respawns)

## Documentation

- **New file:** `Docs/Phase_9_Enemy_Plan.md` — Full design, implementation steps, success criteria.
- **Updated:** Architecture §3 ("Enemies & Actors" section status); this changelog.

**Status:** Plan complete, ready for implementation phase.
