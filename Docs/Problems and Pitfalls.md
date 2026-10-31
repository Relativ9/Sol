# Sol RPG: Problems and Pitfalls

A log of problems that cost real debugging time, with the cause and the fix. Add an entry whenever a problem takes more than one attempt to solve. Each entry gives the symptom, the cause, the fix, and how to recognise it next time.

---

## 1. Player ungrounded on platforms, not terrain

### Symptom
- Standing on a platform (a box collider), the grounded gizmo is red and movement is disabled.
- On terrain it works. Sometimes the gizmo flips to green when the view changes, which made it look view-dependent.
- After a quicksave made on a platform, the gizmo loads far below the player, and the player cannot move.

### Cause (three things together)

**a. `SphereCast` ignores colliders it already overlaps at its start.**
The player's feet sit a few millimetres inside every surface because of contact penetration. Measured at the failure: platform top at `y = 3.130`, feet at `y = 3.125`. If the probe origin is at the feet, the sphere's lowest point is inside the platform, so the cast does not report it. Terrain sits far below the feet, so it never hit this. The penetration changes from frame to frame, which gave the flicker.

**b. The probe origin was baked once, at spawn.**
`GroundChecker.Initialize` measured the collider's bottom and stored an offset in `_checkPoint`. `GameInitializer.RestorePlayerPose` moves the player to the saved position after `Initialize` has run. So after a load onto a platform, the probe kept the spawn-height offset and sat far from the feet.

**c. The scene values differ from the code defaults.**
The scene serialises `_checkRadius = 0.3` and `_checkDistance = 0.4`. The code defaults are `0.2` and `0.3`. Any reasoning from the code defaults was wrong for this scene. The original `bottom + 0.3` offset put the sphere's lowest point exactly on the feet, so penetration always placed the platform inside it.

### Fix
`GroundChecker.ProbeOrigin()` reads the collider every frame:

```
origin = (collider.bounds.center.x, collider.bounds.min.y + ProbeSkin, collider.bounds.center.z)
ProbeSkin = 0.4
```

- With a sphere radius of 0.3, the sphere's lowest point is 0.1 m above the feet, clear of any penetration.
- The sphere cast, the look-ahead probe and the gizmo all use this one origin, so the gizmo shows the probe that actually runs.
- `_checkPoint` is only used as a fallback when the object has no collider.

### Recognise it again
- The gizmo is at the feet but goes red on one kind of surface only. Check `overlapAtOrigin` (see the diagnosis method below).
- After a load the gizmo is far from the feet. Something has baked a position at spawn. Check for any `Initialize` code that measures bounds or positions.
- A slope or movement gate looks wrong. Check the ground state first. The slope gate was not the cause here: the logs showed the slope under the limit (33 degrees against a 45 degree limit) while the ground state was already false.

### Uncommitted changes that made it worse
- A probe origin lifted 0.3 m above the feet, which moved the origin away from the surface.
- A probe origin set to the feet, which put the sphere inside the surface.
- Both were removed by restoring `GroundChecker.cs` with `git show HEAD:...`.

---

## Diagnosis method that worked

1. **Log state changes, not every frame.** A per-frame log buries the one moment that matters. Log only when the value flips.
2. **Measure the ground with a ray masked to the ground layer.** An unmasked ray started inside the player's own capsule and reported the player as the surface.
3. **Run an overlap query at the probe origin.** It lists colliders the sphere already starts inside, which `SphereCast` will ignore.
4. **Log the full hierarchy path and the instance ID.** Object names repeat in the scene (several objects are called `Cube (3)`), so a name alone is not an identity.
5. **Read the Editor log for compile errors.** The console can show stale errors. Use `~/.config/unity3d/Editor.log` and `grep 'error CS'`, and check that the log is from the latest compile.
6. **Remove every diagnostic when done.** Restore committed files with `git show HEAD:<path>` so none are left behind.

---
## 2. Compile pitfalls

- **Backslash inside an interpolation hole** causes `CS1056`. `$"... {x ? "a" : "\"b\""} ..."` is not allowed. One such line broke parsing for the whole file, and the cascade showed as dozens of `CS0106 'modifier not valid'` errors. The first error is the real one. Build such strings with `+` instead.
- **Brace and paren counts** are a quick sanity check. They do not catch a parse error like the one above, so the compile log is the real check.

