using System;

namespace Sol
{
    /// <summary>
    /// Save-specific wrapper around the shared modal. Asks UIManager to show it so it gets the virtual cursor.
    /// </summary>
    public static class SaveRepairPrompt
    {
        public enum Choice
        {
            Repair,
            NewGame,
            KeepFile
        }

        /// <summary>Asks the player how to handle a suspect save. The text describes exactly what each choice does.</summary>
        public static void Show(IUIManager uiManager, string reason, Action<Choice> onChoice)
        {
            string body = $"{reason}\n\nRepair keeps your progress and corrects the talent points. " +
                          "Start new game backs up the current file first.";
            ShowWithButtons(uiManager, body, new[] { "Repair save", "Start new game" }, onChoice);
        }

        /// <summary>
        /// Asks the player how to handle a save file that could not be read. Neither choice touches the file until it has been
        /// backed up. Repair is not offered: an unreadable file has nothing to repair. The player can keep it as it is or start over.
        /// </summary>
        public static void ShowUnreadable(IUIManager uiManager, string reason, Action<Choice> onChoice)
        {
            string body = $"{reason}\n\nStart new game backs up the unreadable file first, then starts a fresh character. " +
                          "Keep file leaves it untouched and starts the game without a save.";
            ShowWithButtons(uiManager, body, new[] { "Start new game", "Keep file" },
                index => onChoice?.Invoke(index == 0 ? Choice.NewGame : Choice.KeepFile));
        }

        private static void ShowWithButtons(IUIManager uiManager, string body, string[] buttons, Action<Choice> onChoice)
        {
            if (uiManager == null)
            {
                // No UI available: keep the save untouched rather than guessing.
                onChoice?.Invoke(Choice.KeepFile);
                return;
            }

            uiManager.ShowModal("Save File Problem", body, buttons,
                index => onChoice?.Invoke(index == 1 ? Choice.NewGame : Choice.Repair));
        }
    }
}
