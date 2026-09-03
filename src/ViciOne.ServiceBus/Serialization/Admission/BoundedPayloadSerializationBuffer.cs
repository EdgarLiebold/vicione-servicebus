#nullable enable

namespace ViciOne.ServiceBus.Serialization;

using System;
using System.Threading;

/// <summary>
/// Managed writer that never owns more memory than its configured hard maximum.
/// </summary>
internal sealed class BoundedPayloadSerializationBuffer : IPayloadSerializationBuffer
{
    private readonly int _maximumBytes;
    private readonly Action<PayloadAdmissionException>? _rejectionObserver;
    private readonly PayloadAdmissionStage _stage;
    private readonly byte[] _buffer;
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
        // IBufferWriter callers may reserve conservatively (Utf8JsonWriter reserves for
        // worst-case escaping). Supplying the entire hard-bounded region up front keeps
        // an exact-size payload admissible without a sizing pass, while the owner can
        // still never retain more than the configured maximum.
        _buffer = new byte[maximumBytes];
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

        // The constructor owns the complete bounded region, so a request that fits
        // the hard maximum is already satisfiable without reallocating.
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