---

## 3. Editing pitfalls

- **The file editor refused a match** whose text included a blank line with trailing whitespace. Replace shorter single-line spans instead.
- **A partly applied edit** can leave some references converted and others not. After any rename or refactor, grep for the old name before reporting it done.
- **"Reverted the code" did not revert the scene.** Scene and prefab files carry their own serialised values, so `git status` should be checked for `.unity` and `.prefab` changes as well as `.cs`.
- **Keep single edits under about 6000 characters.** A larger write is rejected. Split long documents into parts.

---

## 4. Save system pitfalls

- **Deleting a slot did not stop it coming back.** `SaveService.Delete` removed only the live file. Its `.bak` survived, so `SaveExists` and `Load` restored the deleted save. *Fixed:* Delete removes the live file, the `.bak` and the `.tmp`. *Still open:* the slot's cell folder is not removed, so old world state can load into a new game.
- **A corrupt player file loaded as an empty save**, and the next autosave overwrote the damaged file. *Fixed:* the player file is written through `AtomicFile`, and `Load` falls back to the `.bak`.
- **A failed write was reported as a success.** `SaveService.Save` returned `void`. *Fixed:* it returns `bool`, and `SaveManager` stops before writing any cell if the player file fails.
- **`SaveLocation.Directory` is a hard-coded personal path.** It does not follow the design doc (`persistentDataPath`) and will fail on other machines. *Open:* needs a decision on migrating existing saves.

---

## 5. Scene and prefab identity pitfalls

- **Duplicate placed-item identity.** Prefab instances without an `_itemInstanceId` override inherit the prefab's default ID. Two copies then share one identity, so picking up one can remove both on load. *Status:* found in the scene data; the fix (regenerate the IDs in the editor) has not been confirmed. If duplicates reappear, check the `_itemInstanceId` overrides on every placed instance.

---

## 6. Knocked placed item reloads at its old position

### Symptom
- A placed item (the ShinyDagger on a ledge) is knocked onto the ground, settles, and is quicksaved. On reload it is back on the ledge.
- The first knock could be saved, but a second knock from the new spot was not. Reloading showed the ledge pose again.
- In-session logs showed the restore and the saved record were both correct.

### Cause
`WorldMotionTracker` only tracks a body that was registered with `Track`. `Track` was called only from `WorldItem.Start`. A body is removed from tracking once it has been still for about 0.5 s (it "settles"). After that nothing registered it again, so a later knock was never sampled, never written to a cell, and never saved. The first knock worked only because it happened before the body settled.

The in-session logs could not show this. They checked the restore of a record that existed. The bug was a write that never happened, so a passing session has nothing to see.

### Fix
`WorldItem.OnCollisionEnter` calls `IWorldMotionTracker.Track(this)`. A collision wakes a resting body, so it is registered again. `Track` ignores an item that is already tracked, so repeated hits are cheap.

### Related changes made in the same investigation
- Placed items that have moved are restored through their `Rigidbody` (position, rotation, velocity, then `WakeUp`), not the transform. This stops a body resuming in the air or inside a surface.
- Moving records store linear and angular velocity, with the `Moving` and `HasVelocity` flags. Resting records store none.
- The cell file stores yaw only, so a placed item that falls over is restored upright. Position is preserved. Pitch and roll are a known limit (see the design doc, section 6.1).

### Caveat
The collision hook covers bodies that are hit. A body that starts moving without a collision (a push from a non-collision force, an explosion on a trigger) needs the same `Track` call at the point it wakes.

### Recognise it again
- Compare three poses: before the action, at the save, and after the load. The save file is the deciding evidence.
- If the file has a record at the post-action pose, the save is right and the fault is on load. If the file holds only the old pose, the write never happened. Check whether the item is still tracked when the action occurs.
- A log from a session where the bug does not show proves nothing about the failing path. Check what is written, not just what is restored.

---


## Checklist for the next problem

- [ ] Check `git status` for changes in `.cs`, `.unity` and `.prefab` files.
- [ ] Check the scene's serialised values, not the script defaults.
- [ ] Check whether any value is measured or stored once, at startup, and then used after something moves the object.
- [ ] Check the Editor log for compile errors before trusting a diagnostic.
- [ ] Remove diagnostics, and record the cause here.

