using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides one job attempt with its payload, execution lifetime, metadata, progress, and checkpoint operations.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
internal sealed class ConsumeJobContext<TJob> :
    ConsumeContextProxy,
    ConsumeContext<TJob>,
    JobContext<TJob>,
    INotifyJobContext,
    IAsyncDisposable
    where TJob : class
{
    readonly ConsumeContext<StartJob> _context;
    readonly Uri _instanceAddress;
    readonly JobOptions<TJob> _jobOptions;
    readonly CancellationTokenSource _source;
    readonly long _startedAt;
    readonly TimeProvider _timeProvider;
    string? _cancellationReason;
    JobProgressBuffer? _updateBuffer;

    /// <summary>Creates an execution context for one admitted job attempt.</summary>
    /// <param name="context">The start command and transport context.</param>
    /// <param name="instanceAddress">The endpoint address executing the attempt.</param>
    /// <param name="job">The deserialized job payload.</param>
    /// <param name="jobOptions">The validated options registered for the job type.</param>
    public ConsumeJobContext(ConsumeContext<StartJob> context, Uri instanceAddress, TJob job, JobOptions<TJob> jobOptions)
        : base(GetAdvancedContext(context))
    {
        ArgumentNullException.ThrowIfNull(instanceAddress);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(jobOptions);

        _context = context;
        _instanceAddress = instanceAddress;
        _jobOptions = jobOptions;

        JobId = context.Message.JobId;
        AttemptId = context.Message.AttemptId;
        RetryAttempt = context.Message.RetryAttempt;

        Job = job;

        LastProgressValue = context.Message.LastProgressValue;
        LastProgressLimit = context.Message.LastProgressLimit;

        var jobProperties = new JobPropertyCollection();
        if (context.Message.JobProperties is not null)
        {
            foreach (KeyValuePair<string, object> property in context.Message.JobProperties)
                jobProperties.Set(property.Key, property.Value);
        }

        JobProperties = jobProperties;

        _timeProvider = context.GetTimeProvider();
        _source = new CancellationTokenSource(jobOptions.JobTimeout, _timeProvider);
        _startedAt = _timeProvider.GetTimestamp();
    }

    /// <summary>Gets the cancellation token.</summary>
    public override CancellationToken CancellationToken => _source.Token;

    /// <summary>Gets the message.</summary>
    public TJob Message => Job;

    /// <summary>Reports that notify has been consumed.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(_context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(_context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_updateBuffer != null)
                await _updateBuffer.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _source.Dispose();
        }
    }

    /// <summary>Notifies registered observers about canceled.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task NotifyCanceledAsync(CancellationToken cancellationToken = default)
    {
        LogContext.Debug?.Log("Job Canceled: {JobId} {AttemptId} ({RetryAttempt}) {Reason}", JobId, AttemptId, RetryAttempt, _cancellationReason);

        if (_updateBuffer != null)
            await _updateBuffer.FlushAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        await NotifyAsync<JobAttemptCanceled>(new JobAttemptCanceledEvent
        {
            JobId = JobId,
            AttemptId = AttemptId,
            Timestamp = UtcNow,
            Reason = string.IsNullOrWhiteSpace(_cancellationReason) ? JobCancellationReasons.ConsumerInitiated : _cancellationReason!
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Notifies registered observers about started.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task NotifyStartedAsync(CancellationToken cancellationToken = default)
    {
        LogContext.Debug?.Log("Job Started: {JobId} {AttemptId} ({RetryAttempt})", JobId, AttemptId, RetryAttempt);

        var timestamp = UtcNow;

        await NotifyAsync<JobAttemptStarted>(new JobAttemptStartedEvent
        {
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            Timestamp = timestamp,
            InstanceAddress = _instanceAddress
        }, cancellationToken).ConfigureAwait(false);

        var endpoint = await _context.Advanced().ReceiveContext.PublishEndpointProvider.GetPublishSendEndpointAsync<JobStarted<TJob>>(cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync<JobStarted<TJob>>(new JobStartedEvent<TJob>
        {
            Job = Job,
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            Timestamp = timestamp
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reports that notify has completed.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task NotifyCompletedAsync(CancellationToken cancellationToken = default)
    {
        LogContext.Debug?.Log("Job Completed: {JobId} {AttemptId} ({RetryAttempt})", JobId, AttemptId, RetryAttempt);

        if (_updateBuffer != null)
            await _updateBuffer.FlushAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        await NotifyAsync<JobAttemptCompleted>(new JobAttemptCompletedEvent
        {
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            Timestamp = UtcNow,
            Duration = ElapsedTime,
            InstanceProperties = _jobOptions.InstancePropertyValues,
            JobTypeProperties = _jobOptions.JobTypePropertyValues
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Publishes a sequenced progress update to the job coordinator.</summary>
    /// <param name="progress">The progress message to publish.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the progress update has been sent.</returns>
    public Task NotifyProgressAsync(SetJobProgress progress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(progress);
        return NotifyAsync(progress, cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="delay">The delay before the operation is attempted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task NotifyFaultedAsync(Exception exception, TimeSpan? delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        LogContext.Debug?.Log(exception, "Job Faulted: {JobId} {AttemptId} ({RetryAttempt})", JobId, AttemptId, RetryAttempt);

        if (_updateBuffer != null)
            await _updateBuffer.FlushAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        await NotifyAsync<JobAttemptFaulted>(new JobAttemptFaultedEvent
        {
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            RetryDelay = delay,
            Timestamp = UtcNow,
            Exceptions = new FaultExceptionInfo(exception)
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets the job id.</summary>
    public Guid JobId { get; }
    /// <summary>Gets the attempt id.</summary>
    public Guid AttemptId { get; }
    /// <summary>Gets the retry attempt.</summary>
    public int RetryAttempt { get; }
    /// <summary>Gets the last progress value carried by this instance.</summary>
    public long? LastProgressValue { get; }
    /// <summary>Gets the last progress limit.</summary>
    public long? LastProgressLimit { get; }
    /// <summary>Gets the job.</summary>
    public TJob Job { get; }

    /// <summary>Gets the elapsed time.</summary>
    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_startedAt);

    /// <summary>Queues the latest job progress for bounded, ordered publication.</summary>
    /// <param name="value">The current progress value.</param>
    /// <param name="limit">The optional upper bound associated with the value.</param>
    /// <param name="cancellationToken">The token that cancels waiting for buffer capacity.</param>
    /// <returns>A task that completes when the progress value has entered the buffer.</returns>
    public Task ReportProgressAsync(long value, long? limit = null, CancellationToken cancellationToken = default)
    {
        _updateBuffer ??= new JobProgressBuffer(this, _timeProvider, _jobOptions.ProgressBuffer);

        return _updateBuffer.UpdateAsync(new JobProgressBuffer.ProgressUpdate(JobId, AttemptId, value, limit), cancellationToken);
    }

    /// <summary>Publishes a durable checkpoint for recovery by a later attempt.</summary>
    /// <typeparam name="TCheckpoint">The checkpoint contract type.</typeparam>
    /// <param name="checkpoint">The checkpoint to save, or <see langword="null" /> to clear it.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the checkpoint command has been sent.</returns>
    public Task SaveCheckpointAsync<TCheckpoint>(TCheckpoint? checkpoint, CancellationToken cancellationToken = default)
        where TCheckpoint : class
    {
        return NotifyAsync<SaveJobCheckpoint>(new SaveJobCheckpointCommand
        {
            JobId = JobId,
            AttemptId = AttemptId,
            Checkpoint = checkpoint != null ? _context.Advanced().ToDictionary(checkpoint) : null
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
    public IPropertyCollection JobTypeProperties => _jobOptions.JobTypeProperties;
    /// <summary>Gets metadata of the service instance executing this attempt.</summary>
    public IPropertyCollection InstanceProperties => _jobOptions.InstanceProperties;

    DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    async Task NotifyAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        var endpoint = await _context.Advanced().ReceiveContext.PublishEndpointProvider
            .GetPublishSendEndpointAsync<T>(cancellationToken)
            .ConfigureAwait(false);

        await endpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    static ConsumeContext GetAdvancedContext(ConsumeContext<StartJob> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Advanced();
    }

    /// <summary>Cancels the attempt and records the reason reported by its consumer pipeline.</summary>
    /// <param name="reason">The reason.</param>
    internal void Cancel(string? reason)
    {
        _cancellationReason = reason;
        _source.Cancel();
    }
}
