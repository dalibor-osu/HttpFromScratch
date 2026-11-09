using System.Buffers.Binary;
using System.Text;

namespace HttpFromScratch.Stack.Ip;

public struct IpV4Header
{
    public byte Version;
    public byte HeaderWordLength;
    public byte TypeOfService;
    public ushort TotalLength;
    public ushort Identification;
    public FragmentationFlags FragmentationFlags;
    public ushort FragmentOffset;
    public byte TimeToLive;
    public Protocol Protocol;
    public ushort Checksum;
    public uint SourceAddress;
    public uint DestinationAddress;

    // Additional fields
    public bool IsChecksumValid;

    public int HeaderByteLength => HeaderWordLength * 4;

    public static bool TryParse(ReadOnlySpan<byte> packet, out IpV4Header value)
    {
        var result = new IpV4Header();
        value = result;

        if (packet.Length < 20)
        {
            return false;
        }

        result.TotalLength = BinaryPrimitives.ReadUInt16BigEndian(packet[2..]);
        result.HeaderWordLength = (byte)(packet[0] & 0x0F);

        if (result.HeaderWordLength < 5 || result.HeaderByteLength > result.TotalLength ||
            packet.Length < result.HeaderByteLength)
        {
            return false;
        }

        result.Version = (byte)(packet[0] >> 4);

        if (result.Version != 4)
        {
            return false;
        }

        result.TypeOfService = packet[1];
        result.Identification = BinaryPrimitives.ReadUInt16BigEndian(packet[4..]);

        ushort flagsFragmentBytes = BinaryPrimitives.ReadUInt16BigEndian(packet[6..]);
        result.FragmentationFlags = (FragmentationFlags)(flagsFragmentBytes >> 13);
        result.FragmentOffset = (ushort)(flagsFragmentBytes & 0x1FFF);

        result.TimeToLive = packet[8];
        result.Protocol = (Protocol)packet[9];
        result.Checksum = BinaryPrimitives.ReadUInt16BigEndian(packet[10..]);
        result.SourceAddress = BinaryPrimitives.ReadUInt32BigEndian(packet[12..]);
        result.DestinationAddress = BinaryPrimitives.ReadUInt32BigEndian(packet[16..]);
        result.IsChecksumValid = CalculateChecksum(packet) == result.Checksum;

        value = result;
        return true;
    }

    public static ushort CalculateChecksum(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 20)
        {
            throw new ArgumentException("Invalid packet", nameof(packet));
        }

        ushort totalLength = BinaryPrimitives.ReadUInt16BigEndian(packet[2..]);
        ushort headerWordLength = (byte)(packet[0] & 0x0F);
        int headerByteLength = headerWordLength * 4;

        if (headerWordLength < 5 || headerByteLength > totalLength || packet.Length < headerByteLength)
        {
            throw new ArgumentException("Invalid packet", nameof(packet));
        }

        ulong sum = 0;
        for (int i = 0; i < headerByteLength; i += 2)
        {
            if (i == 10)
            {
                // Skip the checksum bytes
                continue;
            }

            sum += BinaryPrimitives.ReadUInt16BigEndian(packet[i..]);
        }

        sum = (sum & 0xFFFF) + (sum >> 16);
        return (ushort)~sum;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Version: {Version}");
        builder.AppendLine($"IHL: {HeaderWordLength}");
        builder.AppendLine($"Type of service: {TypeOfService}");
        builder.AppendLine($"Total length: {TotalLength}");
        builder.AppendLine($"Identification: {Identification}");
        builder.AppendLine($"Fragmentation flags: {FragmentationFlags.ToString("G")}");
        builder.AppendLine($"Fragmentation offset: {FragmentOffset}");
        builder.AppendLine($"Time to live: {TimeToLive}");
        builder.AppendLine($"Protocol: {Protocol}");
        string validStr = IsChecksumValid ? "VALID" : "INVALID";
        builder.AppendLine($"Checksum: {Checksum.ToString("X")} ({validStr})");
        builder.AppendLine($"Source address: {SourceAddress.ToString("X")}");
        builder.AppendLine($"Destination address: {DestinationAddress.ToString("X")}");
        return builder.ToString();
    }
}