using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Sol
{
    public class UIManager : MonoBehaviour, IUIManager
    {
        // Scene names used by Load. The persistent scene holds the game's systems, so it always loads first.
        private const string PersistentSceneName = "Persistant Scene";
        private const string WorldSceneName = "OutdoorsScene";

        [Header("UI References")]
        [SerializeField] private UIDocument _document;
        [SerializeField] private VisualTreeAsset _nodeTemplate;
        [SerializeField] private VisualTreeAsset _tooltipTemplate;
        [Tooltip("Layout for the Player Stats tab (PlayerStatsTab.uxml).")]
        [SerializeField] private VisualTreeAsset _statsLayout;
        [Tooltip("Layout for the Inventory tab (InventoryTab.uxml).")]
        [SerializeField] private VisualTreeAsset _inventoryLayout;
        
        private Dictionary<string, VisualElement> _panels = new();
        private IUIStateService _uiStateService;
        private ITalentTreeGenerator _talentTreeGenerator;
        private ITalentStateService _talentStateService;
        private IVirtualCursor _virtualCursor;
        private ICameraController _cameraController;
        
        
        private bool _isTalentPanelInitialized = false;

        // K menu: vertical tabs (Talents, Player Stats, and future tabs).
        private const string MenuPanelName = "menu-root";
        private MenuTabBar _menuTabBar;
        private TalentMenuTab _talentTab;
        private PlayerStatsTab _statsTab;
        private InventoryMenuTab _inventoryTab;
        private OptionsMenuTab _optionsTab;
        
        void Awake()
        {
            ServiceLocator.RegisterService<IUIManager>(this);
        }

        private void Start()
        {
            if (_document == null) 
                _document = GetComponentInChildren<UIDocument>();
            
            // Get services from locator
            _virtualCursor = ServiceLocator.Get<IVirtualCursor>();
            _uiStateService = ServiceLocator.Get<IUIStateService>();
            _talentTreeGenerator = ServiceLocator.Get<ITalentTreeGenerator>();
            _talentStateService = ServiceLocator.Get<ITalentStateService>();
            _cameraController =  ServiceLocator.Get<ICameraController>();
            
            // Ensure virtual cursor is disabled on start
            _virtualCursor?.UnregisterDocument(_document);
            
            Debug.Log($"[UIManager] Got cursor instance: {_virtualCursor?.GetHashCode()}");
    
            // Cache panel references
            CachePanelReferences();

            // Runtime HUD (vitals bars) is built into the same document's hud-root container.
            if (_document != null)
            {
                var hud = gameObject.AddComponent<PlayerHUD>();
                hud.Build(_document.rootVisualElement);
            }
        }
        
        // Options > Save. Writes through the save system with the reason it is given, never the file directly.
        private bool RequestSaveFromMenu(SaveReason reason)
        {
            return ServiceLocator.TryGet<ISaveManager>()?.RequestSave(reason) ?? false;
        }

        // Options > Load. The chosen slot is recorded, then the whole game is restarted from its first scene, so every scene
        // comes back with the save, not only the one that happened to be active. Startup reads the chosen slot, so the load
        // takes effect exactly like a normal startup from that save.
        private void RequestLoadRestart()
        {
            // Load always uses the newest save, from whichever slot wrote it last. Null means no save exists to load.
            string newest = ServiceLocator.TryGet<ISaveService>()?.NewestSlot();
            if (newest == null)
            {
                ServiceLocator.TryGet<ICombatLog>()?.Add("No save to load.", CombatLogKind.Warning);
                return;
            }
            PendingLoad.Request(newest);

            // The persistent scene holds GameInitializer, so it is loaded first, in single mode, which replaces the open
            // scenes. The world scene is then loaded additively, so the environment comes back with the save.
            string world = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (world == PersistentSceneName)
                world = WorldSceneName;

            UnityEngine.SceneManagement.SceneManager.LoadScene(PersistentSceneName);
            UnityEngine.SceneManagement.SceneManager.LoadScene(world, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        // Feedback from the options tab, shown in the combat log, which the player already reads.
        private void ShowMenuStatus(string message)
        {
            ServiceLocator.TryGet<ICombatLog>()?.Add(message, CombatLogKind.Info);
        }

        void CachePanelReferences()
        {
            var root = _document?.rootVisualElement;
            if (root == null) return;
            
            var menuRoot = root.Q(MenuPanelName);
            if (menuRoot != null)
            {
                _panels[MenuPanelName] = menuRoot;

                _talentTab = new TalentMenuTab(OnTalentTabShow, OnTalentTabHide);
                _statsTab = new PlayerStatsTab(_statsLayout);
                _inventoryTab = new InventoryMenuTab(_inventoryLayout);
                _optionsTab = new OptionsMenuTab(RequestSaveFromMenu, RequestLoadRestart, ShowMenuStatus);
                _menuTabBar = new MenuTabBar();
                _menuTabBar.Build(menuRoot, new IMenuTab[] { _talentTab, _statsTab, _inventoryTab, _optionsTab });
            }
        }

        public void TogglePanel(string panelName)
        {
            if (IsPanelOpen(panelName))
            {
                ClosePanel(panelName);
            }
            else
            {
                OpenPanel(panelName);
            }
            ServiceLocator.Get<ITimeManager>()?.TogglePause();
        }

        public void OpenPanel(string panelName)
        {
            _virtualCursor?.SetActiveDocument(_document);
            
            if (_cameraController != null)
            {
                _cameraController.OnDeactivate();
            }
            else
            {
                Debug.LogError("Camera is null!");
            }
            
            Debug.Log($"[UIManager] OpenPanel - virtualCursor null={_virtualCursor == null}, document null={_document == null}");
            
            if (_panels.TryGetValue(panelName, out VisualElement panel))
            {
                panel.style.display = DisplayStyle.Flex;

                // The menu replaces the HUD while open, so the vitals bars and log don't sit under it.
                SetHudVisible(false);

                // Menu opens on the tab that was last selected (first tab on first open).
                _menuTabBar?.ShowCurrent();
            }
        }

        // Hides or shows the HUD container (vitals bars and combat log) in the shared document.
        void SetHudVisible(bool visible)
        {
            VisualElement hud = _document?.rootVisualElement?.Q("hud-root");
            if (hud != null)
                hud.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Talents tab: builds the tree the first time it's shown. Layout needs geometry, so this waits for it.
        void OnTalentTabShow()
        {
            SetTalentOverlaysVisible(true);

            if (_isTalentPanelInitialized) return;

            VisualElement content = _document.rootVisualElement.Q("talent-tab-content");
            content.RegisterCallbackOnce<GeometryChangedEvent>(e =>
            {
                _talentTreeGenerator.Generate(_document.rootVisualElement, _nodeTemplate);

                var controller = gameObject.AddComponent<TalentTreeController>();
                controller.Initialize(
                    _talentTreeGenerator.GetNodeRegistry(),
                    _document.rootVisualElement,
                    _tooltipTemplate);

                _isTalentPanelInitialized = true;
            });
        }

        // The vignette and tree arrows live on menu-panel (so the vignette covers the whole panel),
        // so they are shown and hidden with the Talents tab here rather than by the tab container.
        void SetTalentOverlaysVisible(bool visible)
        {
            var root = _document?.rootVisualElement;
            if (root == null) return;

            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            SetDisplay(root, "talent-vignette", display);
            SetDisplay(root, "talent-prev-tree", display);
            SetDisplay(root, "talent-next-tree", display);
            SetDisplay(root, "talent-header", display);
        }

        static void SetDisplay(VisualElement root, string name, DisplayStyle display)
        {
            VisualElement element = root.Q(name);
            if (element != null) element.style.display = display;
        }

        // Talents tab: tears down the tree. Talent points are state, so they are written by the next quicksave or autosave,
        // not by closing the tab.
        void OnTalentTabHide()
        {
            SetTalentOverlaysVisible(false);

            if (!_isTalentPanelInitialized) return;

            _talentTreeGenerator.ClearUI();
            var controller = GetComponent<TalentTreeController>();
            if (controller != null)
            {
                Destroy(controller);
            }
            _isTalentPanelInitialized = false;
        }
        
        public void ClosePanel(string panelName)
        {
            _virtualCursor?.UnregisterDocument(_document);
            if (_cameraController != null)
            {
                _cameraController.OnActivate();
            }
            else
            {
                Debug.LogError("Camera is null!");
            }

            if (_panels.TryGetValue(panelName, out VisualElement panel))
            {
                // Hide the current tab first, so OnHide cleanup (e.g. talent tree teardown) runs.
                _menuTabBar?.HideCurrent();
                panel.style.display = DisplayStyle.None;
                SetHudVisible(true);
            }
            // Closing a menu no longer saves. Changes are saved by quicksave, autosave, or Save and Quit.
        }

        // Modal dialogs use the same path as panels: the shared document gets the virtual cursor,
        // and the camera releases the mouse lock. Blocks gameplay while open.
        public VisualElement HudRoot => _document?.rootVisualElement?.Q("hud-root");

        public void ShowModal(string title, string body, string[] buttonLabels, System.Action<int> onChoice)
        {
            if (_document == null)
            {
                Debug.LogError("[UIManager] ShowModal: no UIDocument assigned.");
                onChoice?.Invoke(-1);
                return;
            }

            _modal?.Dispose();
            _modal = new ModalDialog();
            _modal.Build(_document.rootVisualElement);

            _virtualCursor?.SetActiveDocument(_document);
            _cameraController?.OnDeactivate();

            _modal.Show(title, body, buttonLabels, index =>
            {
                _virtualCursor?.UnregisterDocument(_document);
                _cameraController?.OnActivate();
                _modal.Dispose();
                _modal = null;
                onChoice?.Invoke(index);
            });
        }

        private ModalDialog _modal;

        // The virtual cursor never produces UI Toolkit wheel events, so tabs that scroll get their input here.
        // Forwards wheel notches to the stats tab only while it is the selected tab, so a hidden tab never scrolls.
        void Update()
        {
            if (_virtualCursor == null || _statsTab == null || _menuTabBar == null) return;
            if (_menuTabBar.Selected != _statsTab) return;

            int notches = _virtualCursor.GetScrollNotches();
            if (notches != 0)
                _statsTab.ScrollStatsBy(notches);
        }

        // Drives an active inventory drag every frame, so R and the ghost work without mouse movement.
        // The tab ignores the tick when no drag is active. The world pickup prompt is updated here too.
        void LateUpdate()
        {
            if (_inventoryTab != null && _virtualCursor != null && _menuTabBar != null
                && _menuTabBar.Selected == _inventoryTab)
            {
                _inventoryTab.Tick(_virtualCursor);
            }

            UpdatePickupPrompt();
        }

        // Shows the name of the item E would pick up, above the cursor. Hidden while the menu is open, since the menu
        // covers the world and the HUD is hidden with it.
        private void UpdatePickupPrompt()
        {
            if (_virtualCursor == null || _document == null) return;

            bool menuOpen = IsPanelOpen(MenuPanelName);
            if (menuOpen || _pickupPlayer == null)
            {
                _pickupPrompt?.Show(null, Vector2.zero);
                return;
            }

            _pickupPrompt ??= CreatePickupPrompt();
            if (_pickupPrompt == null) return;

            WorldItem target = _pickupPlayer.CurrentTarget;
            string name = target != null && target.Item != null ? target.Item.DisplayName : null;
            // The virtual cursor is only active while a menu is open, so during play the label sits above screen centre.
            UnityEngine.UIElements.IPanel panel = _document.rootVisualElement.panel;
            if (panel == null) return;
            Vector2 anchor = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            _pickupPrompt.Show(name, anchor);
        }

        private PickupPrompt CreatePickupPrompt()
        {
            VisualElement root = _document?.rootVisualElement;
            return root == null ? null : new PickupPrompt(root);
        }

        // The player's pickup component, found once. Null until the player's services are registered.
        private IPlayerPickup _pickupPlayer
        {
            get
            {
                if (_cachedPickup == null) _cachedPickup = ServiceLocator.TryGet<IPlayerPickup>();
                return _cachedPickup;
            }
        }

        private IPlayerPickup _cachedPickup;
        private PickupPrompt _pickupPrompt;

        public bool IsPanelOpen(string panelName)
        {
            if (_panels.TryGetValue(panelName, out VisualElement panel))
            {
                return panel.style.display == DisplayStyle.Flex;
            }
            return false;
        }
    }
}
