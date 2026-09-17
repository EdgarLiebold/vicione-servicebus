using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class InMemorySagaRepositoryServiceCollectionExtensionsDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-BOUNDARIES", "in-memory-saga-registration-exact-public-surface")]
    public void RegistrationExtension_ExposesOnlyTheExactGenericVoidEntryPoint()
    {
        Type extensions = typeof(InMemorySagaRepositoryServiceCollectionExtensions);

        Assert.True(extensions.IsPublic);
        Assert.True(extensions.IsAbstract);
        Assert.True(extensions.IsSealed);

        MethodInfo method = Assert.Single(extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(InMemorySagaRepositoryServiceCollectionExtensions.RegisterInMemorySagaRepository), method.Name);
        Assert.Equal(typeof(void), method.ReturnType);
        Assert.True(method.IsDefined(typeof(ExtensionAttribute), inherit: false));

        Type saga = Assert.Single(method.GetGenericArguments());
        Assert.Equal("TSaga", saga.Name);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            saga.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISaga)], saga.GetGenericParameterConstraints());

        ParameterInfo services = Assert.Single(method.GetParameters());
        Assert.Equal("services", services.Name);
        Assert.Equal(typeof(IServiceCollection), services.ParameterType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-BOUNDARIES", "in-memory-saga-registration-required-service-collection")]
    public void RegistrationExtension_RejectsAMissingServiceCollection()
    {
        IServiceCollection services = null!;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => services.RegisterInMemorySagaRepository<RegistrationSaga>());

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "in-memory-di-descriptor-lifetime-and-capability-shape")]
    public void Registration_AddsTheExactDescriptorLifetimeAndCapabilityShape()
    {
        var services = new ServiceCollection();

        services.RegisterInMemorySagaRepository<RegistrationSaga>();

        Assert.Equal(7, services.Count);
        AssertInstanceDescriptor<IndexedSagaDictionary<RegistrationSaga>>(services, ServiceLifetime.Singleton);
        AssertTypeDescriptor<ILoadSagaRepository<RegistrationSaga>, DependencyInjectionLoadSagaRepository<RegistrationSaga>>(
            services,
            ServiceLifetime.Singleton);
        AssertTypeDescriptor<ILoadSagaRepositoryContextFactory<RegistrationSaga>, InMemorySagaRepositoryContextFactory<RegistrationSaga>>(
            services,
            ServiceLifetime.Scoped);
        AssertTypeDescriptor<IQuerySagaRepository<RegistrationSaga>, DependencyInjectionQuerySagaRepository<RegistrationSaga>>(
            services,
            ServiceLifetime.Singleton);
        AssertTypeDescriptor<IQuerySagaRepositoryContextFactory<RegistrationSaga>, InMemorySagaRepositoryContextFactory<RegistrationSaga>>(
            services,
            ServiceLifetime.Scoped);
        AssertTypeDescriptor<ISagaConsumeContextFactory<IndexedSagaDictionary<RegistrationSaga>, RegistrationSaga>,
            InMemorySagaConsumeContextFactory<RegistrationSaga>>(services, ServiceLifetime.Scoped);
        AssertTypeDescriptor<ISagaRepositoryContextFactory<RegistrationSaga>, InMemorySagaRepositoryContextFactory<RegistrationSaga>>(
            services,
            ServiceLifetime.Scoped);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ISagaRepository<RegistrationSaga>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "in-memory-di-singleton-dictionary-and-scoped-factory-identity")]
    public void Provider_PreservesTheSingletonDictionaryAndScopedFactoryIdentities()
    {
        var services = new ServiceCollection();
        services.RegisterInMemorySagaRepository<RegistrationSaga>();

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        IndexedSagaDictionary<RegistrationSaga> dictionary = provider.GetRequiredService<IndexedSagaDictionary<RegistrationSaga>>();
        Assert.Same(dictionary, provider.GetRequiredService<IndexedSagaDictionary<RegistrationSaga>>());

        using IServiceScope firstScope = provider.CreateScope();
        ISagaRepositoryContextFactory<RegistrationSaga> firstRepositoryFactory =
            firstScope.ServiceProvider.GetRequiredService<ISagaRepositoryContextFactory<RegistrationSaga>>();
        ILoadSagaRepositoryContextFactory<RegistrationSaga> firstLoadFactory =
            firstScope.ServiceProvider.GetRequiredService<ILoadSagaRepositoryContextFactory<RegistrationSaga>>();
        IQuerySagaRepositoryContextFactory<RegistrationSaga> firstQueryFactory =
            firstScope.ServiceProvider.GetRequiredService<IQuerySagaRepositoryContextFactory<RegistrationSaga>>();

        Assert.Same(firstRepositoryFactory,
            firstScope.ServiceProvider.GetRequiredService<ISagaRepositoryContextFactory<RegistrationSaga>>());
        Assert.Same(firstLoadFactory,
            firstScope.ServiceProvider.GetRequiredService<ILoadSagaRepositoryContextFactory<RegistrationSaga>>());
        Assert.Same(firstQueryFactory,
            firstScope.ServiceProvider.GetRequiredService<IQuerySagaRepositoryContextFactory<RegistrationSaga>>());
        AssertFactoryUsesDictionary(firstRepositoryFactory, dictionary);
        AssertFactoryUsesDictionary(firstLoadFactory, dictionary);
        AssertFactoryUsesDictionary(firstQueryFactory, dictionary);

        using IServiceScope secondScope = provider.CreateScope();
        Assert.NotSame(firstRepositoryFactory,
            secondScope.ServiceProvider.GetRequiredService<ISagaRepositoryContextFactory<RegistrationSaga>>());
        Assert.NotSame(firstLoadFactory,
            secondScope.ServiceProvider.GetRequiredService<ILoadSagaRepositoryContextFactory<RegistrationSaga>>());
        Assert.NotSame(firstQueryFactory,
            secondScope.ServiceProvider.GetRequiredService<IQuerySagaRepositoryContextFactory<RegistrationSaga>>());
        Assert.Same(dictionary, secondScope.ServiceProvider.GetRequiredService<IndexedSagaDictionary<RegistrationSaga>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "in-memory-di-repeat-registration-stability")]
    public void RepeatedRegistration_PreservesTheDictionaryAndEveryValidDescriptorShape()
    {
        var services = new ServiceCollection();
        services.RegisterInMemorySagaRepository<RegistrationSaga>();
        ServiceDescriptor dictionaryBeforeRepeat = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IndexedSagaDictionary<RegistrationSaga>));

        services.RegisterInMemorySagaRepository<RegistrationSaga>();

        ServiceDescriptor dictionaryAfterRepeat = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IndexedSagaDictionary<RegistrationSaga>));
        Assert.Same(dictionaryBeforeRepeat, dictionaryAfterRepeat);
        AssertRegistrationDescriptors<ILoadSagaRepository<RegistrationSaga>, DependencyInjectionLoadSagaRepository<RegistrationSaga>>(
            services,
            ServiceLifetime.Singleton);
        AssertRegistrationDescriptors<ILoadSagaRepositoryContextFactory<RegistrationSaga>, InMemorySagaRepositoryContextFactory<RegistrationSaga>>(
            services,
            ServiceLifetime.Scoped);
        AssertRegistrationDescriptors<IQuerySagaRepository<RegistrationSaga>, DependencyInjectionQuerySagaRepository<RegistrationSaga>>(
            services,
            ServiceLifetime.Singleton);
        AssertRegistrationDescriptors<IQuerySagaRepositoryContextFactory<RegistrationSaga>, InMemorySagaRepositoryContextFactory<RegistrationSaga>>(
            services,
            ServiceLifetime.Scoped);
        AssertRegistrationDescriptors<ISagaConsumeContextFactory<IndexedSagaDictionary<RegistrationSaga>, RegistrationSaga>,
            InMemorySagaConsumeContextFactory<RegistrationSaga>>(services, ServiceLifetime.Scoped);
        AssertRegistrationDescriptors<ISagaRepositoryContextFactory<RegistrationSaga>, InMemorySagaRepositoryContextFactory<RegistrationSaga>>(
            services,
            ServiceLifetime.Scoped);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using IServiceScope scope = provider.CreateScope();

        IndexedSagaDictionary<RegistrationSaga> dictionary = provider.GetRequiredService<IndexedSagaDictionary<RegistrationSaga>>();
        Assert.All(
            scope.ServiceProvider.GetServices<ISagaRepositoryContextFactory<RegistrationSaga>>(),
            factory => AssertFactoryUsesDictionary(factory, dictionary));
        Assert.All(
            scope.ServiceProvider.GetServices<ILoadSagaRepositoryContextFactory<RegistrationSaga>>(),
            factory => AssertFactoryUsesDictionary(factory, dictionary));
        Assert.All(
            scope.ServiceProvider.GetServices<IQuerySagaRepositoryContextFactory<RegistrationSaga>>(),
            factory => AssertFactoryUsesDictionary(factory, dictionary));
    }

    private static void AssertInstanceDescriptor<TService>(IServiceCollection services, ServiceLifetime lifetime)
        where TService : class
    {
        ServiceDescriptor descriptor = Assert.Single(services, candidate => candidate.ServiceType == typeof(TService));
        Assert.Equal(lifetime, descriptor.Lifetime);
        Assert.IsType<TService>(descriptor.ImplementationInstance);
        Assert.Null(descriptor.ImplementationType);
        Assert.Null(descriptor.ImplementationFactory);
    }

    private static void AssertTypeDescriptor<TService, TImplementation>(IServiceCollection services, ServiceLifetime lifetime)
    {
        ServiceDescriptor descriptor = Assert.Single(services, candidate => candidate.ServiceType == typeof(TService));
        AssertTypeDescriptor<TImplementation>(descriptor, lifetime);
    }

    private static void AssertRegistrationDescriptors<TService, TImplementation>(IServiceCollection services, ServiceLifetime lifetime)
    {
        ServiceDescriptor[] descriptors = services.Where(candidate => candidate.ServiceType == typeof(TService)).ToArray();
        Assert.NotEmpty(descriptors);
        Assert.All(descriptors, descriptor => AssertTypeDescriptor<TImplementation>(descriptor, lifetime));
    }

    private static void AssertTypeDescriptor<TImplementation>(ServiceDescriptor descriptor, ServiceLifetime lifetime)
    {
        Assert.Equal(lifetime, descriptor.Lifetime);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
        Assert.Null(descriptor.ImplementationInstance);
        Assert.Null(descriptor.ImplementationFactory);
    }

    private static void AssertFactoryUsesDictionary(object factory, IndexedSagaDictionary<RegistrationSaga> dictionary)
    {
        var typedFactory = Assert.IsType<InMemorySagaRepositoryContextFactory<RegistrationSaga>>(factory);
        FieldInfo dictionaryField = Assert.Single(
            typedFactory.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
            field => field.FieldType == typeof(IndexedSagaDictionary<RegistrationSaga>));

        Assert.Same(dictionary, dictionaryField.GetValue(typedFactory));
    }

    private sealed class RegistrationSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
