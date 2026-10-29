using System;
using System.Collections.Generic;
using System.IO;

namespace Sol
{
    /// <summary>
    /// The read half of CellCodec. Every check runs before any record is returned: magic, version, length, then the CRC over
    /// the whole body. A file that fails any check is refused, so a damaged cell is never partly applied.
    /// </summary>
    public static partial class CellCodec
    {
        /// <summary>Result of a read. Records are only set when Ok is true. Reason is for the log on failure.</summary>
        public struct ReadResult
        {
            public bool Ok;
            public string Reason;
            public int CellX, CellZ;
            public List<CellRecord> Records;
        }

        /// <summary>Parses a cell file. Refused on bad magic, unknown version, a length mismatch, or a CRC mismatch.</summary>
        public static ReadResult Read(byte[] file)
        {
            if (file == null || file.Length < HeaderSize)
                return Fail("file is shorter than the header");

            for (int i = 0; i < 4; i++)
                if (file[i] != Magic[i]) return Fail("bad magic");

            ushort version = BitConverter.ToUInt16(file, 4);
            if (version != FormatVersion) return Fail($"unknown format version {version}");

            int cellX = BitConverter.ToInt32(file, 8);
            int cellZ = BitConverter.ToInt32(file, 12);
            uint recordCount = BitConverter.ToUInt32(file, 16);
            uint bodyLength = BitConverter.ToUInt32(file, 20);
            uint storedCrc = BitConverter.ToUInt32(file, 24);

            if ((ulong)file.Length != (ulong)HeaderSize + bodyLength) return Fail("body length does not match the file");
            if (Crc32.Compute(file, HeaderSize, (int)bodyLength) != storedCrc) return Fail("CRC mismatch");

            var records = new List<CellRecord>((int)recordCount);
            using var reader = new BinaryReader(new MemoryStream(file, HeaderSize, (int)bodyLength));
            for (uint i = 0; i < recordCount; i++)
            {
                if (!TryReadRecord(reader, out CellRecord record)) return Fail($"record {i} is truncated or invalid");
                records.Add(record);
            }

            return new ReadResult { Ok = true, CellX = cellX, CellZ = cellZ, Records = records };
        }

        private static ReadResult Fail(string reason) => new ReadResult { Ok = false, Reason = reason };

        // Reads one record in the same layout WriteRecord produces. Returns false on a bad kind or a truncated record.
        private static bool TryReadRecord(BinaryReader r, out CellRecord record)
        {
            record = default;
            try
            {
                record.Kind = (CellRecordKind)r.ReadByte();
                record.Flags = (CellRecordFlags)r.ReadByte();
                record.Index = r.ReadUInt32();
                record.Generation = r.ReadUInt32();

                if (record.Kind == CellRecordKind.Dropped)
                    record.Guid = new Guid(r.ReadBytes(16));

                if (record.Kind == CellRecordKind.Tombstone)
                {
                    record.MovedCellX = r.ReadInt32();
                    record.MovedCellZ = r.ReadInt32();
                    return true;
                }

                if (record.Kind != CellRecordKind.Placed && record.Kind != CellRecordKind.Dropped) return false;

                record.X = r.ReadInt32();
                record.Y = r.ReadInt32();
                record.Z = r.ReadInt32();
                record.YawTenths = r.ReadUInt16();

                if (record.HasVelocity)
                {
                    record.LinearVelocity = ReadVelocity(r);
                    record.AngularVelocity = ReadVelocity(r);
                }
                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        private static Vector3Short ReadVelocity(BinaryReader r)
        {
            return new Vector3Short(r.ReadInt16(), r.ReadInt16(), r.ReadInt16());
        }
    }
}
