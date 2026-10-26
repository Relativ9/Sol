using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Sol
{
    public class UIManager : MonoBehaviour, IUIManager
    {
        [Header("UI References")]
        [SerializeField] private UIDocument _document;
        [SerializeField] private VisualTreeAsset _nodeTemplate;
        [SerializeField] private VisualTreeAsset _tooltipTemplate;
        [SerializeField] private GameEvent _onImmediateSaveRequest;
        
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
        
        void CachePanelReferences()
        {
            var root = _document?.rootVisualElement;
            if (root == null) return;
            
            var menuRoot = root.Q(MenuPanelName);
            if (menuRoot != null)
            {
                _panels[MenuPanelName] = menuRoot;

                _talentTab = new TalentMenuTab(OnTalentTabShow, OnTalentTabHide);
                _statsTab = new PlayerStatsTab();
                _menuTabBar = new MenuTabBar();
                _menuTabBar.Build(menuRoot, new IMenuTab[] { _talentTab, _statsTab });
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

        // Talents tab: tears down the tree and saves. Bypasses debounce; the player expects points to stick.
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
            ServiceLocator.Get<ISaveManager>()?.RequestImmediateSave();
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
            _onImmediateSaveRequest.Raise();
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
