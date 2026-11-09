namespace HttpFromScratch.Device.Linux.Enums;

[Flags]
public enum PollEvents : short
{
    None    = 0x0000,
    In      = 0x0001,
    Pri     = 0x0002,
    Out     = 0x0004,
    Error   = 0x0080,
    HangUp  = 0x0100,
    Invalid = 0x0200,
}