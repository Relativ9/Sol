using UnityEngine;
using UnityEngine.Serialization;

namespace Sol
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Player/PlayerStats")]
    public class PlayerStatSO : ScriptableObject
    {
        [Header("Core Atrributes")]
        public int brawn = 10; //increases melee damage, reduces movement speed penalty (from wearing armor), increases carry weight, increases intimidation.
        public int finesse = 10; //increases archery damage, sprint speed, reduces fall damage, improves stealth, improves lock picking, increases deception.
        public int insight = 10; //increases spell power, critical stike multiplier, magic resistence, increases crafting efficiency/potency, inreases knowledge.
        public int focus = 10; //increases crit chance, increases projectile range, increasing parry, block and spellward timing, highlights hidden loot etc.
        public int vigor = 10; //increases maximum health, increases energy regeneration, reduces enviromental and trap damage.  
        public int willpower = 10; //increases maximum energy, reduces global cooldown, reduces energy cost of blocking holding your breath, drawing an arrow and channeling spells, reduces spell cost energy from armor, increases charm.

        [Header("Movement")]
        public float baseMoveSpeed = 3f;
        public float baseDeceleration = 8f;
        public float baseCrouchSpeed = 3f;
        public float baseJumpDirectionBoost = 1.0f;
        public int baseMaxDoubleJump = 0;
        public float baseJumpCooldown = 0.1f;
        public float baseGravityMultiplier = 2.5f;
        public float baseTerminalVelocity = -20f;
        
        [Header("Resources")]
        public int baseInventorySpace = 10;
        public float baseLootMultiplier = 1f;
        
        [Header("Attribute Scaling")]
        [Tooltip("Brawn → JumpForce. Bonus = attribute value × this rate.")]
        public float brawnToJumpForce = 0.5f;
        [Tooltip("Finesse → FallDamageMultiplier reduction, per point of Finesse. FallDamageMultiplier starts at 1 and goes down: 0.02 means 10 points take it from 1.0 to 0.8. Reduction is clamped at 0.")]
        public float finesseToFallDamageReduction = 0.02f;
        [Tooltip("Brawn → MeleeDamage. Bonus = attribute value × this rate.")]
        public float brawnToMeleeDamage = 0.5f;
        [Tooltip("Brawn → CarryCapacity (kg). Bonus = attribute value × this rate.")]
        public float brawnToCarryCapacity = 2f;
        [Tooltip("Brawn → LiftStrength, Bonus = attribute value × this rate. 1 means lift strength levels one-for-one with Brawn.")]
        public float brawnToLiftStrength = 1f;
        [Tooltip("Finesse → ProjectileDamage (archery). Bonus = attribute value × this rate.")]
        public float finesseToProjectileDamage = 0.5f;
        [Tooltip("Insight → SpellPower. Bonus = attribute value × this rate.")]
        public float insightToSpellPower = 1f;
        [Tooltip("Insight → CritMultiplier. Bonus = attribute value × this rate. Minimum set by minCritMultiplier. Design cap is 3.")]
        public float insightToCritMultiplier = 0.02f;
        [Tooltip("Focus → CritChance. Bonus = attribute value × this rate (0.5 = +0.5%).")]
        public float focusToCritChance = 0.5f;
        [Tooltip("Finesse → Penetration. Bonus = attribute value × this rate, in points. Capped with resistance at 400 points.")]
        public float finesseToPenetration = 1.5f;
        [Tooltip("Vigor → MaxHealth. Bonus = attribute value × this rate.")]
        public float vigorToMaxHealth = 5f;
        [Tooltip("Willpower → MaxEnergy. Bonus = attribute value × this rate.")]
        public float willpowerToMaxEnergy = 5f;
        [Tooltip("Vigor → EnergyRegen. Bonus = attribute value × this rate.")]
        public float vigorToEnergyRegen = 1f;
        [Tooltip("Focus → HealthRegen. Bonus = attribute value × this rate.")]
        public float focusToHealthRegen = 0.1f;
        [Tooltip("Vigor → Hunger. Bonus = attribute value × this rate.")]
        public float vigorToHunger = 1f;
        [Tooltip("Vigor → Thirst. Bonus = attribute value × this rate.")]
        public float vigorToThirst = 1f;
        [Tooltip("Focus → ProjectileRange (metres). Bonus = attribute value × this rate.")]
        public float focusToProjectileRange = 1f;
        [Tooltip("Insight → MagicResistance. Bonus = attribute value × this rate.")]
        public float insightToMagicResistance = 1f;
        [Tooltip("Vigor → EnvironmentalResistance. Bonus = attribute value × this rate.")]
        public float vigorToEnvironmentalResistance = 1f;
        [Tooltip("Focus → ParryAndBlockTime (seconds). Bonus = attribute value × this rate.")]
        public float focusToParryAndBlockTime = 0.01f;
        [Tooltip("Focus → ClueRange (metres). Bonus = attribute value × this rate.")]
        public float focusToClueRange = 1f;
        [Tooltip("Willpower → CharmChance. Bonus = attribute value × this rate (0.5 = +0.5%).")]
        public float willpowerToCharmChance = 0.5f;
        [Tooltip("Insight → KnowledgeChance. Bonus = attribute value × this rate (0.5 = +0.5%).")]
        public float insightToKnowledgeChance = 0.5f;
        [Tooltip("Finesse → DeceptionChance. Bonus = attribute value × this rate (0.5 = +0.5%).")]
        public float finesseToDeceptionChance = 0.5f;
        [Tooltip("Brawn → IntimidationChance. Bonus = attribute value × this rate (0.5 = +0.5%).")]
        public float brawnToIntimidationChance = 0.5f;
        [Tooltip("Finesse → CovertMovement. Bonus = attribute value × this rate.")]
        public float finesseToCovertMovement = 1f;
        [Tooltip("Finesse → Tinker (lock-picking and repair). Bonus = attribute value × this rate.")]
        public float finesseToTinker = 1f;

        [Header("Derived Minimums")]
        [Tooltip("Lowest CritMultiplier a player can have, from any source. Totals above it are unchanged. Design cap is 3.")]
        public float minCritMultiplier = 1.2f;
        [Tooltip("Lowest RunMultiplier a player can have, from any source. 1 means running is never slower than walking.")]
        public float minRunMultiplier = 1f;

        [Header("Combat")]
        public float baseArmorValue = 0f; //Armor lowers all physical damage and with the right talents give extra bonus protection (deflect, reflect etc).
        public float baseDeflectionChance = 0f; //points (5 = 5%). Chance to deflect incoming physical attacks, including physical projectiles. Only plate armour increases it.
        public float baseFireResistance = 0f;
        public float baseIceResistance = 0f;
        public float baseVoidResistance = 0f;
        public float baseSonicResistance = 0f;
        public float baseSoulResistance = 0f;
        public float baseReflectChance = 0f; //chance of reflecting spells back at attacker
        public float baseStaggerResistance = 0f; //from heavy plate (rigid weight x 0.2%)
        public float baseBleedResistance = 0f; //from flexible armour (flex weight x 0.333%)
        public float baseRigidWeight = 0f; //kg of plate equipped. Set by equipment later.
        public float baseFlexWeight = 0f; //kg of flexible armour equipped. Set by equipment later.
        public float basePlateCount = 0f; //plate pieces equipped (0-5). Each adds 3 points of deflection. Set by equipment later.
        public float baseFlexCount = 0f; //flex pieces equipped (0-5). Counts toward a full set. Set by equipment later.
        public float baseClothCount = 0f; //cloth pieces equipped (0-5). Counts toward a full set. Set by equipment later.

        // [Header("Camera")]
        // public float baseFOV = 60f;

    }
}
