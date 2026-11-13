using System.Runtime.CompilerServices;

namespace HttpFromScratch.Common.Results;

// Written like this to prevent allocation on heap
[Union]
public readonly struct Result<TResult, TErr> : IUnion where TErr : struct, Enum
{
    private enum Tag : byte
    {
        None = 0,
        Ok,
        Error,
    }

    private readonly TResult _value;
    private readonly Error<TErr> _error;
    private readonly Tag _tag;

    public Result(TResult value)
    {
        _value = value;
        _error = default;
        _tag = Tag.Ok;
    }

    public Result(Error<TErr> error)
    {
        _value = default!;
        _error = error;
        _tag = Tag.Error;
    }

    public bool Ok => _tag == Tag.Ok;

    public object? Value => _tag switch
    {
        Tag.Ok => _value,
        Tag.Error => _error,
        _ => null,
    };

    public bool HasValue => _tag != Tag.None;

    public bool TryGetValue(out TResult value)
    {
        value = _value;
        return _tag == Tag.Ok;
    }

    public bool TryGetValue(out Error<TErr> error)
    {
        error = _error;
        return _tag == Tag.Error;
    }

    // State should be anything that the match actions need instead of capturing outer variables which would introduce heap allocation
    public void Match<TState>(TState state, Action<TState, TResult> onSuccess, Action<TState, Error<TErr>> onError) where TState : allows ref struct
    {
        switch (_tag)
        {
            case Tag.Ok:
                onSuccess(state, _value);
                break;
            case Tag.Error:
                onError(state, _error);
                break;
            case Tag.None:
                throw new InvalidOperationException("Result is uninitialized (default)");
            default:
                throw new InvalidOperationException();
        }
    }

    public TOut Match<TState, TOut>(TState state, Func<TState, TResult, TOut> onSuccess, Func<TState, Error<TErr>, TOut> onError) where TState : allows ref struct
    {
        switch (_tag)
        {
            case Tag.Ok:
                return onSuccess(state, _value);
            case Tag.Error:
                return onError(state, _error);
            case Tag.None:
                throw new InvalidOperationException("Result is uninitialized (default)");
            default:
                throw new InvalidOperationException();
        }
    }

    public static implicit operator Result<TResult, TErr>(TResult value) => new(value);
    public static implicit operator Result<TResult, TErr>(Error<TErr> error) => new(error);
    public static implicit operator Result<TResult, TErr>(TErr err) => new((Error<TErr>)err);
    public static implicit operator Result<TResult, TErr>((TErr err, string message) tuple) =>
        new(new Error<TErr>(tuple.err, tuple.message));
}
