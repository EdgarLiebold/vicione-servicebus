using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>
/// Provides a batch checkpointer implementation.
/// </summary>
public class BatchCheckpointer :
    ICheckpointer
{
    readonly Channel<IPendingConfirmation> _channel;
    readonly Task _checkpointTask;
    readonly ReceiveSettings _settings;
    readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public BatchCheckpointer(ReceiveSettings settings, CancellationToken cancellationToken)
    {
        _settings = settings;
        _cancellationToken = cancellationToken;
        var channelOptions = new BoundedChannelOptions(settings.CheckpointMessageLimit)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        };

        _channel = Channel.CreateBounded<IPendingConfirmation>(channelOptions);
        _checkpointTask = WaitForBatchAsync();
    }

    /// <summary>
    /// Performs the pending operation.
    /// </summary>
    /// <param name="confirmation">The confirmation value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task PendingAsync(IPendingConfirmation confirmation, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(confirmation, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();

        await _checkpointTask.ConfigureAwait(false);
    }

    async Task WaitForBatchAsync()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync(_cancellationToken).ConfigureAwait(false))
                await ReadBatchAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ChannelClosedException)
        {
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "WaitForBatch Faulted");
        }
    }

    async Task ReadBatchAsync()
    {
        var timeoutToken = new CancellationTokenSource(_settings.CheckpointInterval);
        var batchToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, _cancellationToken);
        var batch = new List<IPendingConfirmation>(_settings.CheckpointMessageCount);

        try
        {
            try
            {
                while (batch.Count < _settings.CheckpointMessageCount)
                {
                    if (_channel.Reader.TryRead(out var confirmation))
                    {
                        await confirmation.Confirmed.OrCanceledAsync(_cancellationToken).ConfigureAwait(false);
                        batch.Add(confirmation);
                    }
                    else if (await _channel.Reader.WaitToReadAsync(batchToken.Token).ConfigureAwait(false) == false)
                    {
                        break;
                    }
                }
            }
            catch (Exception) when (batch.Count > 0)
            {
            }

            await CheckpointAsync(batch).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == batchToken.Token)
        {
        }
        catch (Exception exception)
        {
            for (var i = 0; i < batch.Count; i++)
                batch[i].Faulted(exception);
        }
        finally
        {
            batchToken.Dispose();
            timeoutToken.Dispose();
        }
    }

    async Task CheckpointAsync(List<IPendingConfirmation> batch)
    {
        for (var i = batch.Count - 1; i >= 0; i--)
        {
            if (await TryCheckpointAsync(batch[i]).ConfigureAwait(false) == false)
                continue;

            batch.RemoveRange(0, i + 1);
            return;
        }
    }

    async Task<bool> TryCheckpointAsync(IPendingConfirmation confirmation)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        LogContext.Debug?.Log("Partition: {PartitionId} updating checkpoint with offset: {Offset}", confirmation.Partition.PartitionId,
            confirmation.OffsetString);

        try
        {
            await confirmation.CheckpointAsync(_cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "Partition: {PartitionId} checkpoint failed with offset: {Offset}", confirmation.Partition,
                confirmation.OffsetString);
            confirmation.Faulted(exception);
            return false;
        }
    }
}
