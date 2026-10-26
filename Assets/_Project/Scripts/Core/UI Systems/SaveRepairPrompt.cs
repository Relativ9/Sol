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
            NewGame
        }

        public static void Show(IUIManager uiManager, string reason, Action<Choice> onChoice)
        {
            string body = $"{reason}\n\nRepair keeps your progress and corrects the talent points. " +
                          "Start new game backs up the current file first.";

            if (uiManager == null)
            {
                // No UI available: keep the save untouched rather than guessing.
                onChoice?.Invoke(Choice.Repair);
                return;
            }

            uiManager.ShowModal(
                "Save File Problem",
                body,
                new[] { "Repair save", "Start new game" },
                index => onChoice?.Invoke(index == 1 ? Choice.NewGame : Choice.Repair));
        }
    }
}
