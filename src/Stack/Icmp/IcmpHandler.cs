using System.Buffers.Binary;
using HttpFromScratch.Common.Assertion;
using HttpFromScratch.Stack.Ip;

namespace HttpFromScratch.Stack.Icmp;

public static class IcmpHandler
{
    public static bool HandlePacket(ReadOnlySpan<byte> packet, IpV4Header ipHeader, PacketBuffer response)
    {
        if (packet.Length < 8 || ChecksumHelper.Calculate(packet, packet.Length) != 0)
        {
            return false;
        }

        var header = IcmpHeader.Parse(packet);
        if (header is not IcmpHeader icmpHeader)
        {
            return false;
        }

        if (icmpHeader.Code != 0 || icmpHeader.Type != IcmpMessageType.EchoRequest)
        {
            return false;
        }

        response.Reset();

        byte[] responseIcmpHeaderBytes = new IcmpHeader() { Type = IcmpMessageType.EchoReply, Code = 0, OtherData = icmpHeader.OtherData }.GetBytes();
        var appendBuffer = response.Append(responseIcmpHeaderBytes);
        Assert.That(appendBuffer.Length == responseIcmpHeaderBytes.Length);
        response.Append(packet[8..]);

        ushort checksum = ChecksumHelper.Calculate(response.Data, response.Data.Length);
        BinaryPrimitives.WriteUInt16BigEndian(response.Data[2..], checksum);

        byte[] responseIpHeaderBytes = IpV4Header.ForData(ipHeader.SourceAddress, Protocol.ICMP, response.Data).GetBytes();
        var prependBuffer = response.Prepend(responseIpHeaderBytes);
        Assert.That(prependBuffer.Length == responseIpHeaderBytes.Length);

        return true;
    }
}