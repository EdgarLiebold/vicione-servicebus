using System.Reflection;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipEventAccessorTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(2);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACCESSORS", "reference-and-value-accessors-cover-every-event-contract")]
    public void EventAccessors_ReadReferenceAndValueDataAcrossEveryLifecycleContract()
    {
        Guid trackingNumber = NewId.NextGuid();
        Guid executionId = NewId.NextGuid();
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "north",
            ["count"] = 27,
        };
        ExceptionInfo exception = new FaultExceptionInfo(new InvalidOperationException("expected"));
        ActivityException activityException = new RoutingSlipActivityException(
            "ChargeCard", HostMetadataCache.Host, executionId, Timestamp, Duration, exception);

        var builder = new RoutingSlipBuilder(trackingNumber, new Microsoft.Extensions.Time.Testing.FakeTimeProvider(Timestamp));
        builder.SetVariables(values);
        ConsumeContext<RoutingSlip> routingSlip = CreateContext<RoutingSlip>(builder.Build());
        ConsumeContext<RoutingSlipActivityCompensated> compensated = CreateContext<RoutingSlipActivityCompensated>(
            new RoutingSlipActivityCompensatedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, values, values));
        ConsumeContext<RoutingSlipActivityCompensationFailed> compensationFailed = CreateContext<RoutingSlipActivityCompensationFailed>(
            new RoutingSlipActivityCompensationFailedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, exception, values, values));
        ConsumeContext<RoutingSlipActivityCompleted> activityCompleted = CreateContext<RoutingSlipActivityCompleted>(
            new RoutingSlipActivityCompletedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, values, values, values));
        ConsumeContext<RoutingSlipActivityFaulted> activityFaulted = CreateContext<RoutingSlipActivityFaulted>(
            new RoutingSlipActivityFaultedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, exception, values, values));
        ConsumeContext<RoutingSlipCompensationFailed> slipCompensationFailed = CreateContext<RoutingSlipCompensationFailed>(
            new RoutingSlipCompensationFailedMessage(HostMetadataCache.Host, trackingNumber, Timestamp, Duration, exception, values));
        ConsumeContext<RoutingSlipCompleted> completed = CreateContext<RoutingSlipCompleted>(
            new RoutingSlipCompletedMessage(trackingNumber, Timestamp, Duration, values));
        ConsumeContext<RoutingSlipFaulted> faulted = CreateContext<RoutingSlipFaulted>(
            new RoutingSlipFaultedMessage(trackingNumber, Timestamp, Duration, [activityException], values));
        ConsumeContext<RoutingSlipTerminated> terminated = CreateContext<RoutingSlipTerminated>(
            new RoutingSlipTerminatedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, values, []));

        AssertValues(routingSlip.GetVariable<string>("name"), routingSlip.GetVariable<int>("count"));
        AssertValues(compensated.GetVariable<string>("name"), compensated.GetVariable<int>("count"));
        AssertValues(compensated.GetResult<string>("name"), compensated.GetResult<int>("count"));
        AssertValues(compensationFailed.GetVariable<string>("name"), compensationFailed.GetVariable<int>("count"));
        AssertValues(compensationFailed.GetResult<string>("name"), compensationFailed.GetResult<int>("count"));
        AssertValues(activityCompleted.GetVariable<string>("name"), activityCompleted.GetVariable<int>("count"));
        AssertValues(activityCompleted.GetArgument<string>("name"), activityCompleted.GetArgument<int>("count"));
        AssertValues(activityCompleted.GetResult<string>("name"), activityCompleted.GetResult<int>("count"));
        AssertValues(activityFaulted.GetVariable<string>("name"), activityFaulted.GetVariable<int>("count"));
        AssertValues(activityFaulted.GetArgument<string>("name"), activityFaulted.GetArgument<int>("count"));
        AssertValues(slipCompensationFailed.GetVariable<string>("name"), slipCompensationFailed.GetVariable<int>("count"));
        AssertValues(completed.GetVariable<string>("name"), completed.GetVariable<int>("count"));
        AssertValues(faulted.GetVariable<string>("name"), faulted.GetVariable<int>("count"));
        AssertValues(terminated.GetVariable<string>("name"), terminated.GetVariable<int>("count"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACCESSORS", "activity-arguments-override-slip-variables-and-missing-values-use-defaults")]
    public void ActivityAccessors_ApplyDocumentedPrecedenceAndDefaults()
    {
        Guid trackingNumber = NewId.NextGuid();
        Guid executionId = NewId.NextGuid();
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["shared"] = "variable",
        };
        var arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["shared"] = "argument",
        };
        ConsumeContext<RoutingSlipActivityCompleted> context = CreateContext<RoutingSlipActivityCompleted>(
            new RoutingSlipActivityCompletedMessage(
                HostMetadataCache.Host,
                trackingNumber,
                "ChargeCard",
                executionId,
                Timestamp,
                Duration,
                variables,
                arguments,
                new Dictionary<string, object>()));

        Assert.Equal("argument", context.GetArgument<string>("shared"));
        Assert.Equal("fallback", context.GetArgument("missing", "fallback"));
        Assert.Equal(73, context.GetArgument<int>("missing-count", 73));
        Assert.Equal("fallback", context.GetVariable("missing", "fallback"));
        Assert.Equal(73, context.GetResult<int>("missing-count", 73));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACCESSORS", "revised-event-has-symmetric-variable-accessors")]
    public void RevisedEvent_ExposesReferenceAndValueVariableAccessors()
    {
        Guid trackingNumber = NewId.NextGuid();
        Guid executionId = NewId.NextGuid();
        var values = new Dictionary<string, object>
        {
            ["name"] = "north",
            ["count"] = 27,
        };
        ConsumeContext<RoutingSlipRevised> context = CreateContext<RoutingSlipRevised>(
            new RoutingSlipRevisedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, values, [], []));

        MethodInfo[] accessors = typeof(RoutingSlipEventExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(RoutingSlipEventExtensions.GetVariable))
            .Where(method => method.GetParameters()[0].ParameterType == typeof(ConsumeContext<RoutingSlipRevised>))
            .ToArray();

        Assert.Equal(2, accessors.Length);
        Assert.Equal("north", InvokeAccessor<string>(accessors, context, "name"));
        Assert.Equal(27, InvokeAccessor<int>(accessors, context, "count"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACCESSORS", "null-context-and-invalid-key-rejected-consistently")]
    public void EventAccessors_RejectNullContextsAndInvalidKeys()
    {
        ConsumeContext<RoutingSlipCompleted>? missing = null;
        ConsumeContext<RoutingSlipCompleted> context = CreateContext<RoutingSlipCompleted>(
            new RoutingSlipCompletedMessage(NewId.NextGuid(), Timestamp, Duration, new Dictionary<string, object>()));

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => missing!.GetVariable<string>("name")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => context.GetVariable<string>(" ")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => context.GetVariable<int>("")).ParamName);
    }

    private static void AssertValues(string? referenceValue, int? value)
    {
        Assert.Equal("north", referenceValue);
        Assert.Equal(27, value);
    }

    private static T? InvokeAccessor<T>(IEnumerable<MethodInfo> accessors, ConsumeContext<RoutingSlipRevised> context, string key)
    {
        MethodInfo accessor = accessors.Single(method =>
        {
            GenericParameterAttributes attributes = method.GetGenericArguments()[0].GenericParameterAttributes;
            bool acceptsValueType = (attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            return acceptsValueType == typeof(T).IsValueType;
        });
        return (T?)accessor.MakeGenericMethod(typeof(T)).Invoke(null, [context, key, default(T)]);
    }

    private static ConsumeContext<T> CreateContext<T>(T message)
        where T : class
    {
        TestConsumeContext<T> context = DispatchProxy.Create<TestConsumeContext<T>, ConsumeContextProxy<T>>();
        var proxy = (ConsumeContextProxy<T>)(object)context;
        proxy.Message = message;
        proxy.SerializerContext = DispatchProxy.Create<SerializerContext, SerializerContextProxy>();
        return context;
    }

    private interface TestConsumeContext<out T> : ConsumeContext<T>, ConsumeContext
        where T : class;

    private class ConsumeContextProxy<T> : DispatchProxy
        where T : class
    {
        public T Message { get; set; } = null!;

        public SerializerContext SerializerContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Message" => Message,
            "get_SerializerContext" => SerializerContext,
            _ => throw new NotSupportedException($"Unexpected consume-context member: {targetMethod?.Name}"),
        };
    }

    private class SerializerContextProxy : DispatchProxy
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
