using System;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Talents tab of the K menu. Wraps the existing talent tree flow, so tree generation and
    /// TalentTreeController are unchanged. Its content is the "talent-tab-content" element in GlobalUI.uxml.
    /// </summary>
    public class TalentMenuTab : IMenuTab
    {
        private readonly Action _onShow;
        private readonly Action _onHide;

        public TalentMenuTab(Action onShow, Action onHide)
        {
            _onShow = onShow;
            _onHide = onHide;
        }

        public string Title => "Talents";
        public string ContentName => "talent-tab-content";

        // Talent tree generation needs the document's layout, so UIManager drives it on show.
        public void Build(VisualElement content) { }

        public void OnShow() => _onShow?.Invoke();

        public void OnHide() => _onHide?.Invoke();
    }
}
