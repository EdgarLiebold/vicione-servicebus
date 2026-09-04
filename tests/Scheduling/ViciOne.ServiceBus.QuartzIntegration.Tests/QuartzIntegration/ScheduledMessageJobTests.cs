using System.Reflection;
using Quartz;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;

public sealed class ScheduledMessageJobTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-CANCELLATION", "requested-context-cancellation-propagates")]
    public async Task RequestedContextCancellation_PropagatesWithoutImmediateRefire()
    {
        using var contextCancellation = new CancellationTokenSource();
        contextCancellation.Cancel();
        var cancellation = new OperationCanceledException("Quartz interrupted the job.", contextCancellation.Token);
        IJobExecutionContext context = CreateContext(contextCancellation.Token, refireCount: 0);
        var job = new ScheduledMessageJob(CreateBus(contextCancellation.Token, cancellation), TimeProvider.System);

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            job.Execute(context, contextCancellation.Token).AsTask());

        Assert.Same(cancellation, exception);
        Assert.Equal(contextCancellation.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-CANCELLATION", "unrequested-dependency-cancellation-refires")]
    public async Task UnrequestedDependencyCancellation_RemainsARetryableJobFailure()
    {
        using var contextCancellation = new CancellationTokenSource();
        using var dependencyCancellation = new CancellationTokenSource();
        var cancellation = new OperationCanceledException("The dependency canceled independently.", dependencyCancellation.Token);
        IJobExecutionContext context = CreateContext(contextCancellation.Token, refireCount: 0);
        var job = new ScheduledMessageJob(CreateBus(contextCancellation.Token, cancellation), TimeProvider.System);

        JobExecutionException exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.Execute(context, contextCancellation.Token).AsTask());

        Assert.Same(cancellation, exception.InnerException);
        Assert.True(exception.RefireImmediately);
    }

    private static IBus CreateBus(CancellationToken expectedSendToken, Exception sendException)
    {
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, SendEndpointProxy>();
        ((SendEndpointProxy)(object)endpoint).Configure(expectedSendToken, sendException);

        IBus bus = DispatchProxy.Create<IBus, BusProxy>();
        ((BusProxy)(object)bus).Endpoint = endpoint;
        return bus;
    }

    private static IJobExecutionContext CreateContext(CancellationToken cancellationToken, int refireCount)
    {
        IJobExecutionContext context = DispatchProxy.Create<IJobExecutionContext, JobExecutionContextProxy>();
        ((JobExecutionContextProxy)(object)context).Configure(
            new JobDataMap
            {
                ["ContentType"] = "application/json",
                ["Destination"] = "loopback://localhost/quartz-cancellation",
                ["Body"] = "{}",
            },
            cancellationToken,
            refireCount);
        return context;
    }

    private class BusProxy : DispatchProxy
    {
        public ISendEndpoint? Endpoint { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ISendEndpointProvider.GetSendEndpoint))
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
            if (targetMethod?.Name != nameof(ISendEndpoint.Send) || targetMethod.ReturnType != typeof(Task))
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

        public void Configure(JobDataMap jobData, CancellationToken cancellationToken, int refireCount)
        {
            _jobData = jobData;
            _cancellationToken = cancellationToken;
            _refireCount = refireCount;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_MergedJobDataMap" => _jobData ?? throw new InvalidOperationException("The job data was not configured."),
                "get_CancellationToken" => _cancellationToken,
                "get_RefireCount" => _refireCount,
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
        }
    }
}
