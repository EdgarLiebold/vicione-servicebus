using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides a consume job context implementation.
/// </summary>
/// <typeparam name="TJob">The t job type.</typeparam>
public class ConsumeJobContext<TJob> :
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="instanceAddress">The instance address value.</param>
    /// <param name="job">The job value.</param>
    /// <param name="jobOptions">The job options value.</param>
    public ConsumeJobContext(ConsumeContext<StartJob> context, Uri instanceAddress, TJob job, JobOptions<TJob> jobOptions)
        : base(context.Advanced())
    {
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
        jobProperties.SetMany(context.Message.JobProperties!);

        JobProperties = jobProperties;

        _timeProvider = context.GetTimeProvider();
        _source = new CancellationTokenSource(jobOptions.JobTimeout, _timeProvider);
        _startedAt = _timeProvider.GetTimestamp();
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken => _source.Token;

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public TJob Message => Job;

    /// <summary>
    /// Performs the notify consumed operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(_context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(_context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_updateBuffer != null)
            await _updateBuffer.FlushAsync().ConfigureAwait(false);

        _source.Dispose();
    }

    /// <summary>
    /// Performs the notify canceled operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the notify started operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            Timestamp = timestamp
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the notify completed operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
            InstanceProperties = _jobOptions.InstanceProperties,
            JobTypeProperties = _jobOptions.JobTypeProperties
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the notify job progress operation.
    /// </summary>
    /// <param name="progress">The progress value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyJobProgressAsync(SetJobProgress progress, CancellationToken cancellationToken = default)
    {
        return NotifyAsync(progress, cancellationToken);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="delay">The delay value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task NotifyFaultedAsync(Exception exception, TimeSpan? delay, CancellationToken cancellationToken = default)
    {
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

    /// <summary>
    /// Gets the job id value.
    /// </summary>
    public Guid JobId { get; }
    /// <summary>
    /// Gets the attempt id value.
    /// </summary>
    public Guid AttemptId { get; }
    /// <summary>
    /// Gets the retry attempt value.
    /// </summary>
    public int RetryAttempt { get; }
    /// <summary>
    /// Gets the last progress value value.
    /// </summary>
    public long? LastProgressValue { get; }
    /// <summary>
    /// Gets the last progress limit value.
    /// </summary>
    public long? LastProgressLimit { get; }
    /// <summary>
    /// Gets the job value.
    /// </summary>
    public TJob Job { get; }

    /// <summary>
    /// Gets the elapsed time value.
    /// </summary>
    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_startedAt);

    /// <summary>
    /// Sets job progress.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SetJobProgressAsync(long value, long? limit, CancellationToken cancellationToken = default)
    {
        _updateBuffer ??= new JobProgressBuffer(this, _timeProvider, _jobOptions.ProgressBuffer);

        return _updateBuffer.UpdateAsync(new JobProgressBuffer.ProgressUpdate(JobId, AttemptId, value, limit), cancellationToken);
    }

    /// <summary>
    /// Performs the save job state operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="jobState">The job state value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SaveJobStateAsync<T>(T? jobState, CancellationToken cancellationToken = default)
        where T : class
    {
        return NotifyAsync<SaveJobState>(new SaveJobStateCommand
        {
            JobId = JobId,
            AttemptId = AttemptId,
            JobState = jobState != null ? _context.Advanced().ToDictionary(jobState) : null
        }, cancellationToken);
    }

    /// <summary>
    /// Attempts to get job state.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="jobState">The job state value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetJobState<T>([NotNullWhen(true)] out T? jobState)
        where T : class
    {
        if (_context.Message.JobState != null)
        {
            jobState = _context.Advanced().SerializerContext.DeserializeObject<T>(_context.Message.JobState);
            return jobState != null;
        }

        jobState = null;
        return false;
    }

    /// <summary>
    /// Gets or sets the job properties value.
    /// </summary>
    public IPropertyCollection JobProperties { get; set; }
    /// <summary>
    /// Gets the job type properties value.
    /// </summary>
    public IPropertyCollection JobTypeProperties => _jobOptions.JobTypeProperties;
    /// <summary>
    /// Gets the instance properties value.
    /// </summary>
    public IPropertyCollection InstanceProperties => _jobOptions.InstanceProperties;

    DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    async Task NotifyAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        var endpoint = await _context.Advanced().ReceiveContext.PublishEndpointProvider
            .GetPublishSendEndpointAsync<T>(cancellationToken)
            .ConfigureAwait(false);

        await endpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether the current value can cel.
    /// </summary>
    /// <param name="reason">The reason value.</param>
    public void Cancel(string? reason)
    {
        _cancellationReason = reason;
        _source.Cancel();
    }
}
