namespace HttpFromScratch.Device.Linux.Enums;

[Flags]
public enum OpenFlags
{
    ReadOnly    = 0,
    WriteOnly   = 1,
    ReadWrite   = 2,
    CloseOnExec = 0x80000
}