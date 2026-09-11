using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class HostHandleApiTests
{
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

    private sealed class TestHost(IHostConfiguration configuration, IBusTopology topology) : BaseHost(configuration, topology)
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
    }

    private class HostConfigurationProxy : DispatchProxy
    {
        public required Uri HostAddress { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name switch
            {
                $"get_{nameof(IHostConfiguration.HostAddress)}" => HostAddress,
                $"get_{nameof(IHostConfiguration.LogContext)}" => null,
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
