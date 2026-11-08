using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HttpFromScratch.Device.Linux.Enums;

namespace HttpFromScratch.Device.Linux;

[StructLayout(LayoutKind.Explicit, Size = 40)]
internal struct Ifreq
{
    [FieldOffset(0)] public IfreqName Name;
    [FieldOffset(16)] public IfreqFlags Flags;
}

[InlineArray(16)]
internal struct IfreqName
{
    public byte Value;

    public static IfreqName FromString(string str)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(str);
        int size = bytes.Length < 15 ? bytes.Length : 15;
        var result = new IfreqName();

        for (int i = 0; i < size; i++)
        {
            result[i] = bytes[i];
        }

        result[15] = 0;
        return result;
    }
}