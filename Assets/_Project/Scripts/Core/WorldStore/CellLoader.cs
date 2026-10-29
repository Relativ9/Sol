using System;
using System.IO;
using System.Threading.Tasks;

namespace Sol
{
    /// <summary>
    /// Reads one cell file off the main thread. The live file is used if it passes its checks; otherwise the backup from
    /// the last good write is used. A cell with neither valid copy is reported as untouched, so the world falls back to its
    /// authored state rather than applying damaged data. Plain C#: safe to run on a worker thread.
    /// </summary>
    public static class CellLoader
    {
        /// <summary>The outcome of loading one cell.</summary>
        public readonly struct LoadResult
        {
            public readonly CellKey Key;
            public readonly bool Found;
            public readonly CellCodec.ReadResult Data;
            public readonly string Source;

            public LoadResult(CellKey key, bool found, CellCodec.ReadResult data, string source)
            {
                Key = key;
                Found = found;
                Data = data;
                Source = source;
            }
        }

        /// <summary>Reads a cell from a slot's cells folder. Never throws; failures come back as a result.</summary>
        public static LoadResult Load(string cellsFolder, CellKey key)
        {
            string path = Path.Combine(cellsFolder, key.FileName);

            foreach (string candidate in new[] { path, AtomicFile.BackupPath(path) })
            {
                if (!File.Exists(candidate)) continue;

                try
                {
                    CellCodec.ReadResult parsed = CellCodec.Read(File.ReadAllBytes(candidate));
                    if (parsed.Ok) return new LoadResult(key, true, parsed, candidate);
                    // A damaged copy is passed over. The next candidate is tried.
                }
                catch (Exception)
                {
                    // An unreadable file is treated the same as a damaged one.
                }
            }

            // Neither copy is valid, or there is no file. A cell with no file is simply untouched, which is normal.
            return new LoadResult(key, false, default, null);
        }

        /// <summary>Starts a cell read on a worker thread. The result is delivered to the main thread by the caller.</summary>
        public static Task<LoadResult> LoadAsync(string cellsFolder, CellKey key)
        {
            return Task.Run(() => Load(cellsFolder, key));
        }
    }
}
