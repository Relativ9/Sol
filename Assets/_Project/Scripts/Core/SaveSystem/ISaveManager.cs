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
    }
}
