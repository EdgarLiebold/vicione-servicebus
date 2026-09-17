using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaRepositoryRegistrationProviderDeepContractTests
{
    private const string InMemoryProviderName =
        "ViciOne.ServiceBus.Configuration.InMemorySagaRepositoryRegistrationProvider";
    private const string MissingProviderName =
        "ViciOne.ServiceBus.Configuration.MissingSagaRepositoryRegistrationProvider";

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PROVIDER", "exact-public-extension-and-internal-provider-surface")]
    public void PublicAndInternalSurface_ExposeExactlyTwoExtensionsAndTwoSealedProviders()
    {
        Type extensions = typeof(InMemorySagaRepositoryRegistrationExtensions);
        MethodInfo[] methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.True(extensions.IsPublic && extensions.IsAbstract && extensions.IsSealed);
        Assert.Equal(2, methods.Length);
        Assert.All(methods, method => Assert.True(method.IsDefined(typeof(ExtensionAttribute), inherit: false)));

        MethodInfo repository = Assert.Single(methods, method => method.Name ==
            nameof(InMemorySagaRepositoryRegistrationExtensions.InMemoryRepository));
        Type saga = Assert.Single(repository.GetGenericArguments());
        Assert.Equal("T", saga.Name);
        AssertReferenceConstraint(saga, typeof(ISaga));
        Assert.Equal(["configurator"], repository.GetParameters().Select(parameter => parameter.Name));
        Assert.Equal(
            [typeof(ISagaRegistrationConfigurator<>).MakeGenericType(saga)],
            repository.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(typeof(ISagaRegistrationConfigurator<>).MakeGenericType(saga), repository.ReturnType);

        MethodInfo setProvider = Assert.Single(methods, method => method.Name ==
            nameof(InMemorySagaRepositoryRegistrationExtensions.SetInMemorySagaRepositoryProvider));
        Assert.False(setProvider.IsGenericMethod);
        Assert.Equal(typeof(void), setProvider.ReturnType);
        Assert.Equal(["configurator"], setProvider.GetParameters().Select(parameter => parameter.Name));
        Assert.Equal([typeof(IRegistrationConfigurator)], setProvider.GetParameters().Select(parameter => parameter.ParameterType));

        AssertProviderSurface(GetProviderType(InMemoryProviderName));
        AssertProviderSurface(GetProviderType(MissingProviderName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PROVIDER", "all-receiver-guards-precede-provider-or-callback-effects")]
    public void AllEntryPoints_RejectMissingConfiguratorBeforeAnyEffect()
    {
        AssertNullConfigurator(() =>
            InMemorySagaRepositoryRegistrationExtensions.InMemoryRepository<ContractSaga>(null!));
        AssertNullConfigurator(() =>
            InMemorySagaRepositoryRegistrationExtensions.SetInMemorySagaRepositoryProvider(null!));

        ISagaRepositoryRegistrationProvider inMemory = CreateProvider(InMemoryProviderName);
        ISagaRepositoryRegistrationProvider missing = CreateProvider(MissingProviderName);

        AssertNullConfigurator(() => inMemory.Configure<ContractSaga>(null!));
        AssertNullConfigurator(() => missing.Configure<ContractSaga>(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PROVIDER", "in-memory-extension-callback-order-effect-and-input-result-identity")]
    public void InMemoryRepository_InvokesOneCallbackInOrderAndReturnsTheOriginalConfigurator()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new RegistrationSentinel());
        var delegatedResult = new RecordingSagaRegistrationConfigurator<ContractSaga>(new ServiceCollection());
        var configurator = new RecordingSagaRegistrationConfigurator<ContractSaga>(services, delegatedResult);

        ISagaRegistrationConfigurator<ContractSaga> result = configurator.InMemoryRepository();

        Assert.Same(configurator, result);
        Assert.NotSame(delegatedResult, result);
        Assert.Equal(1, configurator.RepositoryCallCount);
        Assert.NotNull(configurator.Callback);
        Assert.Equal(["repository-enter", "repository-callback-complete", "repository-return"], configurator.Events);
        Assert.Equal(1, configurator.ServiceCountBeforeCallback);
        Assert.True(configurator.ServiceCountAfterCallback > configurator.ServiceCountBeforeCallback);
        AssertInMemoryRepositoryDescriptors<ContractSaga>(services);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PROVIDER", "in-memory-provider-forwards-exact-configurator-through-extension-path")]
    public void InMemoryProvider_ForwardsTheExactConfiguratorThroughTheRepositoryPath()
    {
        ISagaRepositoryRegistrationProvider provider = CreateProvider(InMemoryProviderName);
        var services = new ServiceCollection();
        var configurator = new RecordingSagaRegistrationConfigurator<ContractSaga>(services);

        provider.Configure(configurator);

        Assert.Equal(1, configurator.RepositoryCallCount);
        Assert.Equal(["repository-enter", "repository-callback-complete", "repository-return"], configurator.Events);
        AssertInMemoryRepositoryDescriptors<ContractSaga>(services);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PROVIDER", "provider-installation-replaces-identity-without-service-effects")]
    public void SetInMemorySagaRepositoryProvider_InstallsFreshInternalProviderWithoutServiceMutation()
    {
        var services = new ServiceCollection();
        object? firstParticipant = null;
        object? secondParticipant = null;
        object? firstProvider = null;
        object? secondProvider = null;
        var before = -1;
        var afterFirst = -1;
        var afterSecond = -1;

        services.AddViciOneServiceBus(configurator =>
        {
            before = services.Count;
            configurator.SetInMemorySagaRepositoryProvider();
            afterFirst = services.Count;
            object installedParticipant = FindSagaParticipant(configurator);
            firstParticipant = installedParticipant;
            firstProvider = ReadProperty(installedParticipant, "Provider");

            configurator.SetInMemorySagaRepositoryProvider();
            afterSecond = services.Count;
            object replacedParticipant = FindSagaParticipant(configurator);
            secondParticipant = replacedParticipant;
            secondProvider = ReadProperty(replacedParticipant, "Provider");
        });

        Assert.Equal(before, afterFirst);
        Assert.Equal(before, afterSecond);
        Assert.Same(firstParticipant, secondParticipant);
        Assert.NotSame(firstProvider, secondProvider);
        Assert.Equal(GetProviderType(InMemoryProviderName), firstProvider?.GetType());
        Assert.Equal(GetProviderType(InMemoryProviderName), secondProvider?.GetType());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PROVIDER", "completion-fills-only-missing-repositories-once")]
    public void InMemoryProviderCompletion_ConfiguresOnlyMissingRepositoriesWithoutDuplicatingExplicitOnes()
    {
        var services = new ServiceCollection();

        services.AddViciOneServiceBus(configurator =>
        {
            configurator.SetInMemorySagaRepositoryProvider();
            configurator.AddSaga<ExplicitRepositorySaga>().InMemoryRepository();
            configurator.AddSaga<ConventionRepositorySaga>();
        });

        AssertInMemoryRepositoryDescriptors<ExplicitRepositorySaga>(services);
        AssertInMemoryRepositoryDescriptors<ConventionRepositorySaga>(services);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PROVIDER", "missing-provider-stable-diagnostic-before-configurator-effects")]
    public void MissingProvider_ReportsStableActionableDiagnosticBeforeRepositoryEffects()
    {
        ISagaRepositoryRegistrationProvider provider = CreateProvider(MissingProviderName);
        var services = new ServiceCollection();
        services.AddSingleton(new RegistrationSentinel());
        ServiceDescriptor[] baseline = services.ToArray();
        var configurator = new RecordingSagaRegistrationConfigurator<ContractSaga>(services);

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => provider.Configure(configurator));

        Assert.Equal(
            $"Saga repository for bus '{TypeCache<ContractSaga>.ShortName}': No repository was configured for the saga. "
            + "Configure a repository on the saga registration or select a default saga repository provider.",
            exception.Message);
        Assert.Equal(0, configurator.RepositoryCallCount);
        Assert.Null(configurator.Callback);
        Assert.Empty(configurator.Events);
        AssertServicesUnchanged(services, baseline);
    }

    private static void AssertProviderSurface(Type providerType)
    {
        Assert.True(providerType.IsNotPublic);
        Assert.False(providerType.IsVisible);
        Assert.True(providerType.IsSealed);
        Assert.False(providerType.IsAbstract);
        Assert.Equal([typeof(ISagaRepositoryRegistrationProvider)], providerType.GetInterfaces());

        MethodInfo configure = Assert.Single(
            providerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(ISagaRepositoryRegistrationProvider.Configure), configure.Name);
        Assert.Equal(typeof(void), configure.ReturnType);
        Type saga = Assert.Single(configure.GetGenericArguments());
        Assert.Equal("TSaga", saga.Name);
        AssertReferenceConstraint(saga, typeof(ISaga));
        Assert.Equal(["configurator"], configure.GetParameters().Select(parameter => parameter.Name));
        Assert.Equal(
            [typeof(ISagaRegistrationConfigurator<>).MakeGenericType(saga)],
            configure.GetParameters().Select(parameter => parameter.ParameterType));
    }

    private static void AssertReferenceConstraint(Type parameter, Type contract)
    {
        Assert.True((parameter.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0);
        Assert.Equal([contract], parameter.GetGenericParameterConstraints());
    }

    private static void AssertInMemoryRepositoryDescriptors<TSaga>(IServiceCollection services)
        where TSaga : class, ISaga
    {
        ServiceDescriptor repository = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(ISagaRepositoryContextFactory<TSaga>));
        ServiceDescriptor query = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IQuerySagaRepositoryContextFactory<TSaga>));
        ServiceDescriptor load = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(ILoadSagaRepositoryContextFactory<TSaga>));

        Assert.Equal(ServiceLifetime.Scoped, repository.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, query.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, load.Lifetime);
        Type implementationType = repository.ImplementationType
            ?? throw new InvalidOperationException("The in-memory repository context factory implementation type was missing.");
        Assert.Equal(implementationType, query.ImplementationType);
        Assert.Equal(implementationType, load.ImplementationType);
        Assert.StartsWith("InMemorySagaRepositoryContextFactory", implementationType.Name, StringComparison.Ordinal);
    }

    private static ISagaRepositoryRegistrationProvider CreateProvider(string fullName)
    {
        Type providerType = GetProviderType(fullName);
        object instance = Activator.CreateInstance(providerType, nonPublic: true)
            ?? throw new InvalidOperationException($"Provider type '{fullName}' could not be activated.");
        return Assert.IsAssignableFrom<ISagaRepositoryRegistrationProvider>(instance);
    }

    private static Type GetProviderType(string fullName) =>
        typeof(InMemorySagaRepositoryRegistrationExtensions).Assembly.GetType(fullName, throwOnError: true)
        ?? throw new InvalidOperationException($"Provider type '{fullName}' was not found.");

    private static object FindSagaParticipant(IRegistrationConfigurator configurator) =>
        Assert.Single(
            CompletionParticipants(configurator),
            participant => participant.GetType().Name == "SagaRegistrationCompletionParticipant");

    private static IEnumerable<object> CompletionParticipants(IRegistrationConfigurator configurator)
    {
        var participants = Assert.IsAssignableFrom<IDictionary>(ReadField(configurator, "_registrationCompletionParticipants"));
        return participants.Values.Cast<object>();
    }

    private static object ReadProperty(object target, string name) =>
        target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target)
        ?? throw new InvalidOperationException($"Property '{name}' was not found or was null.");

    private static object ReadField(object target, string name)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(target) ?? throw new InvalidOperationException($"Field '{name}' was null.");
        }

        throw new InvalidOperationException($"Field '{name}' was not found.");
    }

    private static void AssertNullConfigurator(Action action) =>
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(action).ParamName);

    private static void AssertServicesUnchanged(IServiceCollection services, ServiceDescriptor[] baseline)
    {
        Assert.Equal(baseline.Length, services.Count);
        for (var index = 0; index < baseline.Length; index++)
            Assert.Same(baseline[index], services[index]);
    }

    private sealed class RecordingSagaRegistrationConfigurator<TSaga> : ISagaRegistrationConfigurator<TSaga>
        where TSaga : class, ISaga
    {
        private readonly IServiceCollection _services;
        private readonly ISagaRegistrationConfigurator<TSaga>? _repositoryResult;

        public RecordingSagaRegistrationConfigurator(
            IServiceCollection services,
            ISagaRegistrationConfigurator<TSaga>? repositoryResult = null)
        {
            _services = services;
            _repositoryResult = repositoryResult;
        }

        public List<string> Events { get; } = [];
        public Action<ISagaRepositoryRegistrationConfigurator<TSaga>>? Callback { get; private set; }
        public int RepositoryCallCount { get; private set; }
        public int ServiceCountBeforeCallback { get; private set; }
        public int ServiceCountAfterCallback { get; private set; }

        public ISagaRegistrationConfigurator<TSaga> Endpoint(Action<IEndpointRegistrationConfigurator> configure) =>
            throw new NotSupportedException();

        public ISagaRegistrationConfigurator<TSaga> Repository(
            Action<ISagaRepositoryRegistrationConfigurator<TSaga>> configure)
        {
            Events.Add("repository-enter");
            RepositoryCallCount++;
            Callback = configure;
            ServiceCountBeforeCallback = _services.Count;
            configure(new SagaRepositoryRegistrationConfigurator<TSaga>(_services));
            ServiceCountAfterCallback = _services.Count;
            Events.Add("repository-callback-complete");
            Events.Add("repository-return");
            return _repositoryResult ?? this;
        }

        ISagaRegistrationConfigurator ISagaRegistrationConfigurator.Endpoint(
            Action<IEndpointRegistrationConfigurator> configure) => Endpoint(configure);

        void ISagaRegistrationConfigurator.ExcludeFromConfigureEndpoints()
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RegistrationSentinel;

    public sealed class ContractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ExplicitRepositorySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ConventionRepositorySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
