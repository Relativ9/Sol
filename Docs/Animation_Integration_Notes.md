# Animation Integration Notes - Melee Combat

## Overview
Melee attacks are played entirely from Blender-authored Rigify clips. The Animator controller decides every transition. C# only sets parameters and never forces a state or a speed.

- **Animator** plays the swings and owns every transition.
- **`CombatStateController`** sets parameters only (`Attack`, `ComboIndex`, `CanStringToCombo`, `ComboSpeed`). It reads the Combat layer's state every frame to know which swing is playing.
- Full combo rules: `# Sol RPG — Architecture.md` §5b. `CombatStateMachine` was removed.

Procedural IK reach was removed. See `Problems and Pitfalls.md`, section 7.

## Code Responsibilities

### PlayerAnimationController / IAnimationController
- Parameter setters: `SetTrigger`, `ResetTrigger`, `SetBool`, `SetInteger`, `SetFloat`, `SetLayerWeight`.
- `PlayState` and `SetAnimatorSpeed` were removed. Nothing forces a state or changes `Animator.speed`.

### CombatStateController
- Idle press: starts the combo and fires `Attack`. `ComboIndex` stays 0 until swing 1 is actually playing.
- `TrackComboFromAnimator` (every frame): a new `MeleeCombo` state is the next swing. Leaving the `MeleeCombo` states ends the combo and sets `ComboIndex` to 0.
- Press during a swing: queues the next swing (one per swing) and sets `ComboIndex` to its number.
- `AttackRecovery`: opens the hold and slows the rest of the swing through `ComboSpeed`.
- Debug Time section: `_useDebugTimeScale` and `_debugTimeScale` slow `Time.timeScale`, and restore it when the toggle is turned off or the component is disabled. With `_debugCombat` on, the combo state is shown on screen and `[Combo]` lines go to the Console.

## Animation Events

| Event | Where in the clip | Who uses it |
|-------|-------------------|-------------|
| `OnAttackStart` | Start of swing (time 0 on Attacks 2 and 3) | Trail, hitbox. Not the combo |
| `AttackRecovery` | Strike done, blade starting to return | **Combo: opens the hold.** Also ends the trail |
| `OnAttackEnd` | End of clip | Hitbox. Not the combo, because it can be cut or blended out |

Current clips: `1HandedAttack_1_r.fbx`, `1HandedAttack_2_r.fbx`, `1HandedAttack_3_r.fbx` in `Assets/_Project/Art/Player/Animations/`, each 1.96 s (frames 1 to 48).

## Animator Setup (Player.controller)

See the transition table in Architecture §5b. In summary:
- All three attack states: tag `MeleeCombo`, Speed Multiplier = `ComboSpeed`.
- Chains: `ComboIndex` = next swing, plus `CanStringToCombo` true. Exit time off, 0.1 blend, no transition offset.
- Idle exits: unchanged (`CanStringToCombo` false, exit time 0.8 / 0.8 / 0.4).

## Importing a new attack clip

1. Rig tab: Humanoid, copy the avatar from `Mannequin_wIdle`.
2. Animation tab: Root Transform Position (Y) baked, based on **Feet**. Root Transform Position (XZ) baked, based on **Center of Mass** (see pitfalls §13).
3. Add `OnAttackStart`, `AttackRecovery` and `OnAttackEnd`. Put `AttackRecovery` where the strike is done.
4. Make the clip's first frame close to the previous swing's pose at its `AttackRecovery`, or the 0.1 chain blend will pop.

## Inspector Settings (CombatStateController)

| Field | Default | Purpose |
|-------|---------|---------|
| `_comboHoldSeconds` | 0.5 | Real seconds the hold lasts. Longer = slower rest of swing |
| `_holdEndNormalizedTime` | 0.8 | Where the hold ends. Match the idle exit's exit time |
| `_comboSpeedParam` | `ComboSpeed` | Speed Multiplier parameter of the attack states |
| `_comboTag` | `MeleeCombo` | Tag on the attack states |
| `_useDebugTimeScale` | false | Slows game time for inspection |
| `_debugTimeScale` | 0.25 | Time scale while the debug slow-motion is on |

The hold uses game time, so a slowed time scale stretches it in real seconds.

## Testing Checklist

- [x] A single press plays Attack 1 and returns to idle. `ComboIndex` goes 0 → 1 → 0.
- [x] A press at any point in a swing chains into the next swing once `AttackRecovery` arrives.
- [x] Spamming attack does not cut a strike short and does not skip a swing.
- [x] A new combo can be started after the last one ends.
- [x] Attack 2's arc plays at the right height with the 0.1 chain blend.
- [ ] Hold slow-down visible with a hold longer than the rest of the swing (about 1.2 s for 0.5×).
- [ ] Cancel (stagger) and forced sheathe mid-combo return `ComboIndex` to 0.

## Known Limitations

1. Hold length and the slow-down are tuned by eye. The values above are starting points.
2. The `AttackRecovery` time differs per clip. Re-check it when a clip changes.
3. The chain blend looks right only while each clip starts near the previous swing's recovery pose.

## Next Steps

1. Tune `_comboHoldSeconds` for the slow-down feel.
2. Check the trail and hitbox timings on Attacks 2 and 3.
3. Build the locomotion and idle animation set.

## Files

- `Assets/_Project/Scripts/Player/PlayerAnimationController.cs`
- `Assets/_Project/Scripts/Player/PlayerInterfaces/IAnimationController.cs`
- `Assets/_Project/Scripts/Player/AnimationEventDispatcher.cs`
- `Assets/_Project/Scripts/Player/Combat/CombatStateController.cs`
- `Assets/_Project/Systems/Player.controller`
- `Assets/_Project/Art/Player/Animations/1HandedAttack_1_r.fbx`, `_2_r.fbx`, `_3_r.fbx`
