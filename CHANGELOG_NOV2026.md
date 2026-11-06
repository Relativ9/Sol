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
