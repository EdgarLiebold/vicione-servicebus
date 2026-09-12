using System.Reflection;
using Quartz;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

public sealed class QuartzScheduledMessageJobTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-ACTIVATION", "injected-dependency-null-guards")]
    public void Constructor_RejectsMissingInjectedDependencies()
    {
        IBus bus = DispatchProxy.Create<IBus, BusProxy>();

        ArgumentNullException missingBus = Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageJob<IBus>(null!, TimeProvider.System));
        ArgumentNullException missingTimeProvider = Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageJob<IBus>(bus, null!));

        Assert.Equal("bus", missingBus.ParamName);
        Assert.Equal("timeProvider", missingTimeProvider.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-ACTIVATION", "execution-context-null-guard")]
    public async Task ExecuteAsync_RejectsMissingExecutionContextAsync()
    {
        IBus bus = DispatchProxy.Create<IBus, BusProxy>();
        var job = new QuartzScheduledMessageJob<IBus>(bus, TimeProvider.System);

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            job.ExecuteAsync(null!, TestContext.Current.CancellationToken).AsTask());

        Assert.Equal("context", exception.ParamName);
    }

    [Theory]
    [InlineData(QuartzSchedulerContextKeys.Bus)]
    [InlineData(QuartzSchedulerContextKeys.TimeProvider)]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-ACTIVATION", "standalone-context-dependencies")]
    public async Task StandaloneJob_RejectsMissingSchedulerContextDependencyAsync(string missingKey)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IBus bus = DispatchProxy.Create<IBus, BusProxy>();
        var schedulerContext = new SchedulerContext();
        if (missingKey != QuartzSchedulerContextKeys.Bus)
            schedulerContext[QuartzSchedulerContextKeys.Bus] = bus;
        if (missingKey != QuartzSchedulerContextKeys.TimeProvider)
            schedulerContext[QuartzSchedulerContextKeys.TimeProvider] = TimeProvider.System;
        IJobExecutionContext context = CreateContext(
            cancellationToken,
            refireCount: 0,
            schedulerContext: schedulerContext);
        var job = new QuartzScheduledMessageJob<IBus>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            job.ExecuteAsync(context, cancellationToken).AsTask());

        Assert.Contains(missingKey, exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(QuartzSchedulingExtensions.ConfigureQuartzScheduler), exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ContentType", null)]
    [InlineData("ContentType", "   ")]
    [InlineData("DestinationAddress", null)]
    [InlineData("DestinationAddress", "   ")]
    [InlineData("Body", null)]
    [InlineData("MessageTypes", null)]
    [InlineData("MessageTypes", "   ")]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "required-delivery-values")]
    public async Task RequiredJobData_FailsClosedAsync(string key, string? invalidValue)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var jobData = CreateValidJobData();
        if (invalidValue is null)
            jobData.Remove(key);
        else
            jobData[key] = invalidValue;
        var transportFailure = new InvalidOperationException("The transport must not be reached for invalid job data.");
        var job = new QuartzScheduledMessageJob<IBus>(
            CreateBus(cancellationToken, transportFailure),
            TimeProvider.System);

        JobExecutionException exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.ExecuteAsync(CreateContext(cancellationToken, refireCount: 0, jobData), cancellationToken).AsTask());

        Exception dataFailure = Assert.IsAssignableFrom<Exception>(exception.InnerException);
        if (key == QuartzJobDataKeys.DestinationAddress && invalidValue is not null)
            Assert.IsType<FormatException>(dataFailure);
        else
            Assert.IsType<InvalidOperationException>(dataFailure);
        Assert.Contains($"'{key}'", dataFailure.Message, StringComparison.Ordinal);
        Assert.True(exception.UnscheduleFiringTrigger);
        Assert.False(exception.RefireImmediately);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "empty-serialized-body")]
    public async Task EmptySerializedBody_ReachesTheTransportPipelineAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var jobData = CreateValidJobData();
        jobData["Body"] = string.Empty;
        var transportFailure = new InvalidOperationException("transport-reached");
        var job = new QuartzScheduledMessageJob<IBus>(
            CreateBus(cancellationToken, transportFailure),
            TimeProvider.System);

        JobExecutionException exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.ExecuteAsync(CreateContext(cancellationToken, refireCount: 0, jobData), cancellationToken).AsTask());

        Assert.Same(transportFailure, exception.InnerException);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("[\"\"]")]
    [InlineData("[\"   \"]")]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "invalid-message-type-list-is-terminal")]
    public async Task InvalidPersistedMessageTypeList_UnschedulesWithoutReachingTheTransportAsync(string persistedValue)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var jobData = CreateValidJobData();
        jobData[QuartzJobDataKeys.MessageTypes] = persistedValue;
        var transportFailure = new InvalidOperationException("The transport must not be reached for invalid job data.");
        var job = new QuartzScheduledMessageJob<IBus>(
            CreateBus(cancellationToken, transportFailure),
            TimeProvider.System);

        JobExecutionException exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.ExecuteAsync(CreateContext(cancellationToken, refireCount: 0, jobData), cancellationToken).AsTask());

        Assert.NotSame(transportFailure, exception.InnerException);
        Assert.True(exception.UnscheduleFiringTrigger);
        Assert.False(exception.RefireImmediately);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-CANCELLATION", "requested-context-cancellation-propagates")]
    public async Task RequestedContextCancellation_PropagatesWithoutImmediateRefireAsync()
    {
        using var contextCancellation = new CancellationTokenSource();
        contextCancellation.Cancel();
        var cancellation = new OperationCanceledException("Quartz interrupted the job.", contextCancellation.Token);
        IJobExecutionContext context = CreateContext(contextCancellation.Token, refireCount: 0);
        var job = new QuartzScheduledMessageJob<IBus>(CreateBus(contextCancellation.Token, cancellation), TimeProvider.System);

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            job.ExecuteAsync(context, contextCancellation.Token).AsTask());

        Assert.Same(cancellation, exception);
        Assert.Equal(contextCancellation.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-CANCELLATION", "unrequested-dependency-cancellation-uses-trigger-policy")]
    public async Task UnrequestedDependencyCancellation_UsesThePersistentTriggerRetryPolicyAsync()
    {
        using var contextCancellation = new CancellationTokenSource();
        using var dependencyCancellation = new CancellationTokenSource();
        var cancellation = new OperationCanceledException("The dependency canceled independently.", dependencyCancellation.Token);
        IJobExecutionContext context = CreateContext(contextCancellation.Token, refireCount: 0);
        var job = new QuartzScheduledMessageJob<IBus>(CreateBus(contextCancellation.Token, cancellation), TimeProvider.System);

        JobExecutionException exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.ExecuteAsync(context, contextCancellation.Token).AsTask());

        Assert.Same(cancellation, exception.InnerException);
        Assert.False(exception.RefireImmediately);
        Assert.False(exception.UnscheduleFiringTrigger);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-RETRY", "persistent-trigger-policy")]
    public async Task TransportFailure_DelegatesRetryTimingToThePersistentTriggerPolicyAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var transportFailure = new InvalidOperationException("The transport remained unavailable.");
        IJobExecutionContext context = CreateContext(cancellationToken, refireCount: 5);
        var job = new QuartzScheduledMessageJob<IBus>(
            CreateBus(cancellationToken, transportFailure),
            TimeProvider.System);

        JobExecutionException exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.ExecuteAsync(context, cancellationToken).AsTask());

        Assert.Same(transportFailure, exception.InnerException);
        Assert.False(exception.RefireImmediately);
        Assert.False(exception.UnscheduleFiringTrigger);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "terminal-data-failure-unschedules-trigger")]
    public async Task InvalidPersistedMessageData_UnschedulesWithoutRetryAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var dataFailure = new InvalidScheduledMessageDataException(
            "The persisted message is invalid.",
            new FormatException("The body cannot be parsed."));
        var job = new QuartzScheduledMessageJob<IBus>(
            CreateBus(cancellationToken, dataFailure),
            TimeProvider.System);

        JobExecutionException exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.ExecuteAsync(CreateContext(cancellationToken, refireCount: 0), cancellationToken).AsTask());

        Assert.Same(dataFailure, exception.InnerException);
        Assert.True(exception.UnscheduleFiringTrigger);
        Assert.False(exception.RefireImmediately);
    }

    private static IBus CreateBus(CancellationToken expectedSendToken, Exception sendException)
    {
        ITestSendEndpoint endpoint = DispatchProxy.Create<ITestSendEndpoint, SendEndpointProxy>();
        ((SendEndpointProxy)(object)endpoint).Configure(expectedSendToken, sendException);

        IBus bus = DispatchProxy.Create<IBus, BusProxy>();
        ((BusProxy)(object)bus).Endpoint = endpoint;
        return bus;
    }

    private interface ITestSendEndpoint : ISendEndpoint, Advanced.IAdvancedSendEndpoint;

    private static IJobExecutionContext CreateContext(
        CancellationToken cancellationToken,
        int refireCount,
        JobDataMap? jobData = null,
        SchedulerContext? schedulerContext = null)
    {
        IJobExecutionContext context = DispatchProxy.Create<IJobExecutionContext, JobExecutionContextProxy>();
        ((JobExecutionContextProxy)(object)context).Configure(
            jobData ?? CreateValidJobData(),
            cancellationToken,
            refireCount,
            schedulerContext);
        return context;
    }

    private static JobDataMap CreateValidJobData() => new()
    {
        ["ContentType"] = "application/json",
        ["DestinationAddress"] = "loopback://localhost/quartz-cancellation",
        ["Body"] = "{}",
        [QuartzJobDataKeys.MessageTypes] = "[\"urn:message:ViciOne.ServiceBus.Quartz.Tests:ScheduledPayload\"]",
        [QuartzJobDataKeys.MessageIdSeed] = "018f6738-7d4a-7b21-86e2-bdfbb3ed5f90",
    };

    private class BusProxy : DispatchProxy
    {
        public ISendEndpoint? Endpoint { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ISendEndpointProvider.GetSendEndpointAsync))
                return Task.FromResult(Endpoint ?? throw new InvalidOperationException("The endpoint was not configured."));

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class SendEndpointProxy : DispatchProxy
    {
        private CancellationToken _expectedToken;
        private Exception? _sendException;

        public void Configure(CancellationToken expectedToken, Exception sendException)
        {
            _expectedToken = expectedToken;
            _sendException = sendException;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(ISendEndpoint.SendAsync) || targetMethod.ReturnType != typeof(Task))
                throw new NotSupportedException(targetMethod?.Name);

            if (args is null || args.Length == 0 || args[^1] is not CancellationToken actualToken || actualToken != _expectedToken)
                throw new InvalidOperationException("The scheduled-message job did not pass the Quartz cancellation token to the send endpoint.");

            return Task.FromException(_sendException ?? throw new InvalidOperationException("The send exception was not configured."));
        }
    }

    private class JobExecutionContextProxy : DispatchProxy
    {
        private CancellationToken _cancellationToken;
        private JobDataMap? _jobData;
        private int _refireCount;
        private IScheduler? _scheduler;

        public void Configure(
            JobDataMap jobData,
            CancellationToken cancellationToken,
            int refireCount,
            SchedulerContext? schedulerContext)
        {
            _jobData = jobData;
            _cancellationToken = cancellationToken;
            _refireCount = refireCount;
            if (schedulerContext is not null)
            {
                _scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
                ((SchedulerProxy)(object)_scheduler).Context = schedulerContext;
            }
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_MergedJobDataMap" => _jobData ?? throw new InvalidOperationException("The job data was not configured."),
                "get_CancellationToken" => _cancellationToken,
                "get_RefireCount" => _refireCount,
                "get_FireTimeUtc" => new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero),
                "get_ScheduledFireTimeUtc" => new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero),
                "get_NextFireTimeUtc" => null,
                "get_PreviousFireTimeUtc" => null,
                "get_Scheduler" => _scheduler ?? throw new InvalidOperationException("The scheduler was not configured."),
                "get_Trigger" => TriggerBuilder.Create()
                    .WithIdentity("test-trigger", "test-group")
                    .ForJob(new JobKey("test-job", "test-group"))
                    .StartNow()
                    .Build(),
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
        }
    }

    private class SchedulerProxy : DispatchProxy
    {
        public SchedulerContext? Context { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Context" => Context ?? throw new InvalidOperationException("The scheduler context was not configured."),
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
        }
    }
}
