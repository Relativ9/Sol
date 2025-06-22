using UnityEngine;
using System.Collections.Generic;

namespace Sol
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        public string weaponId;
        public string displayName;
        public WeaponType weaponType;
        
        [Header("Visual")]
        public GameObject weaponPrefab;
        public Vector3 sheathedPosition = Vector3.zero;
        public Vector3 sheathedRotation = Vector3.zero;
        public Vector3 unsheathedPosition = Vector3.zero;
        public Vector3 unsheathedRotation = Vector3.zero;
        
        [Header("Combat Stats")]
        public float baseDamage = 10f;
        public float attackSpeed = 1.0f;
        public float range = 1.5f;
        public float critChance = 0.05f;
        public float critMultiplier = 1.5f;
        
        [Header("Animation")]
        public string equipAnimationTrigger = "Unsheathe";
        public string unequipAnimationTrigger = "Sheathe";
        public string attackAnimationTrigger = "Attack";
        public string[] comboAnimationTriggers;
        
        [Header("Effects")]
        public GameObject hitEffectPrefab;
        public AudioClip swingSound;
        public AudioClip hitSound;
        
        // Dictionary for custom stats (can be extended in the inspector)
        [System.Serializable]
        public class StatEntry
        {
            public string key;
            public float value;
        }
        
        [Header("Custom Stats")]
        public List<StatEntry> customStats = new List<StatEntry>();
        
        // Helper method to get a custom stat value
        public float GetStat(string statName)
        {
            // Check built-in stats first
            switch (statName.ToLower())
            {
                case "damage": return baseDamage;
                case "attackspeed": return attackSpeed;
                case "range": return range;
                case "critchance": return critChance;
                case "critmultiplier": return critMultiplier;
            }
            
            // Check custom stats
            foreach (var stat in customStats)
            {
                if (stat.key.ToLower() == statName.ToLower())
                {
                    return stat.value;
                }
            }
            
            // Return 0 if stat not found
            return 0f;
        }
    }
}
