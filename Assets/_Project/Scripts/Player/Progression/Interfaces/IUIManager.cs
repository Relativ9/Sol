using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    public interface IUIManager
    {
        void TogglePanel(string panelName);
        void OpenPanel(string panelName);
        void ClosePanel(string panelName);
        bool IsPanelOpen(string panelName);

        /// <summary>
        /// Shows a modal dialog using the same cursor/camera handoff as panels.
        /// onChoice receives the index of the button pressed.
        /// </summary>
        void ShowModal(string title, string body, string[] buttonLabels, System.Action<int> onChoice);

        /// <summary>The shared UI root that world-space UI (such as enemy health bars) is drawn into.</summary>
        VisualElement HudRoot { get; }
    }

}
