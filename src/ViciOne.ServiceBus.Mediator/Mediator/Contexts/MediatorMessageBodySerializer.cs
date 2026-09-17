using System.Text.Json;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Creates the mediator's canonical JSON body without exceeding its configured byte limit.</summary>
static class MediatorMessageBodySerializer
{
    /// <summary>Serializes one materialized message into a bounded, owned body.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The materialized message.</param>
    /// <param name="serializerOptions">The JSON rules used for mediator body inspection.</param>
    /// <param name="limits">The maximum accepted body size.</param>
    /// <param name="endpointAddress">The mediator address reported by an admission failure.</param>
    /// <param name="cancellationToken">The token that cancels serialization.</param>
    /// <returns>A task containing the readable serialized body.</returns>
    public static async Task<MessageBody> SerializeAsync<T>(
        T message,
        JsonSerializerOptions serializerOptions,
        MessageLimits limits,
        Uri endpointAddress,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(serializerOptions);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(endpointAddress);
        cancellationToken.ThrowIfCancellationRequested();

        var stream = new BoundedMessageBodyStream(limits.MaxBodyBytes, endpointAddress);
        try
        {
            await JsonSerializer.SerializeAsync(
                    stream,
                    message,
                    message.GetType(),
                    serializerOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return stream.Complete();
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal sealed class BoundedMessageBodyStream : Stream
    {
        readonly Uri _endpointAddress;
        readonly int _maximumBytes;
        readonly MemoryStream _stream = new();
        bool _completed;

        public BoundedMessageBodyStream(int maximumBytes, Uri endpointAddress)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
            _maximumBytes = maximumBytes;
            _endpointAddress = endpointAddress ?? throw new ArgumentNullException(nameof(endpointAddress));
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => !_completed;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => throw new NotSupportedException();
        }

        public MessageBody Complete()
        {
            if (_completed)
                throw new InvalidOperationException("The mediator message body has already been completed.");

            byte[] content = _stream.GetBuffer();
            int length = checked((int)_stream.Length);
            _completed = true;
            _stream.Dispose();
            return StringMessageBody.TakeUtf8Ownership(content, length);
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;

        public override void Write(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (offset > buffer.Length - count)
                throw new ArgumentException("Offset and count must identify a valid buffer range.", nameof(offset));

            EnsureWritableCapacity(count);
            _stream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureWritableCapacity(buffer.Length);
            _stream.Write(buffer);
        }

        public override void WriteByte(byte value)
        {
            EnsureWritableCapacity(1);
            _stream.WriteByte(value);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        void EnsureWritableCapacity(int additionalBytes)
        {
            if (_completed)
                throw new NotSupportedException("A completed mediator message body cannot be modified.");

            long actualBytes = checked(_stream.Length + additionalBytes);
            if (actualBytes > _maximumBytes)
                throw new MessageTooLargeException(actualBytes, _maximumBytes, _endpointAddress);

            int requiredCapacity = checked((int)actualBytes);
            if (requiredCapacity <= _stream.Capacity)
                return;

            int growthTarget = _stream.Capacity == 0
                ? Math.Min(256, _maximumBytes)
                : checked((int)Math.Min((long)_stream.Capacity * 2, _maximumBytes));
            _stream.Capacity = Math.Max(requiredCapacity, growthTarget);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _stream.Dispose();

            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            _stream.Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
