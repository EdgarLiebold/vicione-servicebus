using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class HostConfigurationRetryExtensionsTests
{
    private static readonly DateTimeOffset StartTime =
        new(2031, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-RETRY", "configured-delay-is-the-only-delay")]
    public async Task Retry_UsesOnlyTheConfiguredPolicyDelayAndTheExplicitTimeProviderAsync()
    {
        TimeSpan retryDelay = TimeSpan.FromMinutes(7);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var host = new TestHostConfiguration(Retry.Interval(1, retryDelay));
        var attempts = 0;

        Task operation = host.RetryAsync(() =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
                throw new ExpectedTransportException("transient");

            return Task.CompletedTask;
        }, timeProvider, CancellationToken.None, CancellationToken.None);

        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, attempts);
        Assert.False(operation.IsCompleted);

        timeProvider.Advance(retryDelay);
        await operation.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, attempts);
        Assert.Equal(1, timeProvider.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-RETRY", "caller-cancellation-keeps-exact-token")]
    public async Task Retry_CallerCancellationDuringBackoffKeepsTheExactCallerTokenAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var host = new TestHostConfiguration(Retry.Interval(1, TimeSpan.FromHours(1)));
        using var caller = new CancellationTokenSource();
        var attempts = 0;

        Task operation = host.RetryAsync(() =>
        {
            Interlocked.Increment(ref attempts);
            throw new ExpectedTransportException("transient");
        }, timeProvider, stoppingToken: CancellationToken.None, cancellationToken: caller.Token);

        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(TestContext.Current.CancellationToken);
        caller.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(caller.Token, exception.CancellationToken);
        Assert.Equal(1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-RETRY", "stopping-cancellation-is-connection-failure")]
    public async Task Retry_StoppingDuringBackoffProducesAConnectionFailureWithTheLastTransportFailureAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var host = new TestHostConfiguration(Retry.Interval(1, TimeSpan.FromHours(1)));
        using var stopping = new CancellationTokenSource();
        var expected = new ExpectedTransportException("transient");
        var attempts = 0;

        Task operation = host.RetryAsync(() =>
        {
            Interlocked.Increment(ref attempts);
            throw expected;
        }, timeProvider, stoppingToken: stopping.Token, cancellationToken: CancellationToken.None);

        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(TestContext.Current.CancellationToken);
        stopping.Cancel();

        ConnectionException exception = await Assert.ThrowsAsync<ConnectionException>(() => operation);
        Assert.Same(expected, exception.InnerException);
        Assert.Contains(host.HostAddress.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-RETRY", "stopping-precedes-caller-cancellation")]
    public async Task Retry_WhenBothSourcesAreCancelledReportsTheStoppingTransportAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var host = new TestHostConfiguration(Retry.Interval(1, TimeSpan.FromHours(1)));
        using var caller = new CancellationTokenSource();
        using var stopping = new CancellationTokenSource();
        var expected = new ExpectedTransportException("transient");
        var attempts = 0;

        Task operation = host.RetryAsync(() =>
        {
            Interlocked.Increment(ref attempts);
            throw expected;
        }, timeProvider, stoppingToken: stopping.Token, cancellationToken: caller.Token);

        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(TestContext.Current.CancellationToken);
        caller.Cancel();
        stopping.Cancel();

        ConnectionException exception = await Assert.ThrowsAsync<ConnectionException>(() => operation);

        Assert.Same(expected, exception.InnerException);
        Assert.Equal(1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-RETRY", "terminal-failure-keeps-identity")]
    public async Task Retry_ExhaustedPolicyRethrowsTheExactTerminalFailureAsync()
    {
        var host = new TestHostConfiguration(Retry.Immediate(1));
        var first = new ExpectedTransportException("first");
        var terminal = new ExpectedTransportException("terminal");
        var attempts = 0;

        ExpectedTransportException actual = await Assert.ThrowsAsync<ExpectedTransportException>(() =>
            host.RetryAsync(() =>
            {
                int attempt = Interlocked.Increment(ref attempts);
                throw attempt == 1 ? first : terminal;
            }, TimeProvider.System, CancellationToken.None, CancellationToken.None));

        Assert.Same(terminal, actual);
        Assert.Equal(2, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-RETRY", "public-boundary-rejects-null-dependencies")]
    public async Task Retry_RejectsNullConfigurationOperationTimeProviderAndPolicyAsync()
    {
        Func<Task> operation = () => Task.CompletedTask;
        var host = new TestHostConfiguration(Retry.None);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        ArgumentNullException missingHost = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            HostConfigurationRetryExtensions.RetryAsync(null!, operation, TimeProvider.System, cancellationToken, cancellationToken));
        ArgumentNullException missingOperation = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            host.RetryAsync(null!, TimeProvider.System, cancellationToken, cancellationToken));
        ArgumentNullException missingTimeProvider = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            host.RetryAsync(operation, null!, cancellationToken, cancellationToken));
        InvalidOperationException missingPolicy = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TestHostConfiguration(null!).RetryAsync(operation, TimeProvider.System, cancellationToken, cancellationToken));

        Assert.Equal("hostConfiguration", missingHost.ParamName);
        Assert.Equal("factory", missingOperation.ParamName);
        Assert.Equal("timeProvider", missingTimeProvider.ParamName);
        Assert.Equal("The host configuration returned a null send transport retry policy.", missingPolicy.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-LIFECYCLE", "failed-host-connection-releases-admitted-observers")]
    public void ConnectObservers_FailedSecondRegistrationDisconnectsTheFirstAndPreservesThePrimary()
    {
        IHostConfiguration host = CreateActualHostConfiguration();
        var expected = new ExpectedTransportException("second observer connector failed");
        var fixture = new ObserverRegistrationFixture(expected);

        try
        {
            ExpectedTransportException actual = Assert.Throws<ExpectedTransportException>(() =>
            {
                host.ConnectReceiveEndpointContext(fixture.Context);
            });

            Assert.Same(expected, actual);
            Assert.Equal(new[] { "consume", "receive" }, fixture.Calls);
            CountingConnectHandle first = Assert.Single(fixture.Handles);
            Assert.Equal(0, fixture.Consume.Count);
            Assert.Equal(1, first.DisconnectCount);
            Assert.Equal(new[] { 0, 0, 0, 0 }, fixture.Counts);
        }
        finally
        {
            fixture.DisconnectAll();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-LIFECYCLE", "successful-host-connection-owns-all-four-observers")]
    public void ConnectObservers_SuccessReturnsOneHandleThatDisconnectsAllFourRegistrations()
    {
        IHostConfiguration host = CreateActualHostConfiguration();
        var fixture = new ObserverRegistrationFixture(null);
        ConnectHandle? joined = null;

        try
        {
            joined = host.ConnectReceiveEndpointContext(fixture.Context);

            Assert.IsType<MultipleConnectHandle>(joined);
            Assert.Equal(new[] { "consume", "receive", "publish", "send" }, fixture.Calls);
            Assert.Equal(new[] { 1, 1, 1, 1 }, fixture.Counts);
            Assert.Equal(4, fixture.Handles.Count);
            Assert.Same(host.SendObservers, Assert.Single(fixture.Send.Connected));
            Assert.All(fixture.Handles, handle => Assert.Equal(0, handle.DisconnectCount));

            joined.Dispose();

            Assert.Equal(new[] { 0, 0, 0, 0 }, fixture.Counts);
            Assert.All(fixture.Handles, handle => Assert.Equal(1, handle.DisconnectCount));

            joined.Disconnect();
            Assert.All(fixture.Handles, handle => Assert.Equal(1, handle.DisconnectCount));
        }
        finally
        {
            joined?.Dispose();
            fixture.DisconnectAll();
        }
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-LIFECYCLE", "failed-host-connection-attempts-all-cleanup-and-retains-causes")]
    public void ConnectObservers_FailedRegistrationDisconnectsEveryAdmittedHandleAndRetainsCleanupFailures(
        int failurePosition, bool cleanupFails)
    {
        string[] stages = ["consume", "receive", "publish", "send"];
        IHostConfiguration host = CreateActualHostConfiguration();
        var expected = new ExpectedTransportException(stages[failurePosition - 1] + " connector failed");
        var fixture = new ObserverRegistrationFixture(expected, stages[failurePosition - 1], cleanupFails);

        try
        {
            if (cleanupFails)
            {
                AggregateException actual = Assert.Throws<AggregateException>(() =>
                {
                    host.ConnectReceiveEndpointContext(fixture.Context);
                });
                IReadOnlyCollection<Exception> causes = actual.Flatten().InnerExceptions;
                Assert.Equal(failurePosition, causes.Count);
                Assert.Single(causes, cause => ReferenceEquals(expected, cause));
                Assert.Equal(failurePosition - 1, fixture.CleanupFailures.Count);
                Assert.All(fixture.CleanupFailures, cleanup =>
                    Assert.Single(causes, cause => ReferenceEquals(cleanup, cause)));
            }
            else
            {
                ExpectedTransportException actual = Assert.Throws<ExpectedTransportException>(() =>
                {
                    host.ConnectReceiveEndpointContext(fixture.Context);
                });
                Assert.Same(expected, actual);
                Assert.Empty(fixture.CleanupFailures);
            }

            Assert.Equal(stages[..failurePosition], fixture.Calls);
            Assert.Equal(failurePosition - 1, fixture.Handles.Count);
            Assert.Equal(new[] { 0, 0, 0, 0 }, fixture.Counts);
            Assert.All(fixture.Handles, handle => Assert.Equal(1, handle.DisconnectCount));
        }
        finally
        {
            // The body proves injected cleanup failures; final fixture release must not mask an original RED.
            fixture.DisconnectAll();
        }
    }

    private static IHostConfiguration CreateActualHostConfiguration()
    {
        var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var bus = new InMemoryBusConfiguration(topology, new Uri("loopback://observer-ownership/"));
        IHostConfiguration host = ((IBusConfiguration)bus).HostConfiguration;
        Assert.IsType<InMemoryHostConfiguration>(host);
        return host;
    }

    private sealed class ObserverRegistrationFixture
    {
        private readonly bool _cleanupFailures;

        public ObserverRegistrationFixture(Exception? receiveFailure, string failureStage = "receive", bool cleanupFailures = false)
        {
            _cleanupFailures = cleanupFailures;
            IReceivePipe pipe = DispatchProxy.Create<IReceivePipe, ObserverRegistrationProxy>();
            ((ObserverRegistrationProxy)pipe).Dispatch = (method, args) =>
            {
                if (method.Name != nameof(IReceivePipe.ConnectConsumeObserver))
                    throw new NotSupportedException(method.Name);

                Calls.Add("consume");
                return Record(Consume.Connect((IConsumeObserver)args![0]!));
            };

            Context = DispatchProxy.Create<ReceiveEndpointContext, ObserverRegistrationProxy>();
            ((ObserverRegistrationProxy)Context).Dispatch = (method, args) =>
            {
                switch (method.Name)
                {
                    case "get_ReceivePipe":
                        return pipe;
                    case nameof(ReceiveEndpointContext.ConnectReceiveObserver):
                        Calls.Add("receive");
                        if (receiveFailure is not null && failureStage == "receive")
                            throw receiveFailure;
                        return Record(Receive.Connect((IReceiveObserver)args![0]!));
                    case nameof(ReceiveEndpointContext.ConnectPublishObserver):
                        Calls.Add("publish");
                        if (receiveFailure is not null && failureStage == "publish")
                            throw receiveFailure;
                        return Record(Publish.Connect((IPublishObserver)args![0]!));
                    case nameof(ReceiveEndpointContext.ConnectSendObserver):
                        Calls.Add("send");
                        if (receiveFailure is not null && failureStage == "send")
                            throw receiveFailure;
                        return Record(Send.Connect((ISendObserver)args![0]!));
                    default:
                        throw new NotSupportedException(method.Name);
                }
            };
        }

        public ReceiveEndpointContext Context { get; }
        public Connectable<IConsumeObserver> Consume { get; } = new();
        public Connectable<IReceiveObserver> Receive { get; } = new();
        public Connectable<IPublishObserver> Publish { get; } = new();
        public Connectable<ISendObserver> Send { get; } = new();
        public List<string> Calls { get; } = [];
        public List<CountingConnectHandle> Handles { get; } = [];
        public List<Exception> CleanupFailures { get; } = [];
        public int[] Counts => [Consume.Count, Receive.Count, Publish.Count, Send.Count];

        private CountingConnectHandle Record(ConnectHandle actual)
        {
            Exception? cleanupFailure = _cleanupFailures
                ? new ExpectedDisconnectException("disconnect failure " + Handles.Count)
                : null;
            if (cleanupFailure is not null)
                CleanupFailures.Add(cleanupFailure);
            var counted = new CountingConnectHandle(actual, cleanupFailure);
            Handles.Add(counted);
            return counted;
        }

        public void DisconnectAll()
        {
            foreach (CountingConnectHandle handle in Handles)
                handle.DisconnectWithoutInjectedFailure();
        }
    }

    public class ObserverRegistrationProxy : DispatchProxy
    {
        internal Func<MethodInfo, object?[]?, object?> Dispatch { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Dispatch(targetMethod ?? throw new ArgumentNullException(nameof(targetMethod)), args);
    }

    private sealed class CountingConnectHandle(ConnectHandle actual, Exception? cleanupFailure = null) : ConnectHandle
    {
        private int _disconnected;
        public int DisconnectCount => Volatile.Read(ref _disconnected);

        public void Disconnect() => Disconnect(injectFailure: true);

        public void DisconnectWithoutInjectedFailure() => Disconnect(injectFailure: false);

        private void Disconnect(bool injectFailure)
        {
            if (Interlocked.Exchange(ref _disconnected, 1) != 0)
                return;

            actual.Disconnect();
            if (injectFailure && cleanupFailure is not null)
                throw cleanupFailure;
        }

        public void Dispose() => Disconnect();
    }

    private sealed class ExpectedDisconnectException(string message) : Exception(message);

    private sealed class ExpectedTransportException(string message) : Exception(message);

    private sealed class TestHostConfiguration(IRetryPolicy retryPolicy) : IHostConfiguration
    {
        public Uri HostAddress { get; } = new("loopback://host-retry/");
        public IRetryPolicy SendTransportRetryPolicy { get; } = retryPolicy;

        public IBusConfiguration BusConfiguration => throw new NotSupportedException();
        public bool DeployTopologyOnly { get; set; }
        public bool DeployPublishTopology { get; set; }
        public ISendObserver SendObservers => throw new NotSupportedException();
        public ILogContext? LogContext { get; set; }
        public ILogContext? ReceiveLogContext => throw new NotSupportedException();
        public ILogContext? SendLogContext => throw new NotSupportedException();
        public IBusTopology Topology => throw new NotSupportedException();
        public IRetryPolicy ReceiveTransportRetryPolicy => throw new NotSupportedException();
        public TimeSpan? ConsumerStopTimeout { get; set; }
        public TimeSpan? StopTimeout { get; set; }

        public IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(
            string queueName,
            Action<IReceiveEndpointConfigurator>? configure = null) => throw new NotSupportedException();

        public ConnectHandle ConnectReceiveEndpointContext(ReceiveEndpointContext context) =>
            throw new NotSupportedException();

        public IHost Build() => throw new NotSupportedException();

        public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer) =>
            throw new NotSupportedException();

        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => throw new NotSupportedException();

        public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => throw new NotSupportedException();

        public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();

        public IEnumerable<ValidationResult> Validate() => throw new NotSupportedException();
    }
}
