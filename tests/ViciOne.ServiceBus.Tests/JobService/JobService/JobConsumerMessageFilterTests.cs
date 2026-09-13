using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Consumers.Contexts;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class JobConsumerMessageFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONSUMER-PIPELINE", "successful-execution-notifies-start-before-run-and-completion-after-run")]
    public async Task SuccessfulExecution_NotifiesTheCompleteLifecycleInOrderAsync()
    {
        var trace = new List<string>();
        var jobContext = CreateJobContext(retryAttempt: 0, TestContext.Current.CancellationToken);
        var notifications = new RecordingNotifications(trace);
        var consumer = new TestJobConsumer(context =>
        {
            Assert.Same(jobContext, context);
            trace.Add("run");
            return Task.CompletedTask;
        });
        ConsumerConsumeContext<TestJobConsumer, TestJob> context = CreateConsumerContext(consumer, jobContext, notifications);
        var filter = new JobConsumerMessageFilter<TestJobConsumer, TestJob>(Retry.None);
        var next = new RejectingPipe();

        await filter.SendAsync(context, next);

        Assert.Equal(["started", "run", "completed"], trace);
        Assert.Equal(0, next.SendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONSUMER-PIPELINE", "matching-cancellation-is-reported-as-canceled")]
    public async Task MatchingJobCancellation_IsReportedAsCanceledAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var expected = new OperationCanceledException(cancellation.Token);
        var notifications = new RecordingNotifications([]);
        var consumer = new TestJobConsumer(_ => Task.FromException(expected));
        JobContext<TestJob> jobContext = CreateJobContext(0, cancellation.Token);
        ConsumerConsumeContext<TestJobConsumer, TestJob> context = CreateConsumerContext(consumer, jobContext, notifications);
        var filter = new JobConsumerMessageFilter<TestJobConsumer, TestJob>(Retry.None);

        await filter.SendAsync(context, new RejectingPipe());

        Assert.Equal(1, notifications.StartedCount);
        Assert.Equal(1, notifications.CanceledCount);
        Assert.Equal(0, notifications.CompletedCount);
        Assert.Empty(notifications.Faults);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONSUMER-PIPELINE", "retryable-failure-reports-the-next-policy-delay")]
    public async Task RetryableFailure_ReportsTheNextPolicyDelayAsync()
    {
        var expected = new InvalidOperationException("transient");
        TimeSpan delay = TimeSpan.FromMinutes(3);
        var notifications = new RecordingNotifications([]);
        var consumer = new TestJobConsumer(_ => Task.FromException(expected));
        JobContext<TestJob> jobContext = CreateJobContext(1, TestContext.Current.CancellationToken);
        ConsumerConsumeContext<TestJobConsumer, TestJob> context = CreateConsumerContext(consumer, jobContext, notifications);
        var filter = new JobConsumerMessageFilter<TestJobConsumer, TestJob>(Retry.Intervals(TimeSpan.Zero, delay));

        await filter.SendAsync(context, new RejectingPipe());

        (Exception exception, TimeSpan? retryDelay) = Assert.Single(notifications.Faults);
        Assert.Same(expected, exception);
        Assert.Equal(delay, retryDelay);
        Assert.Equal(0, notifications.CanceledCount);
        Assert.Equal(0, notifications.CompletedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONSUMER-PIPELINE", "exhausted-failure-notifies-retry-context-and-terminal-fault")]
    public async Task ExhaustedRetryPolicy_ReportsATerminalFaultAsync()
    {
        var expected = new InvalidOperationException("terminal");
        var notifications = new RecordingNotifications([]);
        var consumer = new TestJobConsumer(_ => Task.FromException(expected));
        JobContext<TestJob> jobContext = CreateJobContext(1, TestContext.Current.CancellationToken);
        ConsumerConsumeContext<TestJobConsumer, TestJob> context = CreateConsumerContext(consumer, jobContext, notifications);
        var filter = new JobConsumerMessageFilter<TestJobConsumer, TestJob>(Retry.Immediate(1));

        await filter.SendAsync(context, new RejectingPipe());

        (Exception exception, TimeSpan? retryDelay) = Assert.Single(notifications.Faults);
        Assert.Same(expected, exception);
        Assert.Null(retryDelay);
        Assert.True(context.TryGetPayload(out RetryContext<JobContext<TestJob>>? retryContext));
        Assert.NotNull(retryContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONSUMER-PIPELINE", "constructor-send-and-probe-required-inputs")]
    public async Task Filter_ValidatesRequiredInputsAndReportsItsInvocationSignatureAsync()
    {
        Assert.Equal("retryPolicy", Assert.Throws<ArgumentNullException>(() =>
            new JobConsumerMessageFilter<TestJobConsumer, TestJob>(null!)).ParamName);

        var filter = new JobConsumerMessageFilter<TestJobConsumer, TestJob>(Retry.None);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => filter.Probe(null!)).ParamName);
        Assert.Equal(
            "context",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => filter.SendAsync(null!, new RejectingPipe()))).ParamName);

        var consumer = new TestJobConsumer(_ => Task.CompletedTask);
        JobContext<TestJob> jobContext = CreateJobContext(0, TestContext.Current.CancellationToken);
        ConsumerConsumeContext<TestJobConsumer, TestJob> context = CreateConsumerContext(
            consumer,
            jobContext,
            new RecordingNotifications([]));
        Assert.Equal(
            "next",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => filter.SendAsync(context, null!))).ParamName);

        IProbeResult probe = filter.GetProbeResult(TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object> consume = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            Assert.Contains("consume", probe.Results));
        Assert.Equal(
            $"RunAsync(JobContext<{TypeCache<TestJob>.ShortName}> context)",
            Assert.Contains("method", consume));
    }

    private static ConsumerConsumeContext<TestJobConsumer, TestJob> CreateConsumerContext(
        TestJobConsumer consumer,
        JobContext<TestJob> jobContext,
        INotifyJobContext notifications)
    {
        ConsumeContext<TestJob> source = InMemoryOutboxTestContextFactory.Create(new TestJob());
        source.AddOrUpdatePayload(() => jobContext, _ => jobContext);
        source.AddOrUpdatePayload(() => notifications, _ => notifications);
        return new ConsumerConsumeContextProxy<TestJobConsumer, TestJob>(source, consumer);
    }

    private static JobContext<TestJob> CreateJobContext(int retryAttempt, CancellationToken cancellationToken)
    {
        TestJobContext context = DispatchProxy.Create<TestJobContext, JobContextProxy>();
        var proxy = (JobContextProxy)(object)context;
        proxy.RetryAttempt = retryAttempt;
        proxy.CancellationToken = cancellationToken;
        return context;
    }

    private interface TestJobContext : JobContext<TestJob>, JobContext;

    private class JobContextProxy : DispatchProxy
    {
        public int RetryAttempt { get; set; }

        public CancellationToken CancellationToken { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_RetryAttempt" => RetryAttempt,
            "get_CancellationToken" => CancellationToken,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private sealed class TestJobConsumer(Func<JobContext<TestJob>, Task> run) : IJobConsumer<TestJob>
    {
        public Task RunAsync(JobContext<TestJob> context) => run(context);
    }

    private sealed class RecordingNotifications(List<string> trace) : INotifyJobContext
    {
        public int StartedCount { get; private set; }

        public int CanceledCount { get; private set; }

        public int CompletedCount { get; private set; }

        public List<(Exception Exception, TimeSpan? Delay)> Faults { get; } = [];

        public Task NotifyCanceledAsync(CancellationToken cancellationToken = default)
        {
            CanceledCount++;
            trace.Add("canceled");
            return Task.CompletedTask;
        }

        public Task NotifyStartedAsync(CancellationToken cancellationToken = default)
        {
            StartedCount++;
            trace.Add("started");
            return Task.CompletedTask;
        }

        public Task NotifyCompletedAsync(CancellationToken cancellationToken = default)
        {
            CompletedCount++;
            trace.Add("completed");
            return Task.CompletedTask;
        }

        public Task NotifyFaultedAsync(
            Exception exception,
            TimeSpan? delay = null,
            CancellationToken cancellationToken = default)
        {
            Faults.Add((exception, delay));
            trace.Add("faulted");
            return Task.CompletedTask;
        }

        public Task NotifyProgressAsync(SetJobProgress progress, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RejectingPipe : IPipe<ConsumerConsumeContext<TestJobConsumer, TestJob>>
    {
        public int SendCount { get; private set; }

        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumerConsumeContext<TestJobConsumer, TestJob> context)
        {
            SendCount++;
            throw new InvalidOperationException("The terminal job-consumer filter must not invoke the continuation.");
        }
    }

    private sealed class TestJob;
}
