using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sol
{

    public class StatsService : MonoBehaviour, IStatsService
    {
        [SerializeField] private PlayerStats _playerStats;
        private Dictionary<string, float> _baseValues = new Dictionary<string, float>();
        
        private Dictionary<string, Dictionary<ModifierCatagory, List<StatModifier>>> _modifiersByCategory = new Dictionary<string, Dictionary<ModifierCatagory, List<StatModifier>>>();
        
        void Awake()
        {
            IntializeStats();
        }

        private void IntializeStats()
        {
            //Movement stats
            _baseValues["moveSpeed"] = _playerStats != null ? _playerStats.baseMoveSpeed : 5f;
            _baseValues["runMultiplier"] = _playerStats != null ? _playerStats.baseRunMultiplier : 1.6f;
            _baseValues["deceleration"] = _playerStats != null ? _playerStats.baseDeceleration : 8f;
            
            //Combat stats
            _baseValues["health"] = _playerStats.baseHealth;
            _baseValues["stamina"] = _playerStats.baseStamina;
            _baseValues["focus"] = _playerStats.baseFocus;
            
            // Jump stats
            _baseValues["JumpForce"] = _playerStats.baseJumpForce;
            _baseValues["JumpCooldown"] = _playerStats.baseJumpCooldown;

            foreach (string statName in _baseValues.Keys)
            {
                _modifiersByCategory[statName] = new Dictionary<ModifierCatagory, List<StatModifier>>();

                foreach (ModifierCatagory catagory in System.Enum.GetValues(typeof(ModifierCatagory)))
                {
                    _modifiersByCategory[statName][catagory] = new List<StatModifier>();
                }

            }
        }

        private void Update()
        {
            UpdateModifierDurations();
        }

        private void UpdateModifierDurations()
        {
            foreach (var statEntry in _modifiersByCategory)
            {
                string statName = statEntry.Key;

                foreach (var catagoryEntry in statEntry.Value)
                {
                    ModifierCatagory catagory = catagoryEntry.Key;
                    List<StatModifier> modifiers = catagoryEntry.Value;
                    
                    List<string> expiredModifiers = new List<string>();

                    for (int i = 0; i < modifiers.Count; i++)
                    {
                        var modifier = modifiers[i];

                        if (modifier.duration > 0)
                        {
                            var updatedModifier = modifier;
                            updatedModifier.duration -= Time.deltaTime;
                            
                            modifiers[i] = updatedModifier;

                            if (updatedModifier.duration <= 0)
                            {
                                expiredModifiers.Add(updatedModifier.id);
                            }
                        }
                    }

                    foreach (var id in expiredModifiers)
                    {
                        RemoveModifier(statName, id);
                    }
                }
            }
        }

        public float GetStat(string statName)
        {
            if (!_baseValues.ContainsKey(statName))
            {
                Debug.LogWarning($"Stat {statName} not found!");
                return 0f;
            }
            
            float finalValue = _baseValues[statName];

            foreach (ModifierCatagory category in System.Enum.GetValues(typeof(ModifierCatagory)))
            {
                if (category == ModifierCatagory.Base) continue;
                
                //Apply additive modifiers for this category
                float additiveModifier = 0f;
                foreach (var mod in _modifiersByCategory[statName][category]
                             .Where(m => m.type == ModifierType.Additive))
                {
                    additiveModifier += mod.value;
                }
                finalValue += additiveModifier;
                
                //Apply multiplication modifiers for this category
                float multiplier = 1f;
                foreach (var mod in _modifiersByCategory[statName][category]
                             .Where(m => m.type == ModifierType.Multiplicative))
                {
                    multiplier *= mod.value;
                }
                finalValue *= multiplier;
            }
            
            return finalValue;
        }
        
        public string ApplyOrReplaceModifier(string statName, StatModifier modifier, ModifierCatagory category, string sourceId)
        {
            if (!_modifiersByCategory.ContainsKey(statName))
            {
                Debug.LogWarning($"Stat {statName} not found!");
                return null;
            }
    
            // Set the sourceId on the modifier
            modifier.sourceId = sourceId;
    
            // Check if there's already a modifier with this sourceId in this category
            int existingIndex = _modifiersByCategory[statName][category].FindIndex(m => m.sourceId == sourceId);
    
            if (existingIndex >= 0)
            {
                // Replace the existing modifier but keep its ID
                string existingId = _modifiersByCategory[statName][category][existingIndex].id;
                modifier.id = existingId;
                _modifiersByCategory[statName][category][existingIndex] = modifier;
        
                Debug.Log($"Replaced modifier for {statName} from source {sourceId}. Value: {modifier.value}, Duration: {modifier.duration}");
                return existingId;
            }
            else
            {
                // Generate a new ID for this modifier
                modifier.id = System.Guid.NewGuid().ToString();
        
                // Add the new modifier
                _modifiersByCategory[statName][category].Add(modifier);
        
                Debug.Log($"Added new modifier for {statName} from source {sourceId}. Value: {modifier.value}, Duration: {modifier.duration}");
                return modifier.id;
            }
        }
        
        public string ApplyModifier(string statName, StatModifier modifier, ModifierCatagory modifierCatagory)
        {
            if (!_modifiersByCategory.ContainsKey(statName))
            {
                Debug.LogWarning($"Stat {statName} not found!");
                return null;
            }
            
            //Generate unique ID
            modifier.id = System.Guid.NewGuid().ToString();
            
            _modifiersByCategory[statName][modifierCatagory].Add(modifier);
            
            return modifier.id;
        }

        public void RemoveModifier(string statName, string modifierId)
        {
            if (!_modifiersByCategory.ContainsKey(statName)) return;

            foreach (var category in _modifiersByCategory[statName].Keys)
            {
                _modifiersByCategory[statName][category].RemoveAll(m => m.id == modifierId);
            }
        }
        
        public void RemoveModifiersFromSource(string statName, string sourceId)
        {
            if (!_modifiersByCategory.ContainsKey(statName))
                return;
                
            foreach (var category in _modifiersByCategory[statName].Keys)
            {
                _modifiersByCategory[statName][category].RemoveAll(m => m.sourceId == sourceId);
            }
        }

        public void RemoveAllModifiersOfType(string statName, ModifierCatagory modifierCatagory)
        {
            if (!_modifiersByCategory.ContainsKey(statName)) return;
            
            _modifiersByCategory[statName][modifierCatagory].Clear();
        }
        
        public float GetSpeedMultiplier()
        {
            float baseSpeed = _baseValues["moveSpeed"];
            float currentSpeed = GetStat("moveSpeed");
            
            return currentSpeed / baseSpeed;
        }
    }
}
