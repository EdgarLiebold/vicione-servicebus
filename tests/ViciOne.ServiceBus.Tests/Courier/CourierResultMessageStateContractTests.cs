using System.Reflection;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierResultMessageStateContractTests
{
    private static readonly Guid TrackingNumber = Guid.Parse("ac4881a6-c715-4960-90fb-4b21aef93aa0");
    private static readonly Guid ExecutionId = Guid.Parse("e2333724-e61f-4e97-a8fe-e6b321e78d16");
    private static readonly DateTimeOffset Timestamp = new(2045, 6, 7, 8, 9, 10, TimeSpan.Zero);
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(17);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "result-message-materializer-api-remains-writable")]
    public void ResultMessageTypes_ExposePublicParameterlessMaterializersAndWritableProperties()
    {
        Type[] messageTypes =
        [
            typeof(RoutingSlipActivityCompensatedMessage),
            typeof(RoutingSlipActivityCompensationFailedMessage),
            typeof(RoutingSlipActivityCompletedMessage),
            typeof(RoutingSlipActivityFaultedMessage),
            typeof(RoutingSlipCompensationFailedMessage),
            typeof(RoutingSlipCompletedMessage),
            typeof(RoutingSlipFaultedMessage),
            typeof(RoutingSlipRevisedMessage),
            typeof(RoutingSlipTerminatedMessage),
        ];

        foreach (Type messageType in messageTypes)
        {
            ConstructorInfo materializer = Assert.IsAssignableFrom<ConstructorInfo>(messageType.GetConstructor(Type.EmptyTypes));
            Assert.True(materializer.IsPublic);
            Assert.Equal(messageType, materializer.Invoke(null).GetType());
            Assert.Contains(messageType.GetConstructors(), constructor => constructor.GetParameters().Length > 0);
            Assert.All(messageType.GetProperties(BindingFlags.Instance | BindingFlags.Public), property =>
            {
                Assert.True(property.GetMethod is { IsPublic: true });
                Assert.True(property.SetMethod is { IsPublic: true });
            });
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "result-message-constructors-preserve-complete-state")]
    public void StrongConstructors_PreserveScalarStateAndRequiredReferenceIdentity()
    {
        HostInfo host = HostMetadataCache.Host;
        ExceptionInfo exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("expected failure"));
        var values = new Dictionary<string, object> { ["value"] = 42 };
        IActivity activity = new MutableActivity("Reserve", new Uri("loopback://localhost/reserve"), values);
        IActivityException activityException = new MutableActivityException(
            "Reserve", host, ExecutionId, Timestamp, Duration, exceptionInfo);

        IRoutingSlipActivityCompensated compensated = new RoutingSlipActivityCompensatedMessage(
            host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, values);
        AssertActivityState(compensated.Host, compensated.TrackingNumber, compensated.ActivityName, compensated.ExecutionId,
            compensated.Timestamp, compensated.Duration, host);

        IRoutingSlipActivityCompensationFailed activityCompensationFailed = new RoutingSlipActivityCompensationFailedMessage(
            host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, exceptionInfo, values, values);
        AssertActivityState(activityCompensationFailed.Host, activityCompensationFailed.TrackingNumber,
            activityCompensationFailed.ActivityName, activityCompensationFailed.ExecutionId,
            activityCompensationFailed.Timestamp, activityCompensationFailed.Duration, host);
        Assert.Same(exceptionInfo, activityCompensationFailed.ExceptionInfo);

        IRoutingSlipActivityCompleted activityCompleted = new RoutingSlipActivityCompletedMessage(
            host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, values, values);
        AssertActivityState(activityCompleted.Host, activityCompleted.TrackingNumber, activityCompleted.ActivityName,
            activityCompleted.ExecutionId, activityCompleted.Timestamp, activityCompleted.Duration, host);

        IRoutingSlipActivityFaulted activityFaulted = new RoutingSlipActivityFaultedMessage(
            host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, exceptionInfo, values, values);
        AssertActivityState(activityFaulted.Host, activityFaulted.TrackingNumber, activityFaulted.ActivityName,
            activityFaulted.ExecutionId, activityFaulted.Timestamp, activityFaulted.Duration, host);
        Assert.Same(exceptionInfo, activityFaulted.ExceptionInfo);

        IRoutingSlipCompensationFailed compensationFailed = new RoutingSlipCompensationFailedMessage(
            host, TrackingNumber, Timestamp, Duration, exceptionInfo, values);
        Assert.Equal(TrackingNumber, compensationFailed.TrackingNumber);
        Assert.Equal(Timestamp, compensationFailed.Timestamp);
        Assert.Equal(Duration, compensationFailed.Duration);
        Assert.Same(host, compensationFailed.Host);
        Assert.Same(exceptionInfo, compensationFailed.ExceptionInfo);

        IRoutingSlipCompleted completed = new RoutingSlipCompletedMessage(TrackingNumber, Timestamp, Duration, values);
        Assert.Equal(TrackingNumber, completed.TrackingNumber);
        Assert.Equal(Timestamp, completed.Timestamp);
        Assert.Equal(Duration, completed.Duration);

        IRoutingSlipFaulted faulted = new RoutingSlipFaultedMessage(
            TrackingNumber, Timestamp, Duration, [activityException], values);
        Assert.Equal(TrackingNumber, faulted.TrackingNumber);
        Assert.Equal(Timestamp, faulted.Timestamp);
        Assert.Equal(Duration, faulted.Duration);

        IRoutingSlipRevised revised = new RoutingSlipRevisedMessage(
            host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, [activity], [activity]);
        AssertActivityState(revised.Host, revised.TrackingNumber, revised.ActivityName, revised.ExecutionId,
            revised.Timestamp, revised.Duration, host);

        IRoutingSlipTerminated terminated = new RoutingSlipTerminatedMessage(
            host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, [activity]);
        AssertActivityState(terminated.Host, terminated.TrackingNumber, terminated.ActivityName, terminated.ExecutionId,
            terminated.Timestamp, terminated.Duration, host);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "result-message-null-payloads-report-public-parameter-names")]
    public void StrongConstructors_RejectEveryNullPayloadUsingItsPublicParameterName()
    {
        HostInfo host = HostMetadataCache.Host;
        ExceptionInfo exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("expected failure"));
        var values = new Dictionary<string, object>();
        IActivity activity = new MutableActivity("Reserve", new Uri("loopback://localhost/reserve"), values);
        IActivityException activityException = new MutableActivityException(
            "Reserve", host, ExecutionId, Timestamp, Duration, exceptionInfo);
        (string ParameterName, Action Construct)[] cases =
        [
            ("host", () => new RoutingSlipActivityCompensatedMessage(null!, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, values)),
            ("activityName", () => new RoutingSlipActivityCompensatedMessage(host, TrackingNumber, null!, ExecutionId, Timestamp, Duration, values, values)),
            ("variables", () => new RoutingSlipActivityCompensatedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, null!, values)),
            ("data", () => new RoutingSlipActivityCompensatedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, null!)),
            ("host", () => new RoutingSlipActivityCompensationFailedMessage(null!, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, exceptionInfo, values, values)),
            ("exceptionInfo", () => new RoutingSlipActivityCompensationFailedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, null!, values, values)),
            ("variables", () => new RoutingSlipActivityCompensationFailedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, exceptionInfo, null!, values)),
            ("data", () => new RoutingSlipActivityCompensationFailedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, exceptionInfo, values, null!)),
            ("host", () => new RoutingSlipActivityCompletedMessage(null!, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, values, values)),
            ("variables", () => new RoutingSlipActivityCompletedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, null!, values, values)),
            ("arguments", () => new RoutingSlipActivityCompletedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, null!, values)),
            ("data", () => new RoutingSlipActivityCompletedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, values, null!)),
            ("host", () => new RoutingSlipActivityFaultedMessage(null!, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, exceptionInfo, values, values)),
            ("exceptionInfo", () => new RoutingSlipActivityFaultedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, null!, values, values)),
            ("variables", () => new RoutingSlipActivityFaultedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, exceptionInfo, null!, values)),
            ("arguments", () => new RoutingSlipActivityFaultedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, exceptionInfo, values, null!)),
            ("host", () => new RoutingSlipCompensationFailedMessage(null!, TrackingNumber, Timestamp, Duration, exceptionInfo, values)),
            ("exceptionInfo", () => new RoutingSlipCompensationFailedMessage(host, TrackingNumber, Timestamp, Duration, null!, values)),
            ("variables", () => new RoutingSlipCompensationFailedMessage(host, TrackingNumber, Timestamp, Duration, exceptionInfo, null!)),
            ("variables", () => new RoutingSlipCompletedMessage(TrackingNumber, Timestamp, Duration, null!)),
            ("activityExceptions", () => new RoutingSlipFaultedMessage(TrackingNumber, Timestamp, Duration, null!, values)),
            ("variables", () => new RoutingSlipFaultedMessage(TrackingNumber, Timestamp, Duration, [activityException], null!)),
            ("activityException", () => new RoutingSlipFaultedMessage(TrackingNumber, Timestamp, Duration, null!)),
            ("host", () => new RoutingSlipRevisedMessage(null!, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, [activity], [activity])),
            ("variables", () => new RoutingSlipRevisedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, null!, [activity], [activity])),
            ("itinerary", () => new RoutingSlipRevisedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, null!, [activity])),
            ("discardedItinerary", () => new RoutingSlipRevisedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, [activity], null!)),
            ("host", () => new RoutingSlipTerminatedMessage(null!, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, [activity])),
            ("variables", () => new RoutingSlipTerminatedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, null!, [activity])),
            ("discardedItinerary", () => new RoutingSlipTerminatedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration, values, null!)),
        ];

        foreach ((string parameterName, Action construct) in cases)
            Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(construct).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "message-state-dictionary-snapshot-is-case-insensitive-read-only-and-detached")]
    public void DictionarySnapshot_IsCaseInsensitiveReadOnlyDetachedAndRejectsAmbiguousKeys()
    {
        var source = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Tenant"] = "north",
            ["Count"] = 3,
        };

        IReadOnlyDictionary<string, object> snapshot = RoutingSlipMessageState.Snapshot(source);
        source["Tenant"] = "south";
        source["Added"] = true;

        Assert.NotSame(source, snapshot);
        Assert.Equal("north", snapshot["tenant"]);
        Assert.Equal(3, snapshot["COUNT"]);
        Assert.False(snapshot.ContainsKey("added"));
        var mutableView = Assert.IsAssignableFrom<IDictionary<string, object>>(snapshot);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add("other", 4));

        var ambiguous = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Tenant"] = "north",
            ["tenant"] = "south",
        };
        Assert.Throws<ArgumentException>(() => RoutingSlipMessageState.Snapshot(ambiguous));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "message-state-activity-snapshot-validates-and-deep-copies-elements")]
    public void ActivitySnapshot_PreservesOrderDeepCopiesEntriesAndRejectsInvalidElements()
    {
        var firstArguments = new Dictionary<string, object>(StringComparer.Ordinal) { ["Amount"] = 12 };
        var first = new MutableActivity("Reserve", new Uri("loopback://localhost/reserve"), firstArguments);
        var second = new MutableActivity("Capture", new Uri("loopback://localhost/capture"), new Dictionary<string, object>());
        var source = new List<IActivity> { first, second };

        IReadOnlyList<IActivity> snapshot = RoutingSlipMessageState.SnapshotActivities(source);
        first.Name = "Changed";
        first.Address = new Uri("loopback://localhost/changed");
        firstArguments["Amount"] = 99;
        source.Reverse();

        Assert.Equal(["Reserve", "Capture"], snapshot.Select(activity => activity.Name));
        Assert.NotSame(first, snapshot[0]);
        Assert.Equal(new Uri("loopback://localhost/reserve"), snapshot[0].Address);
        Assert.Equal(12, snapshot[0].Arguments["amount"]);
        var mutableView = Assert.IsAssignableFrom<IList<IActivity>>(snapshot);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(first));

        IEnumerable<IActivity> activities = [null!];
        ArgumentException nullElement = Assert.Throws<ArgumentException>(() =>
            RoutingSlipMessageState.SnapshotActivities(activities));
        Assert.Equal("activities", nullElement.ParamName);

        ArgumentException itineraryElement = Assert.Throws<ArgumentException>(() => new RoutingSlipRevisedMessage(
            HostMetadataCache.Host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration,
            new Dictionary<string, object>(), [null!], []));
        Assert.Equal("itinerary", itineraryElement.ParamName);
        ArgumentException discardedElement = Assert.Throws<ArgumentException>(() => new RoutingSlipTerminatedMessage(
            HostMetadataCache.Host, TrackingNumber, "Reserve", ExecutionId, Timestamp, Duration,
            new Dictionary<string, object>(), [null!]));
        Assert.Equal("discardedItinerary", discardedElement.ParamName);

        var invalid = new MutableActivity(" ", new Uri("loopback://localhost/invalid"), new Dictionary<string, object>());
        Assert.Throws<SerializationException>(() => RoutingSlipMessageState.SnapshotActivities([invalid]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "message-state-exception-snapshot-validates-and-copies-envelopes")]
    public void ExceptionSnapshot_CopiesMutableEnvelopesPreservesDiagnosticIdentityAndRejectsInvalidElements()
    {
        HostInfo host = HostMetadataCache.Host;
        ExceptionInfo exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("first failure"));
        var first = new MutableActivityException("Reserve", host, ExecutionId, Timestamp, Duration, exceptionInfo);
        var second = new MutableActivityException(
            "Capture", host, Guid.Parse("3a72f992-32c3-4633-b3f9-866201bfa277"), Timestamp.AddMinutes(1),
            Duration.Add(TimeSpan.FromSeconds(1)), new FaultExceptionInfo(new InvalidOperationException("second failure")));
        var source = new List<IActivityException> { first, second };

        IReadOnlyList<IActivityException> snapshot = RoutingSlipMessageState.SnapshotExceptions(source);
        first.Name = "Changed";
        first.ExecutionId = Guid.Empty;
        first.Timestamp = DateTimeOffset.MinValue;
        first.Elapsed = TimeSpan.Zero;
        first.ExceptionInfo = new FaultExceptionInfo(new InvalidOperationException("changed failure"));
        source.Reverse();

        Assert.Equal(["Reserve", "Capture"], snapshot.Select(exception => exception.Name));
        Assert.NotSame(first, snapshot[0]);
        Assert.Equal(ExecutionId, snapshot[0].ExecutionId);
        Assert.Equal(Timestamp, snapshot[0].Timestamp);
        Assert.Equal(Duration, snapshot[0].Elapsed);
        Assert.Same(host, snapshot[0].Host);
        Assert.Same(exceptionInfo, snapshot[0].ExceptionInfo);
        var mutableView = Assert.IsAssignableFrom<IList<IActivityException>>(snapshot);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(first));

        IEnumerable<IActivityException> exceptions = [null!];
        ArgumentException nullElement = Assert.Throws<ArgumentException>(() =>
            RoutingSlipMessageState.SnapshotExceptions(exceptions));
        Assert.Equal("exceptions", nullElement.ParamName);

        ArgumentException activityExceptionElement = Assert.Throws<ArgumentException>(() => new RoutingSlipFaultedMessage(
            TrackingNumber, Timestamp, Duration, [null!], new Dictionary<string, object>()));
        Assert.Equal("activityExceptions", activityExceptionElement.ParamName);

        var invalid = new MutableActivityException(" ", host, ExecutionId, Timestamp, Duration, exceptionInfo);
        Assert.Throws<SerializationException>(() => RoutingSlipMessageState.SnapshotExceptions([invalid]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "single-fault-overload-owns-empty-state-and-detaches-the-failure")]
    public void SingleFaultConstructor_OwnsEmptyVariablesAndDetachesTheFailureEnvelope()
    {
        HostInfo host = HostMetadataCache.Host;
        ExceptionInfo exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("expected failure"));
        var source = new MutableActivityException("Reserve", host, ExecutionId, Timestamp, Duration, exceptionInfo);

        IRoutingSlipFaulted message = new RoutingSlipFaultedMessage(TrackingNumber, Timestamp, Duration, source);
        source.Name = "Changed";
        source.ExceptionInfo = new FaultExceptionInfo(new InvalidOperationException("changed failure"));

        IActivityException snapshot = Assert.Single(message.ActivityExceptions);
        Assert.NotSame(source, snapshot);
        Assert.Equal("Reserve", snapshot.Name);
        Assert.Same(host, snapshot.Host);
        Assert.Same(exceptionInfo, snapshot.ExceptionInfo);
        Assert.Empty(message.Variables);
        Assert.True(Assert.IsAssignableFrom<ICollection<KeyValuePair<string, object>>>(message.Variables).IsReadOnly);
        Assert.True(Assert.IsAssignableFrom<ICollection<IActivityException>>(message.ActivityExceptions).IsReadOnly);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENTS", "result-message-zero-duration-and-empty-state-accepted")]
    public void StrongConstructors_AcceptZeroDurationAndEmptyCollections()
    {
        HostInfo host = HostMetadataCache.Host;
        ExceptionInfo exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("expected failure"));
        var empty = new Dictionary<string, object>();
        IActivityException activityException = new MutableActivityException(
            "Reserve", host, ExecutionId, Timestamp, TimeSpan.Zero, exceptionInfo);
        Action[] cases =
        [
            () => new RoutingSlipActivityCompensatedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, TimeSpan.Zero, empty, empty),
            () => new RoutingSlipActivityCompensationFailedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, TimeSpan.Zero, exceptionInfo, empty, empty),
            () => new RoutingSlipActivityCompletedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, TimeSpan.Zero, empty, empty, empty),
            () => new RoutingSlipActivityFaultedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, TimeSpan.Zero, exceptionInfo, empty, empty),
            () => new RoutingSlipCompensationFailedMessage(host, TrackingNumber, Timestamp, TimeSpan.Zero, exceptionInfo, empty),
            () => new RoutingSlipCompletedMessage(TrackingNumber, Timestamp, TimeSpan.Zero, empty),
            () => new RoutingSlipFaultedMessage(TrackingNumber, Timestamp, TimeSpan.Zero, [activityException], empty),
            () => new RoutingSlipRevisedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, TimeSpan.Zero, empty, [], []),
            () => new RoutingSlipTerminatedMessage(host, TrackingNumber, "Reserve", ExecutionId, Timestamp, TimeSpan.Zero, empty, []),
        ];

        foreach (Action construct in cases)
            Assert.Null(Record.Exception(construct));
    }

    private static void AssertActivityState(
        HostInfo actualHost,
        Guid actualTrackingNumber,
        string actualActivityName,
        Guid actualExecutionId,
        DateTimeOffset actualTimestamp,
        TimeSpan actualDuration,
        HostInfo expectedHost)
    {
        Assert.Same(expectedHost, actualHost);
        Assert.Equal(TrackingNumber, actualTrackingNumber);
        Assert.Equal("Reserve", actualActivityName);
        Assert.Equal(ExecutionId, actualExecutionId);
        Assert.Equal(Timestamp, actualTimestamp);
        Assert.Equal(Duration, actualDuration);
    }

    private sealed class MutableActivity(string name, Uri address, IReadOnlyDictionary<string, object> arguments) : IActivity
    {
        public string Name { get; set; } = name;

        public Uri Address { get; set; } = address;

        public IReadOnlyDictionary<string, object> Arguments { get; set; } = arguments;
    }

    private sealed class MutableActivityException(
        string name,
        HostInfo host,
        Guid executionId,
        DateTimeOffset timestamp,
        TimeSpan elapsed,
        ExceptionInfo exceptionInfo) : IActivityException
    {
        public Guid ExecutionId { get; set; } = executionId;

        public DateTimeOffset Timestamp { get; set; } = timestamp;

        public TimeSpan Elapsed { get; set; } = elapsed;

        public string Name { get; set; } = name;

        public HostInfo Host { get; set; } = host;

        public ExceptionInfo ExceptionInfo { get; set; } = exceptionInfo;
    }
}
