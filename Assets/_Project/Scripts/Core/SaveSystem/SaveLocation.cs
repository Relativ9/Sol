using System.IO;

namespace Sol
{
    /// <summary>
    /// The one place save files live. Both the player files and the world cell folders are built from this directory, so
    /// they can never end up in different places. The folder is created if it is missing.
    /// </summary>
    public static class SaveLocation
    {
        /// <summary>Absolute path of the folder that holds every save file and cell folder.</summary>
        public const string Directory = "/home/relativ9/Documents/Game Saves";

        /// <summary>
        /// The folder that holds one slot's world cell files. Named after the slot, beside its player file. Every reader and
        /// writer of cell files and the delete path use this, so a slot's world state is always found and removed together.
        /// </summary>
        public static string CellsFolder(string slot)
        {
            return System.IO.Path.Combine(Directory, $"save_{slot}_cells");
        }

        /// <summary>Creates the save folder if it does not exist. Safe to call more than once.</summary>
        public static void EnsureExists()
        {
            if (!System.IO.Directory.Exists(Directory))
                System.IO.Directory.CreateDirectory(Directory);
        }
    }
}
