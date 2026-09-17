using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaPublicRoleContractTests
{
    private const BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-ROLE-CONTRACTS", "correlated-role-hierarchy-attributes-variance-and-constraints")]
    public void CorrelatedRoles_HaveExactHierarchyAttributesVarianceConstraintsAndNoMembers()
    {
        AssertCorrelatedRole(typeof(IInitiatedBy<>), "IInitiatedBy`1");
        AssertCorrelatedRole(typeof(IInitiatedByOrOrchestrates<>), "IInitiatedByOrOrchestrates`1");
        AssertCorrelatedRole(typeof(IOrchestrates<>), "IOrchestrates`1");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-ROLE-CONTRACTS", "observes-role-hierarchy-attributes-constraints-and-expression-property")]
    public void ObservesRole_HasExactHierarchyAttributesConstraintsAndExpressionProperty()
    {
        Type role = typeof(IObserves<,>);
        Type[] arguments = role.GetGenericArguments();
        Type message = arguments[0];
        Type saga = arguments[1];

        AssertPublicInterface(role, "IObserves`2");
        AssertGenericParameter(
            message,
            "TMessage",
            GenericParameterAttributes.ReferenceTypeConstraint);
        AssertGenericParameter(
            saga,
            "TSaga",
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISaga));
        AssertDirectInterfaces(role, typeof(IConsumer<>).MakeGenericType(message));
        AssertExclusionAttributes(role);

        PropertyInfo property = Assert.Single(role.GetProperties(DeclaredPublicInstance));
        Assert.Equal("CorrelationExpression", property.Name);
        Assert.Equal(
            typeof(Expression<>).MakeGenericType(typeof(Func<,,>).MakeGenericType(saga, message, typeof(bool))),
            property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
        Assert.NotNull(property.GetMethod);
        Assert.True(property.GetMethod.IsPublic);
        Assert.Null(property.SetMethod);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(property).ReadState);
        Assert.Empty(DeclaredOrdinaryMethods(role));
        Assert.Empty(role.GetEvents(DeclaredPublicInstance));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-ROLE-CONTRACTS", "observes-expression-preserves-instance-and-typed-operand-order")]
    public void ObservesRole_PreservesExpressionInstanceAndTypedOperandOrder()
    {
        Expression<Func<ObservedSaga, ObservedMessage, bool>> expression =
            (saga, message) => saga.CorrelationId == message.CorrelationId && saga.Revision >= message.MinimumRevision;
        IObserves<ObservedMessage, ObservedSaga> role = new ObservingSaga(expression);

        Assert.Same(expression, role.CorrelationExpression);

        Func<ObservedSaga, ObservedMessage, bool> predicate = role.CorrelationExpression.Compile();
        Guid correlationId = Guid.NewGuid();
        var saga = new ObservedSaga { CorrelationId = correlationId, Revision = 7 };

        Assert.True(predicate(saga, new ObservedMessage(correlationId, 7)));
        Assert.False(predicate(saga, new ObservedMessage(correlationId, 8)));
        Assert.False(predicate(saga, new ObservedMessage(Guid.NewGuid(), 1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-ROLE-CONTRACTS", "state-machine-and-version-role-inheritance-and-members")]
    public void SagaStateRoles_HaveExactInheritanceAndVersionMemberShape()
    {
        Type stateMachineInstance = typeof(ISagaStateMachineInstance);
        AssertPublicInterface(stateMachineInstance, nameof(ISagaStateMachineInstance));
        AssertDirectInterfaces(stateMachineInstance, typeof(ISaga));
        Assert.Empty(stateMachineInstance.GetProperties(DeclaredPublicInstance));
        Assert.Empty(DeclaredOrdinaryMethods(stateMachineInstance));
        Assert.Empty(stateMachineInstance.GetEvents(DeclaredPublicInstance));

        Type version = typeof(ISagaVersion);
        AssertPublicInterface(version, nameof(ISagaVersion));
        AssertDirectInterfaces(version, typeof(ISaga));
        PropertyInfo property = Assert.Single(version.GetProperties(DeclaredPublicInstance));
        Assert.Equal("Version", property.Name);
        Assert.Equal(typeof(int), property.PropertyType);
        Assert.True(property.CanRead);
        Assert.True(property.CanWrite);
        Assert.NotNull(property.GetMethod);
        Assert.NotNull(property.SetMethod);
        Assert.True(property.GetMethod.IsPublic);
        Assert.True(property.SetMethod.IsPublic);
        Assert.Empty(DeclaredOrdinaryMethods(version));
        Assert.Empty(version.GetEvents(DeclaredPublicInstance));

        Guid correlationId = Guid.NewGuid();
        ISaga stateMachineSaga = new StateMachineSaga { CorrelationId = correlationId };
        ISagaVersion versionedSaga = new VersionedSaga { CorrelationId = correlationId, Version = 13 };

        Assert.Equal(correlationId, stateMachineSaga.CorrelationId);
        Assert.Equal(correlationId, versionedSaga.CorrelationId);
        Assert.Equal(13, versionedSaga.Version);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLIC-ROLE-CONTRACTS", "factory-delegate-shape-variance-constraints-nullability-and-semantics")]
    public void SagaFactoryMethod_HasExactShapeVarianceConstraintsAndInvocationSemantics()
    {
        Type factory = typeof(SagaFactoryMethod<,>);
        Assert.True(factory.IsPublic);
        Assert.True(factory.IsSealed);
        Assert.Equal(typeof(MulticastDelegate), factory.BaseType);
        Assert.Equal("SagaFactoryMethod`2", factory.Name);

        Type[] arguments = factory.GetGenericArguments();
        Type saga = arguments[0];
        Type message = arguments[1];
        AssertGenericParameter(
            saga,
            "TSaga",
            GenericParameterAttributes.Covariant | GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISaga));
        AssertGenericParameter(
            message,
            "TMessage",
            GenericParameterAttributes.Contravariant | GenericParameterAttributes.ReferenceTypeConstraint);

        MethodInfo invoke = Assert.IsAssignableFrom<MethodInfo>(factory.GetMethod("Invoke", DeclaredPublicInstance));
        Assert.Equal(saga, invoke.ReturnType);
        ParameterInfo parameter = Assert.Single(invoke.GetParameters());
        Assert.Equal("context", parameter.Name);
        Assert.Equal(typeof(ConsumeContext<>).MakeGenericType(message), parameter.ParameterType);
        var nullability = new NullabilityInfoContext();
        Assert.Equal(NullabilityState.NotNull, nullability.Create(invoke.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState);

        ConsumeContext<DerivedFactoryMessage> context = CreateContext<ConsumeContext<DerivedFactoryMessage>>();
        var expected = new DerivedFactorySaga { CorrelationId = Guid.NewGuid() };
        ConsumeContext<BaseFactoryMessage>? observedContext = null;
        var invocationCount = 0;
        SagaFactoryMethod<DerivedFactorySaga, BaseFactoryMessage> broadFactory = input =>
        {
            invocationCount++;
            observedContext = input;
            return expected;
        };
        SagaFactoryMethod<BaseFactorySaga, DerivedFactoryMessage> narrowedFactory = broadFactory;

        BaseFactorySaga actual = narrowedFactory(context);

        Assert.Same(expected, actual);
        Assert.Same(context, observedContext);
        Assert.Equal(1, invocationCount);
    }

    private static void AssertCorrelatedRole(Type role, string expectedName)
    {
        AssertPublicInterface(role, expectedName);
        Type message = Assert.Single(role.GetGenericArguments());
        AssertGenericParameter(
            message,
            "TMessage",
            GenericParameterAttributes.Contravariant | GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ICorrelatedBy<Guid>));
        AssertDirectInterfaces(role, typeof(IConsumer<>).MakeGenericType(message));
        AssertExclusionAttributes(role);
        Assert.Empty(role.GetProperties(DeclaredPublicInstance));
        Assert.Empty(DeclaredOrdinaryMethods(role));
        Assert.Empty(role.GetEvents(DeclaredPublicInstance));
    }

    private static void AssertPublicInterface(Type type, string expectedName)
    {
        Assert.True(type.IsInterface);
        Assert.True(type.IsPublic);
        Assert.Equal(expectedName, type.Name);
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

    private static void AssertExclusionAttributes(Type type)
    {
        string[] attributes = type.CustomAttributes
            .Select(static attribute => attribute.AttributeType.Name)
            .Where(static name => name.EndsWith("ExclusionAttribute", StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["ConsumerRegistrationExclusionAttribute", "MessageContractExclusionAttribute"],
            attributes);
    }

    private static void AssertGenericParameter(
        Type parameter,
        string expectedName,
        GenericParameterAttributes expectedAttributes,
        params Type[] expectedConstraints)
    {
        const GenericParameterAttributes relevantAttributes =
            GenericParameterAttributes.VarianceMask | GenericParameterAttributes.SpecialConstraintMask;

        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(expectedName, parameter.Name);
        Assert.Equal(expectedAttributes, parameter.GenericParameterAttributes & relevantAttributes);
        Assert.Equal(
            expectedConstraints.OrderBy(TypeIdentity, StringComparer.Ordinal),
            parameter.GetGenericParameterConstraints().OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static MethodInfo[] DeclaredOrdinaryMethods(Type type) =>
        type.GetMethods(DeclaredPublicInstance).Where(static method => !method.IsSpecialName).ToArray();

    private static string TypeIdentity(Type type) => type.ToString();

    private static TContext CreateContext<TContext>()
        where TContext : class => DispatchProxy.Create<TContext, ContextProxy>();

    private sealed record ObservedMessage(Guid CorrelationId, int MinimumRevision);

    private sealed class ObservedSaga : ISaga
    {
        public Guid CorrelationId { get; set; }

        public int Revision { get; init; }
    }

    private sealed class ObservingSaga(Expression<Func<ObservedSaga, ObservedMessage, bool>> expression) :
        IObserves<ObservedMessage, ObservedSaga>
    {
        public Expression<Func<ObservedSaga, ObservedMessage, bool>> CorrelationExpression { get; } = expression;

        public Task ConsumeAsync(ConsumeContext<ObservedMessage> context) => Task.CompletedTask;
    }

    private sealed class StateMachineSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class VersionedSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }

        public int Version { get; set; }
    }

    private record BaseFactoryMessage;

    private sealed record DerivedFactoryMessage : BaseFactoryMessage;

    private class BaseFactorySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class DerivedFactorySaga : BaseFactorySaga;

    private class ContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected context member invocation: {targetMethod?.Name}.");
    }
}
