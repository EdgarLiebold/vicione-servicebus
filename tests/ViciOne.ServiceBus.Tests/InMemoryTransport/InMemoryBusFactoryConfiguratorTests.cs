using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Topology;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryBusFactoryConfiguratorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "custom-address-entry-points-and-delay-provider-ownership")]
    public async Task CustomAddressEntryPoints_OwnTheirExactHostsAndDelayProvidersAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(15);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var standaloneAddress = new Uri("loopback://standalone/tenant");
        IBusControl standalone = Bus.Factory.CreateUsingInMemory(standaloneAddress, _ => { });

        await standalone.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Assert.Equal(standaloneAddress.Host, standalone.Address.Host);
            Assert.StartsWith(
                $"{standaloneAddress.AbsolutePath.TrimEnd('/')}/",
                standalone.Address.AbsolutePath,
                StringComparison.Ordinal);
        }
        finally
        {
            await standalone.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        var defaultAddress = new Uri("loopback://default-owner/tenant");
        var typedAddress = new Uri("loopback://typed-owner/tenant");
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory(defaultAddress);
            })
            .AddViciOneServiceBus<ITestBus>(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory(typedAddress);
            })
            .BuildServiceProvider(validateScopes: true);
        IBus defaultBus = provider.GetRequiredService<IBus>();
        ITestBus typedBus = provider.GetRequiredService<ITestBus>();
        IInMemoryDelayProvider defaultDelayProvider = provider.GetRequiredService<IInMemoryDelayProvider>();
        IInMemoryDelayProvider typedDelayProvider = provider
            .GetRequiredService<Bind<ITestBus, IInMemoryDelayProvider>>()
            .Value;

        Assert.Equal(defaultAddress.Host, defaultBus.Address.Host);
        Assert.StartsWith(
            $"{defaultAddress.AbsolutePath.TrimEnd('/')}/",
            defaultBus.Address.AbsolutePath,
            StringComparison.Ordinal);
        Assert.Equal(typedAddress.Host, typedBus.Address.Host);
        Assert.StartsWith(
            $"{typedAddress.AbsolutePath.TrimEnd('/')}/",
            typedBus.Address.AbsolutePath,
            StringComparison.Ordinal);
        Assert.NotSame(defaultDelayProvider, typedDelayProvider);

        Task relativeDelay = defaultDelayProvider.DelayAsync(TimeSpan.FromMinutes(1), cancellationToken);
        defaultDelayProvider.Advance(TimeSpan.FromMinutes(1));
        await relativeDelay.WaitAsync(timeout, cancellationToken);

        Task absoluteDelay = typedDelayProvider.DelayAsync(
            typedDelayProvider.UtcNow.AddMinutes(1),
            cancellationToken);
        typedDelayProvider.Advance(TimeSpan.FromMinutes(1));
        await absoluteDelay.WaitAsync(timeout, cancellationToken);

        await using ServiceProvider typedOnlyProvider = new ServiceCollection()
            .AddViciOneServiceBus<ITestBus>(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory(new Uri("loopback://typed-only/tenant"));
            })
            .BuildServiceProvider(validateScopes: true);
        IInMemoryDelayProvider typedOnlyDefault = typedOnlyProvider.GetRequiredService<IInMemoryDelayProvider>();
        IInMemoryDelayProvider typedOnlyBound = typedOnlyProvider
            .GetRequiredService<Bind<ITestBus, IInMemoryDelayProvider>>()
            .Value;

        Assert.Same(typedOnlyDefault, typedOnlyBound);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "host-identity-is-order-independent")]
    public async Task HostConfiguredAfterAnEndpoint_OwnsThatEndpointsFinalInputAddressAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(15);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observedInputAddress = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = InMemoryBus.Create(configurator =>
        {
            configurator.ReceiveEndpoint("late-host-endpoint", endpoint =>
                endpoint.Handler<HostOrderMessage>(context =>
                {
                    observedInputAddress.TrySetResult(context.Advanced().ReceiveContext.InputAddress);
                    return Task.CompletedTask;
                }));
            configurator.Host(new Uri("loopback://late-host/tenant-blue"), null);
        });

        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(
                new Uri("loopback://late-host/tenant-blue/late-host-endpoint"),
                cancellationToken);
            await endpoint.SendAsync(new HostOrderMessage(), cancellationToken);

            Assert.Equal(
                new Uri("loopback://late-host/tenant-blue/late-host-endpoint"),
                await observedInputAddress.Task.WaitAsync(timeout, cancellationToken));
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "bus-endpoint-auto-start-follows-configured-value")]
    public void AutoStart_ChangesTheBusEndpointStartupPolicy()
    {
        var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var bus = new InMemoryBusConfiguration(topology, new Uri("loopback://localhost/"));
        var configurator = new InMemoryBusFactoryConfigurator(bus);
        ConsumePipeSpecification specification = Assert.IsType<ConsumePipeSpecification>(
            bus.BusEndpointConfiguration.Consume.Specification);

        Assert.True(specification.AutoStart);

        configurator.AutoStart = false;

        Assert.False(specification.AutoStart);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "host-and-publish-callbacks-apply-exactly-once")]
    public void HostAndPublishConfiguration_AppliesEveryCallbackToTheOwnedConfiguration()
    {
        (InMemoryBusFactoryConfigurator configurator, InMemoryBusConfiguration bus) = CreateConfigurator();
        var hostCallbacks = 0;
        var typedPublishCallbacks = 0;
        var runtimePublishCallbacks = 0;

        configurator.Host(host =>
        {
            hostCallbacks++;
            host.QueueCapacity = 17;
        });
        configurator.Host(new Uri("loopback://custom/"), _ => hostCallbacks++);
        configurator.Host("tenant-blue", _ => hostCallbacks++);
        configurator.Publish<ConfiguredMessage>(publish =>
        {
            typedPublishCallbacks++;
            publish.ExchangeType = InMemoryExchangeType.Direct;
        });
        configurator.Publish(typeof(RuntimeConfiguredMessage), _ => runtimePublishCallbacks++);

        Assert.Equal(3, hostCallbacks);
        Assert.Equal(1, typedPublishCallbacks);
        Assert.Equal(1, runtimePublishCallbacks);
        Assert.Equal(17, bus.HostConfiguration.QueueCapacity);
        Assert.Equal(new Uri("loopback://custom/tenant-blue"), bus.HostConfiguration.HostAddress);
        Assert.Equal(
            InMemoryExchangeType.Direct,
            ((IInMemoryMessagePublishTopology<ConfiguredMessage>)
                configurator.PublishTopology.GetMessageTopology<ConfiguredMessage>()).ExchangeType);
        Assert.Same(
            configurator.PublishTopology.GetMessageTopology<RuntimeConfiguredMessage>(),
            configurator.PublishTopology.GetMessageTopology(typeof(RuntimeConfiguredMessage)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "provider-neutral-endpoint-definition-callback")]
    public void ProviderNeutralEndpointDefinition_InvokesItsCallbackOnTheOwnedEndpoint()
    {
        (InMemoryBusFactoryConfigurator configurator, _) = CreateConfigurator();
        var callbackCount = 0;

        configurator.ReceiveEndpoint(
            new NeutralEndpointDefinition(),
            null,
            (Action<IReceiveEndpointConfigurator>)(_ => callbackCount++));

        Assert.Equal(1, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "virtual-host-value-remains-one-address-component")]
    public void Host_PreservesTheCompleteVirtualHostAsOneAddressComponent()
    {
        (InMemoryBusFactoryConfigurator configurator, InMemoryBusConfiguration bus) = CreateConfigurator();

        configurator.Host("tenant/blue", null);

        Assert.Equal("tenant/blue", new InMemoryHostAddress(bus.HostConfiguration.HostAddress).VirtualHost);
        Assert.Equal(new Uri("loopback://localhost/tenant%2Fblue"), bus.HostConfiguration.HostAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "publish-exchange-type-rejected-at-configuration-boundary")]
    public void Publish_RejectsAnUndefinedExchangeTypeAtTheConfigurationBoundary()
    {
        (InMemoryBusFactoryConfigurator configurator, _) = CreateConfigurator();
        IInMemoryMessagePublishTopologyConfigurator<ConfiguredMessage> topology =
            configurator.PublishTopology.GetMessageTopology<ConfiguredMessage>();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            topology.ExchangeType = (InMemoryExchangeType)42);

        Assert.Equal("value", exception.ParamName);
        Assert.Equal((InMemoryExchangeType)42, exception.ActualValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-FACTORY", "build-rejects-null-required-inputs")]
    public void Build_RejectsEveryNullRequiredInput()
    {
        (InMemoryBusFactoryConfigurator configurator, InMemoryBusConfiguration bus) = CreateConfigurator();

        ArgumentNullException factory = Assert.Throws<ArgumentNullException>(() =>
            BusFactoryExtensions.Build(null!, bus));
        ArgumentNullException configuration = Assert.Throws<ArgumentNullException>(() =>
            configurator.Build(null!));
        ArgumentNullException dependencies = Assert.Throws<ArgumentNullException>(() =>
            configurator.Build(bus, null!));

        Assert.Equal("factory", factory.ParamName);
        Assert.Equal("busConfiguration", configuration.ParamName);
        Assert.Equal("dependencies", dependencies.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-FACTORY", "fault-observer-cannot-replace-construction-failure")]
    public void CreationFaultObserverFailure_DoesNotReplaceTheConstructionFailure()
    {
        var observer = new FailingCreationObserver();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            InMemoryBus.Create(configurator => configurator.ConnectBusObserver(observer)));

        Assert.IsType<ExpectedConstructionException>(exception.InnerException);
        Assert.Equal(1, observer.CreateFaultedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "public-entry-points-validate-required-inputs")]
    public void PublicEntryPoints_RejectEveryMissingRequiredInput()
    {
        (InMemoryBusFactoryConfigurator configurator, _) = CreateConfigurator();

        Assert.Equal("busConfiguration", Assert.Throws<ArgumentNullException>(() =>
            new InMemoryBusFactoryConfigurator(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            configurator.CreateBusEndpointConfiguration(null!)).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
            configurator.Publish(null!)).ParamName);
        Assert.Equal("baseAddress", Assert.Throws<ArgumentNullException>(() =>
            configurator.Host((Uri)null!, null)).ParamName);
        Assert.Equal("virtualHost", Assert.Throws<ArgumentException>(() =>
            configurator.Host(" ", null)).ParamName);
        Assert.Equal("definition", Assert.Throws<ArgumentNullException>(() =>
            configurator.ReceiveEndpoint(null!, null, (Action<IInMemoryReceiveEndpointConfigurator>?)null)).ParamName);
        Assert.Equal("definition", Assert.Throws<ArgumentNullException>(() =>
            configurator.ReceiveEndpoint(null!, null, (Action<IReceiveEndpointConfigurator>?)null)).ParamName);
        Assert.Equal("queueName", Assert.Throws<ArgumentException>(() =>
            configurator.ReceiveEndpoint(" ", (Action<IInMemoryReceiveEndpointConfigurator>)(_ => { }))).ParamName);
        Assert.Equal("configureEndpoint", Assert.Throws<ArgumentNullException>(() =>
            configurator.ReceiveEndpoint("queue", (Action<IInMemoryReceiveEndpointConfigurator>)null!)).ParamName);
        Assert.Equal("queueName", Assert.Throws<ArgumentException>(() =>
            configurator.ReceiveEndpoint(" ", (Action<IReceiveEndpointConfigurator>)(_ => { }))).ParamName);
        Assert.Equal("configureEndpoint", Assert.Throws<ArgumentNullException>(() =>
            configurator.ReceiveEndpoint("queue", (Action<IReceiveEndpointConfigurator>)null!)).ParamName);

        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => InMemoryBus.Create(null!)).ParamName);
        Assert.Equal("baseAddress", Assert.Throws<ArgumentNullException>(() => InMemoryBus.Create(null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            InMemoryBus.Create(new Uri("loopback://localhost/"), null!)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.CreateUsingInMemory(null!, _ => { })).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.CreateUsingInMemory(
                null!, new Uri("loopback://localhost/"), _ => { })).ParamName);
        Assert.Equal("baseAddress", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.CreateUsingInMemory(Bus.Factory, null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.CreateUsingInMemory(Bus.Factory, (Action<IInMemoryBusFactoryConfigurator>)null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.CreateUsingInMemory(
                Bus.Factory, new Uri("loopback://localhost/"), null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.UsingInMemory(null!, (Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>?)null)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.UsingInMemory(
                null!, new Uri("loopback://localhost/"), null)).ParamName);
        Assert.Equal("baseAddress", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.UsingInMemory(CreateRegistrationConfigurator(), null!, null)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.UsingInMemory<ITestBus>(null!, (Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>?)null)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.UsingInMemory<ITestBus>(
                null!, new Uri("loopback://localhost/"), null)).ParamName);
        Assert.Equal("baseAddress", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.UsingInMemory<ITestBus>(CreateTypedRegistrationConfigurator(), null!, null)).ParamName);
    }

    private static (InMemoryBusFactoryConfigurator Configurator, InMemoryBusConfiguration Bus) CreateConfigurator()
    {
        var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var bus = new InMemoryBusConfiguration(topology, new Uri("loopback://localhost/"));
        return (new InMemoryBusFactoryConfigurator(bus), bus);
    }

    private static IBusRegistrationConfigurator CreateRegistrationConfigurator()
    {
        var services = new ServiceCollection();
        IBusRegistrationConfigurator? configurator = null;

        services.AddViciOneServiceBus(value => configurator = value);

        return Assert.IsAssignableFrom<IBusRegistrationConfigurator>(configurator);
    }

    private static IBusRegistrationConfigurator<ITestBus> CreateTypedRegistrationConfigurator()
    {
        var services = new ServiceCollection();
        IBusRegistrationConfigurator<ITestBus>? configurator = null;

        services.AddViciOneServiceBus<ITestBus>(value => configurator = value);

        return Assert.IsAssignableFrom<IBusRegistrationConfigurator<ITestBus>>(configurator);
    }

    private sealed class FailingCreationObserver : IBusObserver
    {
        public int CreateFaultedCount { get; private set; }

        public void PostCreate(IBus bus) => throw new ExpectedConstructionException();

        public void CreateFaulted(Exception exception)
        {
            CreateFaultedCount++;
            throw new ExpectedObservationException();
        }

        public Task PreStartAsync(IBus bus) => Task.CompletedTask;

        public Task PostStartAsync(IBus bus, Task<BusReady> busReady) => Task.CompletedTask;

        public Task StartFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;

        public Task PreStopAsync(IBus bus) => Task.CompletedTask;

        public Task PostStopAsync(IBus bus) => Task.CompletedTask;

        public Task StopFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;
    }

    private sealed class ExpectedConstructionException : Exception;

    private sealed class ExpectedObservationException : Exception;

    private sealed record ConfiguredMessage;

    private sealed record RuntimeConfiguredMessage;

    private sealed record HostOrderMessage;

    private sealed class NeutralEndpointDefinition : DefaultEndpointDefinition
    {
        public override string GetEndpointName(IEndpointNameFormatter formatter) => "neutral-endpoint";
    }

    public interface ITestBus : IBus;
}
