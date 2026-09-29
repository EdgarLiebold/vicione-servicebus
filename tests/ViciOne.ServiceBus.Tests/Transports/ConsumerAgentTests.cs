using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ConsumerAgentTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "unexpected-consume-loop-exit-stops-agent")]
    public async Task UnexpectedConsumeLoopExit_StopsTheAgentAndMarksTheShutdownAsUngracefulAsync()
    {
        var consumeLoop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var agent = CreateAgent();
        agent.RegisterConsumeTask(consumeLoop.Task);

        consumeLoop.TrySetResult();

        await agent.Completed.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await agent.StopAsync(TestContext.Current.CancellationToken)
            .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.False(agent.GracefulShutdown);
        Assert.True(agent.Stopping.IsCancellationRequested);
        Assert.True(agent.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "first-consume-loop-task-is-authoritative")]
    public async Task ExplicitStop_WaitsForTheFirstRegisteredTaskAndRemainsGracefulAsync()
    {
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ignored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var agent = CreateAgent();

        Assert.Equal(
            "consumeTask",
            Assert.Throws<ArgumentNullException>(() => agent.RegisterConsumeTask(null!)).ParamName);
        agent.RegisterConsumeTask(first.Task);
        agent.RegisterConsumeTask(ignored.Task);

        Task stop = agent.StopAsync(TestContext.Current.CancellationToken);
        ignored.TrySetResult();
        Assert.False(stop.IsCompleted);

        first.TrySetResult();
        await stop.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.True(agent.GracefulShutdown);
    }

    [Theory]
    [InlineData(ManualConsumeOutcome.Completed)]
    [InlineData(ManualConsumeOutcome.Canceled)]
    [InlineData(ManualConsumeOutcome.Faulted)]
    [RequirementCoverage("REQ-VSB-CONSUMER-AGENT-LIFECYCLE", "manual-consume-loop-terminal-outcomes-are-owned")]
    public async Task ManualConsumeLoopOutcome_IsObservedAndOwnedAsync(ManualConsumeOutcome outcome)
    {
        var agent = CreateAgent();
        agent.CreateManualConsumeTask();
        agent.CreateManualConsumeTask();

        switch (outcome)
        {
            case ManualConsumeOutcome.Completed:
                agent.CompleteManualConsumeTask();
                break;
            case ManualConsumeOutcome.Canceled:
                agent.CancelManualConsumeTask(TestContext.Current.CancellationToken);
                break;
            case ManualConsumeOutcome.Faulted:
                agent.FaultManualConsumeTask(new ExpectedConsumeException());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }

        await agent.Completed.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await agent.StopAsync(TestContext.Current.CancellationToken)
            .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.False(agent.GracefulShutdown);
        Assert.True(agent.Stopped.IsCancellationRequested);
    }

    private static TestConsumerAgent CreateAgent()
    {
        var dispatcher = DispatchProxy.Create<IReceivePipeDispatcher, IdleDispatcherProxy>();
        var context = DispatchProxy.Create<ReceiveEndpointContext, ReceiveEndpointContextProxy>();
        ((ReceiveEndpointContextProxy)(object)context).Initialize(dispatcher);
        return new TestConsumerAgent(context);
    }

    public enum ManualConsumeOutcome
    {
        Completed,
        Canceled,
        Faulted,
    }

    private sealed class TestConsumerAgent(ReceiveEndpointContext context) : ConsumerAgent<string>(context)
    {
        public bool GracefulShutdown => IsGracefulShutdown;

        public void CreateManualConsumeTask() => TrySetManualConsumeTask();

        public void RegisterConsumeTask(Task task) => TrySetConsumeTask(task);

        public void CompleteManualConsumeTask() => TrySetConsumeCompleted();

        public void CancelManualConsumeTask(CancellationToken cancellationToken) => TrySetConsumeCanceled(cancellationToken);

        public void FaultManualConsumeTask(Exception exception) => TrySetConsumeException(exception);
    }

    private class ReceiveEndpointContextProxy : DispatchProxy
    {
        private IReceivePipeDispatcher? _dispatcher;
        private readonly ILogContext _logContext = new BusLogContext(NullLoggerFactory.Instance);

        public void Initialize(IReceivePipeDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name switch
            {
                nameof(ReceiveEndpointContext.CreateReceivePipeDispatcher) => _dispatcher
                    ?? throw new InvalidOperationException("The test context has not been initialized."),
                $"get_{nameof(ReceiveEndpointContext.LogContext)}" => _logContext,
                $"get_{nameof(ReceiveEndpointContext.StopTimeout)}" => OperationTimeout,
                $"get_{nameof(ReceiveEndpointContext.ConsumerStopTimeout)}" => null,
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }
    }

    private class IdleDispatcherProxy : DispatchProxy
    {
        private ZeroActivityHandler? _zeroActivity;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name switch
            {
                $"add_{nameof(IDispatchMetrics.ZeroActivity)}" => AddZeroActivity(args),
                $"remove_{nameof(IDispatchMetrics.ZeroActivity)}" => RemoveZeroActivity(args),
                $"get_{nameof(IDispatchMetrics.ActiveDispatchCount)}" => 0,
                $"get_{nameof(IDispatchMetrics.DispatchCount)}" => 0L,
                $"get_{nameof(IDispatchMetrics.MaxConcurrentDispatchCount)}" => 0,
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        private object? AddZeroActivity(object?[]? args)
        {
            _zeroActivity += (ZeroActivityHandler?)args?[0];
            return null;
        }

        private object? RemoveZeroActivity(object?[]? args)
        {
            _zeroActivity -= (ZeroActivityHandler?)args?[0];
            return null;
        }
    }

    private sealed class ExpectedConsumeException : Exception;
}
