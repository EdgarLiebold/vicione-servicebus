using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public abstract class Batcher<TEntry> :
    IBatcher<TEntry>
{
    readonly Task _batchTask;
    readonly Channel<BatchEntry<TEntry>> _channel;
    readonly TaskExecutor _executor;
    readonly BatchSettings _settings;

    protected Batcher(BatchSettings? settings = null)
    {
        _settings = settings ?? ClientContextBatchSettings.GetBatchSettings();

        var channelOptions = new BoundedChannelOptions(_settings.MessageLimit * 10)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<BatchEntry<TEntry>>(channelOptions);
        _executor = new TaskExecutor(2, _settings.BatchLimit);
        _batchTask = WaitForBatch();
    }

    public async Task Execute(TEntry entry, CancellationToken cancellationToken)
    {
        var batchEntry = new BatchEntry<TEntry>(entry);

        await _channel.Writer.WriteAsync(batchEntry, cancellationToken).ConfigureAwait(false);

        await batchEntry.Completed.ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();

        await _batchTask.ConfigureAwait(false);

        await _executor.DisposeAsync().ConfigureAwait(false);
    }

    async Task WaitForBatch()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
                await ReadBatch().ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "WaitForBatch Faulted");
        }
    }

    async Task ReadBatch()
    {
        var batchToken = new CancellationTokenSource(_settings.Timeout);
        var batch = new List<BatchEntry<TEntry>>(_settings.MessageLimit);
        try
        {
            try
            {
                var entryId = 0;
                var batchLength = 0;

                while (entryId < _settings.MessageLimit && batchLength < _settings.SizeLimit)
                {
                    if (_channel.Reader.TryPeek(out BatchEntry<TEntry>? entry))
                    {
                        var entryLength = CalculateEntryLength(entry.Entry, entryId.ToString());
                        if (entryId > 0 && entryLength + batchLength > _settings.SizeLimit)
                            break;

                        batchLength += entryLength;
                        batch.Add(entry);
                        entryId++;

                        await _channel.Reader.ReadAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    else if (await _channel.Reader.WaitToReadAsync(batchToken.Token).ConfigureAwait(false) == false)
                        break;
                }
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == batchToken.Token && batch.Count > 0)
            {
            }

            await _executor.EnqueueAsync(() => ExecuteBatch(batch), CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == batchToken.Token)
        {
        }
        catch (Exception exception)
        {
            foreach (BatchEntry<TEntry> entry in batch)
                entry.SetFaulted(exception);
        }
        finally
        {
            batchToken.Dispose();
        }
    }

    protected abstract int CalculateEntryLength(TEntry entry, string entryId);

    protected abstract Task SendBatch(IList<BatchEntry<TEntry>> batch);

    protected void ApplyResponse(
        IList<BatchEntry<TEntry>> batch,
        IEnumerable<string>? successfulIds,
        IEnumerable<(string Id, string Code, string Message)>? failures)
    {
        var completed = new HashSet<int>();
        var successful = new List<int>();
        var faulted = new List<(int EntryId, string Code, string Message)>();

        foreach (var id in successfulIds ?? [])
        {
            var entryId = ParseEntryId(id, batch.Count);
            if (!completed.Add(entryId))
                throw new AmazonSqsTransportException($"The AWS batch response contains entry id '{id}' more than once.");

            successful.Add(entryId);
        }

        foreach (var failure in failures ?? [])
        {
            var entryId = ParseEntryId(failure.Id, batch.Count);
            if (!completed.Add(entryId))
                throw new AmazonSqsTransportException($"The AWS batch response contains entry id '{failure.Id}' more than once.");

            faulted.Add((entryId, failure.Code, failure.Message));
        }

        if (completed.Count != batch.Count)
            throw new AmazonSqsTransportException($"The AWS batch response accounted for {completed.Count} of {batch.Count} request entries.");

        foreach (var entryId in successful)
            batch[entryId].SetCompleted();

        foreach (var failure in faulted)
            batch[failure.EntryId].SetFaulted(new AmazonSqsTransportException($"Send failed: {failure.Code}-{failure.Message}"));
    }

    static int ParseEntryId(string id, int batchCount)
    {
        if (!int.TryParse(id, out var entryId) || entryId < 0 || entryId >= batchCount)
            throw new AmazonSqsTransportException($"The AWS batch response contains unknown entry id '{id}'.");

        return entryId;
    }

    async Task ExecuteBatch(IList<BatchEntry<TEntry>> batch)
    {
        try
        {
            await SendBatch(batch).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            foreach (BatchEntry<TEntry> entry in batch)
                entry.SetFaulted(exception);
        }
    }
}
