using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class DependencyInjectionConfigurationContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DI-REQUEST-CLIENT", "destination-registration-inherits-configured-default-timeout")]
    public void DestinationRequestClient_UsesTheConfiguredDefaultTimeout()
    {
        var services = new ServiceCollection();
        var registrar = new RecordingContainerRegistrar(services);
        var configurator = new InspectableRegistrationConfigurator(services, registrar);
        var expected = new RequestTimeout(TimeSpan.FromSeconds(37));
        var destination = new Uri("queue:configured-default-timeout");

        configurator.SetDefaultRequestTimeout(expected);
        configurator.AddRequestClient<RequestMessage>(destination);

        Assert.Equal(typeof(RequestMessage), registrar.RequestType);
        Assert.Equal(destination, registrar.DestinationAddress);
        Assert.Equal(expected, registrar.Timeout);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-REQUEST-CLIENT", "explicit-built-in-timeout-remains-explicit")]
    public void DestinationRequestClient_PreservesAnExplicitBuiltInTimeout()
    {
        var services = new ServiceCollection();
        var registrar = new RecordingContainerRegistrar(services);
        var configurator = new InspectableRegistrationConfigurator(services, registrar);
        var configuredDefault = new RequestTimeout(TimeSpan.FromSeconds(37));
        var destination = new Uri("queue:explicit-built-in-timeout");

        configurator.SetDefaultRequestTimeout(configuredDefault);
        configurator.AddRequestClient<RequestMessage>(destination, RequestTimeout.Default);

        Assert.Equal(typeof(RequestMessage), registrar.RequestType);
        Assert.Equal(destination, registrar.DestinationAddress);
        Assert.Equal(RequestTimeout.Default, registrar.Timeout);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-RIDER-COMPLETION", "participant-completes-once-before-add-rider-returns")]
    public void RiderRegistration_CompletesCapabilityParticipantsExactlyOnce()
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());
        var participant = new RecordingCompletionParticipant();
        IRegistrationConfigurator? riderConfigurator = null;

        configurator.AddRider(rider =>
        {
            riderConfigurator = rider;
            Assert.Same(
                participant,
                rider.Advanced().GetOrAddRegistrationCompletionParticipant(() => participant));
        });

        Assert.Equal(1, participant.CompletionCount);
        Assert.Same(riderConfigurator, participant.Configurator);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-RIDER-COMPLETION", "missing-callback-rejected-before-registration")]
    public void RiderRegistration_RejectsAMissingCallbackBeforeChangingServices()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        int initialCount = services.Count;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            configurator.AddRider((Action<IRiderRegistrationConfigurator>)null!));

        Assert.Equal("configure", exception.ParamName);
        Assert.Equal(initialCount, services.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-ENDPOINT-SETTINGS", "explicit-constructor-value-without-throwing-container-placeholder")]
    public void EndpointSettings_ArePassedExplicitlyWithoutRegisteringAThrowingPlaceholder()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);
        var settings = new RecordingEndpointSettings();

        registrar.AddEndpointDefinition<EndpointMessage, SettingsBackedEndpointDefinition>(settings);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        SettingsBackedEndpointDefinition definition = provider.GetRequiredService<SettingsBackedEndpointDefinition>();

        Assert.Same(settings, definition.Settings);
        Assert.Same(definition, provider.GetRequiredService<IEndpointDefinition<EndpointMessage>>());
        Assert.Null(provider.GetService<IEndpointSettings<IEndpointDefinition<EndpointMessage>>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SCOPED-FILTER", "closed-compensate-filter-is-attached-without-generic-reconstruction")]
    public void CompensateActivityFilter_AcceptsAClosedFilterType()
    {
        var specifications = new List<object>();
        IRegistrationContext context = CreateConfigurationProxy<TestRegistrationContext>(specifications);
        var messageTypes = new MessageTypeFilterConfigurator();
        messageTypes.Include<CompensationLog>();
        var observer = new ScopedCompensateActivityPipeSpecificationObserver(
            typeof(TypedCompensateFilter),
            context,
            messageTypes.Filter);
        ICompensateActivityPipeConfigurator<CompensatingActivity, CompensationLog> configurator =
            CreateConfigurationProxy<ICompensateActivityPipeConfigurator<CompensatingActivity, CompensationLog>>(specifications);

        observer.CompensateActivityConfigured(configurator);

        Assert.Single(specifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-BOUNDARIES", "scope-and-request-client-required-inputs")]
    public void ScopeAndRequestClientEntryPoints_RejectEveryMissingRequiredInput()
    {
        var specifications = new List<object>();
        var context = CreateConfigurationProxy<TestRegistrationContext>(specifications);
        var consumePipe = CreateConfigurationProxy<IConsumePipeConfigurator>(specifications);
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionExtensions.UseServiceScope(null!, context)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionExtensions.UseServiceScope(consumePipe, null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionExtensions.CreateRequestClient<RequestMessage>(null!)).ParamName);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() =>
            provider.CreateRequestClient<RequestMessage>(null!)).ParamName);

        var registration = new ServiceCollectionBusConfigurator(new ServiceCollection());
        Assert.Equal("endpointDefinitionType", Assert.Throws<ArgumentNullException>(() =>
            registration.AddEndpoint(null!)).ParamName);
        Assert.Equal("registration", Assert.Throws<ArgumentNullException>(() =>
            registration.Advanced().AddEndpoint<SettingsBackedEndpointDefinition, EndpointMessage>(null!)).ParamName);
        Assert.Equal("requestType", Assert.Throws<ArgumentNullException>(() =>
            registration.AddRequestClient(null!)).ParamName);
        Assert.Equal("requestType", Assert.Throws<ArgumentNullException>(() =>
            registration.AddRequestClient(null!, new Uri("queue:request"))).ParamName);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() =>
            registration.AddRequestClient(typeof(RequestMessage), null!)).ParamName);
        Assert.Equal("endpointNameFormatter", Assert.Throws<ArgumentNullException>(() =>
            registration.SetEndpointNameFormatter(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-BOUNDARIES", "all-runtime-filter-type-overloads")]
    public void RuntimeFilterEntryPoints_RejectAMissingFilterType()
    {
        var specifications = new List<object>();
        var context = CreateConfigurationProxy<TestRegistrationContext>(specifications);
        var consumePipe = CreateConfigurationProxy<IConsumePipeConfigurator>(specifications);
        var sendPipe = CreateConfigurationProxy<ISendPipelineConfigurator>(specifications);
        var publishPipe = CreateConfigurationProxy<IPublishPipelineConfigurator>(specifications);

        Assert.Equal("filterType", Assert.Throws<ArgumentNullException>(() =>
            consumePipe.UseConsumeFilter(null!, context)).ParamName);
        Assert.Equal("filterType", Assert.Throws<ArgumentNullException>(() =>
            sendPipe.UseSendFilter(null!, context)).ParamName);
        Assert.Equal("filterType", Assert.Throws<ArgumentNullException>(() =>
            publishPipe.UsePublishFilter(null!, context)).ParamName);
        Assert.Equal("filterType", Assert.Throws<ArgumentNullException>(() =>
            consumePipe.UseExecuteActivityFilter(null!, context)).ParamName);
        Assert.Equal("filterType", Assert.Throws<ArgumentNullException>(() =>
            consumePipe.UseCompensateActivityFilter(null!, context)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SCOPED-FILTER", "all-pipelines-reject-incompatible-open-and-closed-filter-types")]
    public void FilterEntryPoints_RejectTypesThatCannotFilterTheirPipelineContext()
    {
        var specifications = new List<object>();
        var context = CreateConfigurationProxy<TestRegistrationContext>(specifications);
        var consumePipe = CreateConfigurationProxy<IConsumePipeConfigurator>(specifications);
        var sendPipe = CreateConfigurationProxy<ISendPipelineConfigurator>(specifications);
        var publishPipe = CreateConfigurationProxy<IPublishPipelineConfigurator>(specifications);

        Assert.Equal("filterType", Assert.Throws<ArgumentException>(() =>
            consumePipe.UseConsumeFilter(typeof(List<>), context)).ParamName);
        Assert.Equal("filterType", Assert.Throws<ArgumentException>(() =>
            sendPipe.UseSendFilter(typeof(List<>), context)).ParamName);
        Assert.Equal("filterType", Assert.Throws<ArgumentException>(() =>
            publishPipe.UsePublishFilter(typeof(List<>), context)).ParamName);
        Assert.Equal("filterType", Assert.Throws<ArgumentException>(() =>
            consumePipe.UseExecuteActivityFilter(typeof(List<>), context)).ParamName);
        Assert.Equal("filterType", Assert.Throws<ArgumentException>(() =>
            consumePipe.UseCompensateActivityFilter(typeof(List<>), context)).ParamName);

        Assert.Equal("TFilter", Assert.Throws<ArgumentException>(() =>
            consumePipe.UseConsumeFilter<string>(context)).ParamName);
        Assert.Equal("TFilter", Assert.Throws<ArgumentException>(() =>
            sendPipe.UseSendFilter<string>(context)).ParamName);
        Assert.Equal("TFilter", Assert.Throws<ArgumentException>(() =>
            publishPipe.UsePublishFilter<string>(context)).ParamName);
        Assert.Equal("TFilter", Assert.Throws<ArgumentException>(() =>
            consumePipe.UseExecuteActivityFilter<string>(context)).ParamName);
        Assert.Equal("TFilter", Assert.Throws<ArgumentException>(() =>
            consumePipe.UseCompensateActivityFilter<string>(context)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-ENDPOINT-DEFINITION", "construct-combine-name-and-configure-boundaries")]
    public void EndpointDefinitions_RejectEveryInvalidRequiredInput()
    {
        var specifications = new List<object>();
        var context = CreateConfigurationProxy<TestRegistrationContext>(specifications);
        IEndpointDefinition first = new NamedEndpointDefinition("first");
        IEndpointDefinition second = new NamedEndpointDefinition("second");

        Assert.Equal("definitions", Assert.Throws<ArgumentNullException>(() =>
            EndpointDefinitionExtensions.Combine(null!, context, "combined")).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new[] { first }.Combine(null!, "combined")).ParamName);
        Assert.Equal("endpointName", Assert.Throws<ArgumentException>(() =>
            new[] { first }.Combine(context, " ")).ParamName);
        Assert.Equal("definitions", Assert.Throws<ArgumentException>(() =>
            new IEndpointDefinition[] { first, null! }.Combine(context, "combined")).ParamName);

        Assert.Equal("endpointName", Assert.Throws<ArgumentException>(() =>
            new NamedEndpointDefinition(" ")).ParamName);
        Assert.Equal("endpointName", Assert.Throws<ArgumentException>(() =>
            new DelegateEndpointDefinition(" ", new RecordingDefinition(), null)).ParamName);
        Assert.Equal("definition", Assert.Throws<ArgumentNullException>(() =>
            new DelegateEndpointDefinition("delegated", null!, null)).ParamName);
        Assert.Equal("endpointDefinition", Assert.Throws<ArgumentNullException>(() =>
            new RegistrationContextEndpointDefinition(null!, context)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new RegistrationContextEndpointDefinition(first, null!)).ParamName);

        var combined = Assert.IsType<CombinedEndpointDefinition>(new[] { first, second }.Combine(context, "combined"));
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() => first.GetEndpointName(null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() => combined.GetEndpointName(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            first.Configure<IReceiveEndpointConfigurator>(null!, context)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            combined.Configure<IReceiveEndpointConfigurator>(null!, context)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-BOUNDARIES", "hosting-and-delayed-scheduler-required-inputs")]
    public void HostingAndDelayedSchedulerEntryPoints_RejectMissingOwners()
    {
        Assert.Equal("hostBuilder", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionHostingExtensions.UseViciOneServiceBus(null!)).ParamName);
        Assert.Equal("hostBuilder", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionHostingExtensions.UseViciOneServiceBus<TestBus>(null!)).ParamName);
        Assert.Equal("hostBuilder", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionHostingExtensions.UseViciOneServiceBus<TestBus, TestBusInstance>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            DelayedMessageSchedulerRegistrationExtensions.AddDelayedMessageScheduler(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            DelayedMessageSchedulerRegistrationExtensions.AddDelayedMessageScheduler<TestBus>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-BOUNDARIES", "consumer-registration-overload-owners-and-runtime-types")]
    public void ConsumerRegistrationEntryPoints_RejectEveryMissingOwnerAndInvalidRuntimeType()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionConsumerRegistrationExtensions.RegisterConsumer<RegistrationConsumer>(null!)).ParamName);
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionConsumerRegistrationExtensions.RegisterConsumer<RegistrationConsumer>(null!, registrar)).ParamName);
        Assert.Equal("registrar", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterConsumer<RegistrationConsumer>((IContainerRegistrar)null!)).ParamName);
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionConsumerRegistrationExtensions.RegisterConsumer<RegistrationConsumer, RegistrationConsumerDefinition>(null!)).ParamName);
        Assert.Equal("registrar", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterConsumer<RegistrationConsumer, RegistrationConsumerDefinition>(null!)).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterConsumer(registrar, null!)).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentException>(() =>
            services.RegisterConsumer(registrar, typeof(string))).ParamName);
        Assert.Equal("consumerDefinitionType", Assert.Throws<ArgumentException>(() =>
            services.RegisterConsumer<RegistrationConsumer>(registrar, typeof(AbstractRegistrationConsumerDefinition))).ParamName);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            RegistrationConfiguratorExtensions.AddConsumer<RegistrationConsumer>(null!)).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
            RegistrationConfiguratorExtensions.AddConsumer(configurator, null!)).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentException>(() =>
            RegistrationConfiguratorExtensions.AddConsumer(configurator, typeof(string))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-BOUNDARIES", "runtime-consumer-type-must-be-closed")]
    public void RuntimeConsumerRegistration_RejectsAnOpenGenericConsumerImplementation()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            services.RegisterConsumer(registrar, typeof(OpenRegistrationConsumer<>)));

        Assert.Equal("consumerType", exception.ParamName);
        Assert.Empty(services);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-BOUNDARIES", "runtime-consumer-registration-preserves-identity-and-definition")]
    public void RuntimeConsumerRegistration_PreservesCanonicalIdentityAndItsMatchingDefinition()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        IConsumerRegistration first = services.RegisterConsumer(registrar, typeof(RegistrationConsumer));
        IConsumerRegistration duplicate = services.RegisterConsumer(registrar, typeof(RegistrationConsumer));

        Assert.Same(first, duplicate);
        Assert.Equal(typeof(RegistrationConsumer), first.Type);
        ServiceDescriptor consumerDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(RegistrationConsumer));
        Assert.Equal(ServiceLifetime.Scoped, consumerDescriptor.Lifetime);

        var definedServices = new ServiceCollection();
        var definedRegistrar = new DependencyInjectionContainerRegistrar(definedServices);
        IConsumerRegistration defined = definedServices.RegisterConsumer(
            definedRegistrar,
            typeof(RegistrationConsumer),
            typeof(RegistrationConsumerDefinition));

        using ServiceProvider provider = definedServices.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using IServiceScope scope = provider.CreateScope();

        Assert.Equal(typeof(RegistrationConsumer), defined.Type);
        Assert.IsType<RegistrationConsumer>(scope.ServiceProvider.GetRequiredService<RegistrationConsumer>());
        Assert.IsType<RegistrationConsumerDefinition>(provider.GetRequiredService<IConsumerDefinition<RegistrationConsumer>>());
        Assert.Same(defined, Assert.Single(definedRegistrar.GetRegistrations<IConsumerRegistration>(provider)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-RIDER", "rider-registrations-preserve-owner-before-and-after-container-build")]
    public void RiderRegistrar_PreservesItsOwnerPartitionBeforeAndAfterContainerBuild()
    {
        var services = new ServiceCollection();
        var busRegistrar = new DependencyInjectionContainerRegistrar<TestBus>(services);
        var riderRegistrar = new DependencyInjectionRiderContainerRegistrar<TestBus>(services);
        var busRegistration = new RecordingRegistration(typeof(BusOwnedMessage));
        var riderRegistration = new RecordingRegistration(typeof(RiderOwnedMessage));

        busRegistrar.GetOrAddRegistration<IRegistration>(busRegistration.Type, _ => busRegistration);
        riderRegistrar.GetOrAddRegistration<IRegistration>(riderRegistration.Type, _ => riderRegistration);

        Assert.Same(riderRegistration, Assert.Single(riderRegistrar.GetRegistrations<IRegistration>()));
        Assert.Same(busRegistration, Assert.Single(busRegistrar.GetRegistrations<IRegistration>()));

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        Assert.Same(riderRegistration, Assert.Single(riderRegistrar.GetRegistrations<IRegistration>(provider)));
        Assert.Same(busRegistration, Assert.Single(busRegistrar.GetRegistrations<IRegistration>(provider)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-ENDPOINT-DEFINITION", "combined-definition-preserves-topology-qos-lifetime-and-context")]
    public void CombinedEndpointDefinition_PreservesEverySharedEndpointContract()
    {
        var observations = new List<object>();
        var capturedContext = CreateConfigurationProxy<TestRegistrationContext>(observations);
        var replacementContext = CreateConfigurationProxy<TestRegistrationContext>(observations);
        var configurator = CreateConfigurationProxy<IReceiveEndpointConfigurator>(observations);
        var first = new EndpointDefinitionProbe("first", isTemporary: true, configureConsumeTopology: true, prefetchCount: 8,
            concurrentMessageLimit: 4);
        var second = new EndpointDefinitionProbe("second", isTemporary: true, configureConsumeTopology: true, prefetchCount: 8,
            concurrentMessageLimit: 4);

        var combined = new CombinedEndpointDefinition([first, second], capturedContext, "shared");
        combined.Configure(configurator, replacementContext);

        Assert.True(combined.IsTemporary);
        Assert.True(combined.ConfigureConsumeTopology);
        Assert.Equal(8, combined.PrefetchCount);
        Assert.Equal(4, combined.ConcurrentMessageLimit);
        Assert.Equal("first", combined.GetEndpointName(DefaultEndpointNameFormatter.Instance));
        Assert.Equal(1, first.ConfigureCount);
        Assert.Equal(1, second.ConfigureCount);
        Assert.Same(replacementContext, first.LastContext);
        Assert.Same(replacementContext, second.LastContext);

        var durable = new EndpointDefinitionProbe("durable", isTemporary: false, configureConsumeTopology: false);
        var durablePeer = new EndpointDefinitionProbe("durable-peer", isTemporary: false, configureConsumeTopology: false);
        var durableCombined = new CombinedEndpointDefinition([durable, durablePeer], capturedContext, "durable-shared");

        Assert.False(durableCombined.IsTemporary);
        Assert.False(durableCombined.ConfigureConsumeTopology);
        Assert.Null(durableCombined.PrefetchCount);
        Assert.Null(durableCombined.ConcurrentMessageLimit);

        Assert.Equal("definitions", Assert.Throws<ArgumentException>(() =>
            new CombinedEndpointDefinition([], capturedContext, "empty")).ParamName);
        ConfigurationException mismatch = Assert.Throws<ConfigurationException>(() =>
            new CombinedEndpointDefinition([first, durable], capturedContext, "mismatch"));
        Assert.Contains("ConfigureConsumeTopology", mismatch.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-DI-BUS-REGISTRATION-CONTEXT", "consumer-kind-owner-precedence-must-be-unambiguous")]
    public void ConsumerKindOwnership_RejectsMultipleOwnersAtTheSamePrecedence(bool isFallback)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConsumerKind>(new ClaimingConsumerKind("Zulu", isFallback));
        services.AddSingleton<IConsumerKind>(new ClaimingConsumerKind("Alpha", isFallback));
        var selector = new DependencyInjectionContainerRegistrar(services);
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        ISetScopedConsumeContext scopedContext = CreateConfigurationProxy<ISetScopedConsumeContext>(new List<object>());
        var context = new BusRegistrationContext(provider, selector, scopedContext, typeof(IBus));
        var configurator = CreateConfigurationProxy<IReceiveConfigurator<IReceiveEndpointConfigurator>>(new List<object>());

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            context.ConfigureEndpoints(configurator, DefaultEndpointNameFormatter.Instance));

        Assert.Contains(nameof(RegistrationConsumer), exception.Message, StringComparison.Ordinal);
        if (isFallback)
            Assert.Contains("fallback", exception.Message, StringComparison.OrdinalIgnoreCase);
        else
        {
            Assert.Contains("Alpha", exception.Message, StringComparison.Ordinal);
            Assert.Contains("Zulu", exception.Message, StringComparison.Ordinal);
            Assert.True(exception.Message.IndexOf("Alpha", StringComparison.Ordinal)
                < exception.Message.IndexOf("Zulu", StringComparison.Ordinal));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-BUS-REGISTRATION-CONTEXT", "service-instance-consumer-kind-requires-one-host-owner")]
    public void ServiceInstanceConsumerKind_RequiresExactlyOneHostOwner()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConsumerKind>(new ClaimingConsumerKind("ServiceInstance", isFallback: false, requiresServiceInstance: true));
        var selector = new DependencyInjectionContainerRegistrar(services);
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        ISetScopedConsumeContext scopedContext = CreateConfigurationProxy<ISetScopedConsumeContext>(new List<object>());
        var context = new BusRegistrationContext(provider, selector, scopedContext, typeof(IBus));
        var configurator = CreateConfigurationProxy<IReceiveConfigurator<IReceiveEndpointConfigurator>>(new List<object>());

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            context.ConfigureEndpoints(configurator, DefaultEndpointNameFormatter.Instance));

        Assert.Contains("Exactly one service-instance host", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Consumer kind host", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-CONTAINER-REGISTRAR", "constructor-address-formatter-lookup-and-factory-boundaries")]
    public void ContainerRegistrar_RejectsEveryMissingRequiredInput()
    {
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            new DependencyInjectionContainerRegistrar(null!)).ParamName);

        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);
        var typedRegistrar = new DependencyInjectionContainerRegistrar<TestBus>(services);
        var riderRegistrar = new DependencyInjectionRiderContainerRegistrar<TestBus>(services);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() =>
            registrar.RegisterRequestClient<RequestMessage>(null!)).ParamName);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() =>
            typedRegistrar.RegisterRequestClient<RequestMessage>(null!)).ParamName);
        Assert.Equal("endpointNameFormatter", Assert.Throws<ArgumentNullException>(() =>
            registrar.RegisterEndpointNameFormatter(null!)).ParamName);
        Assert.Equal("endpointNameFormatter", Assert.Throws<ArgumentNullException>(() =>
            typedRegistrar.RegisterEndpointNameFormatter(null!)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            registrar.GetOrAddRegistration<RecordingRegistration>(null!, _ => new RecordingRegistration())).ParamName);
        Assert.Equal("missingRegistrationFactory", Assert.Throws<ArgumentNullException>(() =>
            registrar.GetOrAddRegistration<RecordingRegistration>(typeof(RequestMessage))).ParamName);

        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            registrar.TryGetRegistration<RecordingRegistration>(null!, typeof(RequestMessage), out _)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            registrar.TryGetRegistration<RecordingRegistration>(new ServiceCollection().BuildServiceProvider(), null!, out _)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            registrar.GetRegistrations<RecordingRegistration>(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            registrar.GetDefinition<RecordingDefinition>(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            registrar.GetEndpointDefinition<RequestMessage>(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            registrar.GetConfigureReceiveEndpoints(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            registrar.GetEndpointNameFormatter(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            riderRegistrar.GetRegistrations<RecordingRegistration>(null!)).ParamName);
        Assert.Equal("TDefinition", Assert.Throws<ArgumentException>(() =>
            registrar.AddDefinition<IDefinition, AbstractRecordingDefinition>()).ParamName);
        Assert.Equal("TDefinition", Assert.Throws<ArgumentException>(() =>
            registrar.AddEndpointDefinition<EndpointMessage, AbstractEndpointDefinition>()).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "one-instance-per-contract-and-bus-owner")]
    public void DefinitionRegistration_UsesOneOwnerSpecificInstanceForEveryServiceContract()
    {
        var defaultServices = new ServiceCollection();
        var defaultRegistrar = new DependencyInjectionContainerRegistrar(defaultServices);
        defaultRegistrar.AddDefinition<IDefinition, PublicRecordingDefinition>();

        using (ServiceProvider provider = defaultServices.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }))
        {
            PublicRecordingDefinition implementation = provider.GetRequiredService<PublicRecordingDefinition>();
            Assert.Same(implementation, provider.GetRequiredService<IDefinition>());
            Assert.Same(implementation, defaultRegistrar.GetDefinition<IDefinition>(provider));
        }

        var typedServices = new ServiceCollection();
        var typedRegistrar = new DependencyInjectionContainerRegistrar<TestBus>(typedServices);
        typedRegistrar.AddDefinition<IDefinition, PublicRecordingDefinition>();

        using ServiceProvider typedProvider = typedServices.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        PublicRecordingDefinition typedImplementation = typedProvider.GetRequiredService<Bind<TestBus, PublicRecordingDefinition>>().Value;
        Assert.Same(typedImplementation, typedProvider.GetRequiredService<Bind<TestBus, IDefinition>>().Value);
        Assert.Same(typedImplementation, typedRegistrar.GetDefinition<IDefinition>(typedProvider));
        Assert.Null(typedProvider.GetService<PublicRecordingDefinition>());
        Assert.Null(typedProvider.GetService<IDefinition>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "endpoint-definition-has-one-instance-per-contract-and-bus-owner")]
    public void EndpointDefinitionRegistration_UsesOneOwnerSpecificInstanceForEveryServiceContract()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar<TestBus>(services);
        var settings = new RecordingEndpointSettings();

        registrar.AddEndpointDefinition<EndpointMessage, SettingsBackedEndpointDefinition>(settings);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        SettingsBackedEndpointDefinition implementation =
            provider.GetRequiredService<Bind<TestBus, SettingsBackedEndpointDefinition>>().Value;
        IEndpointDefinition<EndpointMessage> service =
            provider.GetRequiredService<Bind<TestBus, IEndpointDefinition<EndpointMessage>>>().Value;

        Assert.Same(implementation, service);
        Assert.Same(implementation, registrar.GetEndpointDefinition<EndpointMessage>(provider));
        Assert.Null(provider.GetService<SettingsBackedEndpointDefinition>());
        Assert.Null(provider.GetService<IEndpointDefinition<EndpointMessage>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-ENDPOINT-REGISTRATION", "all-overload-owners-registrations-and-runtime-types")]
    public void EndpointRegistrationEntryPoints_RejectEveryInvalidRequiredInput()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);
        var registration = new RecordingRegistration();

        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionEndpointRegistrationExtensions.RegisterEndpoint<SettingsBackedEndpointDefinition, EndpointMessage>(
                null!, registration)).ParamName);
        Assert.Equal("registration", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterEndpoint<SettingsBackedEndpointDefinition, EndpointMessage>(null!)).ParamName);
        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionEndpointRegistrationExtensions.RegisterEndpoint<SettingsBackedEndpointDefinition, EndpointMessage>(
                null!, registrar, registration)).ParamName);
        Assert.Equal("registrar", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterEndpoint<SettingsBackedEndpointDefinition, EndpointMessage>(null!, registration)).ParamName);
        Assert.Equal("registration", Assert.Throws<ArgumentNullException>(() =>
            services.RegisterEndpoint<SettingsBackedEndpointDefinition, EndpointMessage>(registrar, null!)).ParamName);
        Assert.Equal("endpointDefinitionType", Assert.Throws<ArgumentException>(() =>
            services.RegisterEndpoint(registrar, typeof(IEndpointDefinition<EndpointMessage>))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-RIDER", "constructor-factory-and-typed-callback-boundaries")]
    public void RiderConfigurators_RejectEveryMissingRequiredInput()
    {
        var services = new ServiceCollection();
        var registrar = new DependencyInjectionContainerRegistrar(services);

        Assert.Equal("collection", Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollectionRiderConfigurator(null!, registrar)).ParamName);
        Assert.Equal("registrar", Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollectionRiderConfigurator(services, null!)).ParamName);

        var rider = new ServiceCollectionRiderConfigurator(services, registrar);
        var typedRider = new ServiceCollectionRiderConfigurator<TestBus>(services, registrar);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            rider.TryAddScoped<TestRider, RequestMessage>(null!)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            typedRider.TryAddScoped<TestRider, RequestMessage>(null!)).ParamName);

        var bus = new ServiceCollectionBusConfigurator<TestBus, TestBusInstance>(new ServiceCollection());
        Assert.Equal("callback", Assert.Throws<ArgumentNullException>(() =>
            bus.AddConfigureEndpointsCallback((ConfigureEndpointsCallback)null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-ENDPOINT-CALLBACK", "constructor-context-callback-and-endpoint-boundaries")]
    public void EndpointCallbacks_RejectEveryMissingRequiredInput()
    {
        var specifications = new List<object>();
        var context = CreateConfigurationProxy<TestRegistrationContext>(specifications);
        ConfigureEndpointsCallback callback = (_, _) => { };
        ConfigureEndpointsProviderCallback providerCallback = (_, _, _) => { };

        Assert.Equal("callback", Assert.Throws<ArgumentNullException>(() =>
            new ConfigureReceiveEndpointDelegate(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new ConfigureReceiveEndpointDelegateProvider(null!, providerCallback)).ParamName);
        Assert.Equal("callback", Assert.Throws<ArgumentNullException>(() =>
            new ConfigureReceiveEndpointDelegateProvider(context, null!)).ParamName);

        var endpointCallback = new ConfigureReceiveEndpointDelegate(callback);
        var endpointProviderCallback = new ConfigureReceiveEndpointDelegateProvider(context, providerCallback);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            endpointCallback.Configure("endpoint", null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            endpointProviderCallback.Configure("endpoint", null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-REGISTRATION-COMPLETION", "idempotent-completion-and-closed-participant-set")]
    public void RegistrationCompletion_IsIdempotentAndRejectsLateParticipants()
    {
        var services = new ServiceCollection();
        var configurator = new InspectableRegistrationConfigurator(
            services,
            new DependencyInjectionContainerRegistrar(services));
        var participant = new RecordingCompletionParticipant();
        configurator.GetOrAddRegistrationCompletionParticipant(() => participant);

        configurator.Complete();
        configurator.Complete();

        Assert.Equal(1, participant.CompletionCount);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configurator.GetOrAddRegistrationCompletionParticipant(() => new RecordingCompletionParticipant()));
        Assert.Contains("complete", exception.Message, StringComparison.OrdinalIgnoreCase);

        var openServices = new ServiceCollection();
        var openConfigurator = new InspectableRegistrationConfigurator(
            openServices,
            new DependencyInjectionContainerRegistrar(openServices));
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            openConfigurator.GetOrAddRegistrationCompletionParticipant<RecordingCompletionParticipant>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-REGISTRATION-CONTEXT", "constructor-and-every-required-runtime-input")]
    public void RegistrationContexts_RejectEveryMissingRequiredInputAtTheirPublicBoundary()
    {
        var services = new ServiceCollection();
        using ServiceProvider provider = services.BuildServiceProvider();
        var selector = new DependencyInjectionContainerRegistrar(services);
        ISetScopedConsumeContext scopedContext = DispatchProxy.Create<ISetScopedConsumeContext, ConfigurationProxy>();
        var endpoint = CreateConfigurationProxy<IReceiveEndpointConfigurator>(new List<object>());
        var consumeContext = CreateConfigurationProxy<ConsumeContext>(new List<object>());

        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new RegistrationContext(null!, selector, scopedContext)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            new RegistrationContext(provider, null!, scopedContext)).ParamName);
        Assert.Equal("setScopedConsumeContext", Assert.Throws<ArgumentNullException>(() =>
            new RegistrationContext(provider, selector, null!)).ParamName);

        var registration = new RegistrationContext(provider, selector, scopedContext);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureConsumer(null!, endpoint)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureConsumer(typeof(RegistrationConsumer), null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureConsumer<RegistrationConsumer>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureConsumers(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureConsumerKinds(null!)).ParamName);
        Assert.Equal("sagaType", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureSaga(null!, endpoint)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureSaga(typeof(RequestMessage), null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureSaga<RequestMessage>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureSagas(null!)).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureExecuteActivity(null!, endpoint)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureExecuteActivity(typeof(RequestMessage), null!)).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureActivity(null!, endpoint, endpoint)).ParamName);
        Assert.Equal("executeEndpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureActivity(typeof(RequestMessage), null!, endpoint)).ParamName);
        Assert.Equal("compensateEndpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureActivity(typeof(RequestMessage), endpoint, null!)).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureActivityExecute(null!, endpoint, new Uri("queue:compensate"))).ParamName);
        Assert.Equal("executeEndpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureActivityExecute(typeof(RequestMessage), null!, new Uri("queue:compensate"))).ParamName);
        Assert.Equal("compensateAddress", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureActivityExecute(typeof(RequestMessage), endpoint, null!)).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureActivityCompensate(null!, endpoint)).ParamName);
        Assert.Equal("compensateEndpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureActivityCompensate(typeof(RequestMessage), null!)).ParamName);
        Assert.Equal("futureType", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureFuture(null!, endpoint)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureFuture(typeof(RequestMessage), null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            registration.ConfigureFuture<RequestMessage>(null!)).ParamName);
        Assert.Equal("serviceType", Assert.Throws<ArgumentNullException>(() =>
            registration.GetService(null!)).ParamName);

        using IServiceScope scope = provider.CreateScope();
        Assert.Equal("scope", Assert.Throws<ArgumentNullException>(() =>
            registration.PushContext(null!, consumeContext)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            registration.PushContext(scope, null!)).ParamName);

        Assert.Equal("registration", Assert.Throws<ArgumentNullException>(() =>
            new RiderRegistrationContext(null!, selector)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            new RiderRegistrationContext(registration, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-BUS-REGISTRATION-CONTEXT", "bus-contract-and-endpoint-configurator-boundaries")]
    public void BusRegistrationContext_RejectsInvalidBusContractsAndMissingEndpointConfigurators()
    {
        var services = new ServiceCollection();
        using ServiceProvider provider = services.BuildServiceProvider();
        var selector = new DependencyInjectionContainerRegistrar(services);
        ISetScopedConsumeContext scopedContext = DispatchProxy.Create<ISetScopedConsumeContext, ConfigurationProxy>();

        Assert.Equal("busType", Assert.Throws<ArgumentNullException>(() =>
            new BusRegistrationContext(provider, selector, scopedContext, null!)).ParamName);
        Assert.Equal("busType", Assert.Throws<ArgumentException>(() =>
            new BusRegistrationContext(provider, selector, scopedContext, typeof(string))).ParamName);

        var context = new BusRegistrationContext(provider, selector, scopedContext, typeof(IBus));
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureEndpoints<IReceiveEndpointConfigurator>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureEndpoints<IReceiveEndpointConfigurator>(null!, null, null)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-BUS-REGISTRATION-CONTEXT", "documented-endpoint-formatter-is-optional-in-interface-metadata")]
    public void BusRegistrationContext_ExposesTheDocumentedOptionalEndpointNameFormatter()
    {
        MethodInfo overload = typeof(IBusRegistrationContext)
            .GetMethods()
            .Single(method => method.Name == nameof(IBusRegistrationContext.ConfigureEndpoints)
                && method.GetParameters().Length == 2);
        ParameterInfo endpointNameFormatter = overload.GetParameters()[1];

        Assert.True(endpointNameFormatter.HasDefaultValue);
        Assert.Null(endpointNameFormatter.DefaultValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SCOPED-FILTER", "observer-constructor-required-inputs")]
    public void ScopedFilterObservers_RejectEveryMissingConstructionDependency()
    {
        var specifications = new List<object>();
        var context = CreateConfigurationProxy<TestRegistrationContext>(specifications);
        var consumePipe = CreateConfigurationProxy<IConsumePipeConfigurator>(specifications);
        var filter = new CompositeFilter<Type>();
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        ISetScopedConsumeContext scopedContext = DispatchProxy.Create<ISetScopedConsumeContext, ConfigurationProxy>();

        Assert.Equal("filterType", Assert.Throws<ArgumentNullException>(() =>
            new ScopedConsumePipeSpecificationObserver(null!, context, filter)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new ScopedConsumePipeSpecificationObserver(typeof(List<>), null!, filter)).ParamName);
        Assert.Equal("messageTypeFilter", Assert.Throws<ArgumentNullException>(() =>
            new ScopedConsumePipeSpecificationObserver(typeof(List<>), context, null!)).ParamName);

        Assert.Equal("filterType", Assert.Throws<ArgumentNullException>(() =>
            new ScopedFilterSpecificationObserver(null!, provider, filter)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new ScopedFilterSpecificationObserver(typeof(List<>), null!, filter)).ParamName);
        Assert.Equal("messageTypeFilter", Assert.Throws<ArgumentNullException>(() =>
            new ScopedFilterSpecificationObserver(typeof(List<>), provider, null!)).ParamName);

        Assert.Equal("filterType", Assert.Throws<ArgumentNullException>(() =>
            new ScopedExecuteActivityPipeSpecificationObserver(null!, context, filter)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new ScopedExecuteActivityPipeSpecificationObserver(typeof(List<>), null!, filter)).ParamName);
        Assert.Equal("messageTypeFilter", Assert.Throws<ArgumentNullException>(() =>
            new ScopedExecuteActivityPipeSpecificationObserver(typeof(List<>), context, null!)).ParamName);

        Assert.Equal("filterType", Assert.Throws<ArgumentNullException>(() =>
            new ScopedCompensateActivityPipeSpecificationObserver(null!, context, filter)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new ScopedCompensateActivityPipeSpecificationObserver(typeof(List<>), null!, filter)).ParamName);
        Assert.Equal("messageTypeFilter", Assert.Throws<ArgumentNullException>(() =>
            new ScopedCompensateActivityPipeSpecificationObserver(typeof(List<>), context, null!)).ParamName);

        Assert.Equal("receiveEndpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            new MessageScopeConfigurationObserver(null!, context)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new MessageScopeConfigurationObserver(consumePipe, (IRegistrationContext)null!)).ParamName);
        Assert.Equal("serviceProvider", Assert.Throws<ArgumentNullException>(() =>
            new MessageScopeConfigurationObserver(consumePipe, null!, scopedContext)).ParamName);
        Assert.Equal("setScopedConsumeContext", Assert.Throws<ArgumentNullException>(() =>
            new MessageScopeConfigurationObserver(consumePipe, provider, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-SURFACE", "implementation-types-are-not-consumer-api")]
    public void DependencyInjectionImplementationTypes_AreHiddenFromTheConsumerApi()
    {
        Type[] implementationTypes =
        [
            typeof(BusRegistrationContext),
            typeof(CombinedEndpointDefinition),
            typeof(ConfigureReceiveEndpointDelegate),
            typeof(ConfigureReceiveEndpointDelegateProvider),
            typeof(DelegateEndpointDefinition),
            typeof(DependencyInjectionRiderContainerRegistrar<>),
            typeof(EndpointDefinitionExtensions),
            typeof(NamedEndpointDefinition),
            typeof(RegistrationContext),
            typeof(RegistrationContextEndpointDefinition),
            typeof(RiderRegistrationContext),
            typeof(ServiceCollectionRiderConfigurator),
            typeof(ServiceCollectionRiderConfigurator<>),
        ];

        Assert.All(implementationTypes, type => Assert.False(type.IsPublic, type.FullName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-FACTORY-CONTRACT", "bus-and-client-factories-must-return-instances")]
    public void BusAndClientFactories_RejectMissingResultsAtTheResolutionBoundary()
    {
        var busServices = new ServiceCollection();
        var busConfigurator = new ServiceCollectionBusConfigurator(busServices);
        busConfigurator.SetBusFactory(new NullBusFactory());
        using ServiceProvider busProvider = busServices.BuildServiceProvider();

        InvalidOperationException busException = Assert.Throws<InvalidOperationException>(() =>
            busProvider.GetRequiredService<Bind<IBus, IBusInstance>>());
        Assert.Contains("factory returned null", busException.Message, StringComparison.OrdinalIgnoreCase);

        var typedBusServices = new ServiceCollection();
        var typedBusConfigurator = new ServiceCollectionBusConfigurator<TestBus, TestBusInstance>(typedBusServices);
        typedBusConfigurator.SetBusFactory(new NullBusFactory());
        using ServiceProvider typedBusProvider = typedBusServices.BuildServiceProvider();

        InvalidOperationException typedBusException = Assert.Throws<InvalidOperationException>(() =>
            typedBusProvider.GetRequiredService<IBusInstance<TestBus>>());
        Assert.Contains("factory returned null", typedBusException.Message, StringComparison.OrdinalIgnoreCase);

        var clientServices = new ServiceCollection();
        clientServices.AddSingleton(CreateConfigurationProxy<IBus>(new List<object>()));
        var clientConfigurator = new ServiceCollectionBusConfigurator(clientServices);
        clientConfigurator.SetRequestClientFactory((_, _) => null!);
        using ServiceProvider clientProvider = clientServices.BuildServiceProvider();

        InvalidOperationException clientException = Assert.Throws<InvalidOperationException>(() =>
            clientProvider.GetRequiredService<Bind<IBus, IClientFactory>>());
        Assert.Contains("factory returned null", clientException.Message, StringComparison.OrdinalIgnoreCase);

        var typedClientServices = new ServiceCollection();
        typedClientServices.AddSingleton(CreateConfigurationProxy<TestBus>(new List<object>()));
        var typedClientConfigurator = new ServiceCollectionBusConfigurator<TestBus, TestBusInstance>(typedClientServices);
        typedClientConfigurator.SetRequestClientFactory((_, _) => null!);
        using ServiceProvider typedClientProvider = typedClientServices.BuildServiceProvider();

        InvalidOperationException typedClientException = Assert.Throws<InvalidOperationException>(() =>
            typedClientProvider.GetRequiredService<Bind<TestBus, IClientFactory>>());
        Assert.Contains("factory returned null", typedClientException.Message, StringComparison.OrdinalIgnoreCase);

        var riderServices = new ServiceCollection();
        var riderConfigurator = new ServiceCollectionRiderConfigurator(
            riderServices,
            new DependencyInjectionRiderContainerRegistrar<IBus>(riderServices));
        riderConfigurator.SetRiderFactory(new NullRiderFactory());
        using ServiceProvider riderProvider = riderServices.BuildServiceProvider();

        InvalidOperationException riderException = Assert.Throws<InvalidOperationException>(() =>
            riderProvider.GetRequiredService<Bind<IBus, IBusInstanceSpecification>>());
        Assert.Contains("factory returned null", riderException.Message, StringComparison.OrdinalIgnoreCase);

        var typedRiderServices = new ServiceCollection();
        var typedRiderConfigurator = new ServiceCollectionRiderConfigurator<TestBus>(
            typedRiderServices,
            new DependencyInjectionRiderContainerRegistrar<TestBus>(typedRiderServices));
        typedRiderConfigurator.SetRiderFactory(new NullRiderFactory());
        using ServiceProvider typedRiderProvider = typedRiderServices.BuildServiceProvider();

        InvalidOperationException typedRiderException = Assert.Throws<InvalidOperationException>(() =>
            typedRiderProvider.GetRequiredService<Bind<TestBus, IBusInstanceSpecification>>());
        Assert.Contains("factory returned null", typedRiderException.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-FACTORY-CONTRACT", "bus-specification-collection-rejects-missing-elements")]
    public void TransportBusFactory_RejectsAMissingBusSpecificationBeforeConfigurationBegins()
    {
        var observations = new List<object>();
        IHostConfiguration hostConfiguration = CreateConfigurationProxy<IHostConfiguration>(observations);
        var factory = new InspectableTransportRegistrationBusFactory(hostConfiguration);
        TestBusFactoryConfigurator configurator = CreateConfigurationProxy<TestBusFactoryConfigurator>(observations);
        TestRegistrationContext context = CreateConfigurationProxy<TestRegistrationContext>(observations);
        IBusInstanceSpecification[] specifications = [null!];

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            factory.Create(configurator, context, specifications));

        Assert.Equal("specifications", exception.ParamName);
        Assert.Empty(observations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-FACTORY-CONTRACT", "bus-specifications-validate-apply-and-preserve-construction-failure")]
    public async Task TransportBusFactory_ValidatesAndAppliesSpecificationsAndPreservesTheirFailureAsync()
    {
        var applied = new RecordingBusInstanceSpecification();
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTestHarness();
        services.AddSingleton(Bind<IBus>.Create<IBusInstanceSpecification>(applied));

        await using (ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }))
        {
            IBusControl bus = provider.GetRequiredService<IBusControl>();

            Assert.Equal(1, applied.ValidationCount);
            Assert.Equal(1, applied.ConfigureCount);
            Assert.NotNull(applied.BusInstance);
            Assert.Same(bus, applied.BusInstance.BusControl);
        }

        var expectedFailure = new InvalidOperationException("specification failed");
        var failing = new RecordingBusInstanceSpecification(expectedFailure);
        var failingServices = new ServiceCollection();
        failingServices.AddViciOneServiceBusTestHarness();
        failingServices.AddSingleton(Bind<IBus>.Create<IBusInstanceSpecification>(failing));
        await using ServiceProvider failingProvider = failingServices.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            failingProvider.GetRequiredService<IBusControl>());

        Assert.Equal(1, failing.ValidationCount);
        Assert.Equal(1, failing.ConfigureCount);
        Assert.Same(expectedFailure, exception.InnerException);
        Assert.Contains("bus creation", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static T CreateConfigurationProxy<T>(List<object> specifications)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, ConfigurationProxy>();
        ((ConfigurationProxy)(object)proxy).Specifications = specifications;
        return proxy;
    }

    private sealed class InspectableRegistrationConfigurator(
        IServiceCollection services,
        IContainerRegistrar registrar) : RegistrationConfigurator(services, registrar);

    private sealed class InspectableTransportRegistrationBusFactory(IHostConfiguration hostConfiguration)
        : TransportRegistrationBusFactory<IReceiveEndpointConfigurator>(hostConfiguration)
    {
        public IBusInstance Create(
            TestBusFactoryConfigurator configurator,
            IBusRegistrationContext context,
            IEnumerable<IBusInstanceSpecification> specifications) =>
            CreateBus<TestBusFactoryConfigurator, IBusFactoryConfigurator>(configurator, context, null, specifications);

        public override IBusInstance CreateBus(
            IBusRegistrationContext context,
            IEnumerable<IBusInstanceSpecification> specifications,
            string busName) => throw new NotSupportedException();
    }

    private sealed class RecordingContainerRegistrar(IServiceCollection services)
        : DependencyInjectionContainerRegistrar(services)
    {
        public Type? RequestType { get; private set; }

        public Uri? DestinationAddress { get; private set; }

        public RequestTimeout Timeout { get; private set; }

        public override void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        {
            RequestType = typeof(T);
            DestinationAddress = destinationAddress;
            Timeout = timeout;
        }
    }

    private sealed class RecordingCompletionParticipant : IRegistrationCompletionParticipant
    {
        public int Order => 0;

        public int CompletionCount { get; private set; }

        public IRegistrationConfigurator? Configurator { get; private set; }

        public void Complete(IRegistrationConfigurator configurator)
        {
            CompletionCount++;
            Configurator = configurator;
        }
    }

    private sealed class RecordingEndpointSettings : IEndpointSettings<IEndpointDefinition<EndpointMessage>>
    {
        public string? Name => "settings-backed";

        public bool IsTemporary => false;

        public int? PrefetchCount => 7;

        public int? ConcurrentMessageLimit => 3;

        public bool ConfigureConsumeTopology => true;

        public string? InstanceId => null;

        public void ConfigureEndpoint<TEndpointConfigurator>(
            TEndpointConfigurator configurator,
            IRegistrationContext? context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
        }
    }

    private sealed class SettingsBackedEndpointDefinition(
        IEndpointSettings<IEndpointDefinition<EndpointMessage>> settings) : IEndpointDefinition<EndpointMessage>
    {
        public IEndpointSettings<IEndpointDefinition<EndpointMessage>> Settings { get; } = settings;

        public bool IsTemporary => Settings.IsTemporary;

        public int? PrefetchCount => Settings.PrefetchCount;

        public int? ConcurrentMessageLimit => Settings.ConcurrentMessageLimit;

        public bool ConfigureConsumeTopology => Settings.ConfigureConsumeTopology;

        public string GetEndpointName(IEndpointNameFormatter formatter) => Settings.Name!;

        public void Configure<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
        }
    }

    private class ConfigurationProxy : DispatchProxy
    {
        public List<object>? Specifications { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "Log")
            {
                var configure = Assert.IsAssignableFrom<Delegate>(args![0]);
                Type nestedType = configure.GetType().GetGenericArguments()[0];
                object nested = DispatchProxy.Create(nestedType, typeof(ConfigurationProxy));
                ((ConfigurationProxy)nested).Specifications = Specifications;
                configure.DynamicInvoke(nested);
                return null;
            }

            if (targetMethod.Name.StartsWith("AddPipeSpecification", StringComparison.Ordinal))
            {
                Specifications!.Add(args![0]!);
                return null;
            }

            throw new NotSupportedException($"Unexpected configuration member: {targetMethod.Name}");
        }
    }

    private sealed class TypedCompensateFilter : IFilter<CompensateContext<CompensationLog>>
    {
        public Task SendAsync(
            CompensateContext<CompensationLog> context,
            IPipe<CompensateContext<CompensationLog>> next) => next.SendAsync(context);

        public void Probe(ProbeContext context) => context.CreateFilterScope("typedCompensate");
    }

    private sealed class CompensatingActivity;

    private sealed record CompensationLog;

    private sealed class RecordingDefinition : IDefinition
    {
        public int? ConcurrentMessageLimit => null;
    }

    public sealed class PublicRecordingDefinition : IDefinition
    {
        public int? ConcurrentMessageLimit => null;
    }

    private abstract class AbstractRecordingDefinition : IDefinition
    {
        public int? ConcurrentMessageLimit => null;
    }

    private abstract class AbstractEndpointDefinition : IEndpointDefinition<EndpointMessage>
    {
        public bool IsTemporary => false;

        public int? PrefetchCount => null;

        public int? ConcurrentMessageLimit => null;

        public bool ConfigureConsumeTopology => true;

        public string GetEndpointName(IEndpointNameFormatter formatter) => "abstract";

        public void Configure<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
        }
    }

    private interface TestRegistrationContext : IBusRegistrationContext, ISetScopedConsumeContext;

    private interface TestBusFactoryConfigurator : IBusFactory, IBusFactoryConfigurator;

    private sealed class RecordingRegistration(Type? type = null) : IRegistration
    {
        public Type Type { get; } = type ?? typeof(RequestMessage);

        public bool IncludeInConfigureEndpoints { get; set; }
    }

    private sealed class EndpointDefinitionProbe(
        string name,
        bool isTemporary,
        bool configureConsumeTopology,
        int? prefetchCount = null,
        int? concurrentMessageLimit = null) : IEndpointDefinition
    {
        public bool IsTemporary { get; } = isTemporary;

        public int? PrefetchCount { get; } = prefetchCount;

        public int? ConcurrentMessageLimit { get; } = concurrentMessageLimit;

        public bool ConfigureConsumeTopology { get; } = configureConsumeTopology;

        public int ConfigureCount { get; private set; }

        public IRegistrationContext? LastContext { get; private set; }

        public string GetEndpointName(IEndpointNameFormatter formatter) => name;

        public void Configure<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
            ConfigureCount++;
            LastContext = context;
        }
    }

    private sealed class ClaimingConsumerKind(string name, bool isFallback, bool requiresServiceInstance = false) : IConsumerKind
    {
        public string Name { get; } = name;

        public bool IsFallback { get; } = isFallback;

        public int Order => 0;

        public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context) =>
            [new ClaimingConsumerKindRegistration(requiresServiceInstance)];

        public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
        {
        }
    }

    private sealed class ClaimingConsumerKindRegistration(bool requiresServiceInstance) : IConsumerKindRegistration
    {
        public Type RegistrationType => typeof(RegistrationConsumer);

        public IDefinition Definition { get; } = new RecordingDefinition();

        public string EndpointName => "claimed";

        public IEndpointDefinition EndpointDefinition { get; } = new NamedEndpointDefinition("claimed");

        public bool RequiresServiceInstance { get; } = requiresServiceInstance;

        public IReadOnlyCollection<string> CompanionEndpointNames => [];

        public void Configure(IConsumerKindEndpointContext context)
        {
        }
    }

    private sealed class RecordingBusInstanceSpecification(Exception? configureFailure = null) : IBusInstanceSpecification
    {
        public int ValidationCount { get; private set; }

        public int ConfigureCount { get; private set; }

        public IBusInstance? BusInstance { get; private set; }

        public IEnumerable<ValidationResult> Validate()
        {
            ValidationCount++;
            return [];
        }

        public void Configure(IBusInstance busInstance)
        {
            ConfigureCount++;
            BusInstance = busInstance;
            if (configureFailure != null)
                throw configureFailure;
        }
    }

    private sealed class TestRider : IRider;

    private sealed class RegistrationConsumer : IConsumer<RequestMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RequestMessage> context) => Task.CompletedTask;
    }

    private sealed class OpenRegistrationConsumer<T> : IConsumer<T>
        where T : class
    {
        public Task ConsumeAsync(ConsumeContext<T> context) => Task.CompletedTask;
    }

    private sealed class RegistrationConsumerDefinition : ConsumerDefinition<RegistrationConsumer>;

    private abstract class AbstractRegistrationConsumerDefinition : ConsumerDefinition<RegistrationConsumer>;

    private interface TestBus : IBus;

    private sealed class TestBusInstance(IBusControl busControl) : BusInstance<TestBus>(busControl), TestBus;

    private sealed class NullBusFactory : IRegistrationBusFactory
    {
        public IBusInstance CreateBus(
            IBusRegistrationContext context,
            IEnumerable<IBusInstanceSpecification> specifications,
            string busName) => null!;
    }

    private sealed class NullRiderFactory : IRegistrationRiderFactory<TestRider>
    {
        public IBusInstanceSpecification CreateRider(IRiderRegistrationContext context) => null!;
    }

    private sealed record RequestMessage;

    private sealed record EndpointMessage;

    private sealed record BusOwnedMessage;

    private sealed record RiderOwnedMessage;
}
