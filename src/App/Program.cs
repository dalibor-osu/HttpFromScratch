using System.Runtime.InteropServices;
using HttpFromScratch.Common;
using HttpFromScratch.Device.Linux;
using HttpFromScratch.Stack;
using HttpFromScratch.Stack.Icmp;
using HttpFromScratch.Stack.Ip;

using var device = TunDevice.Open("tun0");

device.PacketEvent += (sender, ref packet) =>
{
    IpV4Header.Parse(packet).Match(packet, (state, header) =>
    {
        Console.WriteLine("Received packet:");
        Console.WriteLine(Util.HexDump(state));
        var packetBuffer = PacketBuffer.Instance;
        bool result = header.Protocol switch
        {
            Protocol.ICMP => IcmpHandler.HandlePacket(state[header.HeaderByteLength..], header, packetBuffer),
            _ => false
        };

        if (!result)
        {
            return;
        }

        Console.WriteLine("Sending response packet:");
        Console.WriteLine(Util.HexDump(packetBuffer.Data));
        bool success = sender.SendIp(0, packetBuffer.Data);
        if (success)
        {
            Console.WriteLine("Response packet sent successfully");
        }
        else
        {
            Console.WriteLine($"Failed to send response packet: {Marshal.GetLastPInvokeError()}");
        }
        Console.WriteLine("------------------------------------------");
    }, static (state, error) =>
    {
        // Console.WriteLine($"Skipping unknown packet: {error.Message}");
    });
};

bool shouldStop = false;

Console.CancelKeyPress += CancelHandler;

while (!Volatile.Read(ref shouldStop))
{
    if (!device.Wait(500))
    {
        continue;
    }

    device.ProcessIncoming();
}

return;

void CancelHandler(object? sender, ConsoleCancelEventArgs args)
{
    shouldStop = true;
    args.Cancel = true;
}