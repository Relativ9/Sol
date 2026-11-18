using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Base stat block for one enemy type. Designed for easy editing in Inspector (not lists).
    /// Perception is derived from Insight. Disposition and Aggressiveness are direct behavioral properties.
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Stats", menuName = "Sol/Enemies/Enemy Stats")]
    public class ActorStatSO : ScriptableObject
    {
        [Header("Core Attributes")]
        public int brawn = 10;
        public int finesse = 10;
        public int insight = 10;
        public int focus = 10;
        public int vigor = 10;
        public int willpower = 10;

        [Header("Combat")]
        public float baseArmorValue = 0f;
        public float baseDeflectionChance = 0f;
        public float baseFireResistance = 0f;
        public float baseIceResistance = 0f;
        public float baseVoidResistance = 0f;
        public float baseSonicResistance = 0f;
        public float baseSoulResistance = 0f;
        public float baseStaggerResistance = 0f;
        public float baseBleedResistance = 0f;
        public float baseRigidWeight = 0f;
        public float baseFlexWeight = 0f;
        public float basePlateCount = 0f;
        public float baseFlexCount = 0f;
        public float baseClothCount = 0f;

        [Header("Status Immunities")]
        public StatusKind immunities = StatusKind.None;

        [Header("Equipment & Loot")]
        [Tooltip("Equipment slots: MainHand, Offhand, Head, Torso, Legs, Feet, Neck.")]
        public EquipmentDataSO mainHandItem;
        public EquipmentDataSO offhandItem;
        public EquipmentDataSO headItem;
        public EquipmentDataSO torsoItem;
        public EquipmentDataSO legsItem;
        public EquipmentDataSO feetItem;
        public EquipmentDataSO neckItem;
        
        [Tooltip("Loot table for drops when enemy dies.")]
        public ItemDataSO[] lootTable = new ItemDataSO[0];

        [Header("AI Behavior")]
        [Range(-1f, 1f)]
        [Tooltip("Disposition: -1 = hostile, 0 = neutral, 1 = friendly.")]
        public float disposition = -1f;

        [Range(0f, 1f)]
        [Tooltip("Aggressiveness: 0 = cautious/tactical, 1 = reckless/charge.")]
        public float aggressiveness = 0.5f;

        [Header("Attribute Scaling")]
        public float brawnToMeleeDamage = 0.5f;
        public float finesseToProjectileDamage = 0.5f;
        public float insightToSpellPower = 1f;
        public float insightToPerception = 0.05f;
        public float insightToCritMultiplier = 0.02f;
        public float focusToCritChance = 0.5f;
        public float finesseToPenetration = 1.5f;
        public float vigorToMaxHealth = 5f;
        public float willpowerToMaxEnergy = 3f;

        [Header("Derived Minimums")]
        public float minCritMultiplier = 1.2f;

        public bool IsImmune(StatusKind kind) => kind != StatusKind.None && (immunities & kind) == kind;

        /// <summary>Get the equipped item for a specific slot, or null if empty.</summary>
        public EquipmentDataSO GetEquipped(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.MainHand => mainHandItem,
                EquipmentSlot.Offhand => offhandItem,
                EquipmentSlot.Head => headItem,
                EquipmentSlot.Torso => torsoItem,
                EquipmentSlot.Legs => legsItem,
                EquipmentSlot.Feet => feetItem,
                EquipmentSlot.Neck => neckItem,
                _ => null,
            };
        }

        public float GetBase(StatTypeEnum stat)
        {
            return stat switch
            {
                StatTypeEnum.MeleeDamage => brawn * brawnToMeleeDamage,
                StatTypeEnum.ProjectileDamage => finesse * finesseToProjectileDamage,
                StatTypeEnum.SpellPower => insight * insightToSpellPower,
                StatTypeEnum.Perception => insight * insightToPerception,
                StatTypeEnum.CritMultiplier => Mathf.Max(minCritMultiplier, insight * insightToCritMultiplier),
                StatTypeEnum.CritChance => focus * focusToCritChance,
                StatTypeEnum.Penetration => finesse * finesseToPenetration,
                StatTypeEnum.MaxHealth => vigor * vigorToMaxHealth,
                StatTypeEnum.MaxEnergy => willpower * willpowerToMaxEnergy,
                StatTypeEnum.ArmorValue => baseArmorValue,
                StatTypeEnum.DeflectionChance => baseDeflectionChance,
                StatTypeEnum.FireResistance => baseFireResistance,
                StatTypeEnum.IceResistance => baseIceResistance,
                StatTypeEnum.VoidResistance => baseVoidResistance,
                StatTypeEnum.SonicResistance => baseSonicResistance,
                StatTypeEnum.SoulResistance => baseSoulResistance,
                StatTypeEnum.StaggerResistance => baseStaggerResistance + (baseRigidWeight * 0.002f),
                StatTypeEnum.BleedResistance => baseBleedResistance + (baseFlexWeight * 0.00333f),
                StatTypeEnum.RigidWeight => baseRigidWeight,
                StatTypeEnum.FlexWeight => baseFlexWeight,
                StatTypeEnum.PlateCount => basePlateCount,
                StatTypeEnum.FlexCount => baseFlexCount,
                StatTypeEnum.ClothCount => baseClothCount,
                _ => 0f,
            };
        }
    }
}
