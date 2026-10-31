using UnityEngine;

namespace Sol
{
    public interface ISaveManager
    {
        /// <summary>
        /// Asks for a save of the given kind. Gameplay does not save on each change: a change is state, and the next
        /// quicksave or autosave writes it. Save and Quit calls this with SaveReason.SaveAndQuit.
        /// </summary>
        bool RequestSave(SaveReason reason);

        /// <summary>The player's transform, so a save can record where the player stands.</summary>
        void SetPlayerTransform(UnityEngine.Transform player);

        /// <summary>
        /// Writes a save-repair choice through the coordinator. The repaired copy always goes to the repair slot first. If
        /// promoteToManual is true, it also becomes the manual save used on the next boot. Only the coordinator writes files.
        /// </summary>
        bool WriteRepair(PlayerSaveData data, bool promoteToManual);
    }
}
