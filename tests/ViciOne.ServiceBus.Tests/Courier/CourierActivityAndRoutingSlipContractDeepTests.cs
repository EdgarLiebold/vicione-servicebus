using System.Reflection;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using ActivityMessageContract = ViciOne.ServiceBus.Courier.Contracts.IActivity;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierActivityAndRoutingSlipContractDeepTests
{
    [Theory]
    [InlineData(typeof(IExecuteActivity<>), 1)]
    [InlineData(typeof(ICompensateActivity<>), 1)]
    [InlineData(typeof(ViciOne.ServiceBus.Courier.IActivity<,>), 2)]
    [RequirementCoverage("REQ-VSB-COURIER-CONTRACTS", "deep-activity-contract-contravariant-reference-constraints")]
    public void ActivityContracts_DeclareContravariantReferenceTypeParameters(Type contract, int parameterCount)
    {
        Type[] parameters = contract.GetGenericArguments();

        Assert.Equal(parameterCount, parameters.Length);
        Assert.All(parameters, parameter =>
        {
            GenericParameterAttributes attributes = parameter.GenericParameterAttributes;
            Assert.Equal(
                GenericParameterAttributes.Contravariant,
                attributes & GenericParameterAttributes.VarianceMask);
            Assert.Equal(
                GenericParameterAttributes.ReferenceTypeConstraint,
                attributes & GenericParameterAttributes.SpecialConstraintMask);
        });

        if (contract == typeof(ViciOne.ServiceBus.Courier.IActivity<,>))
        {
            Type[] inheritedContracts = contract.GetInterfaces()
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .ToArray();
            Assert.Collection(
                inheritedContracts,
                compensate => Assert.Equal(typeof(ICompensateActivity<>), compensate.GetGenericTypeDefinition()),
                execute => Assert.Equal(typeof(IExecuteActivity<>), execute.GetGenericTypeDefinition()));
            Assert.Same(parameters[1], inheritedContracts[0].GetGenericArguments()[0]);
            Assert.Same(parameters[0], inheritedContracts[1].GetGenericArguments()[0]);
            Assert.Empty(contract.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        }
    }

    [Theory]
    [InlineData(typeof(IExecuteActivity<>), "ExecuteAsync", typeof(ExecuteContext<>), typeof(ExecutionResult))]
    [InlineData(typeof(ICompensateActivity<>), "CompensateAsync", typeof(CompensateContext<>), typeof(CompensationResult))]
    [RequirementCoverage("REQ-VSB-COURIER-CONTRACTS", "deep-activity-operation-exact-async-signatures")]
    public void ActivityOperations_ExposeOneNonOptionalContextAndTypedTask(
        Type contract,
        string methodName,
        Type contextDefinition,
        Type resultType)
    {
        MethodInfo method = Assert.Single(contract.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        ParameterInfo parameter = Assert.Single(method.GetParameters());
        var nullability = new NullabilityInfoContext();

        Assert.Equal(methodName, method.Name);
        Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
        Assert.Equal(typeof(Task<>).MakeGenericType(resultType), method.ReturnType);
        Assert.True(parameter.ParameterType.IsGenericType);
        Assert.Equal(contextDefinition, parameter.ParameterType.GetGenericTypeDefinition());
        Assert.Equal("context", parameter.Name);
        Assert.False(parameter.IsOptional);
        Assert.False(parameter.HasDefaultValue);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(method.ReturnParameter).ReadState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTRACTS", "deep-activity-variance-preserves-runtime-identity")]
    public void ContravariantActivityViews_PreserveTheExactImplementationInstance()
    {
        var activity = new BroadActivity();
        IExecuteActivity<object> broadExecute = activity;
        ICompensateActivity<object> broadCompensate = activity;
        ViciOne.ServiceBus.Courier.IActivity<object, object> broadCombined = activity;

        IExecuteActivity<string> narrowExecute = broadExecute;
        ICompensateActivity<string> narrowCompensate = broadCompensate;
        ViciOne.ServiceBus.Courier.IActivity<string, string> narrowCombined = broadCombined;

        Assert.Same(activity, narrowExecute);
        Assert.Same(activity, narrowCompensate);
        Assert.Same(activity, narrowCombined);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTRACTS", "deep-courier-infrastructure-marker-boundaries")]
    public void CourierMarkers_DistinguishOperationalContractsFromTheRoutingSlipMessage()
    {
        const string activityContractAttribute = "ViciOne.ServiceBus.Advanced.ActivityContractAttribute";
        const string activityMessageAttribute = "ViciOne.ServiceBus.Advanced.ActivityMessageAttribute";

        Assert.True(HasDeclaredAttribute(typeof(IExecuteActivity<>), activityContractAttribute));
        Assert.True(HasDeclaredAttribute(typeof(ICompensateActivity<>), activityContractAttribute));
        Assert.False(HasDeclaredAttribute(typeof(ViciOne.ServiceBus.Courier.IActivity<,>), activityContractAttribute));

        Assert.True(HasDeclaredAttribute(typeof(IRoutingSlip), activityMessageAttribute));
        Assert.False(HasDeclaredAttribute(typeof(ActivityMessageContract), activityMessageAttribute));
        Assert.False(HasDeclaredAttribute(typeof(IActivityLog), activityMessageAttribute));
        Assert.False(HasDeclaredAttribute(typeof(ICompensateLog), activityMessageAttribute));
        Assert.False(HasDeclaredAttribute(typeof(IActivityException), activityMessageAttribute));
    }

    [Theory]
    [InlineData(LeafContract.Activity)]
    [InlineData(LeafContract.ActivityLog)]
    [InlineData(LeafContract.CompensateLog)]
    [InlineData(LeafContract.ActivityException)]
    [RequirementCoverage("REQ-VSB-COURIER-CONTRACTS", "deep-leaf-message-exact-getter-only-shapes")]
    public void LeafMessageContracts_ExposeExactGetterOnlyNonNullShapes(LeafContract contract)
    {
        (Type Type, PropertyShape[] Properties) shape = contract switch
        {
            LeafContract.Activity => (typeof(ActivityMessageContract),
            [
                new("Name", typeof(string)),
                new("Address", typeof(Uri)),
                new("Arguments", typeof(IReadOnlyDictionary<string, object>)),
            ]),
            LeafContract.ActivityLog => (typeof(IActivityLog),
            [
                new("ExecutionId", typeof(Guid)),
                new("Name", typeof(string)),
                new("Timestamp", typeof(DateTimeOffset)),
                new("Duration", typeof(TimeSpan)),
                new("Host", typeof(HostInfo)),
            ]),
            LeafContract.CompensateLog => (typeof(ICompensateLog),
            [
                new("ExecutionId", typeof(Guid)),
                new("Address", typeof(Uri)),
                new("Data", typeof(IReadOnlyDictionary<string, object>)),
            ]),
            LeafContract.ActivityException => (typeof(IActivityException),
            [
                new("ExecutionId", typeof(Guid)),
                new("Timestamp", typeof(DateTimeOffset)),
                new("Elapsed", typeof(TimeSpan)),
                new("Name", typeof(string)),
                new("Host", typeof(HostInfo)),
                new("ExceptionInfo", typeof(ExceptionInfo)),
            ]),
            _ => throw new ArgumentOutOfRangeException(nameof(contract), contract, null),
        };

        AssertExactGetterOnlyShape(shape.Type, shape.Properties);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTRACTS", "deep-routing-slip-exact-getter-only-message-shape")]
    public void RoutingSlipContract_ExposesEveryStateCollectionAsAGetterOnlyNonNullView()
    {
        AssertExactGetterOnlyShape(
            typeof(IRoutingSlip),
            new PropertyShape("TrackingNumber", typeof(Guid)),
            new PropertyShape("CreateTimestamp", typeof(DateTimeOffset)),
            new PropertyShape("Itinerary", typeof(IReadOnlyList<ActivityMessageContract>)),
            new PropertyShape("ActivityLogs", typeof(IReadOnlyList<IActivityLog>)),
            new PropertyShape("CompensateLogs", typeof(IReadOnlyList<ICompensateLog>)),
            new PropertyShape("Variables", typeof(IReadOnlyDictionary<string, object>)),
            new PropertyShape("ActivityExceptions", typeof(IReadOnlyList<IActivityException>)),
            new PropertyShape("Subscriptions", typeof(IReadOnlyList<ISubscription>)));
    }

    private static void AssertExactGetterOnlyShape(Type contract, params PropertyShape[] expected)
    {
        PropertyInfo[] properties = contract
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();
        PropertyShape[] orderedExpected = expected
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();
        var nullability = new NullabilityInfoContext();

        Assert.Equal(orderedExpected.Select(property => property.Name), properties.Select(property => property.Name));
        Assert.Equal(orderedExpected.Select(property => property.Type), properties.Select(property => property.PropertyType));

        foreach (PropertyInfo property in properties)
        {
            MethodInfo getter = Assert.IsAssignableFrom<MethodInfo>(property.GetMethod);
            Assert.True(getter.IsPublic);
            Assert.False(getter.IsStatic);
            Assert.Null(property.SetMethod);

            if (!property.PropertyType.IsValueType)
                Assert.Equal(NullabilityState.NotNull, nullability.Create(property).ReadState);
        }

        MethodInfo[] accessors = properties.Select(property => property.GetMethod!).ToArray();
        Assert.Equal(
            accessors.OrderBy(method => method.Name, StringComparer.Ordinal),
            contract.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(method => method.Name, StringComparer.Ordinal));
        Assert.Empty(contract.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    private static bool HasDeclaredAttribute(Type contract, string attributeTypeName) =>
        contract.CustomAttributes.Any(attribute =>
            string.Equals(attribute.AttributeType.FullName, attributeTypeName, StringComparison.Ordinal));

    public enum LeafContract
    {
        Activity,
        ActivityLog,
        CompensateLog,
        ActivityException,
    }

    private sealed class BroadActivity : ViciOne.ServiceBus.Courier.IActivity<object, object>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<object> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<object> context) => throw new NotSupportedException();
    }

    private sealed record PropertyShape(string Name, Type Type);
}
