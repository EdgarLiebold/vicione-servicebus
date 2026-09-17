using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class RegistrationServiceCollectionExtensionsDeepContractTests
{
    static readonly Type[] RepositoryServiceDefinitions =
    [
        typeof(ISagaConsumeContextFactory<,>),
        typeof(ISagaRepositoryContextFactory<>),
        typeof(IQuerySagaRepositoryContextFactory<>),
        typeof(ILoadSagaRepositoryContextFactory<>),
        typeof(IQuerySagaRepository<>),
        typeof(ILoadSagaRepository<>),
        typeof(ISagaRepository<>),
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-SERVICE-REGISTRATION", "exact-public-internal-surface-and-generic-constraints")]
    public void Surface_ExposesTheExactPublicAndInternalMethodsWithTheirGenericConstraints()
    {
        Type extensions = typeof(RegistrationServiceCollectionExtensions);
        MethodInfo[] methods = extensions
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.IsPublic || method.IsAssembly)
            .ToArray();

        Assert.True(extensions.IsPublic && extensions.IsAbstract && extensions.IsSealed);
        Assert.Equal(4, methods.Length);
        Assert.Equal(3, methods.Count(method => method.IsPublic));
        Assert.Single(methods, method => method.IsAssembly);
        Assert.All(methods, method =>
        {
            Assert.Equal(typeof(void), method.ReturnType);
            Assert.True(method.IsDefined(typeof(ExtensionAttribute), inherit: false));
            ParameterInfo collection = Assert.Single(method.GetParameters());
            Assert.Equal("collection", collection.Name);
            Assert.Equal(typeof(IServiceCollection), collection.ParameterType);
        });

        MethodInfo register = Assert.Single(methods, method => method.Name == nameof(RegistrationServiceCollectionExtensions.RegisterSagaRepository));
        Type[] registerArguments = register.GetGenericArguments();
        Assert.Equal(4, registerArguments.Length);
        AssertGenericParameter(registerArguments[0], "TSaga", [typeof(ISaga)]);
        AssertGenericParameter(registerArguments[1], "TContext", []);
        AssertGenericParameter(
            registerArguments[2],
            "TConsumeContextFactory",
            [typeof(ISagaConsumeContextFactory<,>).MakeGenericType(registerArguments[1], registerArguments[0])]);
        AssertGenericParameter(
            registerArguments[3],
            "TRepositoryContextFactory",
            [typeof(ISagaRepositoryContextFactory<>).MakeGenericType(registerArguments[0])]);

        MethodInfo query = Assert.Single(methods, method => method.Name == nameof(RegistrationServiceCollectionExtensions.RegisterQuerySagaRepository));
        Type[] queryArguments = query.GetGenericArguments();
        Assert.Equal(2, queryArguments.Length);
        AssertGenericParameter(queryArguments[0], "TSaga", [typeof(ISaga)]);
        AssertGenericParameter(
            queryArguments[1],
            "TQueryRepositoryContextFactory",
            [typeof(IQuerySagaRepositoryContextFactory<>).MakeGenericType(queryArguments[0])]);

        MethodInfo load = Assert.Single(methods, method => method.Name == nameof(RegistrationServiceCollectionExtensions.RegisterLoadSagaRepository));
        Type[] loadArguments = load.GetGenericArguments();
        Assert.Equal(2, loadArguments.Length);
        AssertGenericParameter(loadArguments[0], "TSaga", [typeof(ISaga)]);
        AssertGenericParameter(
            loadArguments[1],
            "TLoadRepositoryContextFactory",
            [typeof(ILoadSagaRepositoryContextFactory<>).MakeGenericType(loadArguments[0])]);

        MethodInfo remove = GetRemoveMethod(methods);
        Assert.False(remove.IsGenericMethod);
        Assert.True(remove.IsAssembly);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-SERVICE-REGISTRATION", "receiver-first-null-guards")]
    public void AllEntryPoints_RejectAMissingCollectionWithTheStableReceiverName()
    {
        IServiceCollection collection = null!;

        AssertCollectionArgument(() => collection.RegisterSagaRepository<
            RegistrationSaga,
            IndexedSagaDictionary<RegistrationSaga>,
            InMemorySagaConsumeContextFactory<RegistrationSaga>,
            InMemorySagaRepositoryContextFactory<RegistrationSaga>>());
        AssertCollectionArgument(() => collection.RegisterQuerySagaRepository<
            RegistrationSaga,
            InMemorySagaRepositoryContextFactory<RegistrationSaga>>());
        AssertCollectionArgument(() => collection.RegisterLoadSagaRepository<
            RegistrationSaga,
            InMemorySagaRepositoryContextFactory<RegistrationSaga>>());

        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(
            () => GetRemoveMethod().Invoke(null, [null]));
        var exception = Assert.IsType<ArgumentNullException>(invocation.InnerException);
        Assert.Equal("collection", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-SERVICE-REGISTRATION", "saga-descriptor-lifetime-and-order")]
    public void RegisterSagaRepository_AppendsTheExactScopedDescriptorPairInOrder()
    {
        IServiceCollection collection = new ServiceCollection();

        collection.RegisterSagaRepository<
            RegistrationSaga,
            IndexedSagaDictionary<RegistrationSaga>,
            InMemorySagaConsumeContextFactory<RegistrationSaga>,
            InMemorySagaRepositoryContextFactory<RegistrationSaga>>();

        Assert.Collection(
            collection,
            descriptor => AssertTypeDescriptor(
                descriptor,
                typeof(ISagaConsumeContextFactory<IndexedSagaDictionary<RegistrationSaga>, RegistrationSaga>),
                typeof(InMemorySagaConsumeContextFactory<RegistrationSaga>),
                ServiceLifetime.Scoped),
            descriptor => AssertTypeDescriptor(
                descriptor,
                typeof(ISagaRepositoryContextFactory<RegistrationSaga>),
                typeof(InMemorySagaRepositoryContextFactory<RegistrationSaga>),
                ServiceLifetime.Scoped));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-SERVICE-REGISTRATION", "query-load-descriptor-lifetime-and-order")]
    public void RegisterQueryAndLoadRepositories_AppendSingletonFacadesBeforeTheirScopedFactories()
    {
        var collection = new ServiceCollection();

        collection.RegisterQuerySagaRepository<RegistrationSaga, InMemorySagaRepositoryContextFactory<RegistrationSaga>>();
        collection.RegisterLoadSagaRepository<RegistrationSaga, InMemorySagaRepositoryContextFactory<RegistrationSaga>>();

        Assert.Collection(
            collection,
            descriptor => AssertTypeDescriptor(
                descriptor,
                typeof(IQuerySagaRepository<RegistrationSaga>),
                typeof(DependencyInjectionQuerySagaRepository<RegistrationSaga>),
                ServiceLifetime.Singleton),
            descriptor => AssertTypeDescriptor(
                descriptor,
                typeof(IQuerySagaRepositoryContextFactory<RegistrationSaga>),
                typeof(InMemorySagaRepositoryContextFactory<RegistrationSaga>),
                ServiceLifetime.Scoped),
            descriptor => AssertTypeDescriptor(
                descriptor,
                typeof(ILoadSagaRepository<RegistrationSaga>),
                typeof(DependencyInjectionLoadSagaRepository<RegistrationSaga>),
                ServiceLifetime.Singleton),
            descriptor => AssertTypeDescriptor(
                descriptor,
                typeof(ILoadSagaRepositoryContextFactory<RegistrationSaga>),
                typeof(InMemorySagaRepositoryContextFactory<RegistrationSaga>),
                ServiceLifetime.Scoped));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-SERVICE-REGISTRATION", "additive-duplicate-call-block-semantics")]
    public void RepeatedRegistration_AppendsOneCompleteOrderedDescriptorBlockPerCall()
    {
        var saga = new ServiceCollection();
        RegisterSaga(saga);
        RegisterSaga(saga);

        Assert.Equal(4, saga.Count);
        AssertSagaBlock(saga, 0);
        AssertSagaBlock(saga, 2);

        var query = new ServiceCollection();
        query.RegisterQuerySagaRepository<RegistrationSaga, InMemorySagaRepositoryContextFactory<RegistrationSaga>>();
        query.RegisterQuerySagaRepository<RegistrationSaga, InMemorySagaRepositoryContextFactory<RegistrationSaga>>();

        Assert.Equal(4, query.Count);
        AssertQueryBlock(query, 0);
        AssertQueryBlock(query, 2);

        var load = new ServiceCollection();
        load.RegisterLoadSagaRepository<RegistrationSaga, InMemorySagaRepositoryContextFactory<RegistrationSaga>>();
        load.RegisterLoadSagaRepository<RegistrationSaga, InMemorySagaRepositoryContextFactory<RegistrationSaga>>();

        Assert.Equal(4, load.Count);
        AssertLoadBlock(load, 0);
        AssertLoadBlock(load, 2);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-SERVICE-REGISTRATION", "open-closed-removal-scope-and-survivor-order")]
    public void RemoveSagaRepositories_RemovesOnlyRepositoryServiceFamiliesAndPreservesSurvivorOrder()
    {
        IServiceCollection collection = new ServiceCollection();
        ServiceDescriptor before = ServiceDescriptor.Singleton(typeof(BeforeMarker), new BeforeMarker());
        ServiceDescriptor middle = ServiceDescriptor.Singleton(typeof(IEnumerable<RegistrationSaga>), Array.Empty<RegistrationSaga>());
        ServiceDescriptor nearCollision = ServiceDescriptor.Singleton(
            typeof(ISagaConsumeContextFactory<RegistrationSaga>),
            _ => new object());
        ServiceDescriptor after = ServiceDescriptor.Singleton(typeof(AfterMarker), new AfterMarker());
        collection.Add(before);
        RegisterSaga(collection);
        collection.RegisterQuerySagaRepository<RegistrationSaga, InMemorySagaRepositoryContextFactory<RegistrationSaga>>();
        collection.Add(middle);
        collection.RegisterLoadSagaRepository<RegistrationSaga, InMemorySagaRepositoryContextFactory<RegistrationSaga>>();
        collection.Add(ServiceDescriptor.Singleton(
            typeof(ISagaRepository<RegistrationSaga>),
            _ => new object()));

        foreach (Type definition in RepositoryServiceDefinitions)
            collection.Add(ServiceDescriptor.Singleton(definition, _ => new object()));

        collection.Add(nearCollision);
        collection.Add(after);

        GetRemoveMethod().Invoke(null, [collection]);

        Assert.Equal([before, middle, nearCollision, after], collection);
        Assert.DoesNotContain(collection, descriptor => IsRepositoryService(descriptor.ServiceType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-SERVICE-REGISTRATION", "removal-preflight-mutation-atomicity")]
    public void RemoveSagaRepositories_InspectsTheCompletePlanBeforeMutatingTheCollection()
    {
        ServiceDescriptor survivor = ServiceDescriptor.Singleton(typeof(BeforeMarker), new BeforeMarker());
        ServiceDescriptor inspectionFailure = ServiceDescriptor.Singleton(typeof(AfterMarker), new AfterMarker());
        ServiceDescriptor target = ServiceDescriptor.Singleton(typeof(ISagaConsumeContextFactory<,>), _ => new object());
        var collection = new FaultingInspectionServiceCollection([survivor, inspectionFailure, target], faultingIndex: 1);

        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(
            () => GetRemoveMethod().Invoke(null, [collection]));

        InvalidOperationException exception = Assert.IsType<InvalidOperationException>(invocation.InnerException);
        Assert.Equal("inspection failure", exception.Message);
        Assert.Equal(0, collection.MutationCount);
        Assert.Equal([survivor, inspectionFailure, target], collection.Snapshot);
    }

    static MethodInfo GetRemoveMethod(IEnumerable<MethodInfo>? methods = null) =>
        Assert.Single(
            methods ?? typeof(RegistrationServiceCollectionExtensions)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly),
            method => method.Name == "RemoveSagaRepositories");

    static void AssertGenericParameter(Type parameter, string name, Type[] constraints)
    {
        Assert.Equal(name, parameter.Name);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal(constraints, parameter.GetGenericParameterConstraints());
    }

    static void AssertCollectionArgument(Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("collection", exception.ParamName);
    }

    static void RegisterSaga(IServiceCollection collection) =>
        collection.RegisterSagaRepository<
            RegistrationSaga,
            IndexedSagaDictionary<RegistrationSaga>,
            InMemorySagaConsumeContextFactory<RegistrationSaga>,
            InMemorySagaRepositoryContextFactory<RegistrationSaga>>();

    static void AssertSagaBlock(IServiceCollection collection, int offset)
    {
        AssertTypeDescriptor(
            collection[offset],
            typeof(ISagaConsumeContextFactory<IndexedSagaDictionary<RegistrationSaga>, RegistrationSaga>),
            typeof(InMemorySagaConsumeContextFactory<RegistrationSaga>),
            ServiceLifetime.Scoped);
        AssertTypeDescriptor(
            collection[offset + 1],
            typeof(ISagaRepositoryContextFactory<RegistrationSaga>),
            typeof(InMemorySagaRepositoryContextFactory<RegistrationSaga>),
            ServiceLifetime.Scoped);
    }

    static void AssertQueryBlock(IServiceCollection collection, int offset)
    {
        AssertTypeDescriptor(
            collection[offset],
            typeof(IQuerySagaRepository<RegistrationSaga>),
            typeof(DependencyInjectionQuerySagaRepository<RegistrationSaga>),
            ServiceLifetime.Singleton);
        AssertTypeDescriptor(
            collection[offset + 1],
            typeof(IQuerySagaRepositoryContextFactory<RegistrationSaga>),
            typeof(InMemorySagaRepositoryContextFactory<RegistrationSaga>),
            ServiceLifetime.Scoped);
    }

    static void AssertLoadBlock(IServiceCollection collection, int offset)
    {
        AssertTypeDescriptor(
            collection[offset],
            typeof(ILoadSagaRepository<RegistrationSaga>),
            typeof(DependencyInjectionLoadSagaRepository<RegistrationSaga>),
            ServiceLifetime.Singleton);
        AssertTypeDescriptor(
            collection[offset + 1],
            typeof(ILoadSagaRepositoryContextFactory<RegistrationSaga>),
            typeof(InMemorySagaRepositoryContextFactory<RegistrationSaga>),
            ServiceLifetime.Scoped);
    }

    static void AssertTypeDescriptor(ServiceDescriptor descriptor, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        Assert.Equal(serviceType, descriptor.ServiceType);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(lifetime, descriptor.Lifetime);
        Assert.Null(descriptor.ImplementationInstance);
        Assert.Null(descriptor.ImplementationFactory);
        Assert.False(descriptor.IsKeyedService);
    }

    static bool IsRepositoryService(Type serviceType)
    {
        Type candidate = serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : serviceType;
        return RepositoryServiceDefinitions.Contains(candidate);
    }

    sealed class RegistrationSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    sealed class BeforeMarker;

    sealed class AfterMarker;

    sealed class FaultingInspectionServiceCollection(
        IEnumerable<ServiceDescriptor> descriptors,
        int faultingIndex) : IServiceCollection
    {
        readonly List<ServiceDescriptor> _descriptors = descriptors.ToList();

        public ServiceDescriptor[] Snapshot => _descriptors.ToArray();

        public int MutationCount { get; private set; }

        public ServiceDescriptor this[int index]
        {
            get => index == faultingIndex
                ? throw new InvalidOperationException("inspection failure")
                : _descriptors[index];
            set
            {
                MutationCount++;
                _descriptors[index] = value;
            }
        }

        public int Count => _descriptors.Count;

        public bool IsReadOnly => false;

        public void Add(ServiceDescriptor item)
        {
            MutationCount++;
            _descriptors.Add(item);
        }

        public void Clear()
        {
            MutationCount++;
            _descriptors.Clear();
        }

        public bool Contains(ServiceDescriptor item) => _descriptors.Contains(item);

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _descriptors.CopyTo(array, arrayIndex);

        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            for (var index = 0; index < _descriptors.Count; index++)
                yield return this[index];
        }

        public int IndexOf(ServiceDescriptor item) => _descriptors.IndexOf(item);

        public void Insert(int index, ServiceDescriptor item)
        {
            MutationCount++;
            _descriptors.Insert(index, item);
        }

        public bool Remove(ServiceDescriptor item)
        {
            MutationCount++;
            return _descriptors.Remove(item);
        }

        public void RemoveAt(int index)
        {
            MutationCount++;
            _descriptors.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
