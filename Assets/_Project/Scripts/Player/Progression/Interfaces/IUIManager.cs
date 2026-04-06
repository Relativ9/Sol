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
    }

}
