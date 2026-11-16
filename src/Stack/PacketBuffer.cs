using HttpFromScratch.Common.Assertion;

namespace HttpFromScratch.Stack;

public sealed class PacketBuffer
{
    private const int BufferSize = 2048;
    private const int DefaultHeadroomSize = 128;

    private readonly byte[] _buffer = new byte[BufferSize];

    private int _start = DefaultHeadroomSize;
    private int _end = DefaultHeadroomSize;

    private PacketBuffer()
    { }

    public static PacketBuffer Instance
    {
        get
        {
            field ??= new PacketBuffer();
            return field;
        }
    } = null;

    public int Start
    {
        get => _start;
        private set
        {
            Assert.That(value >= 0);
            Assert.That(value <= BufferSize);
            Assert.That(value <= End);

            _start = value;
        }
    }

    public int End
    {
        get => _end;
        private set
        {
            Assert.That(value >= 0);
            Assert.That(value <= BufferSize);
            Assert.That(value >= Start);

            _end = value;
        }
    }

    public Span<byte> Data => _buffer.AsSpan()[Start..End];
    public int Length => End - Start;

    public Span<byte> Prepend(int n)
    {
        Assert.That(n >= 0);
        if (n == 0)
        {
            return Span<byte>.Empty;
        }

        Start -= n;

        return _buffer.AsSpan()[Start..(Start + n)];
    }

    public Span<byte> Prepend(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0)
        {
            return Span<byte>.Empty;
        }

        var data = Prepend(buffer.Length);
        buffer.CopyTo(data);
        return data;
    }

    public void Pull(int n)
    {
        Assert.That(n >= 0);
        Start += n;
    }

    public Span<byte> Append(int n)
    {
        Assert.That(n >= 0);
        if (n == 0)
        {
            return Span<byte>.Empty;
        }

        End += n;

        return _buffer.AsSpan()[(End - n)..End];
    }

    public Span<byte> Append(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0)
        {
            return Span<byte>.Empty;
        }

        var data = Append(buffer.Length);
        buffer.CopyTo(data);
        return data;
    }

    public void Trim(int length)
    {
        Assert.That(length >= 0);

        if (length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        End = Start + length;
    }

    public void Reset(int headroom = DefaultHeadroomSize)
    {
        Array.Clear(_buffer);
        _start = headroom;
        _end = headroom;
    }
}