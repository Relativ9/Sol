using UnityEngine;

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

        [Header("Configuration - Services (MonoBehaviour)")]
        [SerializeField] private StatsService statsService;

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
            
            if (_saveService.SaveExists("default"))
            {
                saveData = _saveService.Load("default");
                Debug.Log($"[{nameof(GameInitializer)}] Loaded existing save");
            }
            else
            {
                saveData = PlayerSaveData.CreateNewGame(playerProgressionSO);
                Debug.Log($"[{nameof(GameInitializer)}] Created new game data");
            }
            
            // Core services
            statsService.Initialize(playerStatSO, saveData);
            _progressionService.Initialize(playerProgressionSO, saveData);
            
            // Talent system: Load layout data first, then hydrate state.
            // LoadData() is on ITalentTreeGenerator - pure position calculation, no UI.
            _talentTreeGenerator = ServiceLocator.Get<ITalentTreeGenerator>();
            _talentTreeGenerator?.LoadData();
            
            // Cast required because ITalentStateService intentionally does not expose 
            // Initialize(PlayerSaveData) - prevents accidental double-initialization 
            // by other systems. GameInitializer owns the one-and-only init call.
            var talentState = ServiceLocator.Get<ITalentStateService>() as TalentStateService;
            talentState?.Initialize(saveData);
            
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
