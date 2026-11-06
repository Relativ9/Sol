Open items, carried into Phase 8:
1. Set bonuses for full plate, flex and cloth (see the armour doc).
2. Player weapon swings use custom Blender animations. Animation Rigging procedural reach and `SwingReach` were removed Nov 2026. Attacks are now 100% driven by animation clips, with no IK adjustments.
3. Second and third swings are chained via `ComboSystem` (in place). All three attack clips play back-to-back if inputs chain within the hold window.