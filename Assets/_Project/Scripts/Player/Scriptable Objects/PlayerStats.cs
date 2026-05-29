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
        public float baseRunMultiplier = 1.6f;
        public float baseDeceleration = 8f;
        public float baseCrouchSpeed = 3f;
        public float baseJumpForce = 5f;
        public float baseJumpDirectionBoost = 1.0f;
        public int baseMaxDoubleJump = 0;
        public float baseJumpCooldown = 0.1f;
        public float baseFallDamageMultiplier = 1f;
        public float baseGravityMultiplier = 2.5f;
        public float baseTerminalVelocity = -20f;
        
        [Header("Resources")]
        public float baseMaxHealth = 100f;
        public float baseHealthRegen = 1f;  //Number of health points added per second
        public float baseMaxEnergy = 100f; //Global resource, spells, attacks, sprinting, jumping, abilities, etc all use Energy.
        public float baseEnergyRegen = 10f;  //Number of energy points added per second
        public float baseCarryCapacity = 40f; //Equivalent to KGs, from the start the max you can carry (including warn gear is 40gk).
        public int baseInventorySpace = 10;
        public float baseLootMultiplier = 1f;
        public float baseHunger = 10f;
        public float baseThirst = 10f;
        
        [Header("Combat")]
        public float baseMeleeDamage = 1f; //Weapon damage on equipped weapons add to this, unarmed attacks do 1 damage (unless boosted)
        public float baseProjectileDamage = 1f; //even throwing rocks deal some damage, effects throwing weapons, arrows, and projectile spells.
        public float baseProjectileRange = 10f; //10 meter effective range, after this drop and damage falloff starts taking noticable effect.
        public float baseSpellPower = 0f; //increases potency of spells usually calculated at base spell damage + spellPower/100*energycost
        public float baseArmorValue = 0f; //Armor lowers all physical damage and with the right talents give extra bonus protection (deflect, reflect etc).
        public float baseCritChance = 0f; //Crit chance is the chance you have to crit on an armored weakpoint (such as a head with a helmet) with projectiles or direct melee damage AOE and indirect target spells have a 0.5 x critChance modifier, unarmored weakpoints are guaranteed crits. 
        public float baseCritMultiplier = 1.2f; //Very difficult to increase, capped at 3f.
        public float baseDeflectionChance = 0f; //chance you have of deflecting incoming physical attacks (including physical projectiles), is only increased by plate armor.
        public float baseMagicResistance = 0f;
        public float baseFireResistance = 0f;
        public float baseIceResistance = 0f;
        public float baseVoidResistance = 0f;
        public float baseSonicResistance = 0f;
        public float baseSoulResistance = 0f;
        public float baseReflectChance = 0f; //chance of reflecting spells back at attacker
        public float baseEnvironmentalResistance = 0f; //resistence to damage caused by enviromental factors (gas, heat, cold, acid, traps, etc).
        public float baseParryAndBlockTime = 0.1f; //How long the perry and perfect block window lasts on incoming enemy attacks (time slows so animations match).
        public float baseClueRange = 0f; //Spells and talents which highlight loot and secrets add to this range, as does certain talents.
        public float baseCharmChance = 0f; //speech check
        public float baseKnowledgeChance = 0f; //speech check
        public float baseDeceptionChance = 0f; //speech check
        public float baseIntimidationChance = 0f; //speech check

        // [Header("Camera")]
        // public float baseFOV = 60f;

    }
}
