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

        [Tooltip("Every item asset the game can produce. Saves store item IDs, and these assets resolve them on load. " +
                 "Add every new item here, or it cannot be restored.")]
        [SerializeField] private ItemDataSO[] itemDatabase = System.Array.Empty<ItemDataSO>();

        [Tooltip("Build-time object index table, at Assets/_Project/Systems/WorldObjectRegistry.asset. Used by cell files.")]
        [SerializeField] private WorldObjectRegistry worldObjectRegistry;

        [Tooltip("Build-time item type index table, at Assets/_Project/Systems/ItemTypeRegistry.asset. Used by cell files.")]
        [SerializeField] private ItemTypeRegistry itemTypeRegistry;
        
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

            // World cell store and streamer. Created here, before any Start, so placed items can register with the streamer as they
            // wake. The streamer reads nothing until the save system points it at a slot (see InitializeWorldStore).
            _cellStore = new CellStore();
            ServiceLocator.RegisterService<CellStore>(_cellStore);
            if (worldObjectRegistry == null || itemTypeRegistry == null)
                Debug.LogError($"[{nameof(GameInitializer)}] World object or item type registry is not assigned. World state will not be saved.", this);
            else
                ServiceLocator.RegisterService<IWorldCellWriter>(new WorldCellWriter(_cellStore, worldObjectRegistry, itemTypeRegistry));
            _worldStreamer = gameObject.AddComponent<WorldStreamer>();
            _worldStreamer.Store = _cellStore;
            ServiceLocator.RegisterService<IWorldStreamer>(_worldStreamer);

            // Tracks physics-driven world items, so their resting and in-flight poses reach the cell store.
            _motionTracker = gameObject.AddComponent<WorldMotionTracker>();
            ServiceLocator.RegisterService<IWorldMotionTracker>(_motionTracker);
        }

        private CellStore _cellStore;
        private WorldStreamer _worldStreamer;
        private WorldMotionTracker _motionTracker;

        void Start()
        {
            PlayerSaveData saveData;
        
            // If forced, or no save exists, create new
            string slot = PendingLoad.Consume(_saveService);
            _activeSlot = slot;
            if (_forceNewGame || !_saveService.SaveExists(slot))
            {
                // A forced new game clears the slot's player file and its world cells. Delete is safe when nothing exists.
                if (_forceNewGame) _saveService.Delete(slot);
            
                saveData = PlayerSaveData.CreateNewGame(playerProgressionSO);
                Debug.Log($"[{nameof(GameInitializer)}] Created new game data");
            }
            else
            {
                saveData = _saveService.Load(slot);

                // A save that exists but cannot be read is never replaced silently. It goes to the repair prompt, which keeps the file.
                if (saveData == null)
                {
                    ShowUnreadablePrompt(slot);
                    return;
                }
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

        // An unreadable save is not repaired: there is no data to correct. The player chooses between starting over (the file
        // is backed up first) and keeping the file untouched. Either way, startup stays blocked until the choice is made.
        private void ShowUnreadablePrompt(string slot)
        {
            IsSaveRepairPending = true;
            Debug.LogError($"[{nameof(GameInitializer)}] Save slot '{slot}' exists but could not be read.");
            StartCoroutine(ShowUnreadablePromptNextFrame(slot));
        }

        private System.Collections.IEnumerator ShowUnreadablePromptNextFrame(string slot)
        {
            yield return null;

            var uiManager = ServiceLocator.Get<IUIManager>();
            SaveRepairPrompt.ShowUnreadable(uiManager,
                "The save file exists but could not be read.",
                choice => OnUnreadableChoice(choice, slot));
        }

        private void OnUnreadableChoice(SaveRepairPrompt.Choice choice, string slot)
        {
            if (choice == SaveRepairPrompt.Choice.KeepFile)
            {
                // Nothing is written. The game starts in memory only, and the file stays exactly as it was. Saves stay blocked
                // so the unreadable file is never overwritten during this session.
                IsSaveRepairPending = true;
                Debug.LogWarning($"[{nameof(GameInitializer)}] Slot '{slot}' kept unchanged. Saves are blocked for this session.");
                ServiceLocator.Get<ICombatLog>()?.Add("Save file kept unchanged. Saving is disabled this session.", CombatLogKind.Warning);
                InitializeGame(PlayerSaveData.CreateNewGame(playerProgressionSO));
                return;
            }

            // Start new game: the unreadable file is backed up under its own slot name before anything is written.
            string backup = _saveService.Backup(slot);
            if (backup == null)
            {
                // The file could not be copied, so it is not replaced. Nothing is lost, and saves stay blocked.
                Debug.LogError($"[{nameof(GameInitializer)}] Backup of '{slot}' failed. The file was not replaced.");
                IsSaveRepairPending = true;
                InitializeGame(PlayerSaveData.CreateNewGame(playerProgressionSO));
                return;
            }

            IsSaveRepairPending = false;
            PlayerSaveData fresh = PlayerSaveData.CreateNewGame(playerProgressionSO);
            if (ServiceLocator.TryGet<ISaveManager>()?.WriteRepair(fresh, promoteToManual: true) != true)
                Debug.LogError($"[{nameof(GameInitializer)}] New game could not be written to disk.");
            Debug.Log($"[{nameof(GameInitializer)}] New game started. Unreadable save backed up to {backup}");
            ServiceLocator.Get<ICombatLog>()?.Add("New game started. Unreadable save backed up.", CombatLogKind.Warning);
            InitializeGame(fresh);
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
                // Written through the save manager, so the coordinator stays the only writer. The player chose to keep this
                // save, so the repaired copy is promoted to the manual save used on the next boot.
                if (ServiceLocator.TryGet<ISaveManager>()?.WriteRepair(saveData, promoteToManual: true) != true)
                    Debug.LogError($"[{nameof(GameInitializer)}] Save repair could not be written. The save file was not changed.");
                Debug.Log($"[{nameof(GameInitializer)}] Save repaired: talent points set to {saveData.totalTalentPoints}");
                ServiceLocator.Get<ICombatLog>()?.Add($"Save repaired: talent points set to {saveData.totalTalentPoints}", CombatLogKind.Warning);
            }
            else
            {
                // Keep the file being replaced as a backup, then start fresh. The backup is of the slot that was loaded, not the repair slot.
                string backup = _saveService.Backup(_activeSlot);
                saveData = PlayerSaveData.CreateNewGame(playerProgressionSO);
                // The player chose a fresh start, so the new game becomes the manual save used on the next boot.
                if (ServiceLocator.TryGet<ISaveManager>()?.WriteRepair(saveData, promoteToManual: true) != true)
                    Debug.LogError($"[{nameof(GameInitializer)}] New game could not be written to disk.");
                Debug.Log($"[{nameof(GameInitializer)}] New game started. Previous save backed up to {backup ?? "(none)"}");
                ServiceLocator.Get<ICombatLog>()?.Add("New game started. Previous save backed up.", CombatLogKind.Warning);
            }

            InitializeGame(saveData);
        }

        // Moves the player to the saved position and facing. The Rigidbody is moved through its own API and its velocity is
        // cleared, so the player lands cleanly instead of carrying an old fall or a frozen state into the new position.
        private void RestorePlayerPose(PlayerSaveData saveData)
        {
            if (saveData == null || !saveData.hasPosition) return;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            Vector3 position = new Vector3(saveData.posX, saveData.posY, saveData.posZ);
            Quaternion rotation = Quaternion.Euler(0f, saveData.yawDegrees, 0f);

            var body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = position;
                body.rotation = rotation;
            }
            else
            {
                player.transform.SetPositionAndRotation(position, rotation);
            }
        }

        // The slot that startup read. Recorded once, so the world loads from the same slot as the player file.
        private string _activeSlot = SaveCoordinator.ManualSlot;

        // Points the streamer at a slot's cell files and loads them as the baseline. Nothing is applied here; the streamer applies
        // cells around the player as it runs. A slot with no cell files simply has no world changes.
        private void LoadWorldForSlot(string slot)
        {
            if (_cellStore == null || _worldStreamer == null) return;

            _worldStreamer.SetCellsFolder(SaveManager.SlotCellsFolder(slot));

            // The streamer follows the player. Without it the ring is never computed and no cell is read or applied.
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null)
                Debug.LogError($"[{nameof(GameInitializer)}] No PlayerController in the scene. World cells will not stream.", this);
            else
                _worldStreamer.SetPlayer(player.transform);
            ServiceLocator.TryGet<ISaveManager>()?.SetPlayerTransform(player.transform);

            _worldStreamer.ObjectRegistry = worldObjectRegistry;
            _worldStreamer.Spawner = new DroppedPileSpawner(itemTypeRegistry, new ItemRegistry(itemDatabase).Resolve);
            _activeSlot = slot;
        }

        private void InitializeGame(PlayerSaveData saveData)
        {
            // Core services
            _statsService.Initialize(playerStatSO, saveData);
            //_progressionService.Initialize(playerProgressionSO, saveData);
            
            // 2. Init progression with TOTAL POOL ONLY
            //_progressionService.Initialize(playerProgressionSO, saveData.level, saveData.talentPoints);
            _progressionService.Initialize(playerProgressionSO, playerStatSO, _statsService, saveData.level, saveData.totalTalentPoints, saveData.totalAttributePoints, saveData.attributeAllocations);
            // Attribute rules must run before vitals are seeded. Derived stats have no SO base, so MaxHealth, MaxEnergy,
            // MaxHunger and MaxThirst are only correct once the rules have applied.
            _progressionService.ReloadAllBaseStats();

            // Vitals need the derived maxima, so they are seeded after the rules above.
            // Use the one PlayerVitals already on the player, so there is exactly one instance.
            _playerVitals = FindFirstObjectByType<PlayerVitals>();
            if (_playerVitals == null)
            {
                Debug.LogError($"[{nameof(GameInitializer)}] No PlayerVitals in the scene. Add it to the player object.");
            }
            _playerVitals?.Initialize(saveData, _statsService);

            // Equipment owns what is worn. It writes Equipment modifiers, and re-derives armour stats through the
            // one progression resolver after every change. Worn items are restored from the save through RestoreInventory.
            ServiceLocator.RegisterService<IEquipmentService>(
                new EquipmentService(_statsService, _progressionService.RecalculateAttributeRules));

            // Stored items. The grid is sized from InventorySpace (total cells), so the tab is never a fixed size.
            int cells = Mathf.RoundToInt(_statsService.GetStat(StatTypeEnum.InventorySpace));
            ServiceLocator.RegisterService<IInventoryService>(new InventoryService(cells));

            // Restore items from the save: stored items to their cells, worn items back into their slots. Worn items
            // are re-equipped through EquipmentService, so their stat modifiers are rebuilt the normal way.
            RestoreInventory(saveData);

            // Put the player back where it was saved. Done here, after the world is set up, so the spawn is not overwritten.
            RestorePlayerPose(saveData);

            // World state comes from this slot's cell files. The store is loaded for the slot, and the streamer then applies the cells
            // around the player: taken placed items are removed, moved ones are placed, and dropped piles are respawned.
            LoadWorldForSlot(_activeSlot);

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
            // Saves are not triggered by changes. SaveManager writes on quicksave (F5), autosave, or Save and Quit.
        }
        
        // Checks the damage type table once at startup: every DamageType needs exactly one row.
        // A missing row silently skips mitigation for that type; a duplicate silently uses only the first.
        // Puts saved items back. Worn items are equipped first, so their weight and piece counts are applied, then
        // stored items are placed at their saved cells. A stored item whose cell no longer fits goes to the nearest
        // free spot and is logged, so it is never silently lost. An item that no longer exists is skipped with a warning.
        private void RestoreInventory(PlayerSaveData saveData)
        {
            if (saveData == null) return;

            IInventoryService inventory = ServiceLocator.TryGet<IInventoryService>();
            IEquipmentService equipment = ServiceLocator.TryGet<IEquipmentService>();
            if (inventory == null || equipment == null) return;

            if (itemDatabase == null || itemDatabase.Length == 0)
                Debug.LogWarning($"[{nameof(GameInitializer)}] Item database is empty. Saved items cannot be restored until it is filled.", this);

            var registry = new ItemRegistry(itemDatabase);

            if (saveData.equipped != null)
            {
                foreach (EquippedSaveEntry entry in saveData.equipped)
                {
                    ItemDataSO item = registry.Resolve(entry.itemId);
                    if (item is not EquipmentDataSO piece) continue;
                    if (!System.Enum.TryParse(entry.slot, out EquipmentSlot slot)) continue;

                    if (!equipment.TryEquip(entry.instanceId, piece, slot))
                        Debug.LogWarning($"[{nameof(GameInitializer)}] Could not re-equip '{entry.itemId}' into {slot}.", this);
                }
            }

            if (saveData.inventory != null)
            {
                // Leads first. A lead takes a cell, and the members then join its stack. A lone item or a legacy save
                // entry has no lead and is its own lead.
                foreach (InventorySaveEntry entry in saveData.inventory)
                {
                    if (!string.IsNullOrEmpty(entry.stackLeadId) && entry.stackLeadId != entry.instanceId) continue;
                    RestoreLead(inventory, registry, entry);
                }

                foreach (InventorySaveEntry entry in saveData.inventory)
                {
                    if (string.IsNullOrEmpty(entry.stackLeadId) || entry.stackLeadId == entry.instanceId) continue;

                    ItemDataSO item = registry.Resolve(entry.itemId);
                    if (item == null) continue;

                    if (!inventory.TryJoinStack(entry.stackLeadId, item, entry.instanceId))
                        Debug.LogWarning($"[{nameof(GameInitializer)}] '{entry.itemId}' could not rejoin its stack. It was not loaded.", this);
                }
            }
        }

        // Stores a lead at its saved cell, falling back to the nearest free spot if that cell is now blocked.
        private void RestoreLead(IInventoryService inventory, ItemRegistry registry, InventorySaveEntry entry)
        {
            ItemDataSO item = registry.Resolve(entry.itemId);
            if (item == null) return;

            if (!inventory.TryStore(entry.instanceId, item))
            {
                Debug.LogWarning($"[{nameof(GameInitializer)}] No room to restore '{entry.itemId}'. It was not loaded.", this);
                return;
            }

            if (!inventory.TryMove(entry.instanceId, entry.x, entry.y, entry.rotated))
                Debug.LogWarning($"[{nameof(GameInitializer)}] '{entry.itemId}' could not return to its saved cell ({entry.x},{entry.y}). It is at the nearest free spot.", this);
        }

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
            // Every slot is cleared, so a fresh boot cannot pick up an old quicksave or autosave.
            SaveService saveService = new SaveService();
            saveService.Delete(SaveCoordinator.ManualSlot);
            saveService.Delete(SaveCoordinator.QuickSlot);
            saveService.Delete(SaveCoordinator.AutoSlot);
            saveService.Delete(SaveCoordinator.RepairSlot);
            Debug.Log($"[{nameof(GameInitializer)}] All save slots deleted. Restart to create new game.");
        }

    }
}
