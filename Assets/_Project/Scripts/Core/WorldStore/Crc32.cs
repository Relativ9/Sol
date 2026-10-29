namespace Sol
{
    /// <summary>
    /// CRC-32 (IEEE 802.3, the same polynomial as zip and PNG). Used to detect a damaged cell file before any of it is applied.
    /// Plain C#, no allocation per call, so it is cheap to run on every load.
    /// </summary>
    public static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[i] = c;
            }
            return table;
        }

        /// <summary>The CRC-32 of a byte range.</summary>
        public static uint Compute(byte[] data, int offset, int count)
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = offset; i < offset + count; i++)
                crc = Table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }
    }
}
