using System;
using System.Threading;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Managed writer that grows on demand without exceeding its configured buffer maximum.</summary>
internal sealed class BoundedPayloadSerializationBuffer : IPayloadSerializationBuffer
{
    private readonly int _maximumBytes;
    private readonly Action<PayloadAdmissionException>? _rejectionObserver;
    private readonly PayloadAdmissionStage _stage;
    private byte[] _buffer = Array.Empty<byte>();
    private int _rejectionObserved;
    private int _writtenCount;

    public BoundedPayloadSerializationBuffer(
        int maximumBytes,
        PayloadAdmissionStage stage,
        Action<PayloadAdmissionException>? rejectionObserver = null)
    {
        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown payload admission stage.");

        _maximumBytes = maximumBytes;
        _stage = stage;
        _rejectionObserver = rejectionObserver;
        // A limit is not a reservation: small messages must not allocate their full maxima.
    }

    public int WrittenCount => _writtenCount;

    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _writtenCount);

    public void Advance(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        int remaining = _buffer.Length - _writtenCount;
        if (count > remaining)
        {
            throw new InvalidOperationException(
                "The serializer advanced beyond the memory returned by the bounded serialization buffer.");
        }

        _writtenCount += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_writtenCount);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_writtenCount);
    }

    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeHint));

        int minimumAdditional = sizeHint == 0 ? 1 : sizeHint;
        long required = (long)_writtenCount + minimumAdditional;
        if (required > _maximumBytes)
        {
            throw CreateRejection(
                required,
                $"Serialization requested at least {required} bytes, exceeding the configured maximum of {_maximumBytes} bytes.");
        }

        if (required <= _buffer.Length)
            return;

        int doubled = _buffer.Length <= _maximumBytes / 2 ? _buffer.Length * 2 : _maximumBytes;
        int newLength = (int)Math.Min(_maximumBytes, Math.Max(required, Math.Max(256, doubled)));
        // Utf8JsonWriter may request a contiguous worst-case expansion larger than the
        // eventual JSON. Once the payload is substantial relative to its limit, expose
        // the complete remaining bounded region before such a speculative request occurs.
        if (newLength >= Math.Max(1, _maximumBytes / 4))
            newLength = _maximumBytes;
        Array.Resize(ref _buffer, newLength);
    }

    private PayloadAdmissionException CreateRejection(long actualBytes, string message)
    {
        var exception = new PayloadAdmissionException(_stage, actualBytes, _maximumBytes, message);
        if (_rejectionObserver is not null && Interlocked.Exchange(ref _rejectionObserved, 1) == 0)
        {
            try
            {
                _rejectionObserver(exception);
            }
            catch
            {
                // Observation cannot replace the original admission failure.
            }
        }

        return exception;
    }
}
