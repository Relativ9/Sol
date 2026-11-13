# Animation Integration Notes - Melee Combat

## Overview
Melee attacks are played entirely from Blender-authored Rigify clips. The Animator controller decides every transition. C# only sets parameters and never forces a state or a speed.

- **Animator** plays the swings and chooses transitions.
- **`CombatStateController`** sets the `Attack` trigger and the `CanStringToCombo` bool, and reacts to animation events.
- **`CombatStateMachine`** holds the combo timing rule only. It has no Animator calls.

Procedural IK reach was removed. See `Problems and Pitfalls.md`, section 7.

## Code Responsibilities

### PlayerAnimationController / IAnimationController
- Parameter setters such as `SetTrigger` and `SetBool`.
- `PlayState` and `SetAnimatorSpeed` were removed. Nothing forces a state now.

### CombatStateController
- Idle press: sets the `Attack` trigger, if the cooldown has passed and the weapon is unsheathed.
- Press during a swing, or during the hold: sets `CanStringToCombo` true.
- Animation events drive the flags (see the events table below).
- Debug Time section: `_useDebugTimeScale` and `_debugTimeScale` slow `Time.timeScale`. The original value is restored when the toggle is turned off or the component is disabled.

### CombatStateMachine
- `StartNewCombo()`: clears the hold and pressed state.
- `RegisterPress()`: records a press. Returns true when the press lands in the press window.
- `BeginComboHold()`: opens the hold at the `AttackRecovery` event. Does nothing if a press already arrived during the swing.
- `EndSwing()`: clears the pressed flag at the end of a swing.
- `EndCombo()`: closes the hold.
- `UpdateComboHoldTimer()`: closes the hold when its time runs out, and raises `OnHoldExpired`.
- `IsPressWindowOpen`: true from the end of the start delay until the hold closes.

## Animation Events

Each attack clip needs these events:

| Event | Where in the clip | What it does |
|-------|-------------------|--------------|
| `OnAttackStart` | Start of swing | Clears `CanStringToCombo` |
| `AttackRecovery` | Blade returning (about half the clip in `1Handed_1stAttack`) | Opens the hold, if `_comboHoldEnabled` is true |
| `OnAttackEnd` | End of clip | Ends the swing. Does not clear the gate |

Check the clip name and event timing in Unity. The copy of `1Handed_1stAttack.fbx.meta` at `Assets/_Project/Art/Player/Animations/Old/1Handed/` has `OnAttackStart` at 0.084 s, `AttackRecovery` at 0.503 s, and `OnAttackEnd` at 1.0 s. The Animator may use a different copy.

## Animator Setup (Player.controller)

- **Attack 1 exit:** `HasExitTime: 1`, `ExitTime: 1`. The clip plays to the end before the exit is checked. Do not change this back to 0.
- **Attack 2 and 3:** states exist, but no motion clips are assigned yet.
- **Parameters:** `Attack` (trigger) and `CanStringToCombo` (bool).

Animator edits require explicit approval first.

## Inspector Settings (CombatStateController)

| Field | Default | Purpose |
|-------|---------|---------|
| `_comboHoldEnabled` | false | Turns the post-swing hold on |
| `_comboHoldSeconds` | 1 | Hold length after the start delay |
| `_comboHoldStartDelay` | 0 | Dead time after recovery before presses count |
| `_useDebugTimeScale` | false | Slows game time for inspection |
| `_debugTimeScale` | 0.25 | Time scale while the debug slow-motion is on |

Hold and cooldown use game time, so a slowed time scale stretches them in real seconds.

## Testing Checklist

- [ ] Attack 1 plays its full clip.
- [ ] With the hold off, the swing exits to idle with no press lingering.
- [ ] Debug slow-motion slows the game and restores when toggled off, or on exit.
- [ ] With the hold on, a press in the window chains into Attack 2 once its clip exists.
- [ ] Animation events fire at the expected times, and the Console shows no "no function name" errors.

## Known Limitations

1. Hold timing is tuned by eye in Unity. The values in the table above are starting points.
2. Attack 2 and 3 do not play until their motion clips are assigned.
3. The `AttackRecovery` timing depends on each clip. Re-check it when a clip changes.

## Next Steps

1. Confirm the Attack 1 checklist items in Unity.
2. Assign the Attack 2 and 3 clips and add the same three events to each.
3. Tune `_comboHoldSeconds` and `_comboHoldStartDelay` against the clips.
4. Build the locomotion and idle animation set.

## Files

- `Assets/_Project/Scripts/Player/PlayerAnimationController.cs`
- `Assets/_Project/Scripts/Player/PlayerInterfaces/IAnimationController.cs`
- `Assets/_Project/Scripts/Player/AnimationEventDispatcher.cs`
- `Assets/_Project/Scripts/Player/Combat/CombatStateController.cs`
- `Assets/_Project/Scripts/Player/Combat/CombatStateMachine.cs`
- `Assets/_Project/Systems/Player.controller`
- `Assets/_Project/Art/Player/Animations/Old/1Handed/1Handed_Idle.fbx.meta`
