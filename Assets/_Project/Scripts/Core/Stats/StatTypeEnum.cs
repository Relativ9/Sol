using UnityEngine;

namespace Sol
{
    // Every stat, with its role. Saved enums are stored as integers, so members must never be reordered. New members go
    // at the end of their group, or at the end of the enum. The role attribute is what the designer and the code read.
    public enum StatTypeEnum
    {
        // Core attributes: player-spent, shown on the stats screen.
        [Stat(StatRole.Attribute)] Brawn,
        [Stat(StatRole.Attribute)] Finesse,
        [Stat(StatRole.Attribute)] Insight,
        [Stat(StatRole.Attribute)] Focus,
        [Stat(StatRole.Attribute)] Vigor,
        [Stat(StatRole.Attribute)] Willpower,

        // Movement: game-tuned. Effects, surfaces and gravity zones change these through modifiers, never as base values.
        [Stat(StatRole.Tuning)] MoveSpeed,
        [Stat(StatRole.Derived)] RunMultiplier,
        [Stat(StatRole.Tuning)] Deceleration,
        [Stat(StatRole.Tuning)] CrouchSpeed,
        [Stat(StatRole.Derived)] JumpForce,
        [Stat(StatRole.Tuning)] JumpCooldown,
        [Stat(StatRole.Tuning)] JumpDirectionBoost,
        [Stat(StatRole.Tuning)] MaxDoubleJump,
        [Stat(StatRole.Derived)] FallDamageMultiplier,
        [Stat(StatRole.Tuning)] GravityMultiplier,
        [Stat(StatRole.Tuning)] TerminalVelocity,

        // Resources.
        [Stat(StatRole.Derived)] MaxHealth,
        [Stat(StatRole.Derived)] MaxEnergy,
        [Stat(StatRole.Derived)] HealthRegen,
        [Stat(StatRole.Derived)] EnergyRegen,
        [Stat(StatRole.Derived)] CarryCapacity,
        [Stat(StatRole.Tuning)] InventorySpace,
        [Stat(StatRole.Tuning)] LootMultiplier,
        [Stat(StatRole.Derived)] Hunger,
        [Stat(StatRole.Derived)] Thirst,

        // Combat. The derived values come from attributes and equipment.
        [Stat(StatRole.Derived)] MeleeDamage,
        [Stat(StatRole.Derived)] ProjectileDamage,
        [Stat(StatRole.Derived)] ProjectileRange,
        [Stat(StatRole.Derived)] SpellPower,
        [Stat(StatRole.Tuning)] ArmorValue,
        [Stat(StatRole.Derived)] CritChance,
        [Stat(StatRole.Derived)] CritMultiplier,
        [Stat(StatRole.Tuning)] DeflectionChance,
        [Stat(StatRole.Derived)] MagicResistance,
        [Stat(StatRole.Tuning)] FireResistance,
        [Stat(StatRole.Tuning)] IceResistance,
        [Stat(StatRole.Tuning)] VoidResistance,
        [Stat(StatRole.Tuning)] SonicResistance,
        [Stat(StatRole.Tuning)] SoulResistance,
        [Stat(StatRole.Tuning)] ReflectChance,
        [Stat(StatRole.Derived)] EnvironmentalResistance,
        [Stat(StatRole.Derived)] ParryAndBlockTime,
        [Stat(StatRole.Derived)] ClueRange,
        [Stat(StatRole.Derived)] CharmChance,
        [Stat(StatRole.Derived)] KnowledgeChance,
        [Stat(StatRole.Derived)] DeceptionChance,
        [Stat(StatRole.Derived)] IntimidationChance,
        [Stat(StatRole.Tuning)] FireAttunement,
        [Stat(StatRole.Tuning)] IceAttunement,
        [Stat(StatRole.Tuning)] VoidAttunement,
        [Stat(StatRole.Tuning)] SonicAttunement,
        [Stat(StatRole.Tuning)] SoulAttunement,
        [Stat(StatRole.Tuning)] MagicAttunement,
        [Stat(StatRole.Derived)] CovertMovement,
        [Stat(StatRole.Derived)] Tinker,
        [Stat(StatRole.Tuning)] StaggerResistance,
        [Stat(StatRole.Tuning)] BleedResistance,
        [Stat(StatRole.Tuning)] RigidWeight,
        [Stat(StatRole.Tuning)] FlexWeight,
        [Stat(StatRole.Tuning)] StaggerChance,
        [Stat(StatRole.Tuning)] BleedChance,
        [Stat(StatRole.Derived)] Penetration,
        [Stat(StatRole.Tuning)] PlateCount,
        [Stat(StatRole.Tuning)] FlexCount,
        [Stat(StatRole.Tuning)] ClothCount,
        [Stat(StatRole.Derived)] LiftStrength,
    }
}
