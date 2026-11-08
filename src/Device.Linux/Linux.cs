using System.Runtime.InteropServices;
using HttpFromScratch.Device.Linux.Enums;

namespace HttpFromScratch.Device.Linux;

internal static partial class Linux
{
    [LibraryImport("libc", EntryPoint = "open", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int Open(string path, OpenFlags openFlags);

    [LibraryImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    public static partial int Ioctl(int fileDescriptor, ulong operation, ref Ifreq ifreq);

    [LibraryImport("libc", EntryPoint = "read", SetLastError = true)]
    public static partial nint Read(int fileDescriptor, Span<byte> buffer, nuint count);

    [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
    public static partial int Close(int fileDescriptor);
}