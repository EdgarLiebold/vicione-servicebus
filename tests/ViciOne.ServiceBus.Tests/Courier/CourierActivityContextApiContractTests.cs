using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierActivityContextApiContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "api-layering-covariance-and-nullable-result-lifecycle")]
    public void ActivityContracts_ExposeLayeredCovariantApiAndNullableLifecycleResults()
    {
        Assert.True(typeof(ConsumeContext).IsAssignableFrom(typeof(ActivityContext)));
        Assert.True(typeof(ActivityContext).IsAssignableFrom(typeof(ExecuteContext)));
        Assert.True(typeof(ActivityContext).IsAssignableFrom(typeof(CompensateContext)));
        Assert.True(typeof(ExecuteContext<TestArguments>).IsAssignableFrom(typeof(ExecuteActivityContext<TestActivity, TestArguments>)));
        Assert.True(typeof(CompensateContext<TestLog>).IsAssignableFrom(typeof(CompensateActivityContext<TestActivity, TestLog>)));
        Assert.True(typeof(ActivityContext).IsAssignableFrom(typeof(ICourierContext)));
        Assert.True(typeof(ConsumeContext<IRoutingSlip>).IsAssignableFrom(typeof(ICourierContext)));

        AssertCovariantReferenceParameters(typeof(ExecuteContext<>), 1);
        AssertCovariantReferenceParameters(typeof(ExecuteActivityContext<>), 1);
        AssertCovariantReferenceParameters(typeof(ExecuteActivityContext<,>), 2);
        AssertCovariantReferenceParameters(typeof(CompensateContext<>), 1);
        AssertCovariantReferenceParameters(typeof(CompensateActivityContext<>), 1);
        AssertCovariantReferenceParameters(typeof(CompensateActivityContext<,>), 2);

        var nullability = new NullabilityInfoContext();
        AssertNullableReadWriteProperty(nullability, typeof(ExecuteContext), nameof(ExecuteContext.Result));
        AssertNullableReadWriteProperty(nullability, typeof(CompensateContext), nameof(CompensateContext.Result));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "activity-consumed-async-signature")]
    public void ActivityConsumedNotification_UsesAsyncSuffixAndOptionalTrailingCancellationToken()
    {
        MethodInfo method = typeof(ActivityContext).GetMethod(
                nameof(ActivityContext.NotifyActivityConsumedAsync),
                [typeof(TimeSpan), typeof(string), typeof(CancellationToken)])
            ?? throw new InvalidOperationException("The activity-consumed notification method is missing.");

        Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
        Assert.Equal(typeof(Task), method.ReturnType);

        ParameterInfo[] parameters = method.GetParameters();
        Assert.Collection(
            parameters,
            duration =>
            {
                Assert.Equal("duration", duration.Name);
                Assert.Equal(typeof(TimeSpan), duration.ParameterType);
            },
            consumerType =>
            {
                Assert.Equal("consumerType", consumerType.Name);
                Assert.Equal(typeof(string), consumerType.ParameterType);
            },
            cancellationToken =>
            {
                Assert.Equal("cancellationToken", cancellationToken.Name);
                Assert.Equal(typeof(CancellationToken), cancellationToken.ParameterType);
                Assert.True(cancellationToken.HasDefaultValue);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "decorators-forward-message-state-and-notification")]
    public void CourierDecorators_ForwardExactActivityStateMessageAndNotification()
    {
        ICourierContext source = DispatchProxy.Create<ICourierContext, RecordingCourierContextProxy>();
        var recording = (RecordingCourierContextProxy)(object)source;
        recording.Configure();

        ICourierContext[] decorators =
        [
            new TestCourierContextProxy(source),
            new TestCourierContextScope(source),
        ];
        var duration = TimeSpan.FromMilliseconds(375);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        foreach (ICourierContext decorator in decorators)
        {
            ActivityContext activity = decorator;
            Assert.Equal(recording.Timestamp, activity.Timestamp);
            Assert.Equal(recording.Elapsed, activity.Elapsed);
            Assert.Equal(recording.TrackingNumber, activity.TrackingNumber);
            Assert.Equal(recording.ExecutionId, activity.ExecutionId);
            Assert.Equal(recording.ActivityName, activity.ActivityName);
            Assert.Same(recording.Variables, activity.Variables);
            Assert.Same(recording.Message, decorator.Message);
            Assert.Same(
                recording.NotificationTask,
                activity.NotifyActivityConsumedAsync(duration, "inventory-activity", cancellationToken));
        }

        Assert.Collection(
            recording.NotificationCalls,
            call => AssertNotification(call, duration, "inventory-activity", cancellationToken),
            call => AssertNotification(call, duration, "inventory-activity", cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "base-context-null-admission")]
    public void BaseCourierContext_RejectsMissingContextBeforeBaseInitialization()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new GuardProbeCourierContext(null!));

        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTEXT", "scope-payloads-preserve-courier-state")]
    public void CourierContextScope_ExposesScopedPayloadsAndPreservesExactCourierState()
    {
        ICourierContext source = DispatchProxy.Create<ICourierContext, RecordingCourierContextProxy>();
        var recording = (RecordingCourierContextProxy)(object)source;
        recording.Configure();
        var payload = new ScopedPayload("north-warehouse");
        var scope = new TestCourierContextScope(source, payload);
        ActivityContext activity = scope;
        var payloadFactoryInvoked = false;

        Assert.True(scope.HasPayloadType(typeof(ScopedPayload)));
        Assert.True(scope.TryGetPayload(out ScopedPayload? selectedPayload));
        Assert.Same(payload, selectedPayload);
        Assert.Same(payload, scope.GetOrAddPayload(() =>
        {
            payloadFactoryInvoked = true;
            return new ScopedPayload("unexpected");
        }));
        Assert.False(payloadFactoryInvoked);

        Assert.Equal(recording.Timestamp, activity.Timestamp);
        Assert.Equal(recording.Elapsed, activity.Elapsed);
        Assert.Equal(recording.TrackingNumber, activity.TrackingNumber);
        Assert.Equal(recording.ExecutionId, activity.ExecutionId);
        Assert.Equal(recording.ActivityName, activity.ActivityName);
        Assert.Same(recording.Variables, activity.Variables);
        Assert.Same(recording.Message, scope.Message);
    }

    [Fact]
    [RequirementCoverage(
        "REQ-VSB-COURIER-CONTEXT",
        "base-consumed-notification-forwards-context-duration-consumer-and-token")]
    public void BaseCourierContext_NotifyActivityConsumedAsync_ForwardsEveryArgumentAndReturnedTask()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2044, 3, 4, 5, 6, 7, TimeSpan.Zero));
        var builder = new RoutingSlipBuilder(Guid.Parse("e74375f2-6982-4297-8ec7-20520bf4410c"), clock);
        ConsumeContext<IRoutingSlip> source = InMemoryOutboxTestContextFactory.Create(
            builder.Build(),
            TestContext.Current.CancellationToken);
        source.SetTimeProvider(clock);
        var recording = new RecordingNotificationConsumeContext(source);
        var context = new GuardProbeCourierContext(recording);
        ActivityContext activity = context;
        TimeSpan duration = TimeSpan.FromMilliseconds(417);
        using var cancellation = new CancellationTokenSource();

        Task returnedTask = activity.NotifyActivityConsumedAsync(
            duration,
            "direct-base-activity",
            cancellation.Token);

        Assert.Same(recording.NotificationTask, returnedTask);
        BaseNotificationCall call = Assert.Single(recording.NotificationCalls);
        Assert.Same(context, call.MessageContext);
        Assert.Equal(duration, call.Duration);
        Assert.Equal("direct-base-activity", call.ConsumerType);
        Assert.Equal(cancellation.Token, call.CancellationToken);
    }

    private static void AssertCovariantReferenceParameters(Type genericType, int expectedCount)
    {
        Type[] parameters = genericType.GetGenericArguments();
        Assert.Equal(expectedCount, parameters.Length);

        foreach (Type parameter in parameters)
        {
            GenericParameterAttributes attributes = parameter.GenericParameterAttributes;
            Assert.Equal(GenericParameterAttributes.Covariant, attributes & GenericParameterAttributes.VarianceMask);
            Assert.Equal(
                GenericParameterAttributes.ReferenceTypeConstraint,
                attributes & GenericParameterAttributes.ReferenceTypeConstraint);
        }
    }

    private static void AssertNullableReadWriteProperty(NullabilityInfoContext nullability, Type declaringType, string propertyName)
    {
        PropertyInfo property = declaringType.GetProperty(propertyName)
            ?? throw new InvalidOperationException($"The {declaringType.Name}.{propertyName} property is missing.");
        NullabilityInfo info = nullability.Create(property);

        Assert.Equal(NullabilityState.Nullable, info.ReadState);
        Assert.Equal(NullabilityState.Nullable, info.WriteState);
        Assert.NotNull(property.GetMethod);
        Assert.NotNull(property.SetMethod);
    }

    private static void AssertNotification(
        NotificationCall call,
        TimeSpan expectedDuration,
        string expectedConsumerType,
        CancellationToken expectedCancellationToken)
    {
        Assert.Equal(expectedDuration, call.Duration);
        Assert.Equal(expectedConsumerType, call.ConsumerType);
        Assert.Equal(expectedCancellationToken, call.CancellationToken);
    }

    private sealed class GuardProbeCourierContext(ConsumeContext<IRoutingSlip> context) : BaseCourierContext(context)
    {
        public override string ActivityName => "GuardProbe";
    }

    private sealed class TestCourierContextProxy(ICourierContext context) : CourierContextProxy(context);

    private sealed class TestCourierContextScope(ICourierContext context, params object[] payloads) :
        CourierContextScope(context, payloads);

    private sealed record TestArguments;

    private sealed record TestLog;

    private sealed record ScopedPayload(string Value);

    private sealed class TestActivity
    {
    }

    private sealed record NotificationCall(TimeSpan Duration, string ConsumerType, CancellationToken CancellationToken);

    private sealed record BaseNotificationCall(
        object MessageContext,
        TimeSpan Duration,
        string ConsumerType,
        CancellationToken CancellationToken);

    private sealed class RecordingNotificationConsumeContext(ConsumeContext<IRoutingSlip> context) :
        ConsumeContextProxy<IRoutingSlip>(context)
    {
        public Task NotificationTask { get; } = Task.FromResult(new object());

        public List<BaseNotificationCall> NotificationCalls { get; } = [];

        public override Task NotifyConsumedAsync<T>(
            ConsumeContext<T> context,
            TimeSpan duration,
            string consumerType,
            CancellationToken cancellationToken = default)
        {
            NotificationCalls.Add(new BaseNotificationCall(context, duration, consumerType, cancellationToken));
            return NotificationTask;
        }
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The API contract test unexpectedly invoked {targetMethod?.Name}.");
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        public IPublishEndpointProvider? PublishEndpointProvider { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_PublishEndpointProvider")
                return PublishEndpointProvider;

            throw new InvalidOperationException($"The receive context test unexpectedly invoked {targetMethod?.Name}.");
        }
    }

    private class RecordingCourierContextProxy : DispatchProxy
    {
        public string ActivityName { get; } = "InventoryReservation";

        public TimeSpan Elapsed { get; } = TimeSpan.FromMilliseconds(125);

        public Guid ExecutionId { get; } = Guid.Parse("fb98f53f-a236-49d9-a66d-c2d269bf9068");

        public IRoutingSlip Message { get; private set; } = null!;

        public Task NotificationTask { get; } = Task.CompletedTask;

        public List<NotificationCall> NotificationCalls { get; } = [];

        public ReceiveContext ReceiveContext { get; private set; } = null!;

        public SerializerContext SerializerContext { get; private set; } = null!;

        public DateTimeOffset Timestamp { get; } = new(2043, 2, 3, 4, 5, 6, TimeSpan.Zero);

        public Guid TrackingNumber { get; } = Guid.Parse("a5a6d84e-6580-40a2-be55-eb28a6168c6a");

        public IReadOnlyDictionary<string, object> Variables { get; } =
            new Dictionary<string, object> { ["warehouse"] = "north" };

        public void Configure()
        {
            Message = DispatchProxy.Create<IRoutingSlip, UnexpectedInvocationProxy>();
            IPublishEndpointProvider publishEndpointProvider =
                DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();
            ReceiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
            ((ReceiveContextProxy)(object)ReceiveContext).PublishEndpointProvider = publishEndpointProvider;
            SerializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("The Courier proxy supplied no method metadata.");
            return method.Name switch
            {
                "get_ActivityName" => ActivityName,
                "get_Elapsed" => Elapsed,
                "get_ExecutionId" => ExecutionId,
                "get_Message" => Message,
                "get_ReceiveContext" => ReceiveContext,
                "get_SerializerContext" => SerializerContext,
                "get_Timestamp" => Timestamp,
                "get_TrackingNumber" => TrackingNumber,
                "get_Variables" => Variables,
                nameof(ActivityContext.NotifyActivityConsumedAsync) => RecordNotificationAsync(args),
                _ => throw new InvalidOperationException($"The Courier context test has no behavior for {method.Name}."),
            };
        }

        private Task RecordNotificationAsync(object?[]? arguments)
        {
            object?[] values = arguments ?? throw new InvalidOperationException("The notification arguments are missing.");
            NotificationCalls.Add(new NotificationCall(
                (TimeSpan)values[0]!,
                (string)values[1]!,
                (CancellationToken)values[2]!));
            return NotificationTask;
        }
    }
}
