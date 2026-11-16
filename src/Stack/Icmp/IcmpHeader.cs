using System.Buffers.Binary;
using HttpFromScratch.Common.Results;
using HttpFromScratch.Stack.Errors;

namespace HttpFromScratch.Stack.Icmp;

public struct IcmpHeader
{
    public IcmpMessageType Type;
    public byte Code;
    public ushort Checksum;
    public uint OtherData;

    public static Result<IcmpHeader, IcmpHeaderParseError> Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 8)
        {
            return IcmpHeaderParseError.InvalidDataLength;
        }

        return new IcmpHeader
        {
            Type = (IcmpMessageType)bytes[0],
            Code = bytes[1],
            Checksum = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2, 2)),
            OtherData = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(4, 4))
        };
    }

    public byte[] GetBytes()
    {
        var buffer = new byte[8].AsSpan();

        buffer[0] = (byte)Type;
        buffer[1] = Code;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4, 4), OtherData);

        return buffer.ToArray();
    }
}