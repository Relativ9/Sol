using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sol
{
    /// <summary>
    /// Turns a cell into bytes and back. The layout follows the design doc: a 32-byte header with magic, version, cell
    /// coordinates, record count, body length and CRC-32 of the body, then the records back to back. Writing is in this
    /// part; reading, with its checks, is in CellCodec.Read.cs.
    /// </summary>
    public static partial class CellCodec
    {
        public const int HeaderSize = 32;
        public const ushort FormatVersion = 1;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("SOLC");

        /// <summary>Serialises a cell to bytes, including its header and CRC.</summary>
        public static byte[] Write(int cellX, int cellZ, IReadOnlyList<CellRecord> records)
        {
            using var body = new MemoryStream();
            using (var w = new BinaryWriter(body))
            {
                foreach (CellRecord record in records) WriteRecord(w, record);
            }
            byte[] bodyBytes = body.ToArray();

            byte[] file = new byte[HeaderSize + bodyBytes.Length];
            Array.Copy(Magic, 0, file, 0, 4);
            BitConverter.GetBytes(FormatVersion).CopyTo(file, 4);
            // Bytes 6-7 are reserved and left zero.
            BitConverter.GetBytes(cellX).CopyTo(file, 8);
            BitConverter.GetBytes(cellZ).CopyTo(file, 12);
            BitConverter.GetBytes((uint)records.Count).CopyTo(file, 16);
            BitConverter.GetBytes((uint)bodyBytes.Length).CopyTo(file, 20);
            BitConverter.GetBytes(Crc32.Compute(bodyBytes, 0, bodyBytes.Length)).CopyTo(file, 24);
            // Bytes 28-31 are reserved and left zero.
            Array.Copy(bodyBytes, 0, file, HeaderSize, bodyBytes.Length);
            return file;
        }

        // A record: kind, flags, index, generation, then identity and pose. Layout is fixed for FormatVersion 1.
        private static void WriteRecord(BinaryWriter w, CellRecord r)
        {
            w.Write((byte)r.Kind);
            w.Write((byte)r.Flags);
            w.Write(r.Index);
            w.Write(r.Generation);

            if (r.Kind == CellRecordKind.Dropped) w.Write(r.Guid.ToByteArray());

            if (r.Kind == CellRecordKind.Tombstone)
            {
                w.Write(r.MovedCellX);
                w.Write(r.MovedCellZ);
                return;
            }

            w.Write(r.X);
            w.Write(r.Y);
            w.Write(r.Z);
            w.Write(r.YawTenths);

            // Velocity is written only for records that carry it. A resting record stays small.
            if (r.HasVelocity)
            {
                WriteVelocity(w, r.LinearVelocity);
                WriteVelocity(w, r.AngularVelocity);
            }
        }

        private static void WriteVelocity(BinaryWriter w, Vector3Short v)
        {
            w.Write(v.X);
            w.Write(v.Y);
            w.Write(v.Z);
        }
    }
}
