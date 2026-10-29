using System;
using System.IO;

namespace Sol
{
    /// <summary>
    /// Writes a file so that a crash at any point leaves a readable copy. The new bytes go to a temp file, which is read back
    /// and verified before it is promoted. Promotion moves the live file aside to ".bak" and then moves the temp file into
    /// place, so the previous good copy is always on disk. Reading falls back to ".bak" when the live file is missing or fails
    /// its check.
    /// </summary>
    public static class AtomicFile
    {
        public static string TempPath(string path) => path + ".tmp";
        public static string BackupPath(string path) => path + ".bak";

        /// <summary>
        /// Writes bytes atomically. The verify step runs on the temp file before anything live is touched. If it fails, the temp
        /// file is removed and the live file is left as it was. Returns true when the new copy is live.
        /// </summary>
        public static bool Write(string path, byte[] bytes, Func<byte[], bool> verify)
        {
            string temp = TempPath(path);
            string backup = BackupPath(path);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // 1. Write the temp file and flush it to disk.
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            // 2. Read it back and verify before anything live is touched.
            if (verify != null && !verify(File.ReadAllBytes(temp)))
            {
                File.Delete(temp);
                return false;
            }

            // 3. Move the live copy aside, so the last good copy is kept as .bak.
            if (File.Exists(backup)) File.Delete(backup);
            if (File.Exists(path)) File.Move(path, backup);

            // 4. Move the verified temp file into place.
            File.Move(temp, path);
            return true;
        }

        /// <summary>
        /// Reads the live file, or the backup if the live file is missing or fails the check. Returns null if neither is valid.
        /// The check is the caller's, so the same fallback serves every file type.
        /// </summary>
        public static byte[] ReadWithFallback(string path, Func<byte[], bool> isValid)
        {
            foreach (string candidate in new[] { path, BackupPath(path) })
            {
                if (!File.Exists(candidate)) continue;
                byte[] bytes = File.ReadAllBytes(candidate);
                if (isValid == null || isValid(bytes)) return bytes;
            }
            return null;
        }
    }
}
