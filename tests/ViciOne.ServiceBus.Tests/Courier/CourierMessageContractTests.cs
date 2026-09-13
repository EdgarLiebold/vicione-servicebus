using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierMessageContractTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 9, 12, 8, 30, 0, TimeSpan.Zero);
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(3);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTRACTS", "public-collections-declare-read-only-intent")]
    public void PublicCollectionProperties_ExposeOnlyReadOnlyContracts()
    {
        AssertPropertyType<IActivity>(nameof(IActivity.Arguments), typeof(IReadOnlyDictionary<string, object>));
        AssertPropertyType<ICompensateLog>(nameof(ICompensateLog.Data), typeof(IReadOnlyDictionary<string, object>));

        AssertPropertyType<IRoutingSlip>(nameof(IRoutingSlip.Itinerary), typeof(IReadOnlyList<IActivity>));
        AssertPropertyType<IRoutingSlip>(nameof(IRoutingSlip.ActivityLogs), typeof(IReadOnlyList<IActivityLog>));
        AssertPropertyType<IRoutingSlip>(nameof(IRoutingSlip.CompensateLogs), typeof(IReadOnlyList<ICompensateLog>));
        AssertPropertyType<IRoutingSlip>(nameof(IRoutingSlip.Variables), typeof(IReadOnlyDictionary<string, object>));
        AssertPropertyType<IRoutingSlip>(nameof(IRoutingSlip.ActivityExceptions), typeof(IReadOnlyList<IActivityException>));
        AssertPropertyType<IRoutingSlip>(nameof(IRoutingSlip.Subscriptions), typeof(IReadOnlyList<ISubscription>));

        AssertDictionaryProperties<IRoutingSlipActivityCompensated>(nameof(IRoutingSlipActivityCompensated.Data), nameof(IRoutingSlipActivityCompensated.Variables));
        AssertDictionaryProperties<IRoutingSlipActivityCompensationFailed>(nameof(IRoutingSlipActivityCompensationFailed.Data), nameof(IRoutingSlipActivityCompensationFailed.Variables));
        AssertDictionaryProperties<IRoutingSlipActivityCompleted>(nameof(IRoutingSlipActivityCompleted.Arguments), nameof(IRoutingSlipActivityCompleted.Data), nameof(IRoutingSlipActivityCompleted.Variables));
        AssertDictionaryProperties<IRoutingSlipActivityFaulted>(nameof(IRoutingSlipActivityFaulted.Arguments), nameof(IRoutingSlipActivityFaulted.Variables));
        AssertDictionaryProperties<IRoutingSlipCompensationFailed>(nameof(IRoutingSlipCompensationFailed.Variables));
        AssertDictionaryProperties<IRoutingSlipCompleted>(nameof(IRoutingSlipCompleted.Variables));
        AssertDictionaryProperties<IRoutingSlipFaulted>(nameof(IRoutingSlipFaulted.Variables));
        AssertDictionaryProperties<IRoutingSlipRevised>(nameof(IRoutingSlipRevised.Variables));
        AssertDictionaryProperties<IRoutingSlipTerminated>(nameof(IRoutingSlipTerminated.Variables));

        AssertPropertyType<IRoutingSlipFaulted>(nameof(IRoutingSlipFaulted.ActivityExceptions), typeof(IReadOnlyList<IActivityException>));
        AssertPropertyType<IRoutingSlipRevised>(nameof(IRoutingSlipRevised.Itinerary), typeof(IReadOnlyList<IActivity>));
        AssertPropertyType<IRoutingSlipRevised>(nameof(IRoutingSlipRevised.DiscardedItinerary), typeof(IReadOnlyList<IActivity>));
        AssertPropertyType<IRoutingSlipTerminated>(nameof(IRoutingSlipTerminated.DiscardedItinerary), typeof(IReadOnlyList<IActivity>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "lifecycle-messages-own-detached-read-only-snapshots")]
    public void LifecycleMessages_DetachEveryCollectionFromCallerOwnedState()
    {
        Guid trackingNumber = NewId.NextGuid();
        Guid executionId = NewId.NextGuid();
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["tenant"] = "north" };
        var arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["order"] = 42 };
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["receipt"] = "original" };
        var activityArguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["input"] = "original" };
        var itinerary = new List<IActivity>
        {
            new MutableActivity("ChargeCard", new Uri("loopback://localhost/charge"), activityArguments),
        };
        var discarded = new List<IActivity>(itinerary);
        var exception = ValidActivityException(executionId);
        var exceptions = new List<IActivityException> { exception };
        ExceptionInfo exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("expected"));

        object[] messages =
        [
            new RoutingSlipActivityCompensatedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, variables, data),
            new RoutingSlipActivityCompensationFailedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, exceptionInfo, variables, data),
            new RoutingSlipActivityCompletedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, variables, arguments, data),
            new RoutingSlipActivityFaultedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, exceptionInfo, variables, arguments),
            new RoutingSlipCompensationFailedMessage(HostMetadataCache.Host, trackingNumber, Timestamp, Duration, exceptionInfo, variables),
            new RoutingSlipCompletedMessage(trackingNumber, Timestamp, Duration, variables),
            new RoutingSlipFaultedMessage(trackingNumber, Timestamp, Duration, exceptions, variables),
            new RoutingSlipRevisedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, variables, itinerary, discarded),
            new RoutingSlipTerminatedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, variables, discarded),
        ];

        variables["tenant"] = "south";
        arguments["order"] = 99;
        data["receipt"] = "mutated";
        activityArguments["input"] = "mutated";
        itinerary.Clear();
        discarded.Clear();
        exceptions.Clear();

        foreach (object message in messages)
        {
            if (TryGetDictionary(message, "Variables", out IReadOnlyDictionary<string, object>? messageVariables))
            {
                Assert.NotNull(messageVariables);
                Assert.Equal("north", messageVariables["tenant"]);
                AssertReadOnlyDictionary(messageVariables);
            }
        }

        IRoutingSlipActivityCompleted completed = Assert.IsAssignableFrom<IRoutingSlipActivityCompleted>(messages[2]);
        Assert.Equal(42, completed.Arguments["order"]);
        Assert.Equal("original", completed.Data["receipt"]);
        AssertReadOnlyDictionary(Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(completed.Arguments));
        AssertReadOnlyDictionary(Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(completed.Data));

        IRoutingSlipFaulted faulted = Assert.IsAssignableFrom<IRoutingSlipFaulted>(messages[6]);
        Assert.Single(faulted.ActivityExceptions);
        AssertReadOnlyCollection(faulted.ActivityExceptions);

        IRoutingSlipRevised revised = Assert.IsAssignableFrom<IRoutingSlipRevised>(messages[7]);
        Assert.Equal("original", Assert.Single(revised.Itinerary).Arguments["input"]);
        Assert.Equal("original", Assert.Single(revised.DiscardedItinerary).Arguments["input"]);
        AssertReadOnlyCollection(revised.Itinerary);
        AssertReadOnlyCollection(revised.DiscardedItinerary);

        IRoutingSlipTerminated terminated = Assert.IsAssignableFrom<IRoutingSlipTerminated>(messages[8]);
        Assert.Equal("original", Assert.Single(terminated.DiscardedItinerary).Arguments["input"]);
        AssertReadOnlyCollection(terminated.DiscardedItinerary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "all-lifecycle-constructors-enforce-common-identities-and-time-bounds")]
    public void LifecycleMessageConstructors_RejectInvalidIdentitiesNamesDurationsAndRequiredObjects()
    {
        Guid trackingNumber = NewId.NextGuid();
        Guid executionId = NewId.NextGuid();
        var values = new Dictionary<string, object>();
        IActivity activity = new MutableActivity("ChargeCard", new Uri("loopback://localhost/charge"), values);
        IActivityException activityException = ValidActivityException(executionId);
        ExceptionInfo exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("expected"));

        Action[] emptyTrackingNumberCases =
        [
            () => new RoutingSlipActivityCompensatedMessage(HostMetadataCache.Host, Guid.Empty, "ChargeCard", executionId, Timestamp, Duration, values, values),
            () => new RoutingSlipActivityCompensationFailedMessage(HostMetadataCache.Host, Guid.Empty, "ChargeCard", executionId, Timestamp, Duration, exceptionInfo, values, values),
            () => new RoutingSlipActivityCompletedMessage(HostMetadataCache.Host, Guid.Empty, "ChargeCard", executionId, Timestamp, Duration, values, values, values),
            () => new RoutingSlipActivityFaultedMessage(HostMetadataCache.Host, Guid.Empty, "ChargeCard", executionId, Timestamp, Duration, exceptionInfo, values, values),
            () => new RoutingSlipCompensationFailedMessage(HostMetadataCache.Host, Guid.Empty, Timestamp, Duration, exceptionInfo, values),
            () => new RoutingSlipCompletedMessage(Guid.Empty, Timestamp, Duration, values),
            () => new RoutingSlipFaultedMessage(Guid.Empty, Timestamp, Duration, [activityException], values),
            () => new RoutingSlipRevisedMessage(HostMetadataCache.Host, Guid.Empty, "ChargeCard", executionId, Timestamp, Duration, values, [activity], [activity]),
            () => new RoutingSlipTerminatedMessage(HostMetadataCache.Host, Guid.Empty, "ChargeCard", executionId, Timestamp, Duration, values, [activity]),
        ];
        AssertAllThrow<ArgumentException>(emptyTrackingNumberCases, "trackingNumber");

        Action[] negativeDurationCases =
        [
            () => new RoutingSlipActivityCompensatedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, TimeSpan.FromTicks(-1), values, values),
            () => new RoutingSlipActivityCompensationFailedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, TimeSpan.FromTicks(-1), exceptionInfo, values, values),
            () => new RoutingSlipActivityCompletedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, TimeSpan.FromTicks(-1), values, values, values),
            () => new RoutingSlipActivityFaultedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, TimeSpan.FromTicks(-1), exceptionInfo, values, values),
            () => new RoutingSlipCompensationFailedMessage(HostMetadataCache.Host, trackingNumber, Timestamp, TimeSpan.FromTicks(-1), exceptionInfo, values),
            () => new RoutingSlipCompletedMessage(trackingNumber, Timestamp, TimeSpan.FromTicks(-1), values),
            () => new RoutingSlipFaultedMessage(trackingNumber, Timestamp, TimeSpan.FromTicks(-1), [activityException], values),
            () => new RoutingSlipRevisedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, TimeSpan.FromTicks(-1), values, [activity], [activity]),
            () => new RoutingSlipTerminatedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, TimeSpan.FromTicks(-1), values, [activity]),
        ];
        AssertAllThrow<ArgumentOutOfRangeException>(negativeDurationCases, "duration");

        Action[] emptyExecutionIdCases =
        [
            () => new RoutingSlipActivityCompensatedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", Guid.Empty, Timestamp, Duration, values, values),
            () => new RoutingSlipActivityCompensationFailedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", Guid.Empty, Timestamp, Duration, exceptionInfo, values, values),
            () => new RoutingSlipActivityCompletedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", Guid.Empty, Timestamp, Duration, values, values, values),
            () => new RoutingSlipActivityFaultedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", Guid.Empty, Timestamp, Duration, exceptionInfo, values, values),
            () => new RoutingSlipRevisedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", Guid.Empty, Timestamp, Duration, values, [activity], [activity]),
            () => new RoutingSlipTerminatedMessage(HostMetadataCache.Host, trackingNumber, "ChargeCard", Guid.Empty, Timestamp, Duration, values, [activity]),
        ];
        AssertAllThrow<ArgumentException>(emptyExecutionIdCases, "executionId");

        Assert.Throws<ArgumentNullException>(() => new RoutingSlipActivityCompletedMessage(
            null!, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, values, values, values));
        Assert.Equal("activityName", Assert.Throws<ArgumentException>(() => new RoutingSlipActivityCompletedMessage(
            HostMetadataCache.Host, trackingNumber, " ", executionId, Timestamp, Duration, values, values, values)).ParamName);
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipActivityFaultedMessage(
            HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, null!, values, values));
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipCompletedMessage(
            trackingNumber, Timestamp, Duration, null!));
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipFaultedMessage(
            trackingNumber, Timestamp, Duration, null!, values));
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipRevisedMessage(
            HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, values, null!, [activity]));
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipTerminatedMessage(
            HostMetadataCache.Host, trackingNumber, "ChargeCard", executionId, Timestamp, Duration, values, null!));
    }

    private static void AssertDictionaryProperties<TContract>(params string[] names)
    {
        foreach (string name in names)
            AssertPropertyType<TContract>(name, typeof(IReadOnlyDictionary<string, object>));
    }

    private static void AssertPropertyType<TContract>(string propertyName, Type expectedType)
    {
        Type? actualType = typeof(TContract).GetProperty(propertyName)?.PropertyType;
        Assert.Equal(expectedType, actualType);
    }

    private static bool TryGetDictionary(object value, string propertyName, out IReadOnlyDictionary<string, object>? dictionary)
    {
        dictionary = value.GetType().GetProperty(propertyName)?.GetValue(value) as IReadOnlyDictionary<string, object>;
        return dictionary is not null;
    }

    private static void AssertReadOnlyCollection<T>(IReadOnlyCollection<T> values)
    {
        if (values is ICollection<T> collection)
            Assert.True(collection.IsReadOnly);
    }

    private static void AssertReadOnlyDictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> values)
        where TKey : notnull
    {
        if (values is ICollection<KeyValuePair<TKey, TValue>> collection)
            Assert.True(collection.IsReadOnly);
    }

    private static void AssertAllThrow<TException>(IEnumerable<Action> cases, string parameterName)
        where TException : ArgumentException
    {
        foreach (Action action in cases)
            Assert.Equal(parameterName, Assert.Throws<TException>(action).ParamName);
    }

    private static IActivityException ValidActivityException(Guid executionId) =>
        new RoutingSlipActivityException(
            "ChargeCard",
            HostMetadataCache.Host,
            executionId,
            Timestamp,
            Duration,
            new FaultExceptionInfo(new InvalidOperationException("expected")));

    private sealed class MutableActivity(string name, Uri address, Dictionary<string, object> arguments) : IActivity
    {
        public string Name { get; } = name;

        public Uri Address { get; } = address;

        public IReadOnlyDictionary<string, object> Arguments { get; } = arguments;
    }
}
