namespace HttpFromScratch.Common.Results;

// Written like this to prevent allocation on heap
public readonly record struct Error<TErr>(TErr Type, string? Message = null) where TErr : struct, Enum
{
    public string Message { get => field ?? Type.ToString(); init; } = Message;

    public static implicit operator Error<TErr>(TErr err) => new(err);
    public static implicit operator Error<TErr>((TErr err, string message) tuple) => new(tuple.err, tuple.message);
}
