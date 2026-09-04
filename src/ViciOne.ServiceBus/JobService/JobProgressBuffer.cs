using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ViciOne.ServiceBus.JobService.Messages;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides a job progress buffer implementation.
/// </summary>
public class JobProgressBuffer
{
    readonly Channel<ProgressUpdate> _channel;
    readonly INotifyJobContext _notifyJobContext;
    readonly ProgressBufferSettings _settings;
    readonly TimeProvider _timeProvider;

    readonly Task _updateTask;
    long _latestSequenceNumber;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="notifyJobContext">The notify job context value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="settings">The settings value.</param>
    public JobProgressBuffer(INotifyJobContext notifyJobContext, TimeProvider timeProvider, ProgressBufferSettings? settings = null)
    {
        _notifyJobContext = notifyJobContext ?? throw new ArgumentNullException(nameof(notifyJobContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _settings = settings ?? new ProgressBufferSettings();

        var channelOptions = new BoundedChannelOptions(_settings.UpdateLimit)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<ProgressUpdate>(channelOptions);
        _updateTask = WaitForUpdateAsync();
    }

    /// <summary>
    /// Performs the flush operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _channel.Writer.TryComplete();

        return _updateTask;
    }

    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <param name="progress">The progress value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
        catch (ChannelClosedException)
        {
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "WaitForUpdate Faulted");
        }
    }

    async Task ReadUpdateAsync()
    {
        using var updateToken = new CancellationTokenSource(_settings.TimeLimit, _timeProvider);

        try
        {
            ProgressUpdate? latestUpdate = null;
            try
            {
                var updateId = 0;

                while (updateId < _settings.UpdateLimit)
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
                try
                {
                    await _notifyJobContext.NotifyJobProgressAsync(new SetJobProgressCommand
                    {
                        JobId = latestUpdate.Value.JobId,
                        AttemptId = latestUpdate.Value.AttemptId,
                        SequenceNumber = ++_latestSequenceNumber,
                        Value = latestUpdate.Value.Value,
                        Limit = latestUpdate.Value.Limit
                    }).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    LogContext.Error?.Log(exception, "Unable to update job progress: {JobId} {AttemptId} {SequenceNumber} {Value} {Limit}",
                        latestUpdate?.JobId, latestUpdate?.AttemptId, _latestSequenceNumber, latestUpdate?.Value, latestUpdate?.Limit);
                }
            }
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == updateToken.Token)
        {
            LogContext.Debug?.Log("operation canceled exception");
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "ReadUpdate faulted");
        }
    }


    /// <summary>
    /// Represents a progress update value.
    /// </summary>
    public readonly struct ProgressUpdate
    {
        /// <summary>
        /// Defines the job id value.
        /// </summary>
        public readonly Guid JobId;
        /// <summary>
        /// Defines the attempt id value.
        /// </summary>
        public readonly Guid AttemptId;
        /// <summary>
        /// Defines the value value.
        /// </summary>
        public readonly long Value;
        /// <summary>
        /// Defines the limit value.
        /// </summary>
        public readonly long? Limit;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="jobId">The job id value.</param>
        /// <param name="attemptId">The attempt id value.</param>
        /// <param name="value">The value.</param>
        /// <param name="limit">The limit value.</param>
        public ProgressUpdate(Guid jobId, Guid attemptId, long value, long? limit)
        {
            JobId = jobId;
            AttemptId = attemptId;
            Value = value;
            Limit = limit;
        }
    }
}
