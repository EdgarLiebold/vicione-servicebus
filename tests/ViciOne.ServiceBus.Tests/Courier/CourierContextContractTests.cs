using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Middleware.Timeout;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierContextContractTests
{
    private static readonly DateTimeOffset ActivityStartedAt = new(2042, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "context-clock-identity-state-and-decorators-are-consistent")]
    public void CourierContext_UsesItsInjectedClockAndDecoratorsPreserveExactActivityState()
    {
        var clock = new FakeTimeProvider(ActivityStartedAt);
        var builder = new RoutingSlipBuilder(Guid.Parse("95baf29e-6842-4052-8c00-ddc40a774aaa"), clock);
        builder.SetVariable("tenant", "north");
        IRoutingSlip routingSlip = builder.Build();
        ConsumeContext<IRoutingSlip> consumeContext = InMemoryOutboxTestContextFactory.Create(
            routingSlip,
            TestContext.Current.CancellationToken);
        consumeContext.SetTimeProvider(clock);
        var owner = new TestCourierContext(consumeContext);
        ActivityContext original = owner;
        clock.Advance(TimeSpan.FromSeconds(17));

        ICourierContext[] contexts =
        [
            owner,
            new TestCourierContextProxy(owner),
            new TestCourierContextScope(owner),
            new TestOutboxCourierContextProxy(owner),
            new TestTimeoutCourierContextProxy(owner, TimeSpan.FromMinutes(1), CancellationToken.None),
        ];

        foreach (ICourierContext context in contexts)
        {
            ActivityContext activity = context;
            Assert.Equal(ActivityStartedAt, activity.Timestamp);
            Assert.Equal(TimeSpan.FromSeconds(17), activity.Elapsed);
            Assert.Equal(routingSlip.TrackingNumber, activity.TrackingNumber);
            Assert.Equal(original.ExecutionId, activity.ExecutionId);
            Assert.NotEqual(Guid.Empty, activity.ExecutionId);
            Assert.Equal("TestActivity", activity.ActivityName);
            Assert.Equal("north", activity.Variables["tenant"]);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "activity-variable-accessors-cover-reference-value-default-and-boundary-contracts")]
    public void ActivityVariables_PreserveReferenceAndValueContractsAndRejectInvalidInputs()
    {
        ActivityContext context = DispatchProxy.Create<ActivityContext, ActivityVariableContextProxy>();
        var proxy = (ActivityVariableContextProxy)(object)context;
        proxy.Variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["tenant"] = "north",
            ["attempt"] = 27,
        };
        proxy.SerializerContext = DispatchProxy.Create<SerializerContext, VariableSerializerContextProxy>();

        Assert.Equal("north", context.GetVariable<string>("tenant"));
        Assert.Equal(27, context.GetVariable<int>("attempt"));
        Assert.Equal("fallback", context.GetVariable("missing-reference", "fallback"));
        Assert.Equal(73, context.GetVariable<int>("missing-value", 73));

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            ActivityContextVariableExtensions.GetVariable<string>(null!, "tenant")).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            ActivityContextVariableExtensions.GetVariable<int>(null!, "attempt")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
            context.GetVariable<string>(" ")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
            context.GetVariable<int>(string.Empty)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "decorators-require-an-underlying-courier-context")]
    public void CourierContextDecorators_RejectAMissingUnderlyingContext()
    {
        Assert.Equal("courierContext", Assert.Throws<ArgumentNullException>(() => new TestCourierContextProxy(null!)).ParamName);
        Assert.Equal("courierContext", Assert.Throws<ArgumentNullException>(() => new TestCourierContextScope(null!)).ParamName);
        Assert.Equal("courierContext", Assert.Throws<ArgumentNullException>(() => new TestOutboxCourierContextProxy(null!)).ParamName);
        Assert.Equal("courierContext", Assert.Throws<ArgumentNullException>(() =>
            new TestTimeoutCourierContextProxy(null!, TimeSpan.FromSeconds(1), CancellationToken.None)).ParamName);
    }

    private sealed class TestCourierContext(ConsumeContext<IRoutingSlip> context) : BaseCourierContext(context)
    {
        public override string ActivityName => "TestActivity";
    }

    private sealed class TestCourierContextProxy(ICourierContext context) : CourierContextProxy(context);

    private sealed class TestCourierContextScope(ICourierContext context) : CourierContextScope(context);

    private sealed class TestOutboxCourierContextProxy(ICourierContext context) : InMemoryOutboxCourierContextProxy(context);

    private sealed class TestTimeoutCourierContextProxy(
        ICourierContext context,
        TimeSpan timeout,
        CancellationToken cancellationToken) : TimeoutCourierContextProxy(context, timeout, cancellationToken);

    private class ActivityVariableContextProxy : DispatchProxy
    {
        public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;

        public SerializerContext SerializerContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Variables" => Variables,
            "get_SerializerContext" => SerializerContext,
            _ => throw new NotSupportedException($"Unexpected activity-context member: {targetMethod?.Name}"),
        };
    }

    private class VariableSerializerContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IObjectDeserializer.DeserializeObject))
                throw new NotSupportedException($"Unexpected serializer-context member: {targetMethod?.Name}");

            object? value = args?[0];
            object? defaultValue = args?[1];
            if (value is null)
                return defaultValue;

            Type targetType = targetMethod.GetGenericArguments()[0];
            return targetType.IsInstanceOfType(value)
                ? value
                : Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
