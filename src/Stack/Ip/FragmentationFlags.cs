namespace HttpFromScratch.Stack.Ip;

[Flags]
public enum FragmentationFlags : byte
{
    NoFragment = 0b010,
    MoreFragments = 0b001
}