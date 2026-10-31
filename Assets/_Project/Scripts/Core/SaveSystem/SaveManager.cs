using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    public class SaveManager : MonoBehaviour, ISaveManager
    {
        [Header("Autosave")]
        [Tooltip("Unpaused play time between autosaves, in seconds. 600 is ten minutes.")]
        [SerializeField] private float _autosaveIntervalSeconds = 600f;

        private ISaveService _saveService;
        private IProgressionService _progressionService;
        private ITalentStateService _talentStateService;
        private IPlayerVitals _vitals;
        private CellStore _cellStore;

        // Saves are requested through the coordinator, which runs one at a time. Autosave counts unpaused play time.
        private SaveCoordinator _coordinator;
        private AutosaveTimer _autosave;

        private void Awake()
        {
            ServiceLocator.RegisterService<ISaveManager>(this);

            // Built here, before anything can request a save. Autosave is allowed unless something says otherwise.
            _coordinator = new SaveCoordinator(WriteSave, CanAutosave);
            _autosave = new AutosaveTimer(_coordinator, _autosaveIntervalSeconds);
        }

        // The single gate for autosave. It returns true today. Combat, cutscenes or a save-repair prompt can block autosave
        // here later, without changing the timer or the coordinator.
        private bool CanAutosave()
        {
            return !GameInitializer.IsSaveRepairPending;
        }

        // Autosave ticks against unpaused play time only. The pause state comes from TimeManager.
        // Quicksave is read here, since it is a save action and SaveManager owns saves.
        void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.f5Key.wasPressedThisFrame)
                RequestSave(SaveReason.Quicksave);

            bool paused = ServiceLocator.TryGet<ITimeManager>()?.IsPaused ?? false;
            _autosave.Tick(Time.unscaledDeltaTime, paused);
        }

        /// <summary>Requests a save of the given kind. Gameplay does not call this on each change.</summary>
        public bool RequestSave(SaveReason reason)
        {
            return _coordinator.RequestSave(reason);
        }

        // The repair payload waiting for the coordinator to write it. Set only for the length of one repair request.
        private PlayerSaveData _pendingRepair;
        private bool _promoteRepair;

        /// <summary>
        /// Writes a repair choice. It runs through the coordinator like every other save, so there is one writer and one place
        /// that decides whether a write is allowed. Repairs ignore the repair-pending block, since that block is what the
        /// choice resolves.
        /// </summary>
        public bool WriteRepair(PlayerSaveData data, bool promoteToManual)
        {
            if (data == null) return false;
            _pendingRepair = data;
            _promoteRepair = promoteToManual;
            bool ok = _coordinator.RequestSave(SaveReason.Repair);
            _pendingRepair = null;
            return ok;
        }

        void Start()
        {
            _saveService = ServiceLocator.Get<ISaveService>();
            _progressionService = ServiceLocator.Get<IProgressionService>();
            _talentStateService = ServiceLocator.Get<ITalentStateService>();
            _vitals = ServiceLocator.TryGet<IPlayerVitals>();
            _cellStore = ServiceLocator.TryGet<CellStore>();
            
            if (_talentStateService == null)
                Debug.LogWarning($"[{nameof(SaveManager)}] TalentTreeController not found in ServiceLocator");
        }
        

        // Changes no longer trigger a save. Inventory, equipment and the world only change state here, and the next quicksave,
        // autosave, or Save and Quit writes it. Nothing is subscribed to, so nothing can save on a change.
        private bool WriteSave(SaveReason reason)
        {
            if (reason == SaveReason.Repair) return PerformRepair();
            return PerformSave(SlotFor(reason));
        }

        // A repair always writes the repair slot, and writes the manual slot too only when the player chose to keep it.
        private bool PerformRepair()
        {
            if (_pendingRepair == null || _saveService == null) return false;

            if (!_saveService.Save(_pendingRepair, SaveCoordinator.RepairSlot)) return false;
            if (_promoteRepair && !_saveService.Save(_pendingRepair, SaveCoordinator.ManualSlot)) return false;
            Debug.Log($"[{nameof(SaveManager)}] Repair written. Promoted to manual: {_promoteRepair}");
            return true;
        }

        // Quicksave and autosave go to their own slots. Save and Quit writes the manual slot.
        private static string SlotFor(SaveReason reason)
        {
            switch (reason)
            {
                case SaveReason.Quicksave: return SaveCoordinator.QuickSlot;
                case SaveReason.Autosave: return SaveCoordinator.AutoSlot;
                case SaveReason.ManualSave:
                case SaveReason.SaveAndQuit:
                default: return SaveCoordinator.ManualSlot;
            }
        }

        // A slot's cell files sit beside its player file. The path is defined once in SaveLocation, and deleting a slot removes both.
        public static string SlotCellsFolder(string slot) => SaveLocation.CellsFolder(slot);

        // Writes one slot. Returns false, and writes nothing, while the save-repair prompt is open, so a suspect save is never
        // overwritten. The player file is written here. World cells are written by the cell store in a later phase.
        private bool PerformSave(string slot)
        {
            if (GameInitializer.IsSaveRepairPending)
            {
                Debug.LogWarning($"[{nameof(SaveManager)}] Save skipped: save repair prompt is open.");
                return false;
            }

            // The player file goes first. If it does not reach disk, the world cells are not written either, so the slot
            // never holds world state that is newer than its player file.
            PlayerSaveData data = BuildSaveData();
            if (!_saveService.Save(data, slot)) return false;

            // World state is written per slot: only the cells this slot has not yet written. The player file is written first, so
            // a slot never has a player file that is newer than its world cells are allowed to be.
            if (_cellStore != null)
            {
                // Live poses first, so a save taken mid-flight records where the bodies are on this frame.
                ServiceLocator.TryGet<IWorldMotionTracker>()?.FlushLive();
                int cells = _cellStore.FlushSlot(slot, SlotCellsFolder(slot));
                Debug.Log($"[{nameof(SaveManager)}] Wrote {cells} world cell(s) to slot '{slot}'");
            }

            Debug.Log($"[{nameof(SaveManager)}] Game saved to slot '{slot}'");
            return true;
        }
        
        PlayerSaveData BuildSaveData()
        {
            var entries = new List<TalentAllocationEntry>();
    
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
                attributeAllocations = BuildAttributeEntries(),
                // Vitals were previously dropped on every save. Written here so a reload restores them.
                currentHealth = _vitals != null && _vitals.IsInitialized ? _vitals.CurrentHealth : 0f,
                currentEnergy = _vitals != null && _vitals.IsInitialized ? _vitals.CurrentEnergy : 0f,
                equipped = BuildEquippedEntries(),
                inventory = BuildInventoryEntries(),
                hasPosition = true,
                posX = _playerTransform != null ? _playerTransform.position.x : 0f,
                posY = _playerTransform != null ? _playerTransform.position.y : 0f,
                posZ = _playerTransform != null ? _playerTransform.position.z : 0f,
                yawDegrees = _playerTransform != null ? _playerTransform.eulerAngles.y : 0f,
            };
        }

        /// <summary>The player's transform, set by GameInitializer so the save can record where the player stands.</summary>
        public void SetPlayerTransform(Transform player) => _playerTransform = player;

        private Transform _playerTransform;

        // One entry per occupied slot. Saved by slot name, so reordering the enum cannot move an item.
        EquippedSaveEntry[] BuildEquippedEntries()
        {
            var list = new List<EquippedSaveEntry>();

            // Looked up at save time, not cached at Start, so the order of service registration cannot drop worn items.
            // A missing service is reported, so an empty list is never written silently.
            IEquipmentService equipment = ServiceLocator.TryGet<IEquipmentService>();
            if (equipment == null)
            {
                Debug.LogError($"[{nameof(SaveManager)}] IEquipmentService is not registered. Worn items were not saved.");
                return list.ToArray();
            }

            foreach (var pair in equipment.GetAllEquipped())
            {
                string instanceId = equipment.GetEquippedInstanceId(pair.Key);
                if (string.IsNullOrEmpty(instanceId) || pair.Value == null) continue;
                if (string.IsNullOrEmpty(pair.Value.ItemId))
                {
                    Debug.LogError($"[{nameof(SaveManager)}] Equipped item '{pair.Value.name}' has no ItemId and was not saved. Set _itemId on its asset.", pair.Value);
                    continue;
                }
                list.Add(new EquippedSaveEntry
                {
                    slot = pair.Key.ToString(),
                    itemId = pair.Value.ItemId,
                    instanceId = instanceId,
                });
            }
            return list.ToArray();
        }

        // Every stored item with its cell and rotation. Layout is saved, because it cannot be rebuilt from a list.
        InventorySaveEntry[] BuildInventoryEntries()
        {
            var list = new List<InventorySaveEntry>();

            // Looked up at save time, for the same reason as equipment. A missing service is reported, never saved as empty.
            IInventoryService inventory = ServiceLocator.TryGet<IInventoryService>();
            if (inventory == null)
            {
                Debug.LogError($"[{nameof(SaveManager)}] IInventoryService is not registered. Inventory was not saved.");
                return list.ToArray();
            }

            // Every individual is written, with its own ID. Only a stack's lead holds a cell, so the cell comes from the
            // lead and each individual names its lead. A lone item leads itself.
            foreach (var entry in inventory.GetAllStored())
            {
                string lead = inventory.StackLeadOf(entry.Key);
                if (lead == null) continue;
                InventoryGrid.Placement? placement = inventory.GetPlacement(lead);
                if (placement == null) continue;
                // An item with no ID cannot be restored, so writing it would silently lose it on the next load.
                if (string.IsNullOrEmpty(entry.Value.ItemId))
                {
                    Debug.LogError($"[{nameof(SaveManager)}] Stored item '{entry.Value.name}' has no ItemId and was not saved. Set _itemId on its asset.", entry.Value);
                    continue;
                }
                list.Add(new InventorySaveEntry
                {
                    itemId = entry.Value.ItemId,
                    instanceId = entry.Key,
                    x = placement.Value.X,
                    y = placement.Value.Y,
                    rotated = placement.Value.Rotated,
                    stackLeadId = lead,
                });
            }
            return list.ToArray();
        }

        // Only attributes with spent points are written.
        AttributeAllocationEntry[] BuildAttributeEntries()
        {
            var list = new List<AttributeAllocationEntry>();
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
        
        
    }
}
