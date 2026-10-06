using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Advanced.Middleware;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class HostHandleApiTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-HOST-LIFECYCLE", "stop-debug-does-not-skip-rider-and-agent-shutdown")]
    public async Task HostStoppingDiagnostic_DoesNotSkipRiderAndAgentShutdownAsync(bool loggerThrows)
    {
        ILogContext? previous = LogContext.Current;
        var logger = new HostStopAdmissionLogger(loggerThrows);
        LogContext.ConfigureCurrentLogContext(logger);
        IHostConfiguration configuration = CreateHostConfiguration();
        var configurationProxy = (HostConfigurationProxy)(object)configuration;
        configurationProxy.LogContext = LogContext.Current;
        var rider = new TrackingRider();
        var agent = new HostOwnedStopAgent();
        var host = new TestHost(configuration, DispatchProxy.Create<IBusTopology, BusTopologyProxy>(), [agent]);
        host.AddRider("owned", rider);
        IHostHandle? handle = null;
        Task<HostReady>? ready = null;
        Task? stop = null;
        try
        {
            handle = host.Start(CancellationToken.None);
            ready = handle.Ready;
            HostReady actualReady = await ready.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.Equal(configuration.HostAddress, actualReady.HostAddress);
            Assert.Equal(1, rider.StartCount);
            stop = handle.StopAsync(CancellationToken.None);
            Exception? failure = await Record.ExceptionAsync(() =>
                stop.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
            Assert.Equal(1, logger.SelectedCalls);
            Assert.Equal(configuration.HostAddress, logger.Address);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            if (failure is not null)
                Assert.Same(logger.Failure, failure);

            // FIRST finite ownership oracle: optional diagnostics must not skip the started rider.
            Assert.Equal(1, rider.StopCount);
            Assert.Equal(1, agent.StopCount);
            Assert.Equal(CancellationToken.None, agent.StopToken);
            Assert.True(agent.Completed.IsCompletedSuccessfully);
            Assert.Null(failure);
            Assert.True(stop.IsCompletedSuccessfully);
        }
        finally
        {
            LogContext.ConfigureCurrentLogContext();
            configurationProxy.LogContext = LogContext.Current;
            try
            {
                if (stop is not null)
                    await ObserveHostStopAdmissionTaskAsync(stop);
            }
            finally
            {
                try
                {
                    if (ready is not null)
                        await ObserveHostStopAdmissionTaskAsync(ready);
                }
                finally
                {
                    try
                    {
                        await host.StopAsync(CancellationToken.None)
                            .WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                        await agent.Completed.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                    }
                    finally
                    {
                        LogContext.Current = previous;
                    }
                }
            }
        }
    }

    private static async Task ObserveHostStopAdmissionTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception failure) when (failure is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private sealed class HostOwnedStopAgent : Agent
    {
        public int StopCount { get; private set; }
        public CancellationToken StopToken { get; private set; }
        protected override Task StopAgentAsync(StopContext context)
        {
            StopCount++;
            StopToken = context.CancellationToken;
            return base.StopAgentAsync(context);
        }
    }

    private sealed class HostStopAdmissionLogger(bool throws) : ILogger
    {
        public IOException Failure { get; } = new("Host stop admission debug failure");
        public int SelectedCalls { get; private set; }
        public int ThrowCount { get; private set; }
        public Uri? Address { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            IEnumerable<KeyValuePair<string, object?>> fields = state as IEnumerable<KeyValuePair<string, object?>>
                ?? Array.Empty<KeyValuePair<string, object?>>();
            if (fields.FirstOrDefault(field => field.Key == "{OriginalFormat}").Value as string != "Stopping bus: {HostAddress}")
                return;
            SelectedCalls++;
            Address = fields.FirstOrDefault(field => field.Key == "HostAddress").Value as Uri;
            if (throws)
            {
                ThrowCount++;
                throw Failure;
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-HANDLE-API", "canonical-interface-shape-and-hidden-implementation")]
    public void HostHandle_UsesCanonicalInterfaceShapeWithoutLeakingItsImplementation()
    {
        Type handleType = typeof(IHostHandle);

        Assert.True(handleType.IsPublic);
        Assert.True(handleType.IsInterface);
        Assert.Null(handleType.Assembly.GetType("ViciOne.ServiceBus.Transports.HostHandle", throwOnError: false));
        Type? implementationType = handleType.Assembly.GetType("ViciOne.ServiceBus.Transports.StartHostHandle", throwOnError: false);
        Assert.NotNull(implementationType);
        Assert.True(implementationType!.IsNotPublic);
        Assert.True(implementationType.IsSealed);

        PropertyInfo ready = Assert.Single(handleType.GetProperties(BindingFlags.Instance | BindingFlags.Public));
        Assert.Equal(nameof(IHostHandle.Ready), ready.Name);
        Assert.Equal(typeof(Task<HostReady>), ready.PropertyType);

        MethodInfo stop = Assert.Single(
            handleType.GetMethods(BindingFlags.Instance | BindingFlags.Public),
            static method => !method.IsSpecialName);
        Assert.Equal(nameof(IHostHandle.StopAsync), stop.Name);
        Assert.Equal(typeof(Task), stop.ReturnType);

        ParameterInfo cancellationToken = Assert.Single(stop.GetParameters());
        Assert.Equal("cancellationToken", cancellationToken.Name);
        Assert.Equal(typeof(CancellationToken), cancellationToken.ParameterType);
        Assert.True(cancellationToken.IsOptional);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-LIFECYCLE", "synchronous-start-failure-rolls-back-started-resources")]
    public async Task Start_WhenALaterRiderFails_RollsBackEarlierRidersAndRemainsRestartableAsync()
    {
        var first = new TrackingRider();
        var second = new FailingOnceRider();
        var host = new TestHost(CreateHostConfiguration(), DispatchProxy.Create<IBusTopology, BusTopologyProxy>());
        host.AddRider("first", first);
        host.AddRider("second", second);

        ExpectedRiderStartException failure = Assert.Throws<ExpectedRiderStartException>(() => host.Start(CancellationToken.None));

        Assert.NotNull(failure);
        Assert.Equal(1, first.StartCount);
        Assert.Equal(1, first.StopCount);
        Assert.Equal(1, second.StartCount);

        IHostHandle handle = host.Start(CancellationToken.None);
        HostReady ready = await handle.Ready;
        await handle.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new Uri("loopback://localhost/"), ready.HostAddress);
        Assert.Equal(2, first.StartCount);
        Assert.Equal(2, first.StopCount);
        Assert.Equal(2, second.StartCount);
        Assert.Equal(1, second.StopCount);
    }

    private static IHostConfiguration CreateHostConfiguration()
    {
        var configuration = DispatchProxy.Create<IHostConfiguration, HostConfigurationProxy>();
        ((HostConfigurationProxy)(object)configuration).HostAddress = new Uri("loopback://localhost/");
        return configuration;
    }

    private sealed class TestHost(IHostConfiguration configuration, IBusTopology topology, IAgent[]? agents = null) : BaseHost(configuration, topology)
    {
        public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(
            IEndpointDefinition definition,
            IEndpointNameFormatter? endpointNameFormatter,
            Action<IReceiveEndpointConfigurator>? configureEndpoint = null) => throw new NotSupportedException();

        public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(
            string queueName,
            Action<IReceiveEndpointConfigurator>? configureEndpoint = null) => throw new NotSupportedException();

        protected override void Probe(ProbeContext context)
        {
        }

        protected override IAgent[] GetAgentHandles() => agents ?? [];
    }

    private class HostConfigurationProxy : DispatchProxy
    {
        public required Uri HostAddress { get; set; }
        public ILogContext? LogContext { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name switch
            {
                $"get_{nameof(IHostConfiguration.HostAddress)}" => HostAddress,
                $"get_{nameof(IHostConfiguration.LogContext)}" => LogContext,
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }
    }

    private class BusTopologyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private class TrackingRider : IRiderControl
    {
        private int _startCount;
        private int _stopCount;

        public int StartCount => Volatile.Read(ref _startCount);
        public int StopCount => Volatile.Read(ref _stopCount);

        public virtual RiderHandle Start(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _startCount);
            return new TrackingRiderHandle(() => Interlocked.Increment(ref _stopCount));
        }

        public IEnumerable<EndpointHealthResult> CheckEndpointHealth() => [];
    }

    private sealed class FailingOnceRider : TrackingRider
    {
        private int _attempt;

        public override RiderHandle Start(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _attempt) == 1)
            {
                base.Start(cancellationToken);
                throw new ExpectedRiderStartException();
            }

            return base.Start(cancellationToken);
        }
    }

    private sealed class TrackingRiderHandle(Action stopped) : RiderHandle
    {
        private int _isStopped;

        public Task Ready => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.Exchange(ref _isStopped, 1) == 0)
                stopped();
            return Task.CompletedTask;
        }
    }

    private sealed class ExpectedRiderStartException : Exception;
}
