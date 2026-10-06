using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Coalesces progress updates and publishes the newest value within each size or time window.</summary>
internal sealed class JobProgressBuffer
{
    readonly Channel<ProgressUpdate> _channel;
    readonly INotifyJobContext _notifyJobContext;
    readonly JobProgressBufferOptions _options;
    readonly TimeProvider _timeProvider;

    readonly Task _updateTask;
    long _latestSequenceNumber;

    /// <summary>Starts a single-reader buffer for one job attempt.</summary>
    /// <param name="notifyJobContext">The publisher that forwards coalesced progress to the coordinator.</param>
    /// <param name="timeProvider">The clock used to enforce the maximum buffering time.</param>
    /// <param name="options">The optional batching limits; defaults are used when omitted.</param>
    public JobProgressBuffer(INotifyJobContext notifyJobContext, TimeProvider timeProvider, JobProgressBufferOptions? options = null)
    {
        _notifyJobContext = notifyJobContext ?? throw new ArgumentNullException(nameof(notifyJobContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _options = options ?? new JobProgressBufferOptions();
        if (_options.UpdateLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), _options.UpdateLimit, "The progress update limit must be greater than zero.");
        if (_options.TimeLimit <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), _options.TimeLimit, "The progress time limit must be greater than zero.");

        var channelOptions = new BoundedChannelOptions(_options.UpdateLimit)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<ProgressUpdate>(channelOptions);
        _updateTask = WaitForUpdateAsync();
    }

    /// <summary>Stops accepting updates and publishes the newest buffered progress value.</summary>
    /// <param name="cancellationToken">The token that cancels waiting for the background publisher.</param>
    /// <returns>A task that completes when all accepted progress has been published.</returns>
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        _channel.Writer.TryComplete();

        return _updateTask.WaitAsync(cancellationToken);
    }

    /// <summary>Queues a progress value for ordered, coalesced publication.</summary>
    /// <param name="progress">The progress value associated with the current attempt.</param>
    /// <param name="cancellationToken">The token that cancels waiting for buffer capacity.</param>
    /// <returns>A task that completes when the value has entered the buffer.</returns>
    public async Task UpdateAsync(ProgressUpdate progress, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(progress, cancellationToken).ConfigureAwait(false);
    }

    async Task WaitForUpdateAsync()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
                await ReadUpdateAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Wake pending writers and reject new progress when the sole reader fails.
            _channel.Writer.TryComplete(exception);
            throw;
        }
        finally
        {
            _channel.Writer.TryComplete();
        }
    }

    async Task ReadUpdateAsync()
    {
        using var updateToken = new CancellationTokenSource(_options.TimeLimit, _timeProvider);

        try
        {
            ProgressUpdate? latestUpdate = null;
            try
            {
                var updateId = 0;

                while (updateId < _options.UpdateLimit)
                {
                    if (_channel.Reader.TryRead(out var update))
                    {
                        latestUpdate = update;
                        updateId++;
                    }
                    else if (await _channel.Reader.WaitToReadAsync(updateToken.Token).ConfigureAwait(false) == false)
                        break;
                }
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == updateToken.Token && latestUpdate != null)
            {
            }

            if (latestUpdate.HasValue)
            {
                await _notifyJobContext.NotifyProgressAsync(new SetJobProgressCommand
                {
                    JobId = latestUpdate.Value.JobId,
                    AttemptId = latestUpdate.Value.AttemptId,
                    SequenceNumber = ++_latestSequenceNumber,
                    Value = latestUpdate.Value.Value,
                    Limit = latestUpdate.Value.Limit
                }).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == updateToken.Token)
        {
        }
    }


    internal readonly record struct ProgressUpdate(Guid JobId, Guid AttemptId, long Value, long? Limit);
}
