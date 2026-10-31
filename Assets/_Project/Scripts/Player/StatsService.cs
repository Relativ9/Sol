using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sol
{
    public class StatsService : MonoBehaviour, IStatsService
    {
        private Dictionary<StatTypeEnum, float> _baseValues = new();
        private Dictionary<StatTypeEnum, float> _totalCache = new();
        private Dictionary<StatTypeEnum, float> _statFloors = new();

        private Dictionary<StatTypeEnum, Dictionary<ModifierCategory, List<StatModifier>>> _modifiersByCategory = new();
        private Dictionary<string, StatTypeEnum> _modifierToStatLookup = new();
        private bool _statsDirty = false;

        private IStatCalculator _calculator;

        [SerializeField] private GameEvent _onStatsChangedEvent;

        void Awake()
        {
            _calculator = new StandardStatCalculator();
            ServiceLocator.RegisterService<IStatsService>(this);
        }

        public void Initialize(PlayerStatSO statSO, PlayerSaveData saveData)
        {
            //Core Attributes
            _baseValues[StatTypeEnum.Brawn] = statSO.brawn;
            _baseValues[StatTypeEnum.Finesse] = statSO.finesse;
            _baseValues[StatTypeEnum.Insight] = statSO.insight;
            _baseValues[StatTypeEnum.Focus] = statSO.focus;
            _baseValues[StatTypeEnum.Vigor] = statSO.vigor;
            _baseValues[StatTypeEnum.Willpower] = statSO.willpower;

            //Movement
            _baseValues[StatTypeEnum.MoveSpeed] = statSO.baseMoveSpeed;
            _baseValues[StatTypeEnum.Deceleration] = statSO.baseDeceleration;
            _baseValues[StatTypeEnum.CrouchSpeed] = statSO.baseCrouchSpeed;
            _baseValues[StatTypeEnum.JumpCooldown] = statSO.baseJumpCooldown;
            _baseValues[StatTypeEnum.JumpDirectionBoost] = statSO.baseJumpDirectionBoost;
            _baseValues[StatTypeEnum.MaxDoubleJump] = statSO.baseMaxDoubleJump;
            _baseValues[StatTypeEnum.GravityMultiplier] = statSO.baseGravityMultiplier;
            _baseValues[StatTypeEnum.TerminalVelocity] = statSO.baseTerminalVelocity;

            //Resources
            _baseValues[StatTypeEnum.InventorySpace] = statSO.baseInventorySpace;
            _baseValues[StatTypeEnum.LootMultiplier] = statSO.baseLootMultiplier;

            //Combat
            _baseValues[StatTypeEnum.ArmorValue] = statSO.baseArmorValue;
            _baseValues[StatTypeEnum.DeflectionChance] = statSO.baseDeflectionChance;
            _baseValues[StatTypeEnum.FireResistance] = statSO.baseFireResistance;
            _baseValues[StatTypeEnum.IceResistance] = statSO.baseIceResistance;
            _baseValues[StatTypeEnum.VoidResistance] = statSO.baseVoidResistance;
            _baseValues[StatTypeEnum.SonicResistance] = statSO.baseSonicResistance;
            _baseValues[StatTypeEnum.SoulResistance] = statSO.baseSoulResistance;
            _baseValues[StatTypeEnum.ReflectChance] = statSO.baseReflectChance;
            _baseValues[StatTypeEnum.StaggerResistance] = statSO.baseStaggerResistance;
            _baseValues[StatTypeEnum.BleedResistance] = statSO.baseBleedResistance;
            _baseValues[StatTypeEnum.RigidWeight] = statSO.baseRigidWeight;
            _baseValues[StatTypeEnum.FlexWeight] = statSO.baseFlexWeight;
            _baseValues[StatTypeEnum.PlateCount] = statSO.basePlateCount;
            _baseValues[StatTypeEnum.FlexCount] = statSO.baseFlexCount;
            _baseValues[StatTypeEnum.ClothCount] = statSO.baseClothCount;

            foreach (StatTypeEnum stat in System.Enum.GetValues(typeof(StatTypeEnum)))
            {
                if (!_baseValues.ContainsKey(stat))
                {
                    _baseValues[stat] = 0f;
                }

                _modifiersByCategory[stat] = new Dictionary<ModifierCategory, List<StatModifier>>();

                foreach (ModifierCategory category in System.Enum.GetValues(typeof(ModifierCategory)))
                {
                    _modifiersByCategory[stat][category] = new List<StatModifier>();
                }

            }

            // saveData used by ProgressionService to push attribute modifiers after initialisation
        }

        private void Update()
        {
            UpdateModifierDurations();
        }

        private void LateUpdate()
        {
            if (_statsDirty && _onStatsChangedEvent != null)
            {
                _onStatsChangedEvent.Raise();
                _statsDirty = false;
            }
        }

        private void UpdateModifierDurations()
        {
            HashSet<StatTypeEnum> affectedStats = new(); // ADD
            foreach (var statEntry in _modifiersByCategory)
            {
                StatTypeEnum statType = statEntry.Key;
                foreach (var categoryEntry in statEntry.Value)
                {
                    List<StatModifier> modifiers = categoryEntry.Value;
                    for (int i = modifiers.Count - 1; i >= 0; i--)
                    {
                        var modifier = modifiers[i];
                        if (modifier.duration > 0f)
                        {
                            modifier.duration -= Time.deltaTime;

                            if (modifier.duration <= 0f)
                            {
                                modifiers.RemoveAt(i);
                                _modifierToStatLookup.Remove(modifier.id); // ADD
                                affectedStats.Add(statType); // ADD
                            }
                            else
                            {
                                modifiers[i] = modifier;
                            }
                        }
                    }
                }
            }

            foreach (StatTypeEnum stat in affectedStats)
                RefreshStat(stat);

            // foreach (var statEntry in _modifiersByCategory)
            // {
            //     var statType = statEntry.Key;
            //
            //     foreach (var catagoryEntry in statEntry.Value)
            //     {
            //         ModifierCategory category = catagoryEntry.Key;
            //         List<StatModifier> modifiers = catagoryEntry.Value;
            //         
            //         List<string> expiredModifiers = new List<string>();
            //
            //         for (int i = 0; i < modifiers.Count; i++)
            //         {
            //             var modifier = modifiers[i];
            //
            //             if (modifier.duration > 0)
            //             {
            //                 var updatedModifier = modifier;
            //                 updatedModifier.duration -= Time.deltaTime;
            //                 
            //                 modifiers[i] = updatedModifier;
            //
            //                 if (updatedModifier.duration <= 0)
            //                 {
            //                     expiredModifiers.Add(updatedModifier.id);
            //                 }
            //             }
            //         }
            //
            //         foreach (var id in expiredModifiers)
            //         {
            //             RemoveModifier(id);
            //         }
            //     }
            // }
        }

        public float GetBaseStat(StatTypeEnum statType)
        {
            return _baseValues.TryGetValue(statType, out float value) ? value : 0f;
        }


        public float GetStat(StatTypeEnum statType)
        {

            if (_totalCache.TryGetValue(statType, out float cached))
                return cached;
            // Stat hasn't been calculated yet (e.g. queried before any modifier applied)
            RefreshStat(statType);
            return _totalCache.GetValueOrDefault(statType, _baseValues.GetValueOrDefault(statType, 0f));
            // if (!_baseValues.TryGetValue(statType, out float baseValue))
            //     return 0f;
            // // Flatten all modifiers for this stat, skipping Base category in the filter if desired
            // StatModifier[] mods = _modifiersByCategory[statType]
            //     .SelectMany(kvp => kvp.Value)
            //     .Where(m => m.category != ModifierCategory.Base)
            //     .ToArray();
            // return _calculator.Calculate(statType, baseValue, mods);

            // if (!_baseValues.ContainsKey(statType))
            // {
            //     Debug.LogWarning($"Stat {statType} not found!");
            //     return 0f;
            // }
            //
            // float finalValue = _baseValues[statType];
            //
            // foreach (ModifierCategory category in System.Enum.GetValues(typeof(ModifierCategory)))
            // {
            //     if (category == ModifierCategory.Base) continue;
            //     
            //     //Apply additive modifiers fAor this category
            //     float additiveModifier = 0f;
            //     foreach (var mod in _modifiersByCategory[statType][category]
            //                  .Where(m => m.type == ModifierType.FlatAdditive))
            //     {
            //         additiveModifier += mod.value;
            //     }
            //     finalValue += additiveModifier;
            //     
            //     //Apply multiplication modifiers for this category
            //     float percentSum = 0f;
            //     foreach (var mod in _modifiersByCategory[statType][category]
            //                  .Where(m => m.type == ModifierType.PercentAdditive))
            //     {
            //         percentSum += mod.value;
            //     }
            //     finalValue *= (1f + percentSum);
            // }
            //
            // return finalValue;
        }

        // Progression is the only writer of attributes. Anything else is refused, so a wrong call is visible, not silent.
        public void SetAttributeBase(StatTypeEnum statType, float value)
        {
            if (!StatRoles.IsAttribute(statType))
            {
                Debug.LogError($"[{nameof(StatsService)}] {statType} is a {StatRoles.RoleOf(statType)} stat, not an attribute. It was not set.");
                return;
            }
            WriteBase(statType, value);
        }

        // One source, one contribution per stat. ApplyOrReplaceModifier swaps the old entry for the same source, so the
        // source's total is the value it last set and never accumulates across calls.
        public void SetSourceContribution(StatTypeEnum statType, string sourceId, float value, ModifierCategory category)
        {
            ApplyOrReplaceModifier(new StatModifier(
                type: ModifierType.FlatAdditive,
                category: category,
                statType: statType,
                value: value,
                sourceId: sourceId,
                duration: -1f));
        }

        // Floors are minimums on a derived total. They are set by the resolver from the stat asset, so the design value
        // of a multiplier is never lost to a negative modifier.
        public void SetDerivedFloor(StatTypeEnum statType, float floor)
        {
            if (!StatRoles.IsDerived(statType))
            {
                Debug.LogError($"[{nameof(StatsService)}] {statType} is a {StatRoles.RoleOf(statType)} stat, not a derived stat. No floor was set.");
                return;
            }
            _statFloors[statType] = floor;
            RefreshStat(statType);
        }

        private void WriteBase(StatTypeEnum statType, float value)
        {
            _baseValues[statType] = value;
            RefreshStat(statType);
        }

        private void RefreshStat(StatTypeEnum statType)
        {
            // Derived stats have no SO base, so a missing base is 0. Returning early here would leave them stuck at 0.
            float baseValue = _baseValues.GetValueOrDefault(statType, 0f);
            StatModifier[] mods = _modifiersByCategory[statType]
                .SelectMany(kvp => kvp.Value)
                .Where(m => m.category != ModifierCategory.Base)
                .ToArray();
            // Caps apply to the stacked total from every source, so the stats screen and every consumer see the same value.
            float total = _calculator.Calculate(statType, baseValue, mods);
            if (_statFloors.TryGetValue(statType, out float floor)) total = Mathf.Max(total, floor);
            float newValue = StatCaps.Apply(statType, total);
            float oldValue = _totalCache.GetValueOrDefault(statType, baseValue);
            if (!Mathf.Approximately(oldValue, newValue))
            {
                _totalCache[statType] = newValue;
                _statsDirty = true;
            }
        }

        public string ApplyOrReplaceModifier(StatModifier modifier)
        {
            StatTypeEnum statType = modifier.statType;
            ModifierCategory category = modifier.category;

            if (!_modifiersByCategory.ContainsKey(statType))
            {
                Debug.LogWarning($"Stat {statType} not found!");
                return null;
            }

            // Defensive: if the inner dictionary is somehow missing this category bucket
            if (!_modifiersByCategory[statType].ContainsKey(category))
            {
                Debug.LogWarning($"Category {category} not initialized for stat {statType}!");
                return null;
            }

            // Work on a copy so the input parameter stays pristine
            StatModifier modToStore = modifier;
            // Ensure identity exists (handles manual struct initialization that skipped the constructor)
            if (string.IsNullOrEmpty(modToStore.id))
                modToStore.id = System.Guid.NewGuid().ToString();
            var modifiers = _modifiersByCategory[statType][category];
            bool wasReplaced = false;
            // Replacement only makes sense when we know the logical source
            if (!string.IsNullOrEmpty(modToStore.sourceId))
            {
                int existingIndex = modifiers.FindIndex(m => m.sourceId == modToStore.sourceId);
                if (existingIndex >= 0)
                {
                    // Preserve the old GUID so the outside world doesn't notice a swap
                    modToStore.id = modifiers[existingIndex].id;
                    modifiers[existingIndex] = modToStore;
                    wasReplaced = true;
                    Debug.Log($"Replaced modifier for {statType} from source {modToStore.sourceId}. " +
                              $"Value: {modToStore.value}, Duration: {modToStore.duration}");
                }
            }

            // Fresh Entry
            if (!wasReplaced)
            {
                modifiers.Add(modToStore);
                Debug.Log(
                    $"Added new modifier for {statType} from source {modToStore.sourceId}. Value: {modToStore.value}, Duration: {modToStore.duration}");
            }

            // Shared tail: runs for both replace and add so the lookup and cache stay in sync
            _modifierToStatLookup[modToStore.id] = statType;
            RefreshStat(statType);

            return modToStore.id;
        }


        public string ApplyModifier(StatModifier modifier)
        {
            if (!_modifiersByCategory.ContainsKey(modifier.statType))
            {
                Debug.LogWarning($"Stat {modifier} not found!");
                return null;
            }

            //Generate unique ID
            modifier.id = System.Guid.NewGuid().ToString();

            _modifiersByCategory[modifier.statType][modifier.category].Add(modifier);

            _modifierToStatLookup[modifier.id] = modifier.statType;
            RefreshStat(modifier.statType);

            return modifier.id;
        }

        public void RemoveModifier(string modifierId)
        {
            if (!_modifierToStatLookup.TryGetValue(modifierId, out StatTypeEnum statType))
                return;
            foreach (var categoryList in _modifiersByCategory[statType].Values)
            {
                int index = categoryList.FindIndex(m => m.id == modifierId);
                if (index >= 0)
                {
                    categoryList.RemoveAt(index);
                    break;
                }
            }

            _modifierToStatLookup.Remove(modifierId);
            RefreshStat(statType);

            // foreach (var statEntry in _modifiersByCategory)
            // {
            //     foreach (var categoryEntry in statEntry.Value)
            //     {
            //         List<StatModifier> modifiers = categoryEntry.Value;
            //         for (int i = 0; i < modifiers.Count; i++)
            //         {
            //             if (modifiers[i].id == modifierId)
            //             {
            //                 modifiers.RemoveAt(i);
            //                 return;
            //             }
            //         }
            //     }
            // }
        }

        public void RemoveModifiersFromSource(string sourceId)
        {
            HashSet<StatTypeEnum>
                affectedStats =
                    new(); // Enforces unique entries, making sure we're never doing a duplicate remove action.

            foreach (var statEntry in _modifiersByCategory)
            {
                StatTypeEnum statType = statEntry.Key;
                bool changed = false;

                foreach (var categoryEntry in statEntry.Value)
                {
                    List<StatModifier> modifiers = categoryEntry.Value;

                    // Collect IDs first, so the reverse lookup can be cleaned up. Without this, every
                    // equip/unequip cycle leaves stale IDs behind in _modifierToStatLookup.
                    for (int i = modifiers.Count - 1; i >= 0; i--)
                    {
                        if (modifiers[i].sourceId != sourceId) continue;
                        _modifierToStatLookup.Remove(modifiers[i].id);
                        modifiers.RemoveAt(i);
                        changed = true;
                    }
                }

                if (changed)
                    affectedStats.Add(statType); // ADD
            }

            foreach (StatTypeEnum stat in affectedStats)
                RefreshStat(stat);

            // foreach (var statEntry in _modifiersByCategory)
            // {
            //     foreach (var categoryEntry in statEntry.Value)
            //     {
            //         List<StatModifier> modifiers = categoryEntry.Value;
            //         modifiers.RemoveAll(m => m.sourceId == sourceId);
            //     }
            // }
        }

        public void RemoveAllModifiersOfType(StatTypeEnum statType, ModifierCategory category)
        {
            if (!_modifiersByCategory.ContainsKey(statType)) return;

            foreach (var mod in _modifiersByCategory[statType][category])
                _modifierToStatLookup.Remove(mod.id);

            _modifiersByCategory[statType][category].Clear();

            RefreshStat(statType);
        }

        public float GetSpeedMultiplier()
        {
            float baseSpeed = GetBaseStat(StatTypeEnum.MoveSpeed);
            if (baseSpeed == 0f) return 0f;
            return GetStat(StatTypeEnum.MoveSpeed) / baseSpeed;
        }

    }
}
