using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class CoreRegistrationOracleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-DI-ENDPOINT-CALLBACK", "global-owner-order-exact-inputs-and-fail-stop")]
    public void EndpointCallbackAggregate_PreservesGlobalThenOwnerOrderAndFailureCutoff(bool typed)
    {
        var services = new ServiceCollection();
        var events = new List<string>();
        var expectedFailure = new InvalidOperationException("global-second");
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, ForbiddenProxy>();
        bool fail = false;
        IConfigureReceiveEndpoint global1 = new Callback((name, actual) => Record("global1", name, actual));
        IConfigureReceiveEndpoint global2 = new Callback((name, actual) =>
        {
            Record("global2", name, actual);
            if (fail)
                throw expectedFailure;
        });
        IConfigureReceiveEndpoint owner1 = new Callback((name, actual) => Record("owner1", name, actual));
        IConfigureReceiveEndpoint owner2 = new Callback((name, actual) => Record("owner2", name, actual));
        IConfigureReceiveEndpoint wrong = new Callback((_, _) => throw new Xunit.Sdk.XunitException("Wrong bus callback was selected."));
        services.AddSingleton(global1).AddSingleton(global2);
        if (typed)
        {
            services.AddSingleton(Bind<TestBus>.Create(owner1));
            services.AddSingleton(Bind<TestBus>.Create(owner2));
            services.AddSingleton(Bind<IBus>.Create(wrong));
        }
        else
        {
            services.AddSingleton(Bind<IBus>.Create(owner1));
            services.AddSingleton(Bind<IBus>.Create(owner2));
            services.AddSingleton(Bind<TestBus>.Create(wrong));
        }
        DependencyInjectionContainerRegistrar registrar = typed
            ? new DependencyInjectionContainerRegistrar<TestBus>(services)
            : new DependencyInjectionContainerRegistrar(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        IConfigureReceiveEndpoint aggregate = registrar.GetConfigureReceiveEndpoints(provider);
        Assert.Empty(events);
        aggregate.Configure(null, endpoint);
        Assert.Equal(new[] { "global1", "global2", "owner1", "owner2" }, events);

        events.Clear();
        fail = true;
        Assert.Same(expectedFailure, Assert.Throws<InvalidOperationException>(() => aggregate.Configure(null, endpoint)));
        Assert.Equal(new[] { "global1", "global2" }, events);

        void Record(string step, string? name, IReceiveEndpointConfigurator actual)
        {
            Assert.Null(name);
            Assert.Same(endpoint, actual);
            events.Add(step);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-DI-FACTORY-CONTRACT", "bus-factory-exact-owner-inputs")]
    public void BusFactoryResolution_ForwardsExactOwnerContextSpecificationsAndName(bool typed)
    {
        var services = new ServiceCollection();
        var marker = new Marker();
        services.AddSingleton(marker);
        ServiceCollectionBusConfigurator configurator = typed
            ? new ServiceCollectionBusConfigurator<TestBus, TestBusInstance>(services)
            : new ServiceCollectionBusConfigurator(services);
        var first = new Specification();
        var second = new Specification();
        var wrong = new Specification();
        if (typed)
        {
            services.AddSingleton(Bind<TestBus>.Create<IBusInstanceSpecification>(first));
            services.AddSingleton(Bind<TestBus>.Create<IBusInstanceSpecification>(second));
            services.AddSingleton(Bind<IBus>.Create<IBusInstanceSpecification>(wrong));
        }
        else
        {
            services.AddSingleton(Bind<IBus>.Create<IBusInstanceSpecification>(first));
            services.AddSingleton(Bind<IBus>.Create<IBusInstanceSpecification>(second));
            services.AddSingleton(Bind<TestBus>.Create<IBusInstanceSpecification>(wrong));
        }
        IBusControl busControl = DispatchProxy.Create<IBusControl, ForbiddenProxy>();
        IBusInstance instance = DispatchProxy.Create<IBusInstance, InstanceProxy>();
        ((InstanceProxy)(object)instance).BusControl = busControl;
        var factory = new Factory(instance);
        configurator.SetBusFactory(factory);
        Assert.Equal(0, factory.Calls);
        using ServiceProvider provider = services.BuildServiceProvider();
        IBusRegistrationContext expectedContext = typed
            ? provider.GetRequiredService<Bind<TestBus, IBusRegistrationContext>>().Value
            : provider.GetRequiredService<Bind<IBus, IBusRegistrationContext>>().Value;
        Assert.Same(marker, expectedContext.GetRequiredService<Marker>());
        Assert.Equal(0, factory.Calls);
        IBusInstance resolved = typed
            ? provider.GetRequiredService<IBusInstance<TestBus>>()
            : provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value;
        IBusInstance repeated = typed
            ? provider.GetRequiredService<IBusInstance<TestBus>>()
            : provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value;
        Assert.Same(resolved, repeated);
        Assert.Equal(1, factory.Calls);
        Assert.Same(expectedContext, factory.Context);
        Assert.Equal(typed ? nameof(TestBus) : string.Empty, factory.Name);
        IBusInstanceSpecification[] actual = Assert.IsType<IBusInstanceSpecification[]>(factory.Specifications);
        Assert.Equal(2, actual.Length);
        Assert.Same(first, actual[0]);
        Assert.Same(second, actual[1]);
        Assert.DoesNotContain(wrong, actual);
        if (typed)
            Assert.Same(instance, Assert.IsAssignableFrom<IBusInstance<TestBus>>(resolved).BusInstance);
        else
            Assert.Same(instance, resolved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-DI-FACTORY-CONTRACT", "bus-factory-null-duplicate-before-descriptor-mutation")]
    public void BusFactorySelection_RejectsInvalidOrRepeatedFactoriesBeforeDescriptorMutation(bool typed)
    {
        var services = new ServiceCollection();
        ServiceCollectionBusConfigurator configurator = typed
            ? new ServiceCollectionBusConfigurator<TestBus, TestBusInstance>(services)
            : new ServiceCollectionBusConfigurator(services);
        ServiceDescriptor[] beforeNull = services.ToArray();
        Assert.Equal("busFactory", Assert.Throws<ArgumentNullException>(() => configurator.SetBusFactory<Factory>(null!)).ParamName);
        AssertDescriptorsSame(beforeNull, services);
        IBusInstance instance = DispatchProxy.Create<IBusInstance, ForbiddenProxy>();
        var accepted = new Factory(instance);
        configurator.SetBusFactory(accepted);
        ServiceDescriptor[] beforeDuplicate = services.ToArray();
        var rejected = new Factory(instance);
        Assert.Throws<ConfigurationException>(() => configurator.SetBusFactory(rejected));
        AssertDescriptorsSame(beforeDuplicate, services);
        Assert.Equal(0, accepted.Calls);
        Assert.Equal(0, rejected.Calls);
    }

    private static void AssertDescriptorsSame(ServiceDescriptor[] before, IServiceCollection actual)
    {
        Assert.Equal(before.Length, actual.Count);
        for (int i = 0; i < before.Length; i++)
            Assert.Same(before[i], actual[i]);
    }

    private sealed class Callback(Action<string?, IReceiveEndpointConfigurator> callback) : IConfigureReceiveEndpoint
    {
        public void Configure(string? name, IReceiveEndpointConfigurator configurator) => callback(name, configurator);
    }

    private sealed class Factory(IBusInstance instance) : IRegistrationBusFactory
    {
        public int Calls { get; private set; }
        public IBusRegistrationContext? Context { get; private set; }
        public IBusInstanceSpecification[]? Specifications { get; private set; }
        public string? Name { get; private set; }
        public IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
        {
            Calls++;
            Context = context;
            Specifications = specifications.ToArray();
            Name = busName;
            return instance;
        }
    }

    private sealed class Specification : IBusInstanceSpecification
    {
        public IEnumerable<ValidationResult> Validate() => throw new Xunit.Sdk.XunitException("The registration forwarder must leave validation to its factory.");
        public void Configure(IBusInstance busInstance) => throw new Xunit.Sdk.XunitException("The registration forwarder must leave specification execution to its factory.");
    }

    public class ForbiddenProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new Xunit.Sdk.XunitException($"Unexpected operation: {targetMethod?.Name}");
    }

    public class InstanceProxy : ForbiddenProxy
    {
        public IBusControl BusControl { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_BusControl" ? BusControl : base.Invoke(targetMethod, args);
    }

    public sealed class Marker;
    public interface TestBus : IBus;
    public sealed class TestBusInstance(IBusControl busControl) : BusInstance<TestBus>(busControl), TestBus;
}
