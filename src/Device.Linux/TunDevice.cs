using System.Buffers;
using System.Runtime.InteropServices;
using HttpFromScratch.Device.Linux.Enums;

namespace HttpFromScratch.Device.Linux;

public sealed class TunDevice : IDisposable
{
    public delegate void PacketEventHandler(object sender, ref Span<byte> packet);

    public event PacketEventHandler? PacketEvent;

    private int _fileDescriptor;

    private const string TunPath = "/dev/net/tun";

    private TunDevice(int fileDescriptor)
    {
        _fileDescriptor = fileDescriptor;
    }

    public static TunDevice Open(string name)
    {
        int fileDescriptor = Libc.Open(TunPath, OpenFlags.ReadWrite);
        if (fileDescriptor == -1)
        {
            int errorCode = Marshal.GetLastPInvokeError();
            throw new LinuxException($"Failed to open TunDevice ({TunPath}). Error code: {errorCode}");
        }

        var ifreq = new Ifreq() { Name = IfreqName.FromString(name), Flags = IfreqFlags.Tun | IfreqFlags.NoPi };

        const ulong TUNSETIFF = 0x400454CA;
        int ioctlResult = Libc.Ioctl(fileDescriptor, TUNSETIFF, ref ifreq);
        if (ioctlResult != 0)
        {
            int errorCode = Marshal.GetLastPInvokeError();
            Libc.Close(fileDescriptor);
            throw new LinuxException(
                $"Failed occurred when calling ioctl on TUN device {name}. Error code: {errorCode}");
        }

        return new TunDevice(fileDescriptor);
    }

    public int Read(Span<byte> buffer)
    {
        IntPtr result = Libc.Read(_fileDescriptor, buffer, (nuint)buffer.Length);
        if (result != -1)
        {
            return result.ToInt32();
        }

        int errorCode = Marshal.GetLastPInvokeError();
        if (errorCode == 4)
        {
            return Read(buffer);
        }

        throw new LinuxException($"Failed to read TUN device. Error code: {errorCode}");
    }

    public void ProcessIncoming()
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(2048);
        var span = buffer.AsSpan();
        int readResult = Read(span);
        if (readResult > 0)
        {
            PacketEvent?.Invoke(this, ref span);
        }

        ArrayPool<byte>.Shared.Return(buffer, true);
    }

    public bool Wait(int timeoutMs)
    {
        PollFd[] polls =
            [new() { FileDescriptor = _fileDescriptor, Events = PollEvents.In, Revents = PollEvents.None }];

        Libc.Poll(polls, (nuint)polls.Length, timeoutMs);
        return polls[0].Revents.HasFlag(PollEvents.In);
    }

    public void SendIp(int destination, ReadOnlySpan<byte> packet)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (_fileDescriptor == -1)
        {
            return;
        }

        Libc.Close(_fileDescriptor);
        _fileDescriptor = -1;
    }
}