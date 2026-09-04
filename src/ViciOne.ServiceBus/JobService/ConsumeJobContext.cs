using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Serialization;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

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

    public override CancellationToken CancellationToken => _source.Token;

    public TJob Message => Job;

    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(_context, duration, consumerType, cancellationToken: cancellationToken);
    }

    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(_context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_updateBuffer != null)
            await _updateBuffer.FlushAsync().ConfigureAwait(false);

        _source.Dispose();
    }

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
        }).ConfigureAwait(false);
    }

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
        }).ConfigureAwait(false);

        var endpoint = await _context.Advanced().ReceiveContext.PublishEndpointProvider.GetPublishSendEndpointAsync<JobStarted<TJob>>(cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync<JobStarted<TJob>>(new JobStartedEvent<TJob>
        {
            JobId = JobId,
            AttemptId = AttemptId,
            RetryAttempt = RetryAttempt,
            Timestamp = timestamp
        }, CancellationToken.None).ConfigureAwait(false);
    }

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
        }).ConfigureAwait(false);
    }

    public Task NotifyJobProgressAsync(SetJobProgress progress, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return NotifyAsync(progress);
    }

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
        }).ConfigureAwait(false);
    }

    public Guid JobId { get; }
    public Guid AttemptId { get; }
    public int RetryAttempt { get; }
    public long? LastProgressValue { get; }
    public long? LastProgressLimit { get; }
    public TJob Job { get; }

    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_startedAt);

    public Task SetJobProgressAsync(long value, long? limit, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _updateBuffer ??= new JobProgressBuffer(this, _timeProvider, _jobOptions.ProgressBuffer);

        return _updateBuffer.UpdateAsync(new JobProgressBuffer.ProgressUpdate(JobId, AttemptId, value, limit), CancellationToken.None);
    }

    public Task SaveJobStateAsync<T>(T? jobState, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return NotifyAsync<SaveJobState>(new SaveJobStateCommand
        {
            JobId = JobId,
            AttemptId = AttemptId,
            JobState = jobState != null ? _context.Advanced().ToDictionary(jobState) : null
        });
    }

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

    public IPropertyCollection JobProperties { get; set; }
    public IPropertyCollection JobTypeProperties => _jobOptions.JobTypeProperties;
    public IPropertyCollection InstanceProperties => _jobOptions.InstanceProperties;

    DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    async Task NotifyAsync<T>(T message)
        where T : class
    {
        var endpoint = await _context.Advanced().ReceiveContext.PublishEndpointProvider.GetPublishSendEndpointAsync<T>().ConfigureAwait(false);

        await endpoint.SendAsync(message, CancellationToken.None).ConfigureAwait(false);
    }

    public void Cancel(string? reason)
    {
        _cancellationReason = reason;
        _source.Cancel();
    }
}
