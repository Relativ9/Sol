using UnityEngine;
using System;

namespace Sol
{
    /// <summary>
    /// Owns one enemy's ActorStats. Assign an ActorStatSO per enemy type. Hit sources on the same enemy
    /// (its damage source, its projectiles) read the stats from here, so modifiers apply everywhere.
    /// Applies equipment modifiers from the ActorStatSO on initialization.
    /// Fires equipment change events for visual managers (weapon spawning, etc).
    /// </summary>
    public class EnemyStats : MonoBehaviour
    {
        [SerializeField] private ActorStatSO _baseStats;

        public ActorStats Stats { get; private set; }

        /// <summary>Fired when equipment changes (slot, newItem). newItem can be null if unequipped.</summary>
        public event Action<EquipmentSlot, EquipmentDataSO> EquipmentChanged;

        void Awake()
        {
            Stats = new ActorStats(_baseStats);
            ApplyEquipmentModifiers();
        }

        /// <summary>Applies modifiers from equipped items in the ActorStatSO and fires equipment change events.</summary>
        private void ApplyEquipmentModifiers()
        {
            if (_baseStats == null) return;

            // Apply modifiers from each equipped item and notify listeners
            ApplyEquipmentModifiersFromSlot(EquipmentSlot.MainHand, "MainHand");
            ApplyEquipmentModifiersFromSlot(EquipmentSlot.Offhand, "Offhand");
            ApplyEquipmentModifiersFromSlot(EquipmentSlot.Head, "Head");
            ApplyEquipmentModifiersFromSlot(EquipmentSlot.Torso, "Torso");
            ApplyEquipmentModifiersFromSlot(EquipmentSlot.Legs, "Legs");
            ApplyEquipmentModifiersFromSlot(EquipmentSlot.Feet, "Feet");
            ApplyEquipmentModifiersFromSlot(EquipmentSlot.Neck, "Neck");
        }

        /// <summary>Applies modifiers from a single equipment slot and fires equipment change event.</summary>
        private void ApplyEquipmentModifiersFromSlot(EquipmentSlot slot, string slotName)
        {
            EquipmentDataSO item = _baseStats.GetEquipped(slot);

            // Create a unique source ID for this slot
            string sourceId = $"{_baseStats.name}_{slotName}";

            // Fire equipment changed event (for visual managers, weapon spawning, etc)
            EquipmentChanged?.Invoke(slot, item);

            // If nothing equipped, just fire the event (no modifiers to apply)
            if (item == null) return;

            // Apply each modifier from the item
            if (item.statModifiers != null)
            {
                foreach (var modifier in item.statModifiers)
                {
                    var equipmentMod = new StatModifier(
                        modifier.modifierType,
                        ModifierCategory.Equipment,
                        modifier.statName,
                        modifier.value,
                        sourceId
                    );
                    Stats.ApplyOrReplaceModifier(equipmentMod);
                }
            }
        }

        /// <summary>True if this enemy type blocks a status effect, from its type's immunities.</summary>
        public bool IsImmuneTo(IStatusEffect effect) => _baseStats != null && _baseStats.IsImmune(effect.Kind);

        /// <summary>Returns the stats for the enemy on this object or a parent, or null when there is none.</summary>
        public static ActorStats Find(Transform from)
        {
            EnemyStats owner = from.GetComponentInParent<EnemyStats>();
            return owner != null ? owner.Stats : null;
        }

        /// <summary>
        /// The enemy's root position: the body, not a weapon or hitbox. Used as the knockback origin.
        /// Falls back to the topmost transform when no EnemyStats is present, with a warning so it can't go silent.
        /// </summary>
        public static Vector3 RootPosition(Transform from)
        {
            EnemyStats owner = from.GetComponentInParent<EnemyStats>();
            if (owner != null) return owner.transform.position;

            Debug.LogWarning($"[EnemyStats] {from.name} has no EnemyStats above it. Using its root transform as the knockback origin.", from);
            return from.root.position;
        }
    }
}
