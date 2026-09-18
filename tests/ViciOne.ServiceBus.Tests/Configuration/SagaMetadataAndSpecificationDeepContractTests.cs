using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaMetadataAndSpecificationDeepContractTests
{
    const BindingFlags DeclaredPublic = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    static readonly Assembly SagaAssembly = typeof(SagaConnector<>).Assembly;
    static readonly Type DescriptorType = SagaAssembly.GetType(
        "ViciOne.ServiceBus.Configuration.SagaMessageConnectorDescriptor",
        throwOnError: true)!;
    static readonly Type MetadataCacheType = SagaAssembly.GetType(
        "ViciOne.ServiceBus.Configuration.SagaMetadataCache`1",
        throwOnError: true)!;
    static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-METADATA-DISCOVERY-CONTRACT", "internal-cache-and-descriptor-exact-surface")]
    public void InternalSurface_ExposesTheExactCacheAndDescriptorContracts()
    {
        Assert.True(MetadataCacheType.IsNotPublic && MetadataCacheType.IsSealed);
        Assert.Empty(MetadataCacheType.GetInterfaces());
        Type saga = Assert.Single(MetadataCacheType.GetGenericArguments());
        AssertGenericParameter(saga, "TSaga", typeof(ISaga));
        Assert.Single(MetadataCacheType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.Empty(MetadataCacheType.GetConstructors(BindingFlags.Instance | BindingFlags.Public));

        PropertyInfo[] cacheProperties = MetadataCacheType.GetProperties(DeclaredPublic);
        Assert.Equal(
            ["FactoryMethod", "InitiatedByOrOrchestratesTypes", "InitiatedByTypes", "ObservesTypes", "OrchestratesTypes"],
            cacheProperties.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        Assert.All(cacheProperties, property =>
        {
            Assert.True(property.GetMethod is { IsPublic: true, IsStatic: true });
            Assert.Null(property.SetMethod);
        });
        Assert.Equal(
            typeof(SagaInstanceFactoryMethod<>).MakeGenericType(saga),
            Assert.Single(cacheProperties, x => x.Name == "FactoryMethod").PropertyType);
        Type descriptorList = typeof(IReadOnlyList<>).MakeGenericType(DescriptorType);
        Assert.All(cacheProperties.Where(x => x.Name != "FactoryMethod"), x => Assert.Equal(descriptorList, x.PropertyType));

        Assert.True(DescriptorType.IsNotPublic && DescriptorType.IsSealed && !DescriptorType.IsGenericType);
        Assert.Empty(DescriptorType.GetInterfaces());
        ConstructorInfo constructor = Assert.Single(DescriptorType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        ParameterInfo[] constructorParameters = constructor.GetParameters();
        Assert.Equal(["messageType", "sagaType"], constructorParameters.Select(x => x.Name));
        Assert.Equal([typeof(Type), typeof(Type)], constructorParameters.Select(x => x.ParameterType));
        Assert.All(constructorParameters, parameter =>
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameter).ReadState));

        PropertyInfo messageType = Assert.Single(DescriptorType.GetProperties(DeclaredPublic));
        Assert.Equal("MessageType", messageType.Name);
        Assert.Equal(typeof(Type), messageType.PropertyType);
        Assert.True(messageType.GetMethod is { IsPublic: true });
        Assert.Null(messageType.SetMethod);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(messageType).ReadState);

        MethodInfo[] methods = DeclaredOrdinaryMethods(DescriptorType);
        Assert.Equal(
            ["CreateInitiatedByConnector", "CreateInitiatedByOrOrchestratesConnector", "CreateObservesConnector", "CreateOrchestratesConnector"],
            methods.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        foreach (MethodInfo method in methods)
        {
            Type methodSaga = Assert.Single(method.GetGenericArguments());
            AssertGenericParameter(methodSaga, "TSaga", typeof(ISaga));
            Assert.Empty(method.GetParameters());
            Assert.Equal(typeof(ISagaMessageConnector<>).MakeGenericType(methodSaga), method.ReturnType);
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-METADATA-DISCOVERY-CONTRACT", "public-specification-inheritance-constraints-members-and-nullability")]
    public void PublicSpecificationSurface_PreservesExactInheritanceConstraintsMembersAndNullability()
    {
        Type messageSpecification = typeof(ISagaMessageSpecification<>);
        Assert.True(messageSpecification.IsPublic && messageSpecification.IsInterface);
        Type messageSaga = Assert.Single(messageSpecification.GetGenericArguments());
        AssertGenericParameter(messageSaga, "TSaga", typeof(ISaga));
        AssertDirectInterfaces(
            messageSpecification,
            typeof(IPipeConfigurator<>).MakeGenericType(typeof(SagaConsumeContext<>).MakeGenericType(messageSaga)),
            typeof(ISagaConfigurationObserverConnector),
            typeof(ISpecification));
        PropertyInfo messageType = Assert.Single(messageSpecification.GetProperties(DeclaredPublic));
        Assert.Equal("MessageType", messageType.Name);
        Assert.Equal(typeof(Type), messageType.PropertyType);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(messageType).ReadState);
        AssertGenericMessageSpecificationMethod(Assert.Single(DeclaredOrdinaryMethods(messageSpecification)));

        Type typedSpecification = typeof(ISagaMessageSpecification<,>);
        Assert.True(typedSpecification.IsPublic && typedSpecification.IsInterface);
        Type[] typedArguments = typedSpecification.GetGenericArguments();
        AssertGenericParameter(typedArguments[0], "TSaga", typeof(ISaga));
        AssertGenericParameter(typedArguments[1], "TMessage");
        AssertDirectInterfaces(
            typedSpecification,
            typeof(ISagaMessageSpecification<>).MakeGenericType(typedArguments[0]),
            typeof(ISagaMessageConfigurator<,>).MakeGenericType(typedArguments),
            typeof(ISagaMessageConfigurator<>).MakeGenericType(typedArguments[1]));
        MethodInfo[] typedMethods = DeclaredOrdinaryMethods(typedSpecification);
        Assert.Equal(["BuildConsumerPipe", "BuildMessagePipe"], typedMethods.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        Assert.All(typedMethods, AssertNonNullableMethodContract);

        Type sagaSpecification = typeof(ISagaSpecification<>);
        Assert.True(sagaSpecification.IsPublic && sagaSpecification.IsInterface);
        Type specificationSaga = Assert.Single(sagaSpecification.GetGenericArguments());
        AssertGenericParameter(specificationSaga, "TSaga", typeof(ISaga));
        AssertDirectInterfaces(
            sagaSpecification,
            typeof(ISagaConfigurator<>).MakeGenericType(specificationSaga),
            typeof(ISpecification));
        MethodInfo[] sagaMethods = DeclaredOrdinaryMethods(sagaSpecification);
        Assert.Equal(["ConfigureMessagePipe", "GetMessageSpecification"], sagaMethods.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        AssertGenericMessageSpecificationMethod(Assert.Single(sagaMethods, x => x.Name == "GetMessageSpecification"));
        AssertNonNullableMethodContract(Assert.Single(sagaMethods, x => x.Name == "ConfigureMessagePipe"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-METADATA-DISCOVERY-CONTRACT", "four-role-deterministic-discovery-invalid-filter-and-observation-owner-pairing")]
    public void MetadataDiscovery_ReturnsFourDeterministicFilteredRoleLists()
    {
        Assert.Equal(
            [typeof(AlphaCorrelatedMessage), typeof(ZuluCorrelatedMessage)],
            GetMessageTypes<DiscoverySaga>("InitiatedByTypes"));
        Assert.Equal(
            [typeof(AlphaCorrelatedMessage), typeof(ZuluCorrelatedMessage)],
            GetMessageTypes<DiscoverySaga>("OrchestratesTypes"));
        Assert.Equal(
            [typeof(AlphaCorrelatedMessage), typeof(ZuluCorrelatedMessage)],
            GetMessageTypes<DiscoverySaga>("InitiatedByOrOrchestratesTypes"));
        Assert.Equal(
            [typeof(AlphaObservedMessage), typeof(ZuluObservedMessage)],
            GetMessageTypes<DiscoverySaga>("ObservesTypes"));

        Assert.DoesNotContain(typeof(Action), GetMessageTypes<DiscoverySaga>("ObservesTypes"));
        Assert.DoesNotContain(typeof(MismatchedObservedMessage), GetMessageTypes<DiscoverySaga>("ObservesTypes"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-METADATA-DISCOVERY-CONTRACT", "guid-constructor-precedes-default-constructor-property-factory")]
    public void FactoryMethod_PrefersTheGuidConstructorOverThePropertyFactory()
    {
        Guid correlationId = Guid.NewGuid();

        var saga = Assert.IsType<ConstructorPreferredSaga>(CreateSaga<ConstructorPreferredSaga>(correlationId));

        Assert.Equal(correlationId, saga.CorrelationId);
        Assert.Equal("guid-constructor", saga.CreationPath);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-METADATA-DISCOVERY-CONTRACT", "property-factory-and-stable-invalid-factory-diagnostic")]
    public void FactoryMethod_UsesThePublicWritablePropertyAndRejectsInvalidShapesStably()
    {
        Guid correlationId = Guid.NewGuid();
        var saga = Assert.IsType<PropertyFactorySaga>(CreateSaga<PropertyFactorySaga>(correlationId));

        Assert.Equal(correlationId, saga.CorrelationId);

        ConfigurationException first = Assert.Throws<ConfigurationException>(() => GetCacheProperty<InvalidFactorySaga>("FactoryMethod"));
        ConfigurationException second = Assert.Throws<ConfigurationException>(() => GetCacheProperty<InvalidFactorySaga>("FactoryMethod"));
        Assert.Contains(nameof(InvalidFactorySaga), first.Message, StringComparison.Ordinal);
        Assert.Contains("public constructor with one Guid parameter", first.Message, StringComparison.Ordinal);
        Assert.Contains("public parameterless constructor and a writable CorrelationId property", first.Message, StringComparison.Ordinal);
        Assert.Equal(first.Message, second.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-METADATA-DISCOVERY-CONTRACT", "descriptor-required-type-validation-before-lazy-state")]
    public void DescriptorConstructor_ValidatesEveryRequiredTypeBeforeCreatingLazyState()
    {
        AssertArgument("messageType", () => CreateDescriptor(null!, null!));
        AssertArgument("sagaType", () => CreateDescriptor(typeof(UniversalMessage), null!));
        AssertArgument("messageType", () => CreateDescriptor(typeof(Action), typeof(AllRoleSaga)));
        AssertArgument("sagaType", () => CreateDescriptor(typeof(UniversalMessage), typeof(string)));
        AssertArgument("sagaType", () => CreateDescriptor(typeof(UniversalMessage), typeof(OpenSaga<>)));

        object descriptor = CreateDescriptor(typeof(UniversalMessage), typeof(AllRoleSaga));
        Assert.Equal(typeof(UniversalMessage), GetMessageType(descriptor));
        Assert.All(GetFactoryLazies(descriptor), lazy => Assert.False(lazy.IsValueCreated));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-METADATA-DISCOVERY-CONTRACT", "four-independent-lazy-factories-exact-pair-and-connector-identity")]
    public void DescriptorFactories_AreIndependentLazyAndPreservePairAndConnectorIdentity()
    {
        object descriptor = CreateDescriptor(typeof(UniversalMessage), typeof(AllRoleSaga));
        Lazy<ISagaConnectorFactory>[] factories = GetFactoryLazies(descriptor);
        Assert.All(factories, lazy => Assert.False(lazy.IsValueCreated));

        object initiated = CreateConnector<AllRoleSaga>(descriptor, "CreateInitiatedByConnector");
        Assert.True(GetFactoryLazy(descriptor, "_initiatedByFactory").IsValueCreated);
        Assert.False(GetFactoryLazy(descriptor, "_orchestratesFactory").IsValueCreated);
        Assert.False(GetFactoryLazy(descriptor, "_initiatedByOrOrchestratesFactory").IsValueCreated);
        Assert.False(GetFactoryLazy(descriptor, "_observesFactory").IsValueCreated);
        object orchestrates = CreateConnector<AllRoleSaga>(descriptor, "CreateOrchestratesConnector");
        object combined = CreateConnector<AllRoleSaga>(descriptor, "CreateInitiatedByOrOrchestratesConnector");
        object observes = CreateConnector<AllRoleSaga>(descriptor, "CreateObservesConnector");

        Assert.All(GetFactoryLazies(descriptor), lazy => Assert.True(lazy.IsValueCreated));
        Assert.Same(initiated, CreateConnector<AllRoleSaga>(descriptor, "CreateInitiatedByConnector"));
        Assert.Same(orchestrates, CreateConnector<AllRoleSaga>(descriptor, "CreateOrchestratesConnector"));
        Assert.Same(combined, CreateConnector<AllRoleSaga>(descriptor, "CreateInitiatedByOrOrchestratesConnector"));
        Assert.Same(observes, CreateConnector<AllRoleSaga>(descriptor, "CreateObservesConnector"));
        Assert.NotSame(initiated, orchestrates);
        Assert.NotSame(orchestrates, combined);

        foreach (object connector in new[] { initiated, orchestrates, combined, observes })
        {
            var typed = Assert.IsAssignableFrom<ISagaMessageConnector<AllRoleSaga>>(connector);
            Assert.Equal(typeof(UniversalMessage), typed.MessageType);
            ISagaMessageSpecification<AllRoleSaga> specification = typed.CreateSagaMessageSpecification();
            Assert.Equal(typeof(UniversalMessage), specification.MessageType);
            Assert.IsAssignableFrom<ISagaMessageSpecification<AllRoleSaga, UniversalMessage>>(specification);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-METADATA-DISCOVERY-CONTRACT", "mismatched-request-rejected-before-lazy-activation")]
    public void Descriptor_RejectsAMismatchedSagaBeforeAnyLazyFactoryIsActivated()
    {
        object descriptor = CreateDescriptor(typeof(UniversalMessage), typeof(InitiatedOnlySaga));

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateConnector<OtherSaga>(descriptor, "CreateInitiatedByConnector"));

        Assert.Equal("TSaga", exception.ParamName);
        Assert.Contains(nameof(OtherSaga), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InitiatedOnlySaga), exception.Message, StringComparison.Ordinal);
        Assert.All(GetFactoryLazies(descriptor), lazy => Assert.False(lazy.IsValueCreated));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-METADATA-DISCOVERY-CONTRACT", "invalid-role-activation-stable-diagnostic-and-factory-isolation")]
    public void Descriptor_IsolatesAnInvalidRoleActivationFromEveryOtherFactory()
    {
        object descriptor = CreateDescriptor(typeof(UniversalMessage), typeof(InitiatedOnlySaga));

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            CreateConnector<InitiatedOnlySaga>(descriptor, "CreateOrchestratesConnector"));
        ConfigurationException repeated = Assert.Throws<ConfigurationException>(() =>
            CreateConnector<InitiatedOnlySaga>(descriptor, "CreateOrchestratesConnector"));

        Assert.Same(exception, repeated);
        Assert.Contains("OrchestratesSagaConnectorFactory", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InitiatedOnlySaga), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(UniversalMessage), exception.Message, StringComparison.Ordinal);
        Assert.NotNull(exception.InnerException);
        Assert.False(GetFactoryLazy(descriptor, "_orchestratesFactory").IsValueCreated);
        Assert.False(GetFactoryLazy(descriptor, "_initiatedByFactory").IsValueCreated);
        Assert.False(GetFactoryLazy(descriptor, "_initiatedByOrOrchestratesFactory").IsValueCreated);
        Assert.False(GetFactoryLazy(descriptor, "_observesFactory").IsValueCreated);

        object initiated = CreateConnector<InitiatedOnlySaga>(descriptor, "CreateInitiatedByConnector");
        Assert.IsAssignableFrom<ISagaMessageConnector<InitiatedOnlySaga>>(initiated);
        Assert.True(GetFactoryLazy(descriptor, "_initiatedByFactory").IsValueCreated);
        Assert.False(GetFactoryLazy(descriptor, "_initiatedByOrOrchestratesFactory").IsValueCreated);
        Assert.False(GetFactoryLazy(descriptor, "_observesFactory").IsValueCreated);
    }

    static Type[] GetMessageTypes<TSaga>(string propertyName)
        where TSaga : class, ISaga
    {
        var descriptors = Assert.IsAssignableFrom<IEnumerable>(GetCacheProperty<TSaga>(propertyName));
        return descriptors.Cast<object>().Select(GetMessageType).ToArray();
    }

    static object CreateSaga<TSaga>(Guid correlationId)
        where TSaga : class, ISaga
    {
        var factory = Assert.IsAssignableFrom<Delegate>(GetCacheProperty<TSaga>("FactoryMethod"));
        return Invoke(() => factory.DynamicInvoke(correlationId))!;
    }

    static object GetCacheProperty<TSaga>(string propertyName)
        where TSaga : class, ISaga
    {
        Type cache = MetadataCacheType.MakeGenericType(typeof(TSaga));
        PropertyInfo property = cache.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Cache property '{propertyName}' was not found.");
        return Invoke(() => property.GetValue(null))!;
    }

    static object CreateDescriptor(Type messageType, Type sagaType) =>
        Invoke(() => Activator.CreateInstance(DescriptorType, messageType, sagaType))!;

    static Type GetMessageType(object descriptor) =>
        Assert.IsAssignableFrom<Type>(DescriptorType.GetProperty("MessageType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(descriptor));

    static object CreateConnector<TSaga>(object descriptor, string methodName)
        where TSaga : class, ISaga
    {
        MethodInfo definition = Assert.Single(DeclaredOrdinaryMethods(DescriptorType), method => method.Name == methodName);
        return Invoke(() => definition.MakeGenericMethod(typeof(TSaga)).Invoke(descriptor, null))!;
    }

    static Lazy<ISagaConnectorFactory>[] GetFactoryLazies(object descriptor) =>
    [
        GetFactoryLazy(descriptor, "_initiatedByFactory"),
        GetFactoryLazy(descriptor, "_orchestratesFactory"),
        GetFactoryLazy(descriptor, "_initiatedByOrOrchestratesFactory"),
        GetFactoryLazy(descriptor, "_observesFactory"),
    ];

    static Lazy<ISagaConnectorFactory> GetFactoryLazy(object descriptor, string fieldName)
    {
        FieldInfo field = DescriptorType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Descriptor field '{fieldName}' was not found.");
        return Assert.IsType<Lazy<ISagaConnectorFactory>>(field.GetValue(descriptor));
    }

    static object? Invoke(Func<object?> action)
    {
        try
        {
            return action();
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    static void AssertArgument(string parameterName, Action action)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static void AssertGenericMessageSpecificationMethod(MethodInfo method)
    {
        Assert.Equal("GetMessageSpecification", method.Name);
        Type message = Assert.Single(method.GetGenericArguments());
        AssertGenericParameter(message, "T");
        Assert.Empty(method.GetParameters());
        Assert.Equal(
            typeof(ISagaMessageSpecification<,>).MakeGenericType(method.DeclaringType!.GetGenericArguments()[0], message),
            method.ReturnType);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
    }

    static void AssertNonNullableMethodContract(MethodInfo method)
    {
        if (method.ReturnType != typeof(void))
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
        Assert.All(method.GetParameters(), parameter =>
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameter).ReadState));
    }

    static void AssertGenericParameter(Type parameter, string name, params Type[] constraints)
    {
        Assert.Equal(name, parameter.Name);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal(constraints, parameter.GetGenericParameterConstraints());
    }

    static void AssertDirectInterfaces(Type type, params Type[] expected)
    {
        Type[] inherited = type.GetInterfaces().SelectMany(x => x.GetInterfaces()).Distinct().ToArray();
        Type[] actual = type.GetInterfaces()
            .Except(inherited)
            .OrderBy(TypeIdentity, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected.OrderBy(TypeIdentity, StringComparer.Ordinal), actual);
    }

    static MethodInfo[] DeclaredOrdinaryMethods(Type type) =>
        type.GetMethods(DeclaredPublic).Where(method => !method.IsSpecialName).ToArray();

    static string TypeIdentity(Type type) => type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

    public sealed class ConstructorPreferredSaga : ISaga
    {
        public ConstructorPreferredSaga()
        {
            CreationPath = "property-factory";
        }

        public ConstructorPreferredSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
            CreationPath = "guid-constructor";
        }

        public Guid CorrelationId { get; set; }

        public string CreationPath { get; }
    }

    public sealed class PropertyFactorySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class InvalidFactorySaga : ISaga
    {
        public InvalidFactorySaga()
        {
        }

        Guid ISaga.CorrelationId { get; set; }
    }

    public sealed class DiscoverySaga :
        ISaga,
        IInitiatedBy<ZuluCorrelatedMessage>,
        IInitiatedBy<AlphaCorrelatedMessage>,
        IOrchestrates<ZuluCorrelatedMessage>,
        IOrchestrates<AlphaCorrelatedMessage>,
        IInitiatedByOrOrchestrates<ZuluCorrelatedMessage>,
        IInitiatedByOrOrchestrates<AlphaCorrelatedMessage>,
        IObserves<ZuluObservedMessage, DiscoverySaga>,
        IObserves<AlphaObservedMessage, DiscoverySaga>,
        IObserves<Action, DiscoverySaga>,
        IObserves<MismatchedObservedMessage, OtherObservationSaga>
    {
        public Guid CorrelationId { get; set; }

        Expression<Func<DiscoverySaga, ZuluObservedMessage, bool>> IObserves<ZuluObservedMessage, DiscoverySaga>.CorrelationExpression =>
            (saga, message) => true;

        Expression<Func<DiscoverySaga, AlphaObservedMessage, bool>> IObserves<AlphaObservedMessage, DiscoverySaga>.CorrelationExpression =>
            (saga, message) => true;

        Expression<Func<DiscoverySaga, Action, bool>> IObserves<Action, DiscoverySaga>.CorrelationExpression =>
            (saga, message) => true;

        Expression<Func<OtherObservationSaga, MismatchedObservedMessage, bool>>
            IObserves<MismatchedObservedMessage, OtherObservationSaga>.CorrelationExpression => (saga, message) => true;

        public Task ConsumeAsync(ConsumeContext<ZuluCorrelatedMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<AlphaCorrelatedMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<ZuluObservedMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<AlphaObservedMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<Action> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<MismatchedObservedMessage> context) => Task.CompletedTask;
    }

    public sealed class AllRoleSaga :
        ISaga,
        IInitiatedBy<UniversalMessage>,
        IOrchestrates<UniversalMessage>,
        IInitiatedByOrOrchestrates<UniversalMessage>,
        IObserves<UniversalMessage, AllRoleSaga>
    {
        public Guid CorrelationId { get; set; }

        public Expression<Func<AllRoleSaga, UniversalMessage, bool>> CorrelationExpression =>
            (saga, message) => saga.CorrelationId == message.CorrelationId;

        public Task ConsumeAsync(ConsumeContext<UniversalMessage> context) => Task.CompletedTask;
    }

    public sealed class InitiatedOnlySaga : ISaga, IInitiatedBy<UniversalMessage>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<UniversalMessage> context) => Task.CompletedTask;
    }

    public sealed class OtherSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class OtherObservationSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class OpenSaga<T> : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record AlphaCorrelatedMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record ZuluCorrelatedMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record UniversalMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record AlphaObservedMessage;
    public sealed record ZuluObservedMessage;
    public sealed record MismatchedObservedMessage;
}
