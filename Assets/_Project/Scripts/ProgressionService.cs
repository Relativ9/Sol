using UnityEngine;

namespace Sol
{
    public class ProgressionService : IProgressionService
    {

        private int _level;
        private int _totalTalentPoints;
        private int _spentTalentPoints;
    
        public void Initialize(PlayerProgressionSO progressionSO, PlayerSaveData saveData)
        {
            _level = saveData.level > 0 ? saveData.level : progressionSO.startingLevel;
            _totalTalentPoints = saveData.level > 0 ? saveData.talentPoints : progressionSO.startingTalentPoints;
            _spentTalentPoints = saveData.spentTalentPoints;
        }
        public int GetLevel() => _level;
        public int GetTotalTalentPoints() => _totalTalentPoints;
        public int GetAvailableTalentPoints() => _totalTalentPoints - _spentTalentPoints;
    
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

    }
}

