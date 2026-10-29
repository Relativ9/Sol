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
        [Stat(StatRole.Tuning)] RunMultiplier,
        [Stat(StatRole.Tuning)] Deceleration,
        [Stat(StatRole.Tuning)] CrouchSpeed,
        [Stat(StatRole.Tuning)] JumpForce,
        [Stat(StatRole.Tuning)] JumpCooldown,
        [Stat(StatRole.Tuning)] JumpDirectionBoost,
        [Stat(StatRole.Tuning)] MaxDoubleJump,
        [Stat(StatRole.Tuning)] FallDamageMultiplier,
        [Stat(StatRole.Tuning)] GravityMultiplier,
        [Stat(StatRole.Tuning)] TerminalVelocity,

        // Resources.
        [Stat(StatRole.Derived)] MaxHealth,
        [Stat(StatRole.Derived)] MaxEnergy,
        [Stat(StatRole.Derived)] HealthRegen,
        [Stat(StatRole.Derived)] EnergyRegen,
        [Stat(StatRole.Derived)] CarryCapacity,
        [Stat(StatRole.Derived)] InventorySpace,
        [Stat(StatRole.Derived)] LootMultiplier,
        [Stat(StatRole.Tuning)] Hunger,
        [Stat(StatRole.Tuning)] Thirst,

        // Combat. The derived values come from attributes and equipment.
        [Stat(StatRole.Derived)] MeleeDamage,
        [Stat(StatRole.Derived)] ProjectileDamage,
        [Stat(StatRole.Derived)] ProjectileRange,
        [Stat(StatRole.Derived)] SpellPower,
        [Stat(StatRole.Derived)] ArmorValue,
        [Stat(StatRole.Derived)] CritChance,
        [Stat(StatRole.Derived)] CritMultiplier,
        [Stat(StatRole.Derived)] DeflectionChance,
        [Stat(StatRole.Derived)] MagicResistance,
        [Stat(StatRole.Derived)] FireResistance,
        [Stat(StatRole.Derived)] IceResistance,
        [Stat(StatRole.Derived)] VoidResistance,
        [Stat(StatRole.Derived)] SonicResistance,
        [Stat(StatRole.Derived)] SoulResistance,
        [Stat(StatRole.Derived)] ReflectChance,
        [Stat(StatRole.Derived)] EnvironmentalResistance,
        [Stat(StatRole.Tuning)] ParryAndBlockTime,
        [Stat(StatRole.Tuning)] ClueRange,
        [Stat(StatRole.Tuning)] CharmChance,
        [Stat(StatRole.Tuning)] KnowledgeChance,
        [Stat(StatRole.Tuning)] DeceptionChance,
        [Stat(StatRole.Tuning)] IntimidationChance,

        // Appended after the original members (see the knowledgebase on saved enum integers).
        [Stat(StatRole.Tuning)] CovertMovement,
        [Stat(StatRole.Tuning)] Tinker,
        [Stat(StatRole.Derived)] StaggerResistance,
        [Stat(StatRole.Derived)] BleedResistance,
        [Stat(StatRole.Derived)] RigidWeight,
        [Stat(StatRole.Derived)] FlexWeight,
        [Stat(StatRole.Tuning)] StaggerChance,
        [Stat(StatRole.Tuning)] BleedChance,
        [Stat(StatRole.Derived)] Penetration,
        [Stat(StatRole.Derived)] PlateCount,
        [Stat(StatRole.Derived)] FlexCount,
        [Stat(StatRole.Derived)] ClothCount,

        // Attunement: boosts spells and effects of one school. Added to the spell power a spell of that school uses.
        // Granted by weapons and armour through their modifier lists, so attunement is equipment-driven and comparable.
        [Stat(StatRole.Derived)] FireAttunement,
        [Stat(StatRole.Derived)] IceAttunement,
        [Stat(StatRole.Derived)] VoidAttunement,
        [Stat(StatRole.Derived)] SonicAttunement,
        [Stat(StatRole.Derived)] SoulAttunement,
        [Stat(StatRole.Derived)] MagicAttunement,
    }
}
