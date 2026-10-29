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
            _worldStreamer = gameObject.AddComponent<WorldStreamer>();
            _worldStreamer.Store = _cellStore;
            ServiceLocator.RegisterService<IWorldStreamer>(_worldStreamer);
        }

        private CellStore _cellStore;
        private WorldStreamer _worldStreamer;

        void Start()
        {
            PlayerSaveData saveData;
        
            // If forced, or no save exists, create new
            string slot = PendingLoad.Consume();
            if (_forceNewGame || !_saveService.SaveExists(slot))
            {
                if (_forceNewGame && _saveService.SaveExists(slot))
                {
                    _saveService.Delete(slot); // Optional: wipe old file immediately
                }
            
                saveData = PlayerSaveData.CreateNewGame(playerProgressionSO);
                Debug.Log($"[{nameof(GameInitializer)}] Created new game data");
            }
            else
            {
                saveData = _saveService.Load(slot);
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
                _saveService.Save(saveData, SaveCoordinator.RepairSlot);
                // The player chose to keep this save, so the repaired copy becomes the manual save used on the next boot.
                _saveService.Save(saveData, SaveCoordinator.ManualSlot);
                Debug.Log($"[{nameof(GameInitializer)}] Save repaired: talent points set to {saveData.totalTalentPoints}");
                ServiceLocator.Get<ICombatLog>()?.Add($"Save repaired: talent points set to {saveData.totalTalentPoints}", CombatLogKind.Warning);
            }
            else
            {
                // Keep the broken file as a backup, then start fresh.
                string backup = _saveService.Backup(SaveCoordinator.RepairSlot);
                saveData = PlayerSaveData.CreateNewGame(playerProgressionSO);
                _saveService.Save(saveData, SaveCoordinator.RepairSlot);
                // The player chose a fresh start, so the new game becomes the manual save used on the next boot.
                _saveService.Save(saveData, SaveCoordinator.ManualSlot);
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

            // Equipment owns what is worn. It writes Equipment modifiers, and re-derives armour stats through the
            // one progression resolver after every change. Equipped items are not restored from save yet (Phase 8b).
            ServiceLocator.RegisterService<IEquipmentService>(
                new EquipmentService(_statsService, _progressionService.RecalculateDerived));

            // Stored items. The grid is sized from InventorySpace (total cells), so the tab is never a fixed size.
            int cells = Mathf.RoundToInt(_statsService.GetStat(StatTypeEnum.InventorySpace));
            ServiceLocator.RegisterService<IInventoryService>(new InventoryService(cells));

            // Restore items from the save: stored items to their cells, worn items back into their slots. Worn items
            // are re-equipped through EquipmentService, so their stat modifiers are rebuilt the normal way.
            RestoreInventory(saveData);

            // World items: which placed items were picked up, and which dropped items lie in the world.
            var worldItems = new WorldItemService();
            ServiceLocator.RegisterService<IWorldItemService>(worldItems);
            worldItems.Restore(saveData.pickedUpItemInstanceIds, saveData.droppedItemInstances);

            // Anything the player already holds (stored or worn) must not also be lying in the world.
            var held = new System.Collections.Generic.List<string>();
            if (saveData.inventory != null)
                foreach (InventorySaveEntry entry in saveData.inventory) held.Add(entry.instanceId);
            if (saveData.equipped != null)
                foreach (EquippedSaveEntry entry in saveData.equipped) held.Add(entry.instanceId);
            WorldItem.ApplySavedState(worldItems, held);

            // Dropped piles lie in the world until picked up. Each one is respawned where it was saved, with its own ID.
            RespawnDroppedPiles(worldItems, new ItemRegistry(itemDatabase));

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

        // Recreates every dropped pile from the world record. A pile whose item no longer exists, or whose world model is
        // not set, is skipped with a warning and left in the record, so it is not lost from the save.
        private void RespawnDroppedPiles(IWorldItemService world, ItemRegistry registry)
        {
            foreach (WorldDropEntry drop in world.GetDropped())
            {
                ItemDataSO item = registry.Resolve(drop.itemId);
                if (item == null)
                {
                    Debug.LogWarning($"[{nameof(GameInitializer)}] Dropped item '{drop.itemId}' no longer exists. It was not respawned.", this);
                    continue;
                }

                GameObject prefab = item.WorldPrefab;
                if (prefab == null || prefab.GetComponent<WorldItem>() == null)
                {
                    Debug.LogWarning($"[{nameof(GameInitializer)}] '{item.DisplayName}' has no world model. It was not respawned.", this);
                    continue;
                }

                GameObject spawned = Instantiate(prefab, drop.Position, drop.Rotation);
                spawned.GetComponent<WorldItem>().Assign(item, drop.itemInstanceId, true);
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
