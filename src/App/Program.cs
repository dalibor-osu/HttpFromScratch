using HttpFromScratch.Common;
using HttpFromScratch.Device.Linux;
using HttpFromScratch.Stack.Ip;

using var device = TunDevice.Open("tun0");
device.PacketEvent += (sender, ref packet) =>
{
    IpV4Header.Parse(packet).Match(packet, static (state, header) =>
    {
        string dump = Util.HexDump(state);
        Console.WriteLine(dump);
        Console.WriteLine(header.ToString());
        Console.WriteLine($"Header checksum: {header.Checksum:X}, Calculated checksum: {IpV4Header.CalculateChecksum(state):X}");
    }, static (state, error) =>
    {
        Console.WriteLine($"Skipping unknown packet: {error.Message}");
    });

    Console.WriteLine("------------------------------------------");
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