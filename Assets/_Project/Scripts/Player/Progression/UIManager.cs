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
        }
        
        void CachePanelReferences()
        {
            var root = _document?.rootVisualElement;
            if (root == null) return;
            
            var talentWheelRoot = root.Q("talent-wheel-root");
            if (talentWheelRoot != null)
            {
                _panels["talent-wheel-root"] = talentWheelRoot;
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
                
                // Talent panel specific initialization
                if (panelName == "talent-wheel-root" && !_isTalentPanelInitialized)
                {
                    InitializeTalentPanel(panel);
                }
            }
        }
        
        void InitializeTalentPanel(VisualElement panel)
        {
            // Register once for geometry change to ensure layout is ready
            panel.RegisterCallbackOnce<GeometryChangedEvent>(e => 
            {
                // Generate visuals (creates VisualElements from cached NodeInstance data)
                _talentTreeGenerator.Generate(_document.rootVisualElement, _nodeTemplate);
                
                // Create controller to handle input/visual updates
                var controller = gameObject.AddComponent<TalentTreeController>();
                controller.Initialize(
                    _talentTreeGenerator.GetNodeRegistry(), 
                    _document.rootVisualElement, 
                    _tooltipTemplate);
                
                _isTalentPanelInitialized = true;
            });
            
            if (panel.name == "talent-wheel-root")
            {
                // Bypass debounce. Player explicitly closed the panel; 
                // they expect their points to stick.
                ServiceLocator.Get<ISaveManager>()?.RequestImmediateSave();
            }
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
                panel.style.display = DisplayStyle.None;
                
                // Optional: Clear UI elements to save memory, 
                // or keep them cached for faster reopening
                if (panelName == "talent-wheel-root")
                {
                    _talentTreeGenerator.ClearUI();
                    // Note: We don't destroy the controller here - 
                    // could pool it or destroy if memory is tight
                    var controller = GetComponent<TalentTreeController>();
                    if (controller != null)
                    {
                        Destroy(controller);
                    }
                    _isTalentPanelInitialized = false;
                }
            }
            _onImmediateSaveRequest.Raise();
        }

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
