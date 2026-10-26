# Sol: Decision Trees

Each tree shows the checks in the order the code makes them, and what happens on each branch. Diamonds are checks. Rectangles are results. Companion to `FLOWS.md` (sequence diagrams) and `diagrams/mindmap.md` (overview).

Each tree names the method and state flag it uses. Scene-wired links are marked `[scene-wired]`.

## 1. Jump (Space)

Source: `PlayerController.OnJump` → `Jumping.HandleJumpInput`.

```mermaid
flowchart TD
  A["Space pressed (started)"] --> B{"Jumping active and\ncooldown 0.1 s passed?"}
  B -- no --> X1["Ignored"]
  B -- yes --> C["Set JumpPriority true\n(cleared after 0.15 s)"]
  C --> D{"GetIsGrounded()\nGroundChecker.IsGrounded"}
  D -- yes --> E["Ground jump:\nAddForce up x force, Impulse\nJumpTriggered true"]
  D -- no --> F{"Double jumps used <\nmax (1)?"}
  F -- yes --> G["Zero negative y velocity\nAddForce up x force, Impulse\nDoubleJumpTriggered true"]
  F -- no --> H["EndJumpPriority\nNo jump"]
  E --> R["Record jump time"]
  G --> R
```

Note: `Jumping.OnCollisionEnter` resets the double-jump count when a contact normal has `y > 0.7`.

## 2. Move and run (WASD, Shift)

Source: `PlayerController.OnMove` and `OnRun`, then `Movement.Update` and `Movement.FixedUpdate`.

```mermaid
flowchart TD
  A["WASD input read in Update"] --> B{"Movement is the active state?\n(CanBeActivated)"}
  B -- no --> X1["Movement idle"]
  B -- yes --> C{"IsGrounded and\nnot IsStunned and\nnot IsInWater?"}
  C -- no --> X2["Air or blocked:\nProcessAirMovement"]
  C -- yes --> D{"JumpPriority set?"}
  D -- yes --> X3["Skip ProcessMovement this step"]
  D -- no --> E{"Run held and moving?"}
  E -- yes --> F["UpdateRunningState:\nApply MoveSpeed modifier\n(source PlayerRunning, value RunMultiplier)"]
  E -- no --> G["RemoveModifiersFromSource\nPlayerRunning"]
  F --> H{"Input backward?"}
  G --> H
  H -- yes --> I["Speed x backward multiplier 0.5"]
  H -- no --> J["Full speed"]
  I --> K["linearVelocity = direction x speed\nGroundSticky force down"]
  J --> K
```

Notes:

- The run modifier is a stat change, not only a flag. Releasing run removes it.
- `Movement.FixedUpdate` and `PlayerController.FixedUpdate` both call `ProcessMovement` (finding F1).
- `IsStunned` and `IsInWater` have no writers in the code traced, so those branches never fire (finding F10).

## 3. Look (mouse)

Source: `PlayerController.OnLook` → `CameraController.Update`.

```mermaid
flowchart TD
  A["Mouse moved (OnLook)"] --> B{"Time.timeScale == 0?"}
  B -- yes --> X1["Return, no look while paused"]
  B -- no --> C{"Camera active?\n(_isActive)"}
  C -- no --> X2["Return"]
  C -- yes --> D["Cursor locked again if needed"]
  D --> E["lookDelta = input x sensitivity"]
  E --> F{"_invertY?"}
  F -- yes --> G["Invert y"]
  F -- no --> H["Keep y"]
  G --> I["Pitch clamp -80 to 80"]
  H --> I
  I --> J["CameraTarget.rotation = Euler(pitch, yaw, 0)"]
```

## 4. Attack (MainHand)

Source: `PlayerController.OnAttack` → `CombatStateController.HandleAttackInput`.

```mermaid
flowchart TD
  A["Attack pressed (started)"] --> B{"Attack, sheathe, or\ndraw animation playing?"}
  B -- yes --> X1["Ignored: cannot attack during animation"]
  B -- no --> C{"State is Unsheathed?"}
  C -- no --> X2["Ignored: wrong combat state"]
  C -- yes --> D{"Cooldown 0.8 s passed?"}
  D -- no --> X3["Ignored: on cooldown"]
  D -- yes --> E["StartAttack:\nIsAttacking true\nattack trigger via animation controller"]
  E --> F["Animator fires OnAttackStart"]
  F --> G["OnAttackImpact (AttackEventHandler) [scene-wired]"]
  G --> H{"Overlap sphere hit\nwith IDamageable?"}
  H -- yes --> I["TakeDamage 25"]
  H -- no --> J["No hit"]
  I --> K["Animator fires OnAttackEnd"]
  J --> K
  K --> L["IsAttacking false"]
```

Notes:

- `IDamageable` has no implementers in the code traced, so the hit branch cannot damage anything yet (finding F9).

## 5. Toggle weapon (ToggleWeapon)

Source: `PlayerController.OnToggleWeapon` → `CombatStateController.ToggleWeaponState`.

