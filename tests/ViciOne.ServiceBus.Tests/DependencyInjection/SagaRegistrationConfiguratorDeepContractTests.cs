using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class SagaRegistrationConfiguratorDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "configurator-constructor-required-owner-and-collection")]
    public void Constructors_RejectMissingOwnersAtTheirOwnBoundary()
    {
        ArgumentNullException owner = Assert.Throws<ArgumentNullException>(() =>
            new SagaRegistrationConfigurator<ContractSaga>(null!));
        Assert.Equal("configurator", owner.ParamName);

        ArgumentNullException collection = Assert.Throws<ArgumentNullException>(() =>
            new SagaRepositoryRegistrationConfigurator<ContractSaga>(null!));
        Assert.Equal("collection", collection.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "configurator-callback-null-guards-precede-owner-effects")]
    public void CallbackGuards_FailBeforeRegistrationStateOrServicesAreObserved()
    {
        var owner = new RecordingRegistrationConfigurator();
        var registration = new StubSagaRegistration(typeof(ContractSaga))
        {
            IncludeInConfigureEndpoints = false,
        };
        var configurator = new SagaRegistrationConfigurator<ContractSaga>(owner, registration);

        ArgumentNullException endpoint = Assert.Throws<ArgumentNullException>(() => configurator.Endpoint(null!));
        Assert.Equal("configure", endpoint.ParamName);
        Assert.Equal(0, registration.IncludeInConfigureEndpointsGetterReadCount);
        Assert.Empty(owner.Events);

        ArgumentNullException repository = Assert.Throws<ArgumentNullException>(() => configurator.Repository(null!));
        Assert.Equal("configure", repository.ParamName);
        Assert.Equal(0, registration.IncludeInConfigureEndpointsGetterReadCount);
        Assert.Equal(0, owner.ServicesAccessCount);
        Assert.Empty(owner.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "configurator-endpoint-settings-side-effect-order-and-return-identity")]
    public void Endpoint_AppliesSettingsBeforeOneRegistrationAndReturnsTheSameTypedAndUntypedOwner()
    {
        var owner = new RecordingRegistrationConfigurator();
        var registration = new StubSagaRegistration(typeof(ContractSaga));
        var configurator = new SagaRegistrationConfigurator<ContractSaga>(owner, registration);

        ISagaRegistrationConfigurator<ContractSaga> typedResult = configurator.Endpoint(endpoint =>
        {
            owner.Events.Add("callback");
            endpoint.Name = "contract-saga";
            endpoint.Temporary = true;
            endpoint.PrefetchCount = 19;
            endpoint.ConcurrentMessageLimit = 7;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.InstanceId = "secondary";
        });

        Assert.Same(configurator, typedResult);
        Assert.Equal(["callback", "add-endpoint"], owner.Events);
        Assert.Same(registration, owner.EndpointRegistration);
        Assert.Equal(typeof(SagaEndpointDefinition<ContractSaga>), owner.EndpointDefinitionType);
        Assert.Equal(typeof(ContractSaga), owner.EndpointOwnerType);
        var settings = Assert.IsAssignableFrom<IEndpointSettings<IEndpointDefinition<ContractSaga>>>(owner.EndpointSettings);
        Assert.Equal("contract-saga", settings.Name);
        Assert.True(settings.IsTemporary);
        Assert.Equal(19, settings.PrefetchCount);
        Assert.Equal(7, settings.ConcurrentMessageLimit);
        Assert.False(settings.ConfigureConsumeTopology);
        Assert.Equal("secondary", settings.InstanceId);

        var untypedOwner = new RecordingRegistrationConfigurator();
        var untypedConfigurator = new SagaRegistrationConfigurator<ContractSaga>(
            untypedOwner,
            new StubSagaRegistration(typeof(ContractSaga)));
        ISagaRegistrationConfigurator untypedResult =
            ((ISagaRegistrationConfigurator)untypedConfigurator).Endpoint(_ => untypedOwner.Events.Add("callback"));

        Assert.Same(untypedConfigurator, untypedResult);
        Assert.Equal(["callback", "add-endpoint"], untypedOwner.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION", "configurator-exclusion-and-repository-only-fail-before-callback")]
    public void ExcludedAndRepositoryOnlyEndpoints_FailWithoutInvokingTheCallbackOrRegisteringAnEndpoint()
    {
        var excludedOwner = new RecordingRegistrationConfigurator();
        var registration = new StubSagaRegistration(typeof(ContractSaga));
        var excluded = new SagaRegistrationConfigurator<ContractSaga>(excludedOwner, registration);
        var excludedCallbackCalls = 0;

        ((ISagaRegistrationConfigurator)excluded).ExcludeFromConfigureEndpoints();
        ((ISagaRegistrationConfigurator)excluded).ExcludeFromConfigureEndpoints();
        ConfigurationException excludedException = Assert.Throws<ConfigurationException>(() =>
            excluded.Endpoint(_ => excludedCallbackCalls++));

        Assert.False(registration.IncludeInConfigureEndpoints);
        Assert.Contains("excluded", excludedException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, excludedCallbackCalls);
        Assert.Empty(excludedOwner.Events);

        var repositoryOwner = new RecordingRegistrationConfigurator();
        var repositoryOnly = new SagaRegistrationConfigurator<ContractSaga>(repositoryOwner);
        var repositoryCallbackCalls = 0;

        ((ISagaRegistrationConfigurator)repositoryOnly).ExcludeFromConfigureEndpoints();
        ConfigurationException repositoryException = Assert.Throws<ConfigurationException>(() =>
            repositoryOnly.Endpoint(_ => repositoryCallbackCalls++));

        Assert.Contains("repository-only", repositoryException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, repositoryCallbackCalls);
        Assert.Empty(repositoryOwner.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONFIGURATION", "configurator-live-service-collection-and-return-identity")]
    public void Repository_ExposesTheLiveServiceCollectionAndReturnsTheSameOwner()
    {
        IServiceCollection services = new ServiceCollection();
        var owner = new RecordingRegistrationConfigurator(services);
        var configurator = new SagaRegistrationConfigurator<ContractSaga>(owner);
        ServiceDescriptor first = ServiceDescriptor.Singleton(typeof(FirstService), new FirstService());
        ServiceDescriptor second = ServiceDescriptor.Singleton(typeof(SecondService), new SecondService());
        ISagaRepositoryRegistrationConfigurator<ContractSaga>? callbackCollection = null;

        ISagaRegistrationConfigurator<ContractSaga> result = configurator.Repository(collection =>
        {
            callbackCollection = collection;
            collection.Add(first);
            Assert.Same(first, Assert.Single(services));

            services.Add(second);
            Assert.Equal(2, collection.Count);
            Assert.Same(second, collection[1]);
        });

        Assert.Same(configurator, result);
        ISagaRepositoryRegistrationConfigurator<ContractSaga> captured =
            Assert.IsAssignableFrom<ISagaRepositoryRegistrationConfigurator<ContractSaga>>(callbackCollection);
        Assert.Equal([first, second], captured.ToArray());
        Assert.Equal(1, owner.ServicesAccessCount);
        Assert.Empty(owner.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONFIGURATION", "complete-ilist-facade-native-exceptions-and-enumerators")]
    public void RepositoryCollection_ForwardsTheCompleteMutableListContractAndNativeEnumeratorBehavior()
    {
        ServiceDescriptor first = ServiceDescriptor.Singleton(typeof(FirstService), new FirstService());
        ServiceDescriptor second = ServiceDescriptor.Singleton(typeof(SecondService), new SecondService());
        ServiceDescriptor inserted = ServiceDescriptor.Singleton(typeof(InsertedService), new InsertedService());
        ServiceDescriptor replacement = ServiceDescriptor.Singleton(typeof(ReplacementService), new ReplacementService());
        ServiceDescriptor appended = ServiceDescriptor.Singleton(typeof(AppendedService), new AppendedService());
        IServiceCollection services = new ServiceCollection();
        services.Add(first);
        var collection = new SagaRepositoryRegistrationConfigurator<ContractSaga>(services);

        services.Add(second);
        Assert.Equal(2, collection.Count);
        Assert.False(collection.IsReadOnly);
        Assert.Equal((true, false), (collection.Contains(first), collection.Contains(inserted)));
        Assert.Contains(first, collection);
        Assert.Equal(1, collection.IndexOf(second));

        collection.Insert(1, inserted);
        Assert.Same(inserted, services[1]);
        Assert.Same(inserted, collection[1]);

        collection[1] = replacement;
        Assert.Same(replacement, services[1]);
        collection.Add(appended);
        Assert.Same(appended, services[^1]);

        var copied = new ServiceDescriptor[collection.Count + 2];
        collection.CopyTo(copied, 1);
        Assert.Equal(collection.ToArray(), copied.Skip(1).Take(collection.Count).ToArray());

        Assert.True(collection.Remove(second));
        Assert.DoesNotContain(second, services);
        collection.RemoveAt(1);
        Assert.DoesNotContain(replacement, services);

        using IEnumerator<ServiceDescriptor> generic = collection.GetEnumerator();
        Assert.True(generic.MoveNext());
        Assert.Same(first, generic.Current);
        collection.Add(second);
        Assert.Throws<InvalidOperationException>(() => generic.MoveNext());

        collection.Clear();
        collection.Add(first);
        IEnumerator nonGeneric = ((IEnumerable)collection).GetEnumerator();
        Assert.True(nonGeneric.MoveNext());
        Assert.Same(first, nonGeneric.Current);
        collection.Add(second);
        Assert.Throws<InvalidOperationException>(() => nonGeneric.MoveNext());

        collection.Clear();
        Assert.Empty(services);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = collection[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1] = first);
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(-1, first));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(0));
        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
    }

    private sealed class RecordingRegistrationConfigurator :
        IRegistrationConfigurator,
        IAdvancedRegistrationConfigurator
    {
        private readonly IServiceCollection _services;

        public RecordingRegistrationConfigurator(IServiceCollection? services = null)
        {
            _services = services ?? new ServiceCollection();
        }

        public List<string> Events { get; } = [];

        public int ServicesAccessCount { get; private set; }

        public IRegistration? EndpointRegistration { get; private set; }

        public Type? EndpointDefinitionType { get; private set; }

        public Type? EndpointOwnerType { get; private set; }

        public object? EndpointSettings { get; private set; }

        public IServiceCollection Services
        {
            get
            {
                ServicesAccessCount++;
                return _services;
            }
        }

        public Type BusType => typeof(IBus);

        public IContainerRegistrar Registrar => throw new NotSupportedException();

        public IConsumerRegistrationConfigurator<T> AddConsumer<T>(
            Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
            where T : class, IConsumer => throw new NotSupportedException();

        public IConsumerRegistrationConfigurator<T> AddConsumer<T>(
            Type? consumerDefinitionType,
            Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
            where T : class, IConsumer => throw new NotSupportedException();

        public void AddEndpoint(Type endpointDefinitionType) => throw new NotSupportedException();

        public void AddRequestClient<T>(RequestTimeout timeout = default)
            where T : class => throw new NotSupportedException();

        public void AddRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
            where T : class => throw new NotSupportedException();

        public void AddRequestClient(Type requestType, RequestTimeout timeout = default) => throw new NotSupportedException();

        public void AddRequestClient(Type requestType, Uri destinationAddress, RequestTimeout timeout = default) =>
            throw new NotSupportedException();

        public void SetDefaultRequestTimeout(RequestTimeout timeout) => throw new NotSupportedException();

        public void SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter) => throw new NotSupportedException();

        public void AddEndpoint<TDefinition, T>(
            IRegistration registration,
            IEndpointSettings<IEndpointDefinition<T>>? settings = null)
            where TDefinition : class, IEndpointDefinition<T>
            where T : class
        {
            Events.Add("add-endpoint");
            EndpointRegistration = registration;
            EndpointDefinitionType = typeof(TDefinition);
            EndpointOwnerType = typeof(T);
            EndpointSettings = settings;
        }

        public TParticipant GetOrAddRegistrationCompletionParticipant<TParticipant>(Func<TParticipant> factory)
            where TParticipant : class, IRegistrationCompletionParticipant => throw new NotSupportedException();
    }

    private sealed class StubSagaRegistration(Type type) : ISagaRegistration
    {
        private bool _includeInConfigureEndpoints = true;

        public Type Type { get; } = type;

        public int IncludeInConfigureEndpointsGetterReadCount { get; private set; }

        public bool IncludeInConfigureEndpoints
        {
            get
            {
                IncludeInConfigureEndpointsGetterReadCount++;
                return _includeInConfigureEndpoints;
            }
            set => _includeInConfigureEndpoints = value;
        }

        public Type? StateMachineType => null;

        public void AddConfigureAction<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure)
            where T : class
        {
        }

        public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context) =>
            throw new NotSupportedException();

        public ISagaDefinition GetDefinition(IRegistrationContext context) => throw new NotSupportedException();
    }

    private sealed class ContractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class FirstService;
    private sealed class SecondService;
    private sealed class InsertedService;
    private sealed class ReplacementService;
    private sealed class AppendedService;
}
