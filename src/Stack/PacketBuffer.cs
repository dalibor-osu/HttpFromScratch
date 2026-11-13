using HttpFromScratch.Common.Assertion;

namespace HttpFromScratch.Stack;

public sealed class PacketBuffer
{
    private const int BufferSize = 2048;
    private const int DefaultHeadroomSize = 128;

    private readonly byte[] _buffer = new byte[BufferSize];

    private int _start;
    private int _end;

    public int Start
    {
        get;
        private set
        {
            Assert.That(value >= 0);
            Assert.That(value <= BufferSize);
            Assert.That(value <= End);

            _start = value;
        }
    } = DefaultHeadroomSize;

    public int End
    {
        get;
        private set
        {
            Assert.That(value >= 0);
            Assert.That(value <= BufferSize);
            Assert.That(value >= Start);

            _end = value;
        }
    } = DefaultHeadroomSize;

    public Span<byte> Data => _buffer.AsSpan()[Start..End];
    public int Length => End - Start;

    public Span<byte> Prepend(int n)
    {
        Assert.That(n > 0);
        Start -= n;

        return _buffer.AsSpan()[Start..(Start + n)];
    }

    public void Pull(int n)
    {
        Assert.That(n > 0);
        Start += n;
    }

    public Span<byte> Append(int n)
    {
        Assert.That(n > 0);
        End += n;

        return _buffer.AsSpan()[(End - n)..End];
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