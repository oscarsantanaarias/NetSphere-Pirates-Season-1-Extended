using System;

namespace Netsphere.Network.Serializers
{
    public static class PacketSecurity
    {
        public const int MaxArrayElements = 4096;

        public static void EnsureArrayLength(int length, string source)
        {
            if (length < 0)
                throw new InvalidOperationException($"{source}: negative array length {length}");

            if (length > MaxArrayElements)
                throw new InvalidOperationException($"{source}: array length {length} exceeds max {MaxArrayElements}");
        }
    }
}
