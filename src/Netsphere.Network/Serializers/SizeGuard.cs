using System.IO;

namespace Netsphere.Network.Serializers
{
    // Every array on the wire starts with its own length and every one of them used to be
    // believed: the deserializer read the number and allocated that many elements before a
    // single one of them had been read. One packet claiming 0x7FFFFFFF entries was enough to
    // take the process down, no account needed, because this runs while the message is being
    // decoded.
    //
    // The length is checked against two things now: a hard ceiling no real message comes close
    // to, and what is actually left in the buffer. The second one is what makes it exact, an
    // array of a hundred entries cannot be announced in a packet that has ten bytes left.
    internal static class SizeGuard
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
