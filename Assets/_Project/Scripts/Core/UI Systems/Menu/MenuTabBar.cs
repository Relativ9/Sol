using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Owns the vertical tab bar and the tab content switching for the K menu.
    /// Tabs are registered in order; the first registered tab is selected by default.
    /// </summary>
    public class MenuTabBar
    {
        private readonly List<IMenuTab> _tabs = new();
        private readonly Dictionary<IMenuTab, Button> _buttons = new();
        private readonly Dictionary<IMenuTab, VisualElement> _contents = new();
        private VisualElement _tabBar;
        private IMenuTab _selected;

        public IMenuTab Selected => _selected;

        public void Build(VisualElement menuRoot, IEnumerable<IMenuTab> tabs)
        {
            _tabBar = menuRoot.Q("menu-tab-bar");
            if (_tabBar == null)
            {
                Debug.LogError("[MenuTabBar] 'menu-tab-bar' not found in GlobalUI.uxml.");
                return;
            }

            _tabBar.Clear();
            _tabs.Clear();
            _buttons.Clear();
            _contents.Clear();


            foreach (IMenuTab tab in tabs)
            {
                VisualElement content = menuRoot.Q(tab.ContentName);
                if (content == null)
                {
                    Debug.LogError($"[MenuTabBar] Content '{tab.ContentName}' not found for tab '{tab.Title}'.");
                    continue;
                }

                tab.Build(content);
                _tabs.Add(tab);
                _contents[tab] = content;

                IMenuTab captured = tab; // capture for the lambda
                var button = new Button(() => Select(captured)) { text = tab.Title };
                StyleTabButton(button);
                _tabBar.Add(button);
                _buttons[tab] = button;
            }
        }

        /// <summary>Shows the menu's current tab. Selects the first tab if none is selected yet.</summary>
        public void ShowCurrent()
        {
            if (_tabs.Count == 0) return;
            Select(_selected ?? _tabs[0]);
        }

        /// <summary>Hides the current tab's content, e.g. when the menu closes.</summary>
        public void HideCurrent()
        {
            if (_selected == null) return;
            _selected.OnHide();
            _contents[_selected].style.display = DisplayStyle.None;
            // Clear the selection so OnHide runs exactly once per OnShow. ShowCurrent reselects the first tab.
            _selected = null;
        }

        public void Select(IMenuTab tab)
        {
            if (!_contents.ContainsKey(tab)) return;

            if (_selected != null && _selected != tab)
            {
                _selected.OnHide();
                _contents[_selected].style.display = DisplayStyle.None;
            }

            _selected = tab;
            _contents[tab].style.display = DisplayStyle.Flex;
            tab.OnShow();
            RefreshButtonStates();
        }

        private void RefreshButtonStates()
        {
            foreach (var kvp in _buttons)
            {
                bool active = kvp.Key == _selected;
                // Fully opaque in both states: unselected matches the bar, so nothing behind it shows through.
                kvp.Value.style.backgroundColor = active
                    ? new Color(0.45f, 0.32f, 0.12f, 1f)
                    : new Color(12f / 255f, 9f / 255f, 6f / 255f, 1f);
                kvp.Value.style.color = active
                    ? new Color(1f, 0.9f, 0.6f)
                    : new Color(0.8f, 0.78f, 0.7f);
            }
        }

        private static void StyleTabButton(Button button)
        {
            button.style.height = 40f;
            button.style.marginBottom = 6f;
            button.style.fontSize = 15f;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.paddingLeft = 12f;
            button.style.borderTopWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.borderRightWidth = 0f;
        }
    }
}
