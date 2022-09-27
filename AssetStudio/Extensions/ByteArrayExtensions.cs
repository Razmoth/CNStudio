namespace AssetStudio
{
    public static class ByteArrayExtensions
    {
        public static byte[] ToUInt4Array(this byte[] source) => ToUInt4Array(source, 0, source.Length);
        public static byte[] ToUInt4Array(this byte[] source, int offset, int size)
        {
            var buffer = new byte[size * 2];
            for (var (i, j) = (0, 0); j < size; i += 2, j++)
            {
                buffer[i] = (byte)(source[offset + j] >> 4);
                buffer[i + 1] = (byte)(source[offset + j] & 0xF);
            }
            return buffer;
        }
    }
}
