using System.Buffers.Binary;
using HttpFromScratch.Common.Assertion;

namespace HttpFromScratch.Stack;

public static class ChecksumHelper
{
    public static ushort Calculate(ReadOnlySpan<byte> bytes, int bytesCount)
    {
        Assert.That(bytes.Length >= bytesCount);

        ulong sum = 0;
        int i = 0;
        while (bytesCount > 1)
        {
            sum += BinaryPrimitives.ReadUInt16BigEndian(bytes[i..]);
            bytesCount -= 2;
            i += 2;
        }

        if (bytesCount > 0)
        {
            sum += (ushort)(bytes[i] << 8);
        }

        while (sum >> 16 > 0)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }

        return (ushort)~sum;
    }
}