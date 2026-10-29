using System;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// The 100 m grid cell a world position belongs to. Plain data. Cell = floor(position / 100) on each horizontal axis, so
    /// the same cell is found whichever scene loaded the object. Negative positions give negative cells.
    /// </summary>
    public readonly struct CellKey : IEquatable<CellKey>
    {
        // Cell size in metres. Chosen to divide evenly into the 200 m terrains.
        public const float CellSizeMetres = 100f;

        public readonly int X;
        public readonly int Z;

        public CellKey(int x, int z)
        {
            X = x;
            Z = z;
        }

        /// <summary>The cell containing a world position. Only X and Z are used; height does not split cells.</summary>
        public static CellKey FromWorld(Vector3 position)
        {
            return new CellKey(
                Mathf.FloorToInt(position.x / CellSizeMetres),
                Mathf.FloorToInt(position.z / CellSizeMetres));
        }

        /// <summary>The file name for this cell inside a slot's cells folder, for example "-2_5.bin".</summary>
        public string FileName => $"{X}_{Z}.bin";

        public bool Equals(CellKey other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is CellKey other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Z;
        public override string ToString() => $"({X}, {Z})";

        public static bool operator ==(CellKey a, CellKey b) => a.Equals(b);
        public static bool operator !=(CellKey a, CellKey b) => !a.Equals(b);
    }
}
