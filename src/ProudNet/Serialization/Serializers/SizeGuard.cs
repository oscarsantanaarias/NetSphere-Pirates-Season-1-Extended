using System.IO;

namespace ProudNet.Serialization.Serializers
{
    public static class SizeGuard
    {
        public const int MaxArrayLength = 4096;

        public static int CheckLength(int length, BinaryReader reader)
        {
            if (length < 0 || length > MaxArrayLength)
                throw new InvalidDataException($"Array length {length} out of range");

            var stream = reader.BaseStream;
            if (stream.CanSeek && length > stream.Length - stream.Position)
                throw new InvalidDataException($"Array length {length} does not fit in the packet");

            return length;
        }
    }
}
