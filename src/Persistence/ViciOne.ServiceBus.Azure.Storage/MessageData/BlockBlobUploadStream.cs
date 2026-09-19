using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace ViciOne.ServiceBus.Azure.Storage.MessageData;

/// <summary>Stages a bounded sequence of block-blob writes and exposes the result only after an explicit commit.</summary>
internal sealed class BlockBlobUploadStream :
    Stream
{
    private readonly BlockBlobClient _client;
    private readonly List<string> _blockIds = [];
    private readonly string _blockIdPrefix = Guid.NewGuid().ToString("N");
    private readonly byte[] _buffer;
    private int _bufferedLength;
    private bool _committed;
    private bool _disposed;

    /// <summary>Creates a buffered stream for one block blob.</summary>
    /// <param name="client">The client used to stage and commit blocks.</param>
    /// <param name="bufferSize">The positive maximum number of bytes retained between block uploads.</param>
    public BlockBlobUploadStream(BlockBlobClient client, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        _client = client;
        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        BufferSize = bufferSize;
    }

    /// <summary>Gets the maximum number of payload bytes retained before a block is staged.</summary>
    public int BufferSize { get; }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !_disposed && !_committed;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>Commits all staged blocks as a new blob with the supplied properties.</summary>
    /// <param name="metadata">Optional metadata stored with the blob.</param>
    /// <param name="contentEncoding">The HTTP content encoding applied to the staged bytes.</param>
    /// <param name="cancellationToken">The token used to cancel staging or commit.</param>
    public async Task CommitAsync(
        IDictionary<string, string>? metadata,
        string contentEncoding,
        CancellationToken cancellationToken)
    {
        ThrowIfUnavailable();
        ArgumentException.ThrowIfNullOrWhiteSpace(contentEncoding);

        await StageBufferedBlockAsync(cancellationToken).ConfigureAwait(false);
        await _client.CommitBlockListAsync(
                _blockIds,
                new CommitBlockListOptions
                {
                    Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
                    HttpHeaders = new BlobHttpHeaders { ContentEncoding = contentEncoding },
                    Metadata = metadata,
                },
                cancellationToken)
            .ConfigureAwait(false);

        _committed = true;
    }

    public override void Flush()
    {
        ThrowIfUnavailable();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfUnavailable();
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Synchronous writes are not supported.");

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();

        while (!buffer.IsEmpty)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int writableLength = Math.Min(BufferSize - _bufferedLength, buffer.Length);
            buffer[..writableLength].CopyTo(_buffer.AsMemory(_bufferedLength));
            _bufferedLength += writableLength;
            buffer = buffer[writableLength..];

            if (_bufferedLength == BufferSize)
                await StageBufferedBlockAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    private async Task StageBufferedBlockAsync(CancellationToken cancellationToken)
    {
        if (_bufferedLength == 0)
            return;

        string blockId = Convert.ToBase64String(
            Encoding.ASCII.GetBytes(
                _blockIdPrefix + _blockIds.Count.ToString("D8", CultureInfo.InvariantCulture)));
        using var content = new MemoryStream(_buffer, 0, _bufferedLength, writable: false, publiclyVisible: true);
        await _client.StageBlockAsync(blockId, content, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _blockIds.Add(blockId);
        _bufferedLength = 0;
    }

    private void ThrowIfUnavailable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_committed)
            throw new InvalidOperationException("The block blob has already been committed.");
    }
}
