using HttpFromScratch.Common.Results;
using HttpFromScratch.Stack.Ip;

namespace HttpFromScratch.Stack;

public interface IPacketHandler
{
    public bool HandlePacket(ReadOnlySpan<byte> packet, IpV4Header ipHeader, ref PacketBuffer response);
}