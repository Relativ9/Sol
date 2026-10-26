using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// One tab in the K menu. The menu container owns the tab bar and shows/hides content;
    /// each tab owns its own content element and any data it displays.
    /// </summary>
    public interface IMenuTab
    {
        /// <summary>Label shown on the vertical tab bar.</summary>
        string Title { get; }

        /// <summary>Name of the content element in GlobalUI.uxml this tab draws into.</summary>
        string ContentName { get; }

        /// <summary>Called once with the tab's content element. Build visuals here.</summary>
        void Build(VisualElement content);

        /// <summary>Called each time the tab becomes visible. Refresh data here.</summary>
        void OnShow();

        /// <summary>Called when the tab is hidden or the menu closes.</summary>
        void OnHide();
    }
}
