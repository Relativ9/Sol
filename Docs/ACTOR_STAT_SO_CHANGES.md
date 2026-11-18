# ActorStatSO Redesign - Final Summary

**Status:** COMPLETE ✅  
**Date:** November 18, 2026

---

## Changes Made

### OLD Design (REJECTED)
- Perception range bonus as separate slider: `perceptionRangeBonus` (0-1)
- List-based stats entry (confusing)
- Disconnected from player attributes

### NEW Design (IMPLEMENTED)
- **Perception derived from Insight stat**
- `Perception = Insight * insightToPerception (default 0.05)`
- Matches PlayerStatSO structure (editable fields, not lists)
- Easy inspector editing

---

## How It Works

### Calculation
```
Insight = 9 (from ActorStatSO)
insightToPerception = 0.05 (multiplier)
Perception = GetBase(StatTypeEnum.Perception)
           = 9 * 0.05
           = 0.45 (45% bonus)

Vision Range = 20m * (1 + 0.45) = 29m
Hearing Range = 10m * (1 + 0.45) = 14.5m (walk), 21.75m (sprint)
```

### In EnemySenses
```csharp
float perceptionBonus = _stats.Stats.GetStat(StatTypeEnum.Perception);
float effectiveVisionRange = _baseVisionRange * (1f + perceptionBonus);
```

---

## ActorStatSO Fields

### Core Attributes (int, default 10)
- brawn
- finesse
- insight ← **Drives Perception!**
- focus
- vigor
- willpower

### Combat Stats
- baseArmorValue, baseDeflectionChance
- Fire/Ice/Void/Sonic/Soul Resistance
- Stagger/Bleed Resistance
- Rigid/Flex/Cloth Weight and Count

### Status Immunities
- immunities (StatusKind flags)

### Equipment & Loot
- equippedItems[] (ItemDataSO array)
- lootTable[] (ItemDataSO array)

### AI Behavior (NEW - not in player)
- disposition (-1 to +1: hostile to friendly)
- aggressiveness (0 to 1: cautious to reckless)

### Attribute Scaling
- brawnToMeleeDamage: 0.5
- finesseToProjectileDamage: 0.5
- insightToSpellPower: 1.0
- **insightToPerception: 0.05** ← **KEY**
- insightToCritMultiplier: 0.02
- focusToCritChance: 0.5
- finesseToPenetration: 1.5
- vigorToMaxHealth: 5.0
- willpowerToMaxEnergy: 3.0

---

## Removed (Player-Only)
- Hunger, Thirst
- Knowledge, Deception, Intimidation, Charm
- Clue Range
- Inventory Space, Loot Multiplier
- Camera FOV

Will add when needed (conversation systems, quest systems, faction systems)

---

## Files Modified

1. **ActorStatSO.cs**
   - Changed from list-based to inspector fields
   - Removed perceptionRangeBonus
   - Added all individual fields
   - Added GetBase() switch statement
   - Added behavior stats (disposition, aggressiveness)

2. **EnemySenses.cs**
   - Changed perception calculation:
     ```csharp
     // Old: float bonus = _stats.Stats.ActorStatSO.perceptionRangeBonus;
     // New: float perceptionBonus = _stats.Stats.GetStat(StatTypeEnum.Perception);
     ```

3. **ActorStats.cs**
   - Added public property: `public ActorStatSO ActorStatSO => _base;`

---

## Example: Bandit Enemy

**Stats:**
```
Brawn: 12       → Melee damage +6
Finesse: 11     → Penetration +16.5
Insight: 9      → Perception +45% (vision 29m, hearing 14.5m)
Focus: 10       → Crit chance +5%
Vigor: 14       → Max health +70 HP
Willpower: 8    → Max energy +24

Disposition: -1.0  (hostile)
Aggressiveness: 0.7 (tactical)
```

---

## Benefits

✅ Consistent with PlayerStatSO structure
✅ Perception tied to narrative stat (Insight makes sense)
✅ Easy to edit in Inspector (no lists)
✅ Scales smoothly (Insight 7 = 35%, Insight 15 = 75%)
✅ Ready for future systems (NPC disposition, AI tactics)

