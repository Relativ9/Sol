using UnityEngine;

namespace Sol
{
    public class SaveManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float debounceInterval = 2f;
        
        private ISaveService _saveService;
        private IProgressionService _progressionService;
        private ITalentStateService _talentStateService;
        private float lastSaveTime;
        private bool pendingSave;
        

        void Start()
        {
            _saveService = ServiceLocator.Get<ISaveService>();
            _progressionService = ServiceLocator.Get<IProgressionService>();
            _talentStateService = ServiceLocator.Get<ITalentStateService>();
            
            if (_talentStateService == null)
                Debug.LogWarning($"[{nameof(SaveManager)}] TalentTreeController not found in ServiceLocator");
        }
        
        // This is called by GameEventListener (configured in Inspector)
        public void OnGameStateChanged()
        {
            if (Time.time - lastSaveTime < debounceInterval)
            {
                if (!pendingSave)
                {
                    pendingSave = true;
                    float delay = debounceInterval - (Time.time - lastSaveTime);
                    Invoke(nameof(PerformSave), delay);
                }
                return;
            }
            
            PerformSave();
        }
        
        void PerformSave()
        {
            pendingSave = false;
            lastSaveTime = Time.time;
            
            PlayerSaveData data = BuildSaveData();
            _saveService.Save(data, "default");
            
            Debug.Log($"[{nameof(SaveManager)}] Game saved");
        }
        
        PlayerSaveData BuildSaveData()
        {
            var data = new PlayerSaveData
            {
                level = _progressionService.GetLevel(),
                selectedTalentIds = _talentStateService?.GetAllocatedNodeIds() ?? new string[0]
            };
            
            int total = _progressionService.GetTotalTalentPoints();
            int available = _progressionService.GetAvailableTalentPoints();
            data.spentTalentPoints = total - available;
            
            return data;
        }
    }
}