```mermaid
flowchart TD
  A["ToggleWeapon pressed"] --> B{"Attack, sheathe, or\ndraw animation playing?"}
  B -- yes --> X1["Ignored: blocked during animation"]
  B -- no --> C{"Current combat state"}
  C -- Sheathed --> D["UnsheatheWeapon\nSetTrigger Draw animation"]
  C -- Unsheathed --> E["SheatheWeapon\nSetTrigger Sheathe animation"]
  C -- other --> X2["Ignored: state is transitioning"]
  D --> F["IsWeaponDrawn and CombatState state values update"]
  E --> F
```

Notes:

- When movement changes to another state, `CombatStateController.OnDeactivate` force-sheathes the weapon `[verified]`.

## 6. Talent allocate (left click on a node)

Source: `TalentTreeController.OnNodeClicked` (button 0) → `TryAllocate`.

```mermaid
flowchart TD
  A["Left click on talent node"] --> B{"Node exists in registry?"}
  B -- no --> X1["Ignored"]
  B -- yes --> C{"Current points < maxPoints?"}
  C -- no --> X2["Ignored: node full"]
  C -- yes --> D{"PrerequisitesMet?\n(any prerequisite allocated,\nor no prerequisites)"}
  D -- no --> X3["Ignored: prerequisites not met"]
  D -- yes --> E{"ProgressionService.SpendTalentPoint\n(pool has points?)"}
  E -- no --> X4["Ignored: no talent points"]
  E -- yes --> F{"TalentStateService.TryAllocatePoint"}
  F -- no --> G["RefundTalentPoint\nPool restored"]
  F -- yes --> H["Points incremented\nTalent GameEvent Raise"]
  H --> I["Scene listener: RequestLazySave [scene-wired]"]
  H --> J["Refresh node visual, dependents, points label"]
```

## 7. Talent remove (right click on a node)

Source: `TalentTreeController.OnNodeClicked` (button 1) → `TryRemove`.

```mermaid
flowchart TD
  A["Right click on talent node"] --> B{"Node has allocated points?"}
  B -- no --> X1["Ignored"]
  B -- yes --> C{"HasAllocatedDependent?\n(another allocated node depends on it)"}
  C -- yes --> X2["Refused: remove dependents first"]
  C -- no --> D{"TalentStateService.TryRemovePoint"}
  D -- no --> X3["Ignored"]
  D -- yes --> E["RefundTalentPoint\nPool +1"]
  E --> F["Talent GameEvent Raise\nRefresh visuals"]
```

## 8. Talent panel (K)

Source: `UIInputController.OnOpenTalentWheel` → `UIManager.TogglePanel("talent-wheel-root")`.

```mermaid
flowchart TD
  A["K pressed (started)"] --> B["UIManager.TogglePanel"]
  B --> C{"Panel currently open?\n(display Flex)"}
  C -- yes --> D["Close:\nUnregisterDocument from VirtualCursor\nCamera OnActivate (lock cursor)\nDisplay None"]
  D --> E["Talent panel only: ClearUI, destroy TalentTreeController"]
  E --> F["RequestImmediateSave via _onImmediateSaveRequest"]
  C -- no --> G["Open:\nSetActiveDocument on VirtualCursor\nCamera OnDeactivate (unlock cursor)\nDisplay Flex"]
  G --> H{"First open of talent panel?"}
  H -- yes --> I["Generate nodes after GeometryChanged\nAdd TalentTreeController, Initialize"]
  H -- no --> J["Reuse existing UI"]
  I --> K["RequestImmediateSave"]
  J --> K
  F --> L["TogglePause on TimeManager (both branches)"]
  K --> L
```


## 9. UI cursor and clicks

Source: `VirtualCursorController.Update` → `VirtualCursor.Update` → `UpdatePosition` and `HandleClicks`.

```mermaid
flowchart TD
  A["Each frame, unscaled delta"] --> B{"Mouse moved beyond 0.01?"}
  B -- yes --> C["Cursor delta = raw mouse delta"]
  B -- no --> D["Cursor delta = Navigate input"]
  C --> E["Move cursor, clamp to document"]
  D --> E
  E --> F{"Left or right button pressed\nthis frame?"}
  F -- yes --> G["SynthesizeClick into UI Toolkit tree"]
  G --> H["Talent nodes receive MouseDownEvent (button 0 or 1)"]
  F -- no --> I["Hover: pointer enter, move, leave events"]
```

## 10. Gravity trigger

Source: `Modifier_GravityMultiplierTrigger.OnTriggerEnter` and `OnTriggerExit`.

```mermaid
flowchart TD
  A["Collider enters trigger"] --> B{"Collider tag is Player?"}
  B -- no --> X1["Ignored"]
  B -- yes --> C{"Collider has IPlayerContext\n(GetComponent on same object)?"}
  C -- no --> X2["Silently skipped (finding F3)"]
  C -- yes --> D["ApplyOrReplaceModifier\nGravityMultiplier, PercentAdditive\nvalue = multiplier - 1"]
  D --> E{"Persist while in trigger?"}
  E -- yes --> F["Duration -1 until exit"]
  E -- no --> G["Duration = inspector value"]
  F --> H["Effects, 0.5 s cooldown"]
  G --> H
  I["Collider exits trigger"] --> J["RemoveModifiersFromSource sourceId"]
```

