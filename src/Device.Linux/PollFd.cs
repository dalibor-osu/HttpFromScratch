using System.Runtime.InteropServices;
using HttpFromScratch.Device.Linux.Enums;

namespace HttpFromScratch.Device.Linux;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal struct PollFd
{
    [FieldOffset(0)] public int FileDescriptor;
    [FieldOffset(4)] public PollEvents Events;
    [FieldOffset(6)] public PollEvents Revents;
};