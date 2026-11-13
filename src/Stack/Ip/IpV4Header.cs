using System.Buffers.Binary;
using System.Text;
using HttpFromScratch.Common.Results;
using HttpFromScratch.Stack.Errors;

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

    private static ushort _nextIdentification = 0;

    public static Result<IpV4Header, IpHeaderParseError> Parse(ReadOnlySpan<byte> packet)
    {
        var result = new IpV4Header();
        if (packet.Length < 20)
        {
            return (IpHeaderParseError.InvalidPacketLength, "Packet is too short");
        }

        result.Version = (byte)(packet[0] >> 4);

        if (result.Version != 4)
        {
            return IpHeaderParseError.InvalidVersion;
        }

        result.TotalLength = BinaryPrimitives.ReadUInt16BigEndian(packet[2..]);
        result.HeaderWordLength = (byte)(packet[0] & 0x0F);

        if (result.HeaderWordLength < 5 || result.HeaderByteLength > result.TotalLength ||
            packet.Length < result.HeaderByteLength)
        {
            return IpHeaderParseError.InvalidHeaderLength;
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
        result.IsChecksumValid = CalculateChecksum(packet) == 0;

        return result;
    }

    public static IpV4Header ForData(uint destination, Protocol protocol, ReadOnlySpan<byte> data)
    {
        ushort identification = _nextIdentification;
        unchecked
        {
            _nextIdentification++;
        }

        var header = new IpV4Header
        {
            Version = 4,
            HeaderWordLength = 5,
            TypeOfService = 0,
            TotalLength = (ushort)(data.Length + 20),
            FragmentationFlags = FragmentationFlags.NoFragment,
            FragmentOffset = 0,
            TimeToLive = 64,
            Protocol = protocol,
            SourceAddress = 0x0A_00_00_02,
            DestinationAddress = destination,
            Identification = identification,
            Checksum = 0 // Explicitly set checksum to 0 just to be sure
        };

        byte[] bytes = header.GetBytes();
        header.Checksum = CalculateChecksum(bytes);
        return header;
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

        return ChecksumHelper.Calculate(packet, headerByteLength);
    }

    public byte[] GetBytes()
    {
        var buffer = new byte[HeaderByteLength].AsSpan();
        buffer[0] = (byte)(Version << 4);
        buffer[0] = (byte)(buffer[0] | HeaderWordLength);
        buffer[1] = TypeOfService;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], TotalLength);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[4..], Identification);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[6..], (ushort)(FragmentOffset & 0x1FFF));
        buffer[6] = (byte)(buffer[6] | ((byte)FragmentationFlags << 5));
        buffer[8] = TimeToLive;
        buffer[9] = (byte)Protocol;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[10..], Checksum);
        BinaryPrimitives.WriteUInt32BigEndian(buffer[12..], SourceAddress);
        BinaryPrimitives.WriteUInt32BigEndian(buffer[16..], DestinationAddress);
        return buffer.ToArray();
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