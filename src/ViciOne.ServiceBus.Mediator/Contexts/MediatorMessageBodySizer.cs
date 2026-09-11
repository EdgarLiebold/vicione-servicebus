using System.Text.Json;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Measures the mediator's canonical JSON body through a bounded counting stream.</summary>
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
        // The materialized runtime type defines the complete dispatched object shape, including
        // generated implementations of non-public contract interfaces.
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
