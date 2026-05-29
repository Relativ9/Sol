using UnityEngine;
using System.Collections.Generic;
namespace Sol
{
    public class ProgressionService : IProgressionService
    {

        private int _level;
        private int _totalTalentPoints;
        private int _spentTalentPoints;
        private int _totalAttributePoints;
        
        private IStatsService _statsService;
        private PlayerStatSO _defaultStats;
        private Dictionary<StatTypeEnum, int> _attributeAllocations = new();

        
        public void Initialize(PlayerProgressionSO progressionSO, PlayerStatSO defaultStats, IStatsService statsService, int level, int totalTalentPoints, int totalAttributePoints, AttributeAllocationEntry[] attributeAllocations)
        {
            _statsService = statsService;
            _defaultStats = defaultStats;
            _level = level > 0 ? level : progressionSO.startingLevel;
            _totalTalentPoints = totalTalentPoints;
            _totalAttributePoints = totalAttributePoints;  // ADD
            _spentTalentPoints = 0;
            
            foreach (var entry in attributeAllocations ?? System.Array.Empty<AttributeAllocationEntry>())
            {
                _attributeAllocations[entry.statType] = entry.pointsAllocated;
            }
        }

        public float GetDefaultAttributeValue(StatTypeEnum statType)
        {
            return statType switch
            {
                StatTypeEnum.Brawn => _defaultStats.brawn,
                StatTypeEnum.Finesse => _defaultStats.finesse,
                StatTypeEnum.Insight => _defaultStats.insight,
                StatTypeEnum.Focus => _defaultStats.focus,
                StatTypeEnum.Vigor => _defaultStats.vigor,
                StatTypeEnum.Willpower => _defaultStats.willpower,

                _ => LogMissingDefaultAndReturnZero(statType)
            };
        }
        
        private float LogMissingDefaultAndReturnZero(StatTypeEnum statType)
        {
            Debug.LogWarning($"[{nameof(ProgressionService)}] No default value mapped for {statType} in PlayerStatSO. Returning 0.");
            return 0f;
        }
        
        public int GetLevel() => _level;
        public int GetTotalTalentPoints() => _totalTalentPoints;
        public int GetAvailableTalentPoints() => _totalTalentPoints - _spentTalentPoints;

        public void RebuildBaseStat(StatTypeEnum statType)
        {
            float defaultValue = GetDefaultAttributeValue(statType);
            int allocatedValue = _attributeAllocations.GetValueOrDefault(statType);
            _statsService.SetBaseStat(statType, defaultValue + allocatedValue);
        }

        public bool TrySpendAttributePoint(StatTypeEnum statType) //call this on level up attribute assignment
        {
            if (!IsCoreAttribute(statType))
            {
                Debug.LogError($"[{nameof(ProgressionService)}] {statType} is not a core attribute.");
                return false;
            }
            
            if (GetTotalSpentAttributePoints() >= _totalAttributePoints)
            {
                Debug.Log($"[{nameof(ProgressionService)}] No attribute points available.");
                return false;
            }
            
            int current = _attributeAllocations.GetValueOrDefault(statType);
            _attributeAllocations[statType] = current + 1;
            RebuildBaseStat(statType);
            return true;
        }
        
        public bool TryRefundAttributePoint(StatTypeEnum statType)
        {
            if (!IsCoreAttribute(statType))
            {
                Debug.LogWarning($"[{nameof(ProgressionService)}] Cannot refund: {statType} is not a core attribute.");
                return false;
            }

            int current = _attributeAllocations.GetValueOrDefault(statType);
            if (current <= 0)
            {
                Debug.Log($"[{nameof(ProgressionService)}] No points allocated in {statType} to refund.");
                return false;
            }

            _attributeAllocations[statType] = current - 1;
            RebuildBaseStat(statType);
            return true;
        }

        
        public int GetAvailableAttributePoints()
        {
            return _totalAttributePoints - GetTotalSpentAttributePoints();
        }
        
        public void ReloadAllBaseStats() //call on load game
        {
            foreach (var kvp in _attributeAllocations)
            {
                RebuildBaseStat(kvp.Key);
            }
        }
        
        public void SetSpentTalentPoints(int spent)
        {
            _spentTalentPoints = spent;
            Debug.Log($"[{nameof(ProgressionService)}] Synced spent points: {spent}. Available: {GetAvailableTalentPoints()}");
        }
    
        public bool SpendTalentPoint()
        {
            if (_spentTalentPoints >= _totalTalentPoints)
            {
                Debug.Log($"[{nameof(ProgressionService)}] No talent points available");
                return false;
            }
            _spentTalentPoints++;
            Debug.Log($"[{nameof(ProgressionService)}] Point spent. Remaining: {GetAvailableTalentPoints()}/{_totalTalentPoints}");
            return true;
        }

        public void RefundTalentPoint()
        {
            if (_spentTalentPoints <= 0) return;
            _spentTalentPoints--;
            Debug.Log($"[{nameof(ProgressionService)}] Point refunded. Remaining: {GetAvailableTalentPoints()}/{_totalTalentPoints}");
        }
        
        public void ResetAllTalentPoints()
        {
            if (_spentTalentPoints <= 0)
            {
                Debug.Log($"[{nameof(ProgressionService)}] No talent points spent to refund.");
                return;
            }
            _spentTalentPoints = 0;
            Debug.Log($"[{nameof(ProgressionService)}] All talent points refunded. Pool: {GetAvailableTalentPoints()}/{_totalTalentPoints}");
        }
        
        public void ResetAllAttributes()
        {
            _attributeAllocations.Clear();
            foreach (StatTypeEnum coreStat in GetCoreAttributes())
            {
                float defaultValue = GetDefaultAttributeValue(coreStat);
                _statsService.SetBaseStat(coreStat, defaultValue);
            }
            Debug.Log($"[{nameof(ProgressionService)}] Attributes reset. Available points: {GetAvailableAttributePoints()}/{_totalAttributePoints}");
        }
        
        // Helper Methods
        private int GetTotalSpentAttributePoints()
        {
            int sum = 0;
            foreach (var v in _attributeAllocations.Values)
            {
                sum += v;
            }
            return sum;
        }
        
        private bool IsCoreAttribute(StatTypeEnum statType)
        {
            return statType is StatTypeEnum.Brawn 
                or StatTypeEnum.Finesse 
                or StatTypeEnum.Insight 
                or StatTypeEnum.Focus 
                or StatTypeEnum.Vigor 
                or StatTypeEnum.Willpower;
        }
        
        private static IEnumerable<StatTypeEnum> GetCoreAttributes()
        {
            yield return StatTypeEnum.Brawn;
            yield return StatTypeEnum.Finesse;
            yield return StatTypeEnum.Insight;
            yield return StatTypeEnum.Focus;
            yield return StatTypeEnum.Vigor;
            yield return StatTypeEnum.Willpower;
        }
    }
}

