using System.Reflection;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contracts;

public sealed class SagaAdvancedRequestContractTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ADVANCED-REQUEST-CONTRACTS", "correlation-and-saga-contract-shape-variance-attributes-and-semantics")]
    public void CorrelationAndSagaContracts_HaveExactShapeVarianceAttributesAndSemantics()
    {
        Type correlated = typeof(ICorrelatedBy<>);
        Type key = AssertSingleGenericParameter(correlated, "TKey");
        AssertPublicInterface(correlated, "ICorrelatedBy`1");
        AssertGenericParameter(key, GenericParameterAttributes.Covariant);
        AssertDirectInterfaces(correlated, typeof(IMessageCorrelation<>).MakeGenericType(key));
        AssertExclusionAttributes(correlated, "MessageContractExclusionAttribute");
        AssertProperty(correlated, "CorrelationId", key, canWrite: false, NullabilityState.Nullable);
        AssertNoOrdinaryMethodsOrEvents(correlated);

        var expectedKey = new DerivedCorrelationKey("order-42");
        ICorrelatedBy<DerivedCorrelationKey> narrow = new CorrelatedMessage(expectedKey);
        ICorrelatedBy<BaseCorrelationKey> widened = narrow;
        IMessageCorrelation<BaseCorrelationKey> topology = widened;

        Assert.Same(expectedKey, widened.CorrelationId);
        Assert.Same(expectedKey, topology.CorrelationId);

        Type saga = typeof(ISaga);
        AssertPublicInterface(saga, nameof(ISaga));
        AssertDirectInterfaces(saga);
        AssertExclusionAttributes(saga, "ConsumerRegistrationExclusionAttribute");
        AssertProperty(saga, "CorrelationId", typeof(Guid), canWrite: true, NullabilityState.NotNull);
        AssertNoOrdinaryMethodsOrEvents(saga);

        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        ISaga instance = new ContractSaga { CorrelationId = first };

        Assert.Equal(first, instance.CorrelationId);
        instance.CorrelationId = second;
        Assert.Equal(second, instance.CorrelationId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ADVANCED-REQUEST-CONTRACTS", "load-repository-shape-constraints-nullability-default-token-and-forwarding")]
    public async Task LoadSagaRepository_HasExactShapeConstraintsNullabilityAndForwardingAsync()
    {
        Type repository = typeof(ILoadSagaRepository<>);
        Type saga = AssertSingleGenericParameter(repository, "TSaga");
        AssertPublicInterface(repository, "ILoadSagaRepository`1");
        AssertGenericParameter(
            saga,
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISaga));
        AssertDirectInterfaces(repository, typeof(IProbeSite));
        Assert.Empty(repository.GetProperties(DeclaredPublicInstance));
        Assert.Empty(repository.GetEvents(DeclaredPublicInstance));

        MethodInfo method = Assert.Single(DeclaredOrdinaryMethods(repository));
        Assert.Equal("LoadAsync", method.Name);
        Assert.False(method.IsGenericMethod);
        Assert.Equal(typeof(Task<>).MakeGenericType(saga), method.ReturnType);
        ParameterInfo[] parameters = method.GetParameters();
        Assert.Equal([typeof(Guid), typeof(CancellationToken)], parameters.Select(static parameter => parameter.ParameterType));
        Assert.Equal(["correlationId", "cancellationToken"], parameters.Select(static parameter => parameter.Name));
        Assert.True(parameters[1].IsOptional);
        Assert.True(parameters[1].HasDefaultValue);
        Assert.Null(parameters[1].DefaultValue);

        NullabilityInfo returnNullability = new NullabilityInfoContext().Create(method.ReturnParameter);
        Assert.Equal(NullabilityState.NotNull, returnNullability.ReadState);
        Assert.Equal(NullabilityState.Nullable, Assert.Single(returnNullability.GenericTypeArguments).ReadState);

        Guid correlationId = Guid.NewGuid();
        var expected = new ContractSaga { CorrelationId = correlationId };
        var implementation = new RecordingLoadSagaRepository(expected);
        ILoadSagaRepository<ContractSaga> contract = implementation;

#pragma warning disable xUnit1051 // The omitted argument is the contract under test.
        Task<ContractSaga?> defaultLoad = contract.LoadAsync(correlationId);
#pragma warning restore xUnit1051

        Assert.Same(implementation.ResultTask, defaultLoad);
        Assert.Same(expected, await defaultLoad);
        Assert.Equal(correlationId, implementation.ObservedCorrelationId);
        Assert.Equal(CancellationToken.None, implementation.ObservedCancellationToken);

        using var source = new CancellationTokenSource();
        Guid secondCorrelationId = Guid.NewGuid();
        Task<ContractSaga?> explicitLoad = contract.LoadAsync(secondCorrelationId, source.Token);

        Assert.Same(implementation.ResultTask, explicitLoad);
        Assert.Same(expected, await explicitLoad);
        Assert.Equal(secondCorrelationId, implementation.ObservedCorrelationId);
        Assert.Equal(source.Token, implementation.ObservedCancellationToken);
        Assert.Equal(2, implementation.LoadCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ADVANCED-REQUEST-CONTRACTS", "completed-and-faulted-exact-members-nullability-and-value-preservation")]
    public void CompletedAndFaultedContracts_HaveExactMembersNullabilityAndPreserveValues()
    {
        AssertRequestContract(
            typeof(IRequestCompleted),
            ("CorrelationId", typeof(Guid), NullabilityState.NotNull),
            ("Payload", typeof(object), NullabilityState.NotNull),
            ("PayloadType", typeof(string[]), NullabilityState.NotNull),
            ("Timestamp", typeof(DateTimeOffset), NullabilityState.NotNull));
        AssertRequestContract(
            typeof(IRequestFaulted),
            ("CorrelationId", typeof(Guid), NullabilityState.NotNull),
            ("Payload", typeof(object), NullabilityState.NotNull),
            ("PayloadType", typeof(string[]), NullabilityState.NotNull));

        Guid correlationId = Guid.NewGuid();
        DateTimeOffset timestamp = new(2026, 9, 18, 12, 34, 56, TimeSpan.Zero);
        string[] completedTypes = ["urn:message:response"];
        object completedPayload = new object();
        IRequestCompleted completed = new CompletedRequest(
            correlationId,
            timestamp,
            completedTypes,
            completedPayload);

        Assert.Equal(correlationId, completed.CorrelationId);
        Assert.Equal(timestamp, completed.Timestamp);
        Assert.Same(completedTypes, completed.PayloadType);
        Assert.Same(completedPayload, completed.Payload);

        string[] faultedTypes = ["urn:message:fault"];
        object faultedPayload = new object();
        IRequestFaulted faulted = new FaultedRequest(correlationId, faultedTypes, faultedPayload);

        Assert.Equal(correlationId, faulted.CorrelationId);
        Assert.Same(faultedTypes, faulted.PayloadType);
        Assert.Same(faultedPayload, faulted.Payload);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ADVANCED-REQUEST-CONTRACTS", "started-exact-members-nullability-optional-deadline-and-value-preservation")]
    public void StartedContract_HasExactMembersNullabilityAndPreservesOptionalDeadlineAndValues()
    {
        AssertRequestContract(
            typeof(IRequestStarted),
            ("CorrelationId", typeof(Guid), NullabilityState.NotNull),
            ("ExpirationTime", typeof(DateTimeOffset?), NullabilityState.Nullable),
            ("FaultAddress", typeof(Uri), NullabilityState.NotNull),
            ("Payload", typeof(object), NullabilityState.NotNull),
            ("PayloadType", typeof(string[]), NullabilityState.NotNull),
            ("RequestId", typeof(Guid), NullabilityState.NotNull),
            ("ResponseAddress", typeof(Uri), NullabilityState.NotNull));

        Guid correlationId = Guid.NewGuid();
        Guid requestId = Guid.NewGuid();
        var responseAddress = new Uri("loopback://response");
        var faultAddress = new Uri("loopback://fault");
        string[] payloadTypes = ["urn:message:request"];
        object payload = new object();
        DateTimeOffset deadline = new(2026, 9, 18, 13, 0, 0, TimeSpan.Zero);
        IRequestStarted started = new StartedRequest(
            correlationId,
            requestId,
            responseAddress,
            faultAddress,
            deadline,
            payloadTypes,
            payload);

        Assert.Equal(correlationId, started.CorrelationId);
        Assert.Equal(requestId, started.RequestId);
        Assert.Same(responseAddress, started.ResponseAddress);
        Assert.Same(faultAddress, started.FaultAddress);
        Assert.Equal(deadline, started.ExpirationTime);
        Assert.Same(payloadTypes, started.PayloadType);
        Assert.Same(payload, started.Payload);

        IRequestStarted withoutDeadline = new StartedRequest(
            correlationId,
            requestId,
            responseAddress,
            faultAddress,
            null,
            payloadTypes,
            payload);
        Assert.Null(withoutDeadline.ExpirationTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ADVANCED-REQUEST-CONTRACTS", "timeout-exact-members-covariance-nullable-message-and-value-preservation")]
    public void TimeoutContract_HasExactMembersCovarianceNullableMessageAndPreservesValues()
    {
        Type timeout = typeof(IRequestTimeoutExpired<>);
        Type request = AssertSingleGenericParameter(timeout, "TRequest");
        AssertPublicInterface(timeout, "IRequestTimeoutExpired`1");
        AssertGenericParameter(
            request,
            GenericParameterAttributes.Covariant | GenericParameterAttributes.ReferenceTypeConstraint);
        AssertDirectInterfaces(timeout);
        AssertRequestContract(
            timeout,
            ("CorrelationId", typeof(Guid), NullabilityState.NotNull),
            ("ExpirationTime", typeof(DateTimeOffset), NullabilityState.NotNull),
            ("Message", request, NullabilityState.Nullable),
            ("RequestId", typeof(Guid), NullabilityState.NotNull),
            ("Timestamp", typeof(DateTimeOffset), NullabilityState.NotNull));

        Guid correlationId = Guid.NewGuid();
        Guid requestId = Guid.NewGuid();
        DateTimeOffset timestamp = new(2026, 9, 18, 14, 0, 0, TimeSpan.Zero);
        DateTimeOffset expirationTime = timestamp.AddMinutes(2);
        var message = new DerivedRequest("payload");
        IRequestTimeoutExpired<DerivedRequest> narrow = new TimeoutRequest<DerivedRequest>(
            correlationId,
            timestamp,
            expirationTime,
            requestId,
            message);
        IRequestTimeoutExpired<BaseRequest> widened = narrow;

        Assert.Equal(correlationId, widened.CorrelationId);
        Assert.Equal(timestamp, widened.Timestamp);
        Assert.Equal(expirationTime, widened.ExpirationTime);
        Assert.Equal(requestId, widened.RequestId);
        Assert.Same(message, widened.Message);

        IRequestTimeoutExpired<BaseRequest> withoutMessage = new TimeoutRequest<BaseRequest>(
            correlationId,
            timestamp,
            expirationTime,
            requestId,
            null);
        Assert.Null(withoutMessage.Message);
    }

    private static Type AssertSingleGenericParameter(Type type, string name)
    {
        Type parameter = Assert.Single(type.GetGenericArguments());
        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(name, parameter.Name);
        return parameter;
    }

    private static void AssertPublicInterface(Type type, string name)
    {
        Assert.True(type.IsPublic);
        Assert.True(type.IsInterface);
        Assert.Equal(name, type.Name);
        Assert.Equal("ViciOne.ServiceBus.Sagas", type.Assembly.GetName().Name);
    }

    private static void AssertGenericParameter(
        Type parameter,
        GenericParameterAttributes attributes,
        params Type[] constraints)
    {
        const GenericParameterAttributes relevantAttributes =
            GenericParameterAttributes.VarianceMask | GenericParameterAttributes.SpecialConstraintMask;

        Assert.Equal(attributes, parameter.GenericParameterAttributes & relevantAttributes);
        Assert.Equal(
            constraints.OrderBy(TypeIdentity, StringComparer.Ordinal),
            parameter.GetGenericParameterConstraints().OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static void AssertDirectInterfaces(Type type, params Type[] expected)
    {
        Type[] inherited = type.GetInterfaces()
            .SelectMany(static candidate => candidate.GetInterfaces())
            .Distinct()
            .ToArray();
        Type[] actual = type.GetInterfaces()
            .Except(inherited)
            .OrderBy(TypeIdentity, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.OrderBy(TypeIdentity, StringComparer.Ordinal), actual);
    }

    private static void AssertExclusionAttributes(Type type, params string[] expected)
    {
        string[] actual = type.CustomAttributes
            .Select(static attribute => attribute.AttributeType.Name)
            .Where(static name => name.EndsWith("ExclusionAttribute", StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
    }

    private static void AssertRequestContract(
        Type type,
        params (string Name, Type PropertyType, NullabilityState Nullability)[] expectedProperties)
    {
        AssertPublicInterface(type, type.Name);
        AssertDirectInterfaces(type);
        Assert.Equal(
            expectedProperties.Select(static property => property.Name).Order(StringComparer.Ordinal),
            type.GetProperties(DeclaredPublicInstance).Select(static property => property.Name).Order(StringComparer.Ordinal));

        foreach ((string name, Type propertyType, NullabilityState nullability) in expectedProperties)
            AssertProperty(type, name, propertyType, canWrite: false, nullability);

        AssertNoOrdinaryMethodsOrEvents(type);
    }

    private static void AssertProperty(
        Type type,
        string name,
        Type propertyType,
        bool canWrite,
        NullabilityState nullability)
    {
        PropertyInfo property = Assert.IsAssignableFrom<PropertyInfo>(type.GetProperty(name, DeclaredPublicInstance));
        Assert.Equal(propertyType, property.PropertyType);
        Assert.True(property.CanRead);
        Assert.NotNull(property.GetMethod);
        Assert.True(property.GetMethod!.IsPublic);
        Assert.Equal(canWrite, property.CanWrite);
        Assert.Equal(canWrite, property.SetMethod is not null);
        if (canWrite)
            Assert.True(property.SetMethod!.IsPublic);
        Assert.Equal(nullability, new NullabilityInfoContext().Create(property).ReadState);
    }

    private static void AssertNoOrdinaryMethodsOrEvents(Type type)
    {
        Assert.Empty(DeclaredOrdinaryMethods(type));
        Assert.Empty(type.GetEvents(DeclaredPublicInstance));
    }

    private static MethodInfo[] DeclaredOrdinaryMethods(Type type) =>
        type.GetMethods(DeclaredPublicInstance).Where(static method => !method.IsSpecialName).ToArray();

    private static string TypeIdentity(Type type) => type.ToString();

    private class BaseCorrelationKey(string value)
    {
        public string Value { get; } = value;
    }

    private sealed class DerivedCorrelationKey(string value) : BaseCorrelationKey(value);

    private sealed record CorrelatedMessage(DerivedCorrelationKey CorrelationId) :
        ICorrelatedBy<DerivedCorrelationKey>;

    private sealed class ContractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class RecordingLoadSagaRepository : ILoadSagaRepository<ContractSaga>
    {
        public RecordingLoadSagaRepository(ContractSaga result)
        {
            ResultTask = Task.FromResult<ContractSaga?>(result);
        }

        public Task<ContractSaga?> ResultTask { get; }

        public Guid ObservedCorrelationId { get; private set; }

        public CancellationToken ObservedCancellationToken { get; private set; }

        public int LoadCount { get; private set; }

        public Task<ContractSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            ObservedCorrelationId = correlationId;
            ObservedCancellationToken = cancellationToken;
            LoadCount++;
            return ResultTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed record CompletedRequest(
        Guid CorrelationId,
        DateTimeOffset Timestamp,
        string[] PayloadType,
        object Payload) : IRequestCompleted;

    private sealed record FaultedRequest(
        Guid CorrelationId,
        string[] PayloadType,
        object Payload) : IRequestFaulted;

    private sealed record StartedRequest(
        Guid CorrelationId,
        Guid RequestId,
        Uri ResponseAddress,
        Uri FaultAddress,
        DateTimeOffset? ExpirationTime,
        string[] PayloadType,
        object Payload) : IRequestStarted;

    private class BaseRequest(string value)
    {
        public string Value { get; } = value;
    }

    private sealed class DerivedRequest(string value) : BaseRequest(value);

    private sealed record TimeoutRequest<TRequest>(
        Guid CorrelationId,
        DateTimeOffset Timestamp,
        DateTimeOffset ExpirationTime,
        Guid RequestId,
        TRequest? Message) : IRequestTimeoutExpired<TRequest>
        where TRequest : class;
}
