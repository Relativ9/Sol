using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Options tab of the K menu. Holds the game-level actions: Options (not yet implemented), Save, Load, Save and Quit, and
    /// Quit. Saving and loading go through the save system, never through the file layer directly. Its content is the
    /// "options-tab-content" element in GlobalUI.uxml.
    /// </summary>
    public class OptionsMenuTab : IMenuTab
    {
        private readonly Func<SaveReason, bool> _requestSave;
        private readonly Action _requestLoad;
        private readonly Action<string> _showStatus;

        /// <param name="requestSave">Asks the save system for a save of the given kind. Returns whether it was accepted.</param>
        /// <param name="requestLoad">Reloads the game from the manual slot.</param>
        /// <param name="showStatus">Shows a short message to the player, for feedback on the action taken.</param>
        public OptionsMenuTab(Func<SaveReason, bool> requestSave, Action requestLoad, Action<string> showStatus)
        {
            _requestSave = requestSave;
            _requestLoad = requestLoad;
            _showStatus = showStatus;
        }

        public string Title => "Options";
        public string ContentName => "options-tab-content";

        public void Build(VisualElement content)
        {
            content.Clear();
            content.style.paddingTop = 24f;
            content.style.paddingLeft = 24f;

            var column = new VisualElement();
            column.style.flexDirection = FlexDirection.Column;
            column.style.width = 260f;

            // Options is listed but not built yet. Its button is shown disabled, so it is visible without doing anything.
            Button options = MakeButton("Options");
            options.SetEnabled(false);
            column.Add(options);

            column.Add(MakeButton("Save", () => _showStatus?.Invoke(
                _requestSave(SaveReason.ManualSave) ? "Game saved." : "Save was not accepted.")));

            column.Add(MakeButton("Load", () => _requestLoad?.Invoke()));

            column.Add(MakeButton("Save and Quit", () =>
            {
                // Saves the manual slot, then quits. The quit happens only once the save has been accepted.
                if (_requestSave(SaveReason.SaveAndQuit)) QuitGame();
                else _showStatus?.Invoke("Save was not accepted. The game was not quit.");
            }));

            column.Add(MakeButton("Quit", QuitGame));

            content.Add(column);
        }

        public void OnShow() { }

        public void OnHide() { }

        // Quits the application. In the editor this stops play mode, since Application.Quit does nothing there.
        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static Button MakeButton(string text, Action onClick = null)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text };
            button.style.height = 40f;
            button.style.marginBottom = 8f;
            button.style.fontSize = 15f;
            return button;
        }
    }
}
