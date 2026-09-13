using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides one job attempt with its payload, execution lifetime, metadata, progress, and checkpoint operations.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
internal sealed class ConsumeJobContext<TJob> :
    ConsumeContextProxy,
    ConsumeContext<TJob>,
    IJobContext<TJob>,
    INotifyJobContext,
    IAsyncDisposable
    where TJob : class
{
    readonly ConsumeContext<IStartJob> _context;
    readonly Uri _instanceAddress;
    readonly Lazy<JobProgressBuffer> _progressBuffer;
    readonly CancellationTokenSource _source;
    readonly long _startedAt;
    readonly TimeProvider _timeProvider;
    string? _cancellationReason;
    IReadOnlyDictionary<string, object>? _checkpoint;
    bool _checkpointChanged;

    /// <summary>Creates an execution context for one admitted job attempt.</summary>
    /// <param name="context">The start command and transport context.</param>
    /// <param name="instanceAddress">The endpoint address executing the attempt.</param>
    /// <param name="job">The deserialized job payload.</param>
    /// <param name="jobOptions">The validated options registered for the job type.</param>
    public ConsumeJobContext(ConsumeContext<IStartJob> context, Uri instanceAddress, TJob job, JobOptions<TJob> jobOptions)
        : base(GetAdvancedContext(context))
    {
        ArgumentNullException.ThrowIfNull(instanceAddress);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(jobOptions);

        _context = context;
        _instanceAddress = instanceAddress;

        JobId = context.Message.JobId;
        AttemptId = context.Message.AttemptId;
        RetryAttempt = context.Message.RetryAttempt;

        Job = job;

        LastProgressValue = context.Message.LastProgressValue;
        LastProgressLimit = context.Message.LastProgressLimit;

        JobProperties = new ReadOnlyJobPropertyCollection(context.Message.JobProperties);
        JobTypeProperties = new ReadOnlyJobPropertyCollection(jobOptions.JobTypePropertyValues);
        InstanceProperties = new ReadOnlyJobPropertyCollection(jobOptions.InstancePropertyValues);

        _timeProvider = context.GetTimeProvider();
        _source = new CancellationTokenSource(jobOptions.JobTimeout, _timeProvider);
        _startedAt = _timeProvider.GetTimestamp();
        var progressBufferOptions = new JobProgressBufferOptions
        {
            TimeLimit = jobOptions.ProgressBuffer.TimeLimit,
            UpdateLimit = jobOptions.ProgressBuffer.UpdateLimit,
        };
        _progressBuffer = new Lazy<JobProgressBuffer>(
            () => new JobProgressBuffer(this, _timeProvider, progressBufferOptions),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Gets the token canceled by the job timeout or an explicit attempt cancellation.</summary>
    public override CancellationToken CancellationToken => _source.Token;

    /// <summary>Gets the job payload delivered to the consumer.</summary>
    public TJob Message => Job;

    /// <summary>Forwards successful consumption to the underlying transport observers.</summary>
    /// <param name="duration">The time spent executing the job consumer.</param>
    /// <param name="consumerType">The consumer type reported to observers.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes when all observers have been notified.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(_context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Forwards a consumer failure to the underlying transport observers.</summary>
    /// <param name="duration">The time spent executing the job consumer.</param>
    /// <param name="consumerType">The consumer type reported to observers.</param>
    /// <param name="exception">The exception raised by the consumer.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes when all observers have been notified.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(_context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Flushes buffered progress before releasing the attempt cancellation source.</summary>
    /// <returns>A task that completes after pending progress has been published.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_progressBuffer.IsValueCreated)
                await _progressBuffer.Value.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _source.Dispose();
        }
    }

    /// <summary>Flushes pending progress and publishes cancellation with the attempt's final checkpoint update.</summary>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when cancellation has been published.</returns>
    public async Task NotifyCanceledAsync(CancellationToken cancellationToken = default)
    {
        LogContext.Debug?.Log("Job Canceled: {JobId} {AttemptId} ({RetryAttempt}) {Reason}", JobId, AttemptId, RetryAttempt, _cancellationReason);

        if (_progressBuffer.IsValueCreated)
            await _progressBuffer.Value.FlushAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        await NotifyAsync<IJobAttemptCanceled>(new JobAttemptCanceledEvent
        {
            JobId = JobId,
            AttemptId = AttemptId,
            Timestamp = UtcNow,
            Reason = string.IsNullOrWhiteSpace(_cancellationReason) ? JobCancellationReasons.ConsumerInitiated : _cancellationReason!,
            CheckpointChanged = _checkpointChanged,
            Checkpoint = _checkpoint
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Publishes attempt and typed job-started events before consumer execution begins.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after both start notifications have been sent.</returns>
    public async Task NotifyStartedAsync(CancellationToken cancellationToken = default)
    {
        LogContext.Debug?.Log("Job Started: {JobId} {AttemptId} ({RetryAttempt})", JobId, AttemptId, RetryAttempt);

        var timestamp = UtcNow;

        await NotifyAsync<IJobAttemptStarted>(new JobAttemptStartedEvent
        {
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            Timestamp = timestamp,
            InstanceAddress = _instanceAddress
        }, cancellationToken).ConfigureAwait(false);

        var endpoint = await _context.Advanced().ReceiveContext.PublishEndpointProvider.GetPublishSendEndpointAsync<IJobStarted<TJob>>(cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync<IJobStarted<TJob>>(new JobStartedEvent<TJob>
        {
            Job = Job,
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            Timestamp = timestamp
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Flushes pending progress updates and publishes successful attempt completion.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after buffered progress and the completion event have been sent.</returns>
    public async Task NotifyCompletedAsync(CancellationToken cancellationToken = default)
    {
        LogContext.Debug?.Log("Job Completed: {JobId} {AttemptId} ({RetryAttempt})", JobId, AttemptId, RetryAttempt);

        if (_progressBuffer.IsValueCreated)
            await _progressBuffer.Value.FlushAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        await NotifyAsync<IJobAttemptCompleted>(new JobAttemptCompletedEvent
        {
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            Timestamp = UtcNow,
            Duration = ElapsedTime,
            InstanceProperties = InstanceProperties,
            JobTypeProperties = JobTypeProperties,
            CheckpointChanged = _checkpointChanged,
            Checkpoint = _checkpoint
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Publishes a sequenced progress update to the job coordinator.</summary>
    /// <param name="progress">The progress message to publish.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the progress update has been sent.</returns>
    public Task NotifyProgressAsync(ISetJobProgress progress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(progress);
        return NotifyAsync(progress, cancellationToken);
    }

    /// <summary>Flushes pending progress and publishes failure with retry timing and the final checkpoint update.</summary>
    /// <param name="exception">The exception raised while executing the attempt.</param>
    /// <param name="delay">The optional delay before the next attempt.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the failure has been published.</returns>
    public async Task NotifyFaultedAsync(Exception exception, TimeSpan? delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        LogContext.Debug?.Log(exception, "Job Faulted: {JobId} {AttemptId} ({RetryAttempt})", JobId, AttemptId, RetryAttempt);

        if (_progressBuffer.IsValueCreated)
            await _progressBuffer.Value.FlushAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        await NotifyAsync<IJobAttemptFaulted>(new JobAttemptFaultedEvent
        {
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            RetryDelay = delay,
            Timestamp = UtcNow,
            Exceptions = new FaultExceptionInfo(exception),
            CheckpointChanged = _checkpointChanged,
            Checkpoint = _checkpoint
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets the stable identifier shared by every attempt of the job.</summary>
    public Guid JobId { get; }
    /// <summary>Gets the generation identifier of this execution attempt.</summary>
    public Guid AttemptId { get; }
    /// <summary>Gets the zero-based retry number of this attempt.</summary>
    public int RetryAttempt { get; }
    /// <summary>Gets the last progress value carried by this instance.</summary>
    public long? LastProgressValue { get; }
    /// <summary>Gets the optional limit associated with the restored progress value.</summary>
    public long? LastProgressLimit { get; }
    /// <summary>Gets the deserialized job payload.</summary>
    public TJob Job { get; }

    /// <summary>Gets the elapsed time measured by the configured time provider.</summary>
    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_startedAt);

    /// <summary>Queues the latest job progress for bounded, ordered publication.</summary>
    /// <param name="value">The current progress value.</param>
    /// <param name="limit">The optional upper bound associated with the value.</param>
    /// <param name="cancellationToken">The token that cancels waiting for buffer capacity.</param>
    /// <returns>A task that completes when the progress value has entered the buffer.</returns>
    public Task ReportProgressAsync(long value, long? limit = null, CancellationToken cancellationToken = default)
    {
        return _progressBuffer.Value.UpdateAsync(
            new JobProgressBuffer.ProgressUpdate(JobId, AttemptId, value, limit),
            cancellationToken);
    }

    /// <summary>Publishes a durable checkpoint for recovery by a later attempt.</summary>
    /// <typeparam name="TCheckpoint">The checkpoint contract type.</typeparam>
    /// <param name="checkpoint">The checkpoint to save, or <see langword="null" /> to clear it.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the checkpoint command has been sent.</returns>
    public Task SaveCheckpointAsync<TCheckpoint>(TCheckpoint? checkpoint, CancellationToken cancellationToken = default)
        where TCheckpoint : class
    {
        _checkpoint = checkpoint != null ? _context.Advanced().ToDictionary(checkpoint) : null;
        _checkpointChanged = true;

        return NotifyAsync<ISaveJobCheckpoint>(new SaveJobCheckpointCommand
        {
            JobId = JobId,
            AttemptId = AttemptId,
            Checkpoint = _checkpoint
        }, cancellationToken);
    }

    /// <summary>Attempts to deserialize the latest durable checkpoint.</summary>
    /// <typeparam name="TCheckpoint">The expected checkpoint contract type.</typeparam>
    /// <param name="checkpoint">Receives the checkpoint when present and compatible.</param>
    /// <returns><see langword="true" /> when a compatible checkpoint exists; otherwise, <see langword="false" />.</returns>
    public bool TryGetCheckpoint<TCheckpoint>([NotNullWhen(true)] out TCheckpoint? checkpoint)
        where TCheckpoint : class
    {
        if (_context.Message.Checkpoint != null)
        {
            checkpoint = _context.Advanced().SerializerContext.DeserializeObject<TCheckpoint>(_context.Message.Checkpoint);
            return checkpoint != null;
        }

        checkpoint = null;
        return false;
    }

    /// <summary>Gets metadata supplied with the current job.</summary>
    public IPropertyCollection JobProperties { get; }
    /// <summary>Gets metadata shared by all consumers of this job type.</summary>
    public IPropertyCollection JobTypeProperties { get; }
    /// <summary>Gets metadata of the service instance executing this attempt.</summary>
    public IPropertyCollection InstanceProperties { get; }

    DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    async Task NotifyAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        var endpoint = await _context.Advanced().ReceiveContext.PublishEndpointProvider
            .GetPublishSendEndpointAsync<T>(cancellationToken)
            .ConfigureAwait(false);

        await endpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    static ConsumeContext GetAdvancedContext(ConsumeContext<IStartJob> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Advanced();
    }

    /// <summary>Cancels the attempt and records the reason reported by its consumer pipeline.</summary>
    /// <param name="reason">The reason propagated with the cancellation event.</param>
    internal void Cancel(string? reason)
    {
        _cancellationReason = reason;
        _source.Cancel();
    }
}
