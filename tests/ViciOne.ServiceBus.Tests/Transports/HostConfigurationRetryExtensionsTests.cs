using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Transports;
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