## 11. Gravity zone

Source: `Modifier_GravityDirectionZone.OnTriggerEnter` and `OnTriggerExit`. The trigger methods and the custom-direction setter (`GravityController.SetGravityScale`) are verified. The zone's configured values were not traced, so its exact effect is `[incomplete]`.

```mermaid
flowchart TD
  A["Collider enters zone"] --> B{"Collider tag is Player?\n(checked in trigger)"}
  B -- no --> X1["Ignored"]
  B -- yes --> C["Set custom gravity direction\n_useCustomGravityDirection true"]
  C --> D["GravityController applies direction x external scale"]
  E["Collider exits zone"] --> F["Restore default gravity direction\n[incomplete]"]
```

## 12. Speed boost

Source: `Modifier_MovementSpeedBoost.OnTriggerEnter`.

```mermaid
flowchart TD
  A["Collider enters trigger"] --> B{"Tag is Player?"}
  B -- no --> X1["Ignored"]
  B -- yes --> C{"Collider has IPlayerContext?"}
  C -- no --> X2["Return, no boost"]
  C -- yes --> D["ApplySpeedBoost:\nMoveSpeed, PercentAdditive\nvalue = RunMultiplier"]
  D --> E["ApplyOrReplaceModifier, timed by boost duration"]
  E --> F{"Effect cooldown passed?"}
  F -- yes --> G["PlayBoostEffects"]
  F -- no --> H["No effect this time"]
```


## 13. Attribute spend and refund

Source: `ProgressionService.TrySpendAttributePoint` and `TryRefundAttributePoint` (`Assets/_Project/Scripts/ProgressionService.cs`).

```mermaid
flowchart TD
  A["Attribute spend requested"] --> B{"Stat is a core attribute?"}
  B -- no --> X1["Error logged, false"]
  B -- yes --> C{"Spent attribute points <\ntotal attribute points?"}
  C -- no --> X2["No points, false"]
  C -- yes --> D["Increment allocation\nRebuildBaseStat: SetBaseStat on StatsService"]
  E["Refund requested"] --> F["Decrement allocation\nRebuildBaseStat"]
```

Status: no caller of either method exists in the code traced. The UI for this choice is not built yet, so this tree is unreachable from code today (finding F7).

## 14. Talent reset

Source: `TalentTreeController` reset method. No caller in code, so it is reachable only from a scene button or Inspector action `[scene-wired]`.

```mermaid
flowchart TD
  A["Reset requested [scene-wired]"] --> B{"Controller initialized?"}
  B -- no --> X1["Warning, return"]
  B -- yes --> C["Count all allocated points"]
  C --> D["TalentStateService.ResetAll:\nRemoveModifiersFromSource per node\nClear allocations\nRaise talent GameEvent"]
  D --> E["Refund the same number of points to ProgressionService"]
  E --> F["Refresh visuals and points label"]
  F --> G["No RequestImmediateSave: reset reaches disk on next save"]
```

## 15. Save triggers

Source: `SaveManager.RequestLazySave`, `RequestImmediateSave`, `PerformSave`.

```mermaid
flowchart TD
  A["Save request"] --> B{"Which trigger?"}
  B -- "Talent GameEvent [scene-wired]" --> C["RequestLazySave"]
  B -- "Panel open or close [scene-wired]" --> D["RequestImmediateSave"]
  C --> E{"Save already queued?"}
  E -- yes --> X1["Return, one save is enough"]
  E -- no --> F{"Time since last save < 2 s?"}
  F -- yes --> G["Start SaveAfterDelay for the remainder"]
  F -- no --> D
  G --> H["Timer ends: PerformSave"]
  D --> I["Cancel queued save, PerformSave"]
  H --> J["BuildSaveData:\nlevel, total talent points,\ntalent allocations"]
  I --> J
  J --> K["SaveService.Save(data, default)\nJsonUtility to persistentDataPath"]
  K --> L{"Write fails?"}
  L -- yes --> M["Log error, player not told (finding F13)"]
  L -- no --> N["Saved"]
```

## 16. Game start

Source: `GameInitializer.Start`.

```mermaid
flowchart TD
  A["Scene starts"] --> B{"_forceNewGame or no save file?"}
  B -- yes --> C{"_forceNewGame and save exists?"}
  C -- yes --> D["Delete default save"]
  D --> E["CreateNewGame"]
  C -- no --> E
  B -- no --> F["SaveService.Load(default)"]
  F --> G{"JSON parses?"}
  G -- no --> H["Empty PlayerSaveData, silently (finding F13 related)"]
  G -- yes --> I["Loaded PlayerSaveData"]
  E --> J["Initialize stats, progression, talents"]
  H --> J
  I --> J
  J --> K["ReloadAllBaseStats, LoadData,\nTalentState.Initialize, SetSpentTalentPoints"]
```

- `AttackEventHandler` is reachable only if a scene wires it up `[scene-wired]`.
