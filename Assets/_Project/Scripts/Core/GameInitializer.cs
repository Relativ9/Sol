using UnityEngine;
using UnityEngine.Serialization;

namespace Sol
{
    public class GameInitializer : MonoBehaviour
    {
        /// <summary>True while the save-repair prompt is waiting for the player. SaveManager checks this to block writes.</summary>
        public static bool IsSaveRepairPending { get; private set; }

        [Header("Configuration - Data Assets")]
        [SerializeField] private PlayerStatSO playerStatSO;
        [SerializeField] private PlayerProgressionSO playerProgressionSO;

        [Tooltip("The one shared damage type table. Every receiver (player, enemies) looks mitigation up through it.")]
        [SerializeField] private DamageTypeTableSO damageTypeTable;
        
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
        // The single PlayerVitals lives on the player object in the scene. Found in Start, not created here.
        private PlayerVitals _playerVitals;

        void Awake()
        {
            // CRITICAL: Register services immediately so other Awakes can find them.
            // TalentTreeGenerator (MonoBehaviour on [Services]) self-registers 
            // ITalentTreeGenerator and factories ITalentStateService in its own Awake.
            
            _saveService = new SaveService();
            ServiceLocator.RegisterService<ISaveService>(_saveService);
            
            
            _progressionService = new ProgressionService();
            ServiceLocator.RegisterService<IProgressionService>(_progressionService);

            // Single source of randomness for every chance roll (deflection, procs, crits, etc).
            ServiceLocator.RegisterService<IChanceRoller>(new ChanceRoller());

            // Shared damage type table. Without it, every hit is unmitigated, so say so once, clearly.
            if (damageTypeTable == null)
            {
                Debug.LogError($"[{nameof(GameInitializer)}] No DamageTypeTable assigned. Hits will take unmitigated damage.", this);
            }
            else
            {
                ServiceLocator.RegisterService<IDamageTypeTable>(damageTypeTable);
                ValidateDamageTypeTable(damageTypeTable);
            }


            // CombatLog registers itself as ICombatLog in its Awake.
            gameObject.AddComponent<CombatLog>();
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

                if (SaveValidator.IsSuspect(saveData, playerProgressionSO, out string reason))
                {
                    // Hold startup until the player decides. Services stay uninitialized and saves are blocked.
                    ShowRepairPrompt(saveData, reason);
                    return;
                }
            }

            InitializeGame(saveData);
        }

        private void ShowRepairPrompt(PlayerSaveData saveData, string reason)
        {
            IsSaveRepairPending = true;
            Debug.LogWarning($"[{nameof(GameInitializer)}] Suspect save: {reason}");

            // Deferred to the next frame: UIManager.Start() runs in an unspecified order relative to this Start(),
            // and it unregisters the document from the cursor. Showing now could be undone by that.
            StartCoroutine(ShowRepairPromptNextFrame(saveData, reason));
        }

        private System.Collections.IEnumerator ShowRepairPromptNextFrame(PlayerSaveData saveData, string reason)
        {
            yield return null;

            var uiManager = ServiceLocator.Get<IUIManager>();
            SaveRepairPrompt.Show(uiManager, reason, choice => OnRepairChoice(choice, saveData));
        }

        private void OnRepairChoice(SaveRepairPrompt.Choice choice, PlayerSaveData saveData)
        {
            IsSaveRepairPending = false;

            if (choice == SaveRepairPrompt.Choice.Repair)
            {
                // Keep everything the player has. Only the talent pool is corrected to the asset value.
                saveData.totalTalentPoints = playerProgressionSO.startingTalentPoints;
                _saveService.Save(saveData, "default");
                Debug.Log($"[{nameof(GameInitializer)}] Save repaired: talent points set to {saveData.totalTalentPoints}");
                ServiceLocator.Get<ICombatLog>()?.Add($"Save repaired: talent points set to {saveData.totalTalentPoints}", CombatLogKind.Warning);
            }
            else
            {
                // Keep the broken file as a backup, then start fresh.
                string backup = _saveService.Backup("default");
                saveData = PlayerSaveData.CreateNewGame(playerProgressionSO);
                _saveService.Save(saveData, "default");
                Debug.Log($"[{nameof(GameInitializer)}] New game started. Previous save backed up to {backup ?? "(none)"}");
                ServiceLocator.Get<ICombatLog>()?.Add("New game started. Previous save backed up.", CombatLogKind.Warning);
            }

            InitializeGame(saveData);
        }

        private void InitializeGame(PlayerSaveData saveData)
        {
            // Core services
            _statsService.Initialize(playerStatSO, saveData);
            // Vitals need StatsService seeded first (for max values).
            // Use the one PlayerVitals already on the player, so there is exactly one instance.
            _playerVitals = FindFirstObjectByType<PlayerVitals>();
            if (_playerVitals == null)
            {
                Debug.LogError($"[{nameof(GameInitializer)}] No PlayerVitals in the scene. Add it to the player object.");
            }
            _playerVitals?.Initialize(saveData, _statsService);
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
        
        // Checks the damage type table once at startup: every DamageType needs exactly one row.
        // A missing row silently skips mitigation for that type; a duplicate silently uses only the first.
        void ValidateDamageTypeTable(DamageTypeTableSO table)
        {
            var missing = new System.Collections.Generic.List<string>();
            foreach (DamageType type in table.MissingTypes())
                missing.Add(type.ToString());

            var counts = new System.Collections.Generic.Dictionary<DamageType, int>();
            foreach (var entry in table.entries)
            {
                if (entry == null) continue;
                counts[entry.type] = counts.TryGetValue(entry.type, out int n) ? n + 1 : 1;
            }
            var duplicated = new System.Collections.Generic.List<string>();
            foreach (var kvp in counts)
                if (kvp.Value > 1) duplicated.Add(kvp.Key.ToString());

            if (missing.Count > 0)
                Debug.LogError($"[{nameof(GameInitializer)}] DamageTypeTable has no row for: {string.Join(", ", missing)}. " +
                               "Those types take unmitigated damage. Use 'Fill missing types with defaults' on the table.", table);

            if (duplicated.Count > 0)
                Debug.LogError($"[{nameof(GameInitializer)}] DamageTypeTable has more than one row for: {string.Join(", ", duplicated)}. " +
                               "Only the first is used. Remove the extra rows.", table);
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
