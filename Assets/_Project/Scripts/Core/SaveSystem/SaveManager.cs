using System;
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
        private IInventoryService _inventoryService;
        private IEquipmentService _equipmentService;
        private IPlayerVitals _vitals;
        private IWorldItemService _worldItemService;

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

        void Start()
        {
            _saveService = ServiceLocator.Get<ISaveService>();
            _progressionService = ServiceLocator.Get<IProgressionService>();
            _talentStateService = ServiceLocator.Get<ITalentStateService>();
            _inventoryService = ServiceLocator.TryGet<IInventoryService>();
            _equipmentService = ServiceLocator.TryGet<IEquipmentService>();
            _vitals = ServiceLocator.TryGet<IPlayerVitals>();
            _worldItemService = ServiceLocator.TryGet<IWorldItemService>();
            
            if (_talentStateService == null)
                Debug.LogWarning($"[{nameof(SaveManager)}] TalentTreeController not found in ServiceLocator");
        }
        

        // Changes no longer trigger a save. Inventory, equipment and the world only change state here, and the next quicksave,
        // autosave, or Save and Quit writes it. Nothing is subscribed to, so nothing can save on a change.
        private bool WriteSave(SaveReason reason)
        {
            return PerformSave(SlotFor(reason));
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

        // Writes one slot. Returns false, and writes nothing, while the save-repair prompt is open, so a suspect save is never
        // overwritten. The player file is written here. World cells are written by the cell store in a later phase.
        private bool PerformSave(string slot)
        {
            if (GameInitializer.IsSaveRepairPending)
            {
                Debug.LogWarning($"[{nameof(SaveManager)}] Save skipped: save repair prompt is open.");
                return false;
            }

            PlayerSaveData data = BuildSaveData();
            _saveService.Save(data, slot);

            Debug.Log($"[{nameof(SaveManager)}] Game saved to slot '{slot}'");
            return true;
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
                attributeAllocations = BuildAttributeEntries(),
                // Vitals were previously dropped on every save. Written here so a reload restores them.
                currentHealth = _vitals != null && _vitals.IsInitialized ? _vitals.CurrentHealth : 0f,
                currentEnergy = _vitals != null && _vitals.IsInitialized ? _vitals.CurrentEnergy : 0f,
                equipped = BuildEquippedEntries(),
                inventory = BuildInventoryEntries(),
                pickedUpItemInstanceIds = BuildPickedUpInstanceIds(),
                droppedItemInstances = BuildDroppedItemInstances(),
            };
        }

        string[] BuildPickedUpInstanceIds()
        {
            if (_worldItemService == null) return System.Array.Empty<string>();
            var ids = new System.Collections.Generic.List<string>(_worldItemService.GetPickedUpIds());
            return ids.ToArray();
        }

        // Every dropped pile is saved where it actually lies. A pile that has moved or settled since it was dropped is read
        // from its live transform first, so the save is never stale.
        WorldDropEntry[] BuildDroppedItemInstances()
        {
            if (_worldItemService == null) return System.Array.Empty<WorldDropEntry>();

            foreach (WorldItem pile in FindObjectsByType<WorldItem>(FindObjectsSortMode.None))
            {
                if (!pile.IsDropped || pile.IsTaken) continue;
                _worldItemService.UpdateDropped(pile.ItemInstanceId, pile.transform.position, pile.transform.rotation);
            }

            return new System.Collections.Generic.List<WorldDropEntry>(_worldItemService.GetDropped()).ToArray();
        }

        // One entry per occupied slot. Saved by slot name, so reordering the enum cannot move an item.
        EquippedSaveEntry[] BuildEquippedEntries()
        {
            var list = new System.Collections.Generic.List<EquippedSaveEntry>();
            if (_equipmentService == null) return list.ToArray();

            foreach (var pair in _equipmentService.GetAllEquipped())
            {
                string instanceId = _equipmentService.GetEquippedInstanceId(pair.Key);
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
            var list = new System.Collections.Generic.List<InventorySaveEntry>();
            if (_inventoryService == null) return list.ToArray();

            // Every individual is written, with its own ID. Only a stack's lead holds a cell, so the cell comes from the
            // lead and each individual names its lead. A lone item leads itself.
            foreach (var entry in _inventoryService.GetAllStored())
            {
                string lead = _inventoryService.StackLeadOf(entry.Key);
                if (lead == null) continue;
                InventoryGrid.Placement? placement = _inventoryService.GetPlacement(lead);
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
        
        
    }
}
