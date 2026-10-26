using UnityEngine;

namespace Sol
{
    public enum StatTypeEnum
    {
        // Core Attributes
        Brawn,
        Finesse,
        Insight,
        Focus,
        Vigor,
        Willpower,
        
        // Movement
        MoveSpeed,
        RunMultiplier,
        Deceleration,
        CrouchSpeed,
        JumpForce,
        JumpCooldown,
        JumpDirectionBoost,
        MaxDoubleJump,
        FallDamageMultiplier,
        GravityMultiplier,
        TerminalVelocity,
        
        // Resources
        MaxHealth,
        MaxEnergy,
        HealthRegen,
        EnergyRegen,
        CarryCapacity,
        InventorySpace,
        LootMultiplier,
        Hunger,
        Thirst,
        
        //Combat
        MeleeDamage,
        ProjectileDamage,
        ProjectileRange,
        SpellPower,
        ArmorValue,
        CritChance,
        CritMultiplier,
        DeflectionChance,
        MagicResistance,
        FireResistance,
        IceResistance,
        VoidResistance,
        SonicResistance,
        SoulResistance,
        ReflectChance,
        EnvironmentalResistance,
        ParryAndBlockTime,
        ClueRange,
        CharmChance,
        KnowledgeChance,
        DeceptionChance,
        IntimidationChance,

        // Appended after the original members. Saved enums are stored as integers (see knowledgebase),
        // so new members must only ever go at the end.
        CovertMovement,
        Tinker,
        StaggerResistance,
        BleedResistance,
        RigidWeight,  // kg of plate equipped. Drives stagger chance.
        FlexWeight,   // kg of flexible armour equipped. Drives bleed chance.
    }
}
