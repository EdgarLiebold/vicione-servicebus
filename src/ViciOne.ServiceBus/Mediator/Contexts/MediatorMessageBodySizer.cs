using System.Text.Json;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Measures the mediator's canonical JSON application body without retaining a second message copy.</summary>
static class MediatorMessageBodySizer
{
    public static async Task<long> MeasureAsync<T>(
        T message,
        JsonSerializerOptions serializerOptions,
        MessageLimits limits,
        Uri inputAddress,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(serializerOptions);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(inputAddress);

        await using var stream = new CountingMessageBodyStream(limits.MaxBodyBytes, inputAddress);
        // Mediator messages can be runtime-generated implementations of non-public contract
        // interfaces. Serializing the declared interface would ask the converter factory to
        // generate a second implementation and fails for an inaccessible interface. The object
        // that is actually dispatched is already materialized, so its concrete runtime type is
        // the canonical and complete shape to measure.
        await JsonSerializer.SerializeAsync(
                stream,
                message,
                message.GetType(),
                serializerOptions,
                cancellationToken)
            .ConfigureAwait(false);
        return stream.Length;
    }

    sealed class CountingMessageBodyStream : Stream
    {
        readonly Uri _inputAddress;
        readonly int _maximumBytes;
        long _length;

        public CountingMessageBodyStream(int maximumBytes, Uri inputAddress)
        {
            _maximumBytes = maximumBytes;
            _inputAddress = inputAddress;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _length;

        public override long Position
        {
            get => _length;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
            => cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;

        public override void Write(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (offset > buffer.Length - count)
                throw new ArgumentException("Offset and count must identify a valid buffer range.", nameof(offset));

            Advance(count);
        }

        public override void Write(ReadOnlySpan<byte> buffer) => Advance(buffer.Length);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            Advance(buffer.Length);
            return ValueTask.CompletedTask;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        void Advance(int count)
        {
            long actualBytes = checked(_length + count);
            if (actualBytes > _maximumBytes)
                throw new MessageTooLargeException(actualBytes, _maximumBytes, _inputAddress);

            _length = actualBytes;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
