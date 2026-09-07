using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryBusFactoryConfiguratorTests
{
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
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => InMemoryBus.Create(null, null!)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.CreateUsingInMemory(null!, _ => { })).ParamName);
        Assert.Equal("baseAddress", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.CreateUsingInMemory(Bus.Factory, null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.CreateUsingInMemory(Bus.Factory, (Action<IInMemoryBusFactoryConfigurator>)null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.UsingInMemory(null!, (Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>?)null)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            InMemoryConfigurationExtensions.UsingInMemory<ITestBus>(null!, (Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>?)null)).ParamName);
    }

    private static (InMemoryBusFactoryConfigurator Configurator, InMemoryBusConfiguration Bus) CreateConfigurator()
    {
        var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var bus = new InMemoryBusConfiguration(topology, new Uri("loopback://localhost/"));
        return (new InMemoryBusFactoryConfigurator(bus), bus);
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

    private interface ITestBus : IBus;
}
