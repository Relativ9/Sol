using System;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Why a save was requested. Used for logging and for deciding whether a save may be skipped.
    /// </summary>
    public enum SaveReason
    {
        Quicksave,
        Autosave,
        ManualSave,
        SaveAndQuit,
    }

    /// <summary>
    /// The one entry point for writing a save. Gameplay never writes a file directly: it changes state, and the coordinator
    /// writes it on a quicksave, an autosave, or Save and Quit. Only one save runs at a time. A request made while a save is
    /// running is queued, not run in parallel.
    /// </summary>
    public class SaveCoordinator
    {
        // Slot names. Quicksave and autosave are separate, so an autosave never overwrites a manual slot's state.
        public const string QuickSlot = "quicksave";
        public const string AutoSlot = "autosave";
        public const string ManualSlot = "default";

        // Written only by the save-repair prompt. Kept apart from every other slot, so a repair never overwrites the manual
        // save before the player has chosen what to keep.
        public const string RepairSlot = "repair";

        private readonly Func<SaveReason, bool> _writeSave;
        private readonly Func<bool> _canAutosave;

        private bool _saving;
        private bool _queued;
        private SaveReason _queuedReason;

        /// <summary>Raised after a save finishes, so the UI can confirm it. Carries whether the write succeeded.</summary>
        public event Action<SaveReason, bool> Completed;

        /// <param name="writeSave">Performs the save for a reason and returns whether it succeeded. Supplied by SaveManager.</param>
        /// <param name="canAutosave">
        /// Asked before every autosave. Returns true today. Combat, cutscenes or menus can make it return false later, and
        /// nothing else needs to change.
        /// </param>
        public SaveCoordinator(Func<SaveReason, bool> writeSave, Func<bool> canAutosave)
        {
            _writeSave = writeSave ?? throw new ArgumentNullException(nameof(writeSave));
            _canAutosave = canAutosave ?? throw new ArgumentNullException(nameof(canAutosave));
        }

        public bool IsSaving => _saving;

        /// <summary>
        /// Requests a save. Autosave is skipped if it is not allowed right now. Quicksave and Save and Quit always go
        /// through. Returns false if the request was refused, and true if it ran or was queued.
        /// </summary>
        public bool RequestSave(SaveReason reason)
        {
            if (reason == SaveReason.Autosave && !_canAutosave())
            {
                Debug.Log($"[{nameof(SaveCoordinator)}] Autosave skipped: not allowed right now.");
                return false;
            }

            if (_saving)
            {
                // Keep the strongest pending request. A manual save outranks a queued autosave.
                if (!_queued || reason != SaveReason.Autosave)
                {
                    _queued = true;
                    _queuedReason = reason;
                }
                return true;
            }

            Run(reason);
            return true;
        }

        private void Run(SaveReason reason)
        {
            _saving = true;
            bool ok = false;
            try
            {
                ok = _writeSave(reason);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveCoordinator)}] Save failed: {e.Message}");
            }
            finally
            {
                _saving = false;
                Completed?.Invoke(reason, ok);
            }

            // A request made during the save runs now, so nothing is lost.
            if (_queued)
            {
                _queued = false;
                Run(_queuedReason);
            }
        }
    }
}
