using System;
using UnityEngine;

namespace Sol
{
    /// <summary>What a cell record describes. Stored as one byte.</summary>
    public enum CellRecordKind : byte
    {
        Placed = 0,
        Dropped = 1,
        Tombstone = 2,
    }

    /// <summary>Bit flags on a cell record. Stored as one byte.</summary>
    [Flags]
    public enum CellRecordFlags : byte
    {
        None = 0,
        Taken = 1 << 0,
        Moving = 1 << 1,
        HasVelocity = 1 << 2,

        // A placed object that has been moved from its authored pose. Its record carries the new pose.
        Moved = 1 << 3,
    }

    /// <summary>
    /// One object in a cell, as stored. Plain data. The identity is either a build index (placed objects) or a runtime
    /// GUID with an item index (dropped objects). The generation resolves duplicates when an object is in two cells.
    /// </summary>
    public struct CellRecord
    {
        public CellRecordKind Kind;
        public CellRecordFlags Flags;

        // Placed: the build-time object index. Dropped: the item type index. Tombstone: the object index it refers to.
        public uint Index;

        // Dropped only. The runtime GUID. Empty for the other kinds.
        public Guid Guid;

        // Increases on every change to this object. The higher generation is the live copy.
        public uint Generation;

        // Centimetres, as whole numbers. Yaw in tenths of a degree, 0 to 3599.
        public int X, Y, Z;
        public ushort YawTenths;

        // Moving records only. Centimetres per second, and centi-radians per second.
        public Vector3Short LinearVelocity;
        public Vector3Short AngularVelocity;

        public bool IsMoving => (Flags & CellRecordFlags.Moving) != 0;
        public bool HasVelocity => (Flags & CellRecordFlags.HasVelocity) != 0;
        public bool IsTaken => (Flags & CellRecordFlags.Taken) != 0;
    }

    /// <summary>Three signed 16-bit components. Used for velocity, where the value range is small and size matters.</summary>
    public struct Vector3Short
    {
        public short X, Y, Z;

        public Vector3Short(short x, short y, short z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
