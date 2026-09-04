using System.Collections.Concurrent;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

namespace ViciOne.ServiceBus.Tests.Courier;

internal static class CourierTestSupport
{
    public static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public static InMemoryTestHarness CreateHarness(string prefix)
    {
        TimeSpan timeout = OperationTimeout();
        return new InMemoryTestHarness($"{prefix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
    }
}

internal sealed class CourierMessageRecorder<T> : IDisposable
    where T : class
{
    private readonly TaskCompletionSource<bool> _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentQueue<ConsumeContext<T>> _messages = new();
    private readonly int _expectedCount;
    private Action<IInMemoryReceiveEndpointConfigurator>? _configure;
    private InMemoryTestHarness? _harness;
    private int _count;

    public CourierMessageRecorder(int expectedCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedCount, 1);
        _expectedCount = expectedCount;
    }

    public int Count => Volatile.Read(ref _count);

    public IReadOnlyList<ConsumeContext<T>> Messages => _messages.ToArray();

    public void Configure(InMemoryTestHarness harness)
    {
        ArgumentNullException.ThrowIfNull(harness);

        if (_configure is not null)
            throw new InvalidOperationException("The recorder is already configured.");

        _harness = harness;
        _configure = endpoint => endpoint.Handler<T>(context =>
        {
            _messages.Enqueue(context);
            if (Interlocked.Increment(ref _count) == _expectedCount)
                _completed.TrySetResult(true);

            return Task.CompletedTask;
        });
        harness.OnConfigureInMemoryReceiveEndpoint += _configure;
    }

    public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _completed.Task.WaitAsync(timeout, cancellationToken);

    public void Dispose()
    {
        if (_harness is not null && _configure is not null)
            _harness.OnConfigureInMemoryReceiveEndpoint -= _configure;

        _configure = null;
        _harness = null;
    }
}

internal sealed record CourierArguments(string Value);

internal sealed record CourierLog(string OriginalValue);

internal sealed class FirstCourierActivity(ConcurrentQueue<string>? compensationOrder = null) :
    IActivity<CourierArguments, CourierLog>
{
    public Task<ExecutionResult> ExecuteAsync(ExecuteContext<CourierArguments> context) =>
        Task.FromResult(context.Completed(
            new CourierLog(context.Arguments.Value),
            options => options.SetVariables(new
            {
                ActivityValue = "first-output",
                RemovedBySecond = "present",
            })));

    public Task<CompensationResult> CompensateAsync(CompensateContext<CourierLog> context)
    {
        compensationOrder?.Enqueue($"first:{context.Log.OriginalValue}");
        return Task.FromResult(context.Compensated());
    }
}

internal sealed class SecondCourierActivity(ConcurrentQueue<string>? compensationOrder = null) :
    IActivity<CourierArguments, CourierLog>
{
    public Task<ExecutionResult> ExecuteAsync(ExecuteContext<CourierArguments> context) =>
        Task.FromResult(context.Completed(
            new CourierLog(context.Arguments.Value),
            options => options.SetVariables(new
            {
                SecondValue = "second-output",
                RemovedBySecond = (string?)null,
            })));

    public Task<CompensationResult> CompensateAsync(CompensateContext<CourierLog> context)
    {
        compensationOrder?.Enqueue($"second:{context.Log.OriginalValue}");
        return Task.FromResult(context.Compensated());
    }
}

internal sealed record FaultingCourierArguments(string Reason);

internal sealed class FaultingCourierActivity : IExecuteActivity<FaultingCourierArguments>
{
    public Task<ExecutionResult> ExecuteAsync(ExecuteContext<FaultingCourierArguments> context) =>
        Task.FromResult(context.FaultedWithVariables(
            new CourierExpectedException(context.Arguments.Reason),
            new { FaultVariable = "fault-output" }));
}

internal sealed class ThrowingCourierActivity : IExecuteActivity<FaultingCourierArguments>
{
    public Task<ExecutionResult> ExecuteAsync(ExecuteContext<FaultingCourierArguments> context) =>
        throw new CourierExpectedException(context.Arguments.Reason);
}

internal sealed class CourierExpectedException(string message) : Exception(message);
