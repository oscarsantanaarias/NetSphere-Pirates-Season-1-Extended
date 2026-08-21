using DotNetty.Buffers;

namespace BlubLib.DotNetty
{
    public static class ByteBufferCompat
    {
        public static byte[] ToArray(this IByteBuffer buffer)
        {
            var data = new byte[buffer.ReadableBytes];
            buffer.GetBytes(buffer.ReaderIndex, data);
            return data;
        }
    }
}
