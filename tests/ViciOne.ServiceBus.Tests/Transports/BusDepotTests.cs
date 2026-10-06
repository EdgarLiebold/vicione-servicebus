using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class BusDepotTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "lifecycle-joins-started-buses-after-fault-cancellation-or-invalid-control")]
    public async Task Lifecycle_JoinsStartedBusesAfterLaterProviderFailureAsync(bool stop, int failureMode)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var firstCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("second bus lifecycle refused synchronously");
        IBusControl first = NewControl(_ => firstCompletion.Task, out LifecycleControlProxy firstRecording);
        IBusControl second = NewControl(_ => failureMode switch
        {
            0 => throw expected,
            1 => throw new OperationCanceledException(canceled.Token),
            2 => null!,
            _ => throw new InvalidOperationException("Null control must not be invoked."),
        }, out LifecycleControlProxy secondRecording);
        IBusControl third = NewControl(_ => Task.CompletedTask, out LifecycleControlProxy thirdRecording);
        Task? operation = null;
        try
        {
            Exception? synchronous = Record.Exception(() =>
            {
                operation = BusDepotTestDriver.RunLifecycleAsync(
                    [(typeof(IBus), first), (typeof(ISecondBus), failureMode == 3 ? null : second), (typeof(IThirdBus), third)], stop, token);
            });
            Assert.Null(synchronous);
            Assert.NotNull(operation);
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, firstRecording.Invocations);
            Assert.Equal(failureMode == 3 ? 0 : 1, secondRecording.Invocations);
            Assert.Equal(1, thirdRecording.Invocations);
            Assert.Equal(token, firstRecording.Token);
            Assert.Equal(token, thirdRecording.Token);
            Assert.Equal(stop ? nameof(IBusControl.StopAsync) : nameof(IBusControl.StartAsync), firstRecording.Method);

            firstCompletion.TrySetResult();
            if (failureMode == 1)
            {
                OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(timeout, token));
                Assert.Equal(canceled.Token, actual.CancellationToken);
                Assert.True(operation.IsCanceled);
            }
            else
            {
                InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(timeout, token));
                if (failureMode == 0)
                    Assert.Same(expected, actual);
                else
                    Assert.Contains(failureMode == 2 ? (stop ? "no stop task" : "no start task") : "no lifecycle control", actual.Message, StringComparison.Ordinal);
            }
        }
        finally
        {
            firstCompletion.TrySetResult();
            await firstCompletion.Task.WaitAsync(timeout, CancellationToken.None);
            if (operation is not null)
            {
                try { await operation.WaitAsync(timeout, CancellationToken.None); }
                catch (InvalidOperationException) { }
                catch (OperationCanceledException exception) when (exception.CancellationToken == canceled.Token) { }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "lifecycle-retains-all-original-provider-faults-after-drain")]
    public async Task Lifecycle_RetainsAllOriginalFaultsAfterDrainingStartedBusesAsync(bool stop)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken token = TestContext.Current.CancellationToken;
        var firstCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstFailure = new InvalidOperationException("first bus failed after admission");
        var secondFailure = new InvalidOperationException("second bus refused synchronously");
        IBusControl first = NewControl(_ => firstCompletion.Task, out _);
        IBusControl second = NewControl(_ => throw secondFailure, out _);
        Task? operation = null;
        try
        {
            Exception? synchronous = Record.Exception(() =>
            {
                operation = BusDepotTestDriver.RunLifecycleAsync([(typeof(IBus), first), (typeof(ISecondBus), second)], stop, token);
            });
            Assert.Null(synchronous);
            Assert.NotNull(operation);
            Assert.False(operation.IsCompleted);
            firstCompletion.TrySetException(firstFailure);
            InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(timeout, token));
            AggregateException aggregate = Assert.IsType<AggregateException>(operation.Exception);
            Assert.Equal(2, aggregate.InnerExceptions.Count);
            Assert.Contains(aggregate.InnerExceptions, exception => ReferenceEquals(exception, firstFailure));
            Assert.Contains(aggregate.InnerExceptions, exception => ReferenceEquals(exception, secondFailure));
            Assert.Contains(aggregate.InnerExceptions, exception => ReferenceEquals(exception, observed));
        }
        finally
        {
            firstCompletion.TrySetResult();
            try { await firstCompletion.Task.WaitAsync(timeout, CancellationToken.None); }
            catch (InvalidOperationException exception) when (ReferenceEquals(exception, firstFailure)) { }
            if (operation is not null)
            {
                try { await operation.WaitAsync(timeout, CancellationToken.None); }
                catch (InvalidOperationException exception) when (ReferenceEquals(exception, firstFailure) || ReferenceEquals(exception, secondFailure)) { }
            }
        }
    }

    private static IBusControl NewControl(Func<CancellationToken, Task> operation, out LifecycleControlProxy recording)
    {
        IBusControl control = DispatchProxy.Create<IBusControl, LifecycleControlProxy>();
        recording = (LifecycleControlProxy)(object)control;
        recording.Operation = operation;
        return control;
    }

    public interface ISecondBus : IBus { }
    public interface IThirdBus : IBus { }

    private class LifecycleControlProxy : DispatchProxy
    {
        public Func<CancellationToken, Task> Operation { get; set; } = _ => throw new InvalidOperationException("Unconfigured lifecycle.");
        public int Invocations { get; private set; }
        public CancellationToken Token { get; private set; }
        public string? Method { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name is not (nameof(IBusControl.StartAsync) or nameof(IBusControl.StopAsync))
                || args is not { Length: 1 } || args[0] is not CancellationToken token)
                throw new InvalidOperationException($"Unexpected lifecycle call: {targetMethod?.Name}");
            Invocations++;
            Token = token;
            Method = targetMethod.Name;
            return Operation(token);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "constructor-rejects-null-dependencies")]
    public void Constructor_RejectsNullDependencies()
    {
        ArgumentNullException instancesException = Assert.Throws<ArgumentNullException>(BusDepotTestDriver.CreateWithNullInstances);
        ArgumentNullException loggerException = Assert.Throws<ArgumentNullException>(BusDepotTestDriver.CreateWithNullLogger);

        Assert.Equal("instances", instancesException.ParamName);
        Assert.Equal("logger", loggerException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "constructor-rejects-null-instance")]
    public void Constructor_RejectsNullInstance()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(BusDepotTestDriver.CreateWithNullInstance);

        Assert.Equal("instances", exception.ParamName);
        Assert.Contains("cannot contain null values", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "constructor-rejects-duplicate-instance-type")]
    public void Constructor_RejectsDuplicateInstanceType()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(BusDepotTestDriver.CreateWithDuplicateInstanceType);

        Assert.Equal("instances", exception.ParamName);
        Assert.Contains(typeof(IBus).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "empty-start-fails-with-actionable-configuration")]
    public async Task EmptyDepot_StartFailsWithActionableConfigurationAsync()
    {
        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            BusDepotTestDriver.StartEmptyAsync(TestContext.Current.CancellationToken));

        Assert.Contains("No bus instances were found", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddViciOneServiceBus()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "empty-stop-is-idempotent")]
    public async Task EmptyDepot_StopIsIdempotentAsync()
    {
        await BusDepotTestDriver.StopEmptyAsync(TestContext.Current.CancellationToken);
        await BusDepotTestDriver.StopEmptyAsync(TestContext.Current.CancellationToken);
    }
}
