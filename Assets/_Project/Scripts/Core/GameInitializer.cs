using UnityEngine;
using UnityEngine.Serialization;

namespace Sol
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Configuration - Data Assets")]
        [SerializeField] private PlayerStatSO playerStatSO;
        [SerializeField] private PlayerProgressionSO playerProgressionSO;
        
        // TreeLayoutCollectionSO and TalentTreeSettingsSO removed.
        // They now live as serialized fields on the TalentTreeGenerator 
        // MonoBehaviour under [Services], keeping this class free of 
        // talent-specific asset dependencies.

        [FormerlySerializedAs("statsService")]
        [Header("Configuration - Services (MonoBehaviour)")]
        [SerializeField] private StatsService _statsService;
        
        [SerializeField] private bool _forceNewGame; // Add this

        // Concrete references kept for initialization sequencing
        // (interfaces intentionally do not expose Initialize).
        private ISaveService _saveService;
        private IProgressionService _progressionService;
        private ITalentTreeGenerator _talentTreeGenerator;

        void Awake()
        {
            // CRITICAL: Register services immediately so other Awakes can find them.
            // TalentTreeGenerator (MonoBehaviour on [Services]) self-registers 
            // ITalentTreeGenerator and factories ITalentStateService in its own Awake.
            
            _saveService = new SaveService();
            ServiceLocator.RegisterService<ISaveService>(_saveService);
            
            
            _progressionService = new ProgressionService();
            ServiceLocator.RegisterService<IProgressionService>(_progressionService);
        }

        void Start()
        {
            PlayerSaveData saveData;
        
            // If forced, or no save exists, create new
            if (_forceNewGame || !_saveService.SaveExists("default"))
            {
                if (_forceNewGame && _saveService.SaveExists("default"))
                {
                    _saveService.Delete("default"); // Optional: wipe old file immediately
                }
            
                saveData = PlayerSaveData.CreateNewGame(playerProgressionSO);
                Debug.Log($"[{nameof(GameInitializer)}] Created new game data");
            }
            else
            {
                saveData = _saveService.Load("default");
                Debug.Log($"[{nameof(GameInitializer)}] Loaded existing save");
            }
            
            // Core services
            _statsService.Initialize(playerStatSO, saveData);
            //_progressionService.Initialize(playerProgressionSO, saveData);
            
            // 2. Init progression with TOTAL POOL ONLY
            //_progressionService.Initialize(playerProgressionSO, saveData.level, saveData.talentPoints);
            _progressionService.Initialize(playerProgressionSO, playerStatSO, _statsService, saveData.level, saveData.totalTalentPoints, saveData.totalAttributePoints, saveData.attributeAllocations);
            _progressionService.ReloadAllBaseStats();
            // Talent system: Load layout data first, then hydrate state.
            // LoadData() is on ITalentTreeGenerator - pure position calculation, no UI.
            _talentTreeGenerator = ServiceLocator.Get<ITalentTreeGenerator>();
            _talentTreeGenerator?.LoadData();
            
            // Cast required because ITalentStateService intentionally does not expose 
            // Initialize(PlayerSaveData) - prevents accidental double-initialization 
            // by other systems. GameInitializer owns the one-and-only init call.
            var talentState = ServiceLocator.Get<ITalentStateService>() as TalentStateService;
            talentState?.Initialize(saveData, _statsService);
            
            int derivedSpent = talentState?.GetTotalAllocatedPoints() ?? 0;
            _progressionService.SetSpentTalentPoints(derivedSpent);
            // SaveManager auto-save setup:
            // Add a GameEventListener MonoBehaviour to your SaveManager GameObject.
            // Drag the same GameEvent SO assigned to TalentTreeGenerator's 
            // "_talentStateChangedEvent" field into the listener's event slot, 
            // and wire its UnityEvent to SaveManager.QueueSave().
        }
        
        [ContextMenu("Delete Save (Debug)")]
        public void DeleteSaveDebug()
        {
            SaveService saveService = new SaveService();
            saveService.Delete("default");
            Debug.Log($"[{nameof(GameInitializer)}] Save deleted. Restart to create new game.");
        }
    }
}
