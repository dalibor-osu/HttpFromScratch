using System.Runtime.InteropServices;
using HttpFromScratch.Device.Linux.Enums;

namespace HttpFromScratch.Device.Linux;

public sealed class TunDevice : IDisposable
{
    private int _fileDescriptor;

    private const string TunPath = "/dev/net/tun";

    private TunDevice(int fileDescriptor)
    {
        _fileDescriptor = fileDescriptor;
    }

    public static TunDevice Open(string name)
    {
        int fileDescriptor = Linux.Open(TunPath, OpenFlags.ReadWrite);
        if (fileDescriptor == -1)
        {
            int errorCode = Marshal.GetLastPInvokeError();
            throw new LinuxException($"Failed to open TunDevice ({TunPath}). Error code: {errorCode}");
        }

        var ifreq = new Ifreq()
        {
            Name = IfreqName.FromString(name),
            Flags = IfreqFlags.Tun | IfreqFlags.NoPi
        };

        const ulong TUNSETIFF = 0x400454CA;
        int ioctlResult = Linux.Ioctl(fileDescriptor, TUNSETIFF, ref ifreq);
        if (ioctlResult != 0)
        {
            int errorCode = Marshal.GetLastPInvokeError();
            Linux.Close(fileDescriptor);
            throw new LinuxException($"Failed occurred when calling ioctl on TUN device {name}. Error code: {errorCode}");
        }

        return new TunDevice(fileDescriptor);
    }

    public int Read(Span<byte> buffer)
    {
        IntPtr result = Linux.Read(_fileDescriptor, buffer, (nuint)buffer.Length);
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

    public void Write(ReadOnlySpan<byte> packet)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (_fileDescriptor == -1)
        {
            return;
        }

        Linux.Close(_fileDescriptor);
        _fileDescriptor = -1;
    }
}