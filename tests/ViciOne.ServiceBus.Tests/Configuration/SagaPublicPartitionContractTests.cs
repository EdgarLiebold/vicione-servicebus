using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaPublicPartitionContractTests
{
    private const BindingFlags DeclaredPublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private const string PartitionSagaSpecificationTypeName =
        "ViciOne.ServiceBus.Configuration.PartitionSagaSpecification`1";

    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-PARTITION-CONTRACT", "event-correlation-builder-exact-interface-shape")]
    public void EventCorrelationBuilder_HasExactInheritanceMemberAndNullabilityShape()
    {
        Type builder = typeof(IEventCorrelationBuilder);
        AssertPublicInterface(builder, genericArity: 0);
        AssertDirectInterfaces(builder);
        AssertNoDeclaredDataMembers(builder);

        MethodInfo build = Assert.Single(DeclaredMethods(builder));
        Assert.Equal(nameof(IEventCorrelationBuilder.Build), build.Name);
        AssertInterfaceMethod(build, builder);
        Assert.False(build.IsGenericMethod);
        Assert.Empty(build.GetParameters());
        Assert.Equal(typeof(IEventCorrelation), build.ReturnType);
        AssertReturnNotNull(build);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-PARTITION-CONTRACT", "saga-definition-exact-interface-shapes")]
    public void SagaDefinitionContracts_HaveExactInheritanceConstraintsMembersAndNullability()
    {
        Type untyped = typeof(ISagaDefinition);
        AssertPublicInterface(untyped, genericArity: 0);
        AssertDirectInterfaces(untyped, typeof(IDefinition));

        PropertyInfo[] untypedProperties = untyped.GetProperties(DeclaredPublicMembers);
        Assert.Equal(2, untypedProperties.Length);
        AssertReadOnlyInterfaceProperty(
            Assert.Single(untypedProperties, property => property.Name == nameof(ISagaDefinition.SagaType)),
            untyped,
            nameof(ISagaDefinition.SagaType),
            typeof(Type),
            NullabilityState.NotNull);
        AssertReadOnlyInterfaceProperty(
            Assert.Single(untypedProperties, property => property.Name == nameof(ISagaDefinition.EndpointDefinition)),
            untyped,
            nameof(ISagaDefinition.EndpointDefinition),
            typeof(IEndpointDefinition),
            NullabilityState.Nullable);

        MethodInfo getEndpointName = Assert.Single(DeclaredMethods(untyped));
        Assert.Equal(nameof(ISagaDefinition.GetEndpointName), getEndpointName.Name);
        AssertInterfaceMethod(getEndpointName, untyped);
        Assert.False(getEndpointName.IsGenericMethod);
        Assert.Equal(typeof(string), getEndpointName.ReturnType);
        AssertParameters(getEndpointName, ("formatter", typeof(IEndpointNameFormatter)));
        AssertReturnNotNull(getEndpointName);
        AssertNoDeclaredFieldsEventsOrNestedTypes(untyped);

        Type typed = typeof(ISagaDefinition<>);
        AssertPublicInterface(typed, genericArity: 1);
        AssertDirectInterfaces(typed, untyped);
        Type saga = Assert.Single(typed.GetGenericArguments());
        AssertSagaParameter(saga, "TSaga");

        PropertyInfo endpointDefinition = Assert.Single(typed.GetProperties(DeclaredPublicMembers));
        Assert.Equal(typed, endpointDefinition.DeclaringType);
        Assert.Equal(nameof(ISagaDefinition<ContractSaga>.EndpointDefinition), endpointDefinition.Name);
        Assert.Equal(typeof(IEndpointDefinition<>).MakeGenericType(saga), endpointDefinition.PropertyType);
        Assert.False(endpointDefinition.CanRead);
        Assert.Null(endpointDefinition.GetMethod);
        Assert.True(endpointDefinition.CanWrite);
        Assert.NotNull(endpointDefinition.SetMethod);
        AssertInterfaceAccessor(endpointDefinition.SetMethod!, typed);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(endpointDefinition).WriteState);

        MethodInfo configure = Assert.Single(DeclaredMethods(typed));
        Assert.Equal(nameof(ISagaDefinition<ContractSaga>.Configure), configure.Name);
        AssertInterfaceMethod(configure, typed);
        Assert.False(configure.IsGenericMethod);
        Assert.Equal(typeof(void), configure.ReturnType);
        AssertParameters(
            configure,
            ("endpointConfigurator", typeof(IReceiveEndpointConfigurator)),
            ("sagaConfigurator", typeof(ISagaConfigurator<>).MakeGenericType(saga)),
            ("context", typeof(IRegistrationContext)));
        AssertNoDeclaredFieldsEventsOrNestedTypes(typed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-PARTITION-CONTRACT", "state-machine-interface-type-exact-shape")]
    public void StateMachineInterfaceType_HasExactGenericConnectorMemberShape()
    {
        Type interfaceType = typeof(IStateMachineInterfaceType);
        AssertPublicInterface(interfaceType, genericArity: 0);
        AssertDirectInterfaces(interfaceType);
        AssertNoDeclaredDataMembers(interfaceType);

        MethodInfo getConnector = Assert.Single(DeclaredMethods(interfaceType));
        Assert.Equal(nameof(IStateMachineInterfaceType.GetConnector), getConnector.Name);
        AssertInterfaceMethod(getConnector, interfaceType);
        Assert.True(getConnector.IsGenericMethodDefinition);
        Type saga = Assert.Single(getConnector.GetGenericArguments());
        AssertSagaParameter(saga, "T");
        Assert.Empty(getConnector.GetParameters());
        Assert.Equal(typeof(ISagaMessageConnector<>).MakeGenericType(saga), getConnector.ReturnType);
        AssertReturnNotNull(getConnector);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-PARTITION-CONTRACT", "partition-constructor-and-apply-guard-contract")]
    public void PartitionSpecification_RejectsMissingDependenciesAtTheExactOwningBoundary()
    {
        Type specificationType = GetPartitionSpecificationType();
        ConstructorInfo constructor = GetPartitionConstructor(specificationType);
        var partitioner = new RecordingPartitioner(new RecordingTypedPartitioner());
        PartitionKeyProvider<SagaConsumeContext<ContractSaga>> keyProvider = SelectPartitionKey;

        ArgumentNullException missingPartitioner = AssertConstructorThrows<ArgumentNullException>(
            constructor,
            null,
            null);
        ArgumentNullException missingKeyProvider = AssertConstructorThrows<ArgumentNullException>(
            constructor,
            partitioner,
            null);

        Assert.Equal("partitioner", missingPartitioner.ParamName);
        Assert.Equal("keyProvider", missingKeyProvider.ParamName);
        Assert.Equal(0, partitioner.GetPartitionerCallCount);

        IPipeSpecification<SagaConsumeContext<ContractSaga>> specification =
            CreatePartitionSpecification(partitioner, keyProvider);
        ArgumentNullException missingBuilder = Assert.Throws<ArgumentNullException>(() => specification.Apply(null!));

        Assert.Equal("builder", missingBuilder.ParamName);
        Assert.Equal(0, partitioner.GetPartitionerCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-PARTITION-CONTRACT", "partition-shared-identities-filter-insertion-and-empty-validation")]
    public void PartitionSpecification_PreservesSharedIdentitiesAddsOneFilterAndValidatesEmpty()
    {
        var typedPartitioner = new RecordingTypedPartitioner();
        var partitioner = new RecordingPartitioner(typedPartitioner);
        PartitionKeyProvider<SagaConsumeContext<ContractSaga>> keyProvider = SelectPartitionKey;
        IPipeSpecification<SagaConsumeContext<ContractSaga>> specification =
            CreatePartitionSpecification(partitioner, keyProvider);
        var builder = new RecordingPipeBuilder<SagaConsumeContext<ContractSaga>>();

        Assert.Empty(specification.Validate());
        Assert.Equal(0, partitioner.GetPartitionerCallCount);

        specification.Apply(builder);

        Assert.Equal(1, partitioner.GetPartitionerCallCount);
        Assert.Equal(typeof(SagaConsumeContext<ContractSaga>), partitioner.ContextType);
        Assert.Same(keyProvider, partitioner.KeyProvider);

        IFilter<SagaConsumeContext<ContractSaga>> filter = Assert.Single(builder.Filters);
        Type filterType = filter.GetType();
        Assert.True(filterType.IsGenericType);
        Assert.Equal(
            "ViciOne.ServiceBus.Middleware.Partitioning.PartitionFilter`1",
            filterType.GetGenericTypeDefinition().FullName);
        Assert.Equal([typeof(SagaConsumeContext<ContractSaga>)], filterType.GetGenericArguments());
        FieldInfo partitionerField = Assert.IsAssignableFrom<FieldInfo>(filterType.GetField(
            "_partitioner",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        Assert.Same(typedPartitioner, partitionerField.GetValue(filter));

        IEnumerable<ValidationResult> validationResults = specification.Validate();

        Assert.NotNull(validationResults);
        Assert.Empty(validationResults);
        Assert.Equal(1, partitioner.GetPartitionerCallCount);
        Assert.Single(builder.Filters);
    }

    private static void AssertPublicInterface(Type type, int genericArity)
    {
        Assert.Equal("ViciOne.ServiceBus.Configuration", type.Namespace);
        Assert.True(type.IsPublic);
        Assert.True(type.IsVisible);
        Assert.True(type.IsInterface);
        Assert.True(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.False(type.IsNested);
        Assert.Null(type.BaseType);
        Assert.Equal(genericArity, type.GetGenericArguments().Length);
        Assert.Equal(genericArity > 0, type.IsGenericTypeDefinition);
        Assert.Empty(type.GetConstructors(DeclaredPublicMembers));
        Assert.Empty(type.GetNestedTypes(BindingFlags.Public));
    }

    private static void AssertSagaParameter(Type parameter, string name) =>
        AssertGenericParameter(
            parameter,
            name,
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISaga));

    private static void AssertGenericParameter(
        Type parameter,
        string name,
        GenericParameterAttributes attributes,
        params Type[] constraints)
    {
        const GenericParameterAttributes relevantAttributes =
            GenericParameterAttributes.VarianceMask | GenericParameterAttributes.SpecialConstraintMask;

        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(name, parameter.Name);
        Assert.Equal(attributes, parameter.GenericParameterAttributes & relevantAttributes);
        Assert.Equal(
            constraints.OrderBy(TypeIdentity, StringComparer.Ordinal),
            parameter.GetGenericParameterConstraints().OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static void AssertDirectInterfaces(Type type, params Type[] expected)
    {
        Type[] all = type.GetInterfaces();
        Type[] direct = all.Where(candidate => !all.Any(
            other => other != candidate && other.GetInterfaces().Contains(candidate))).ToArray();
        Assert.Equal(
            expected.OrderBy(TypeIdentity, StringComparer.Ordinal),
            direct.OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static void AssertInterfaceMethod(MethodInfo method, Type declaringType)
    {
        Assert.Equal(declaringType, method.DeclaringType);
        Assert.True(method.IsPublic);
        Assert.True(method.IsAbstract);
        Assert.True(method.IsVirtual);
        Assert.True(method.IsHideBySig);
        Assert.True((method.Attributes & MethodAttributes.NewSlot) != 0);
        Assert.False(method.IsFinal);
        Assert.False(method.IsStatic);
        Assert.False(method.IsSpecialName);
        Assert.Equal(method, method.GetBaseDefinition());
    }

    private static void AssertInterfaceAccessor(MethodInfo accessor, Type declaringType)
    {
        Assert.True(accessor.IsSpecialName);
        Assert.Equal(declaringType, accessor.DeclaringType);
        Assert.True(accessor.IsPublic);
        Assert.True(accessor.IsAbstract);
        Assert.True(accessor.IsVirtual);
        Assert.True(accessor.IsHideBySig);
        Assert.True((accessor.Attributes & MethodAttributes.NewSlot) != 0);
        Assert.False(accessor.IsFinal);
        Assert.False(accessor.IsStatic);
        Assert.Equal(accessor, accessor.GetBaseDefinition());
    }

    private static void AssertReadOnlyInterfaceProperty(
        PropertyInfo property,
        Type declaringType,
        string name,
        Type propertyType,
        NullabilityState readState)
    {
        Assert.Equal(declaringType, property.DeclaringType);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
        AssertInterfaceAccessor(property.GetMethod!, declaringType);
        Assert.Empty(property.GetMethod!.GetParameters());
        Assert.Equal(propertyType, property.GetMethod.ReturnType);
        Assert.Equal(readState, Nullability.Create(property).ReadState);
    }

    private static void AssertParameters(MethodInfo method, params (string Name, Type Type)[] expected)
    {
        ParameterInfo[] actual = method.GetParameters();
        Assert.Equal(expected.Length, actual.Length);

        for (var index = 0; index < expected.Length; index++)
        {
            ParameterInfo parameter = actual[index];
            Assert.Equal(index, parameter.Position);
            Assert.Equal(expected[index].Name, parameter.Name);
            Assert.Equal(expected[index].Type, parameter.ParameterType);
            Assert.False(parameter.IsIn);
            Assert.False(parameter.IsOut);
            Assert.False(parameter.IsOptional);
            Assert.False(parameter.HasDefaultValue);
            Assert.False(parameter.ParameterType.IsByRef);
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameter).ReadState);
        }
    }

    private static void AssertReturnNotNull(MethodInfo method)
    {
        Assert.False(method.ReturnType.IsByRef);
        Assert.False(method.ReturnType.IsPointer);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
    }

    private static void AssertNoDeclaredDataMembers(Type type)
    {
        Assert.Empty(type.GetProperties(DeclaredPublicMembers));
        AssertNoDeclaredFieldsEventsOrNestedTypes(type);
    }

    private static void AssertNoDeclaredFieldsEventsOrNestedTypes(Type type)
    {
        Assert.Empty(type.GetFields(DeclaredPublicMembers));
        Assert.Empty(type.GetEvents(DeclaredPublicMembers));
        Assert.Empty(type.GetNestedTypes(BindingFlags.Public));
    }

    private static MethodInfo[] DeclaredMethods(Type type) =>
        type.GetMethods(DeclaredPublicMembers).Where(static method => !method.IsSpecialName).ToArray();

    private static Type GetPartitionSpecificationType()
    {
        Type? openType = typeof(IEventCorrelationBuilder).Assembly.GetType(
            PartitionSagaSpecificationTypeName,
            throwOnError: false);
        Assert.NotNull(openType);
        Assert.True(openType.IsNotPublic);
        Assert.True(openType.IsClass);
        Assert.True(openType.IsSealed);
        Assert.False(openType.IsAbstract);
        Assert.True(openType.IsGenericTypeDefinition);
        Type saga = Assert.Single(openType.GetGenericArguments());
        AssertSagaParameter(saga, "TSaga");
        AssertDirectInterfaces(
            openType,
            typeof(IPipeSpecification<>).MakeGenericType(
                typeof(SagaConsumeContext<>).MakeGenericType(saga)));

        return openType.MakeGenericType(typeof(ContractSaga));
    }

    private static ConstructorInfo GetPartitionConstructor(Type specificationType)
    {
        ConstructorInfo constructor = Assert.Single(specificationType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        Assert.True(constructor.IsAssembly);
        AssertParameters(
            constructor.GetParameters(),
            ("partitioner", typeof(IPartitioner)),
            ("keyProvider", typeof(PartitionKeyProvider<SagaConsumeContext<ContractSaga>>)));
        return constructor;
    }

    private static IPipeSpecification<SagaConsumeContext<ContractSaga>> CreatePartitionSpecification(
        IPartitioner partitioner,
        PartitionKeyProvider<SagaConsumeContext<ContractSaga>> keyProvider)
    {
        ConstructorInfo constructor = GetPartitionConstructor(GetPartitionSpecificationType());
        object? instance = constructor.Invoke([partitioner, keyProvider]);
        return Assert.IsAssignableFrom<IPipeSpecification<SagaConsumeContext<ContractSaga>>>(instance);
    }

    private static TException AssertConstructorThrows<TException>(
        ConstructorInfo constructor,
        params object?[] arguments)
        where TException : Exception
    {
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() =>
            constructor.Invoke(arguments));
        return Assert.IsType<TException>(invocation.InnerException);
    }

    private static void AssertParameters(ParameterInfo[] actual, params (string Name, Type Type)[] expected)
    {
        Assert.Equal(expected.Length, actual.Length);

        for (var index = 0; index < expected.Length; index++)
        {
            ParameterInfo parameter = actual[index];
            Assert.Equal(index, parameter.Position);
            Assert.Equal(expected[index].Name, parameter.Name);
            Assert.Equal(expected[index].Type, parameter.ParameterType);
            Assert.False(parameter.IsIn);
            Assert.False(parameter.IsOut);
            Assert.False(parameter.IsOptional);
            Assert.False(parameter.HasDefaultValue);
            Assert.False(parameter.ParameterType.IsByRef);
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameter).ReadState);
        }
    }

    private static byte[] SelectPartitionKey(SagaConsumeContext<ContractSaga> context) =>
        context.Saga.CorrelationId.ToByteArray();

    private static string TypeIdentity(Type type) => type.ToString();

    private sealed class RecordingPartitioner(RecordingTypedPartitioner typedPartitioner) : IPartitioner
    {
        public int GetPartitionerCallCount { get; private set; }
        public Type? ContextType { get; private set; }
        public Delegate? KeyProvider { get; private set; }

        public IPartitioner<T> GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
            where T : class, PipeContext
        {
            GetPartitionerCallCount++;
            ContextType = typeof(T);
            KeyProvider = keyProvider;
            return Assert.IsAssignableFrom<IPartitioner<T>>(typedPartitioner);
        }

        public void Probe(ProbeContext context) => throw new NotSupportedException();
    }

    private sealed class RecordingTypedPartitioner : IPartitioner<SagaConsumeContext<ContractSaga>>
    {
        public Task SendAsync(
            SagaConsumeContext<ContractSaga> context,
            IPipe<SagaConsumeContext<ContractSaga>> next,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void Probe(ProbeContext context) => throw new NotSupportedException();
    }

    private sealed class RecordingPipeBuilder<TContext> : IPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        public List<IFilter<TContext>> Filters { get; } = [];

        public void AddFilter(IFilter<TContext> filter) => Filters.Add(filter);
    }

    private sealed class ContractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
