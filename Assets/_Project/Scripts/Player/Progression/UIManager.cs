using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Sol
{
    public class UIManager : MonoBehaviour, IUIManager
    {
        
        public UIDocument _document;

        public bool panelOpen = false;
        private Dictionary<string, VisualElement> _panels = new();
        private IUIStateService _uiStateService;
        
        [SerializeField] private TalentTreeSettingsSO _talentTreeSettings;
        [SerializeField] private VisualTreeAsset _nodeTemplate;
        [SerializeField] private TreeLayoutCollectionSO _treeCollection;
        
        private ITalentTreeGenerator _talentTreeGenerator;
        private TooltipSystem _tooltipSystem;
        
        void Awake()
        {
            ServiceLocator.RegisterService<IUIManager>(this); //Registers itself with the service locator.
            _talentTreeGenerator = new TalentTreeGenerator(_document, _nodeTemplate, _treeCollection, _talentTreeSettings);
            ServiceLocator.RegisterService<ITalentTreeGenerator>(_talentTreeGenerator);
        }

        private void Start()
        {
            if (_document == null) _document = GetComponentInChildren<UIDocument>();
            
            _uiStateService = ServiceLocator.Get<IUIStateService>();
            
            var element = _document.rootVisualElement.Q("talent-wheel-root"); // Q() not Query()
            _panels["talent-wheel-root"] = element;
        }

        public void TogglePanel(string panelName)
        {
            if (IsPanelOpen(panelName))
            {
                ClosePanel(panelName);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                OpenPanel(panelName);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            ServiceLocator.Get<ITimeManager>().TogglePause();
        }

        public void OpenPanel(string panelName)
        {
            if (_panels.TryGetValue(panelName, out VisualElement panel))
            {
                panel.style.display = DisplayStyle.Flex;
                
                panel.RegisterCallbackOnce<GeometryChangedEvent>(e => 
                {
                    _talentTreeGenerator.Generate();
                    var controller = gameObject.AddComponent<TalentTreeController>(); // Or use existing one
                    controller.Initialize(_talentTreeGenerator.GetNodeRegistry(), _document.rootVisualElement);
                });
                
            }
        }

        public void ClosePanel(string panelName)
        {
            if (_panels.TryGetValue(panelName, out VisualElement panel))
            {
                panel.style.display = DisplayStyle.None;
            }
        }

        public bool IsPanelOpen(string panelName)
        {
            bool _panelOpen;

            if (_panels.TryGetValue(panelName, out VisualElement panel))
            {
                _panelOpen = panel.style.display.Equals(DisplayStyle.Flex) ? true : false;
                return _panelOpen;
            }

            return false;
        }
    }
}
