namespace HttpFromScratch.Device.Linux.Enums;

[Flags]
public enum IfreqFlags : short
{
    None    = 0x0000,
    Tun     = 0x0001,
    Tap     = 0x0002,
    Napi    = 0x0010,
    NoPi    = 0x1000,
}