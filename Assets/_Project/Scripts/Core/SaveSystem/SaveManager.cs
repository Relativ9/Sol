using System;
using UnityEngine;

namespace Sol
{
    public class SaveManager : MonoBehaviour, ISaveManager
    {
        [Header("Settings")]
        [SerializeField] private float debounceInterval = 2f;
        
        private ISaveService _saveService;
        private IProgressionService _progressionService;
        private ITalentStateService _talentStateService;
        private Coroutine _pendingSaveCoroutine;
        
        private float lastSaveTime;
        private bool pendingSave;

        private void Awake()
        {
            ServiceLocator.RegisterService<ISaveManager>(this);
        }

        void Start()
        {
            _saveService = ServiceLocator.Get<ISaveService>();
            _progressionService = ServiceLocator.Get<IProgressionService>();
            _talentStateService = ServiceLocator.Get<ITalentStateService>();
            
            if (_talentStateService == null)
                Debug.LogWarning($"[{nameof(SaveManager)}] TalentTreeController not found in ServiceLocator");
        }
        

        public void RequestLazySave()
        {
            if (_pendingSaveCoroutine != null) return; // Already waiting
    
            float elapsed = Time.unscaledTime - lastSaveTime;
            if (elapsed < debounceInterval)
            {
                float wait = debounceInterval - elapsed;
                _pendingSaveCoroutine = StartCoroutine(SaveAfterDelay(wait));
            }
            else
            {
                RequestImmediateSave();
            }
        }

        public void RequestImmediateSave()
        {
            if (_pendingSaveCoroutine != null)
            {
                StopCoroutine(_pendingSaveCoroutine);
                _pendingSaveCoroutine = null;
            }
            pendingSave = false;
            PerformSave();
        }

        
        void PerformSave()
        {
            // Never overwrite a suspect save while the player is still choosing how to repair it.
            if (GameInitializer.IsSaveRepairPending)
            {
                Debug.LogWarning($"[{nameof(SaveManager)}] Save skipped: save repair prompt is open.");
                return;
            }

            pendingSave = false;
            lastSaveTime = Time.unscaledTime;
            
            PlayerSaveData data = BuildSaveData();
            _saveService.Save(data, "default");
            
            Debug.Log($"[{nameof(SaveManager)}] Game saved");
        }
        
        PlayerSaveData BuildSaveData()
        {
            var entries = new System.Collections.Generic.List<TalentAllocationEntry>();
    
            if (_talentStateService != null)
            {
                foreach (var nodeId in _talentStateService.GetAllocatedNodeIds())
                {
                    int pts = _talentStateService.GetAllocatedPoints(nodeId);
                    if (pts > 0)
                        entries.Add(new TalentAllocationEntry { nodeId = nodeId, allocatedPoints = pts });
                }
            }
            int available = _progressionService.GetAvailableTalentPoints();
            return new PlayerSaveData
            {
                level = _progressionService.GetLevel(),
                totalTalentPoints = _progressionService.GetTotalTalentPoints(),
                // Derived on load, but written so the file reflects the real pool.
                totalAttributePoints = _progressionService.GetTotalAttributePoints(),
                talentAllocations = entries.ToArray(),
                attributeAllocations = BuildAttributeEntries()
            };
        }

        // Only attributes with spent points are written.
        AttributeAllocationEntry[] BuildAttributeEntries()
        {
            var list = new System.Collections.Generic.List<AttributeAllocationEntry>();
            foreach (StatTypeEnum attr in new[]
                     { StatTypeEnum.Brawn, StatTypeEnum.Finesse, StatTypeEnum.Insight,
                       StatTypeEnum.Focus, StatTypeEnum.Vigor, StatTypeEnum.Willpower })
            {
                int points = _progressionService.GetAllocatedAttributePoints(attr);
                if (points > 0)
                    list.Add(new AttributeAllocationEntry { statType = attr, pointsAllocated = points });
            }
            return list.ToArray();
        }
        
        
        System.Collections.IEnumerator SaveAfterDelay(float delay)
        {
            pendingSave = true;
            yield return new WaitForSecondsRealtime(delay);
            _pendingSaveCoroutine = null;
            PerformSave();
        }
    }
}
