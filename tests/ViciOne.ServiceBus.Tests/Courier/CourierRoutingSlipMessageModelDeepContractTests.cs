using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierRoutingSlipMessageModelDeepContractTests
{
    private static readonly DateTimeOffset Timestamp = new(2049, 10, 11, 12, 13, 14, TimeSpan.Zero);
    private static readonly Uri ExecuteAddress = new("loopback://localhost/message-model-execute");
    private static readonly Uri CompensateAddress = new("loopback://localhost/message-model-compensate");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTRACTS", "routing-slip-message-model-direct-constructor-boundaries")]
    public void DirectConstructors_ValidateEveryRequiredScalarCollectionAndTimeBoundary()
    {
        Guid executionId = NewId.NextGuid();
        var values = new Dictionary<string, object>();
        ExceptionInfo exceptionInfo = CreateExceptionInfo();

        AssertParameter("name", () => new RoutingSlipActivity(" ", ExecuteAddress, values));
        AssertParameter("address", () => new RoutingSlipActivity("Execute", null!, values));
        AssertParameter("arguments", () => new RoutingSlipActivity("Execute", ExecuteAddress, null!));

        AssertParameter("host", () => new RoutingSlipActivityLog(null!, executionId, "Execute", Timestamp, TimeSpan.Zero));
        AssertParameter("executionId", () => new RoutingSlipActivityLog(HostMetadataCache.Host, Guid.Empty, "Execute", Timestamp, TimeSpan.Zero));
        AssertParameter("name", () => new RoutingSlipActivityLog(HostMetadataCache.Host, executionId, "", Timestamp, TimeSpan.Zero));
        AssertParameter("duration", () => new RoutingSlipActivityLog(
            HostMetadataCache.Host, executionId, "Execute", Timestamp, TimeSpan.FromTicks(-1)));

        AssertParameter("activityName", () => new RoutingSlipActivityException(
            " ", HostMetadataCache.Host, executionId, Timestamp, TimeSpan.Zero, exceptionInfo));
        AssertParameter("host", () => new RoutingSlipActivityException(
            "Execute", null!, executionId, Timestamp, TimeSpan.Zero, exceptionInfo));
        AssertParameter("executionId", () => new RoutingSlipActivityException(
            "Execute", HostMetadataCache.Host, Guid.Empty, Timestamp, TimeSpan.Zero, exceptionInfo));
        AssertParameter("elapsed", () => new RoutingSlipActivityException(
            "Execute", HostMetadataCache.Host, executionId, Timestamp, TimeSpan.FromTicks(-1), exceptionInfo));
        AssertParameter("exceptionInfo", () => new RoutingSlipActivityException(
            "Execute", HostMetadataCache.Host, executionId, Timestamp, TimeSpan.Zero, null!));

        AssertParameter("executionId", () => new RoutingSlipCompensateLog(Guid.Empty, CompensateAddress, values));
        AssertParameter("address", () => new RoutingSlipCompensateLog(executionId, null!, values));
        AssertParameter("data", () => new RoutingSlipCompensateLog(executionId, CompensateAddress, null!));

        AssertParameter("trackingNumber", () => CreateRoutingSlip(Guid.Empty, Timestamp));
        AssertParameter("createTimestamp", () => CreateRoutingSlip(NewId.NextGuid(), default(DateTimeOffset)));
        AssertParameter("activities", () => new RoutingSlipRoutingSlip(
            NewId.NextGuid(), Timestamp, null!, [], [], [], [], []));
        AssertParameter("activityLogs", () => new RoutingSlipRoutingSlip(
            NewId.NextGuid(), Timestamp, [], null!, [], [], [], []));
        AssertParameter("compensateLogs", () => new RoutingSlipRoutingSlip(
            NewId.NextGuid(), Timestamp, [], [], null!, [], [], []));
        AssertParameter("exceptions", () => new RoutingSlipRoutingSlip(
            NewId.NextGuid(), Timestamp, [], [], [], null!, [], []));
        AssertParameter("variables", () => new RoutingSlipRoutingSlip(
            NewId.NextGuid(), Timestamp, [], [], [], [], null!, []));
        AssertParameter("subscriptions", () => new RoutingSlipRoutingSlip(
            NewId.NextGuid(), Timestamp, [], [], [], [], [], null!));

        Assert.Equal(TimeSpan.Zero, new RoutingSlipActivityLog(
            HostMetadataCache.Host, executionId, "Execute", Timestamp, TimeSpan.Zero).Duration);
        Assert.Equal(TimeSpan.Zero, new RoutingSlipActivityException(
            "Execute", HostMetadataCache.Host, executionId, Timestamp, TimeSpan.Zero, exceptionInfo).Elapsed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SERIALIZATION", "routing-slip-message-model-received-leaf-validation-and-null-normalization")]
    public void CopyMaterializers_RejectMalformedLeafContractsAndNormalizeMissingDictionaries()
    {
        Guid executionId = NewId.NextGuid();

        Assert.Equal("activity", Assert.Throws<ArgumentNullException>(() => new RoutingSlipActivity(null!)).ParamName);
        Assert.Equal("activityLog", Assert.Throws<ArgumentNullException>(() => new RoutingSlipActivityLog(null!)).ParamName);
        Assert.Equal("activityException", Assert.Throws<ArgumentNullException>(() => new RoutingSlipActivityException(null!)).ParamName);
        Assert.Equal("compensateLog", Assert.Throws<ArgumentNullException>(() => new RoutingSlipCompensateLog(null!)).ParamName);

        Action[] malformedCopies =
        [
            () => new RoutingSlipActivity(new MutableActivity { Name = " " }),
            () => new RoutingSlipActivity(new MutableActivity { Address = null! }),
            () => new RoutingSlipActivityLog(new MutableActivityLog { Host = null! }),
            () => new RoutingSlipActivityLog(new MutableActivityLog { ExecutionId = Guid.Empty }),
            () => new RoutingSlipActivityLog(new MutableActivityLog { Name = " " }),
            () => new RoutingSlipActivityLog(new MutableActivityLog { Duration = TimeSpan.FromTicks(-1) }),
            () => new RoutingSlipActivityException(new MutableActivityException { Name = "" }),
            () => new RoutingSlipActivityException(new MutableActivityException { Host = null! }),
            () => new RoutingSlipActivityException(new MutableActivityException { ExecutionId = Guid.Empty }),
            () => new RoutingSlipActivityException(new MutableActivityException { Elapsed = TimeSpan.FromTicks(-1) }),
            () => new RoutingSlipActivityException(new MutableActivityException { ExceptionInfo = null! }),
            () => new RoutingSlipCompensateLog(new MutableCompensateLog { ExecutionId = Guid.Empty }),
            () => new RoutingSlipCompensateLog(new MutableCompensateLog { Address = null! }),
        ];

        foreach (Action copy in malformedCopies)
            Assert.Throws<SerializationException>(copy);

        var activity = new MutableActivity { Arguments = null! };
        var compensateLog = new MutableCompensateLog
        {
            ExecutionId = executionId,
            Data = null!,
        };

        Assert.Empty(new RoutingSlipActivity(activity).Arguments);
        Assert.Empty(new RoutingSlipCompensateLog(compensateLog).Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SERIALIZATION", "routing-slip-message-model-copy-materializers-read-source-members-once")]
    public void CopyMaterializers_ReadEverySourceMemberOnceAndRetainTheValidatedSnapshot()
    {
        var activity = new SingleReadActivity();
        var activityLog = new SingleReadActivityLog();
        var activityException = new SingleReadActivityException();
        var compensateLog = new SingleReadCompensateLog();

        var activityCopy = new RoutingSlipActivity(activity);
        var activityLogCopy = new RoutingSlipActivityLog(activityLog);
        var exceptionCopy = new RoutingSlipActivityException(activityException);
        var compensateCopy = new RoutingSlipCompensateLog(compensateLog);

        Assert.Equal("Execute", activityCopy.Name);
        Assert.Equal(ExecuteAddress, activityCopy.Address);
        Assert.Equal("Execute", activityLogCopy.Name);
        Assert.Equal("Execute", exceptionCopy.Name);
        Assert.Equal(CompensateAddress, compensateCopy.Address);
        Assert.All(
            activity.Probes.Concat(activityLog.Probes).Concat(activityException.Probes).Concat(compensateLog.Probes),
            probe => Assert.Equal(1, probe.ReadCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-ISOLATION", "routing-slip-message-model-leaf-dictionaries-detach-shallowly-and-case-insensitively")]
    public void LeafModels_DetachDictionaryContainersButPreserveNestedPayloadIdentityAndKeyOrder()
    {
        var nestedArgument = new MutablePayload("argument");
        var nestedData = new MutablePayload("data");
        var arguments = new Dictionary<string, object>
        {
            ["First"] = nestedArgument,
            ["second"] = 2,
        };
        var data = new Dictionary<string, object>
        {
            ["Receipt"] = nestedData,
            ["sequence"] = 3,
        };
        var activity = new RoutingSlipActivity("Execute", ExecuteAddress, arguments);
        var compensateLog = new RoutingSlipCompensateLog(NewId.NextGuid(), CompensateAddress, data);

        arguments["First"] = new MutablePayload("replacement");
        arguments.Clear();
        data["Receipt"] = new MutablePayload("replacement");
        data.Clear();

        Assert.Same(nestedArgument, activity.Arguments["FIRST"]);
        Assert.Equal(["First", "second"], activity.Arguments.Keys);
        Assert.Same(nestedData, compensateLog.Data["RECEIPT"]);
        Assert.Equal(["Receipt", "sequence"], compensateLog.Data.Keys);
        AssertReadOnlyDictionary(activity.Arguments);
        AssertReadOnlyDictionary(compensateLog.Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-ISOLATION", "routing-slip-message-model-aggregate-detaches-preserves-order-and-reference-payloads")]
    public void RoutingSlipSnapshot_DetachesCollectionsAndLeafModelsWhilePreservingOrderAndReferencePayloads()
    {
        Guid firstExecutionId = NewId.NextGuid();
        Guid secondExecutionId = NewId.NextGuid();
        var nestedArgument = new MutablePayload("argument");
        var nestedVariable = new MutablePayload("variable");
        var firstArguments = new Dictionary<string, object> { ["Nested"] = nestedArgument };
        var firstActivity = new MutableActivity { Name = "First", Arguments = firstArguments };
        var secondActivity = new MutableActivity { Name = "Second" };
        var activities = new List<IActivity>
        {
            firstActivity,
            secondActivity,
        };
        var firstActivityLog = new MutableActivityLog { ExecutionId = firstExecutionId, Name = "First" };
        var secondActivityLog = new MutableActivityLog { ExecutionId = secondExecutionId, Name = "Second" };
        var activityLogs = new List<IActivityLog>
        {
            firstActivityLog,
            secondActivityLog,
        };
        var compensationData = new Dictionary<string, object> { ["Receipt"] = "original" };
        var compensateLogs = new List<ICompensateLog>
        {
            new MutableCompensateLog { ExecutionId = firstExecutionId, Data = compensationData },
        };
        var exceptionInfo = CreateExceptionInfo();
        var exceptions = new List<IActivityException>
        {
            new MutableActivityException
            {
                ExecutionId = secondExecutionId,
                Name = "Second",
                ExceptionInfo = exceptionInfo,
            },
        };
        var variables = new List<KeyValuePair<string, object>>
        {
            new("Zulu", 1),
            new("alpha", nestedVariable),
            new("Middle", 3),
        };
        var routingSlip = CreateRoutingSlip(
            activities: activities,
            activityLogs: activityLogs,
            compensateLogs: compensateLogs,
            exceptions: exceptions,
            variables: variables);

        firstActivity.Name = "mutated";
        firstArguments["Nested"] = new MutablePayload("replacement");
        firstActivityLog.Name = "mutated";
        compensationData["Receipt"] = "mutated";
        ((MutableActivityException)exceptions[0]).Name = "mutated";
        activities.Clear();
        activityLogs.Clear();
        compensateLogs.Clear();
        exceptions.Clear();
        variables.Clear();

        Assert.Equal(["First", "Second"], routingSlip.Itinerary.Select(activity => activity.Name));
        Assert.NotSame(firstActivity, routingSlip.Itinerary[0]);
        Assert.NotSame(secondActivity, routingSlip.Itinerary[1]);
        Assert.Equal([firstExecutionId, secondExecutionId], routingSlip.ActivityLogs.Select(log => log.ExecutionId));
        Assert.NotSame(firstActivityLog, routingSlip.ActivityLogs[0]);
        Assert.NotSame(secondActivityLog, routingSlip.ActivityLogs[1]);
        Assert.Equal("First", routingSlip.ActivityLogs[0].Name);
        Assert.Same(HostMetadataCache.Host, routingSlip.ActivityLogs[0].Host);
        Assert.Equal("original", Assert.Single(routingSlip.CompensateLogs).Data["receipt"]);
        Assert.Equal("Second", Assert.Single(routingSlip.ActivityExceptions).Name);
        Assert.Same(exceptionInfo, routingSlip.ActivityExceptions[0].ExceptionInfo);
        Assert.Same(nestedArgument, routingSlip.Itinerary[0].Arguments["nested"]);
        Assert.Same(nestedVariable, routingSlip.Variables["ALPHA"]);
        Assert.Equal(["Zulu", "alpha", "Middle"], routingSlip.Variables.Keys);
        AssertReadOnlyList(routingSlip.Itinerary);
        AssertReadOnlyList(routingSlip.ActivityLogs);
        AssertReadOnlyList(routingSlip.CompensateLogs);
        AssertReadOnlyList(routingSlip.ActivityExceptions);
        AssertReadOnlyList(routingSlip.Subscriptions);
        AssertReadOnlyDictionary(routingSlip.Variables);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONTRACTS", "routing-slip-message-model-aggregate-variables-reject-case-ambiguous-duplicates")]
    public void RoutingSlipSnapshot_RejectsVariablesThatDifferOnlyByCase()
    {
        KeyValuePair<string, object>[] variables =
        [
            new("Tenant", "north"),
            new("tenant", "south"),
        ];

        Assert.Throws<ArgumentException>(() => CreateRoutingSlip(variables: variables));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SERIALIZATION", "routing-slip-message-model-parameterless-materializers-remain-hydratable")]
    public void ParameterlessMaterializers_ExposeWritableWireStateForSerializerHydration()
    {
        Guid trackingNumber = NewId.NextGuid();
        Guid executionId = NewId.NextGuid();
        ExceptionInfo exceptionInfo = CreateExceptionInfo();
        var values = new Dictionary<string, object> { ["Value"] = 27 };
        var activity = new RoutingSlipActivity
        {
            Name = "Execute",
            Address = ExecuteAddress,
            Arguments = values,
        };
        var activityLog = new RoutingSlipActivityLog
        {
            ExecutionId = executionId,
            Name = "Execute",
            Timestamp = Timestamp,
            Duration = TimeSpan.Zero,
            Host = HostMetadataCache.Host,
        };
        var compensateLog = new RoutingSlipCompensateLog
        {
            ExecutionId = executionId,
            Address = CompensateAddress,
            Data = values,
        };
        var activityException = new RoutingSlipActivityException
        {
            ExecutionId = executionId,
            Timestamp = Timestamp,
            Elapsed = TimeSpan.Zero,
            Name = "Execute",
            Host = HostMetadataCache.Host,
            ExceptionInfo = exceptionInfo,
        };
        var routingSlip = new RoutingSlipRoutingSlip
        {
            TrackingNumber = trackingNumber,
            CreateTimestamp = Timestamp,
            Itinerary = [activity],
            ActivityLogs = [activityLog],
            CompensateLogs = [compensateLog],
            Variables = values,
            ActivityExceptions = [activityException],
            Subscriptions = [],
        };

        Assert.Equal("Execute", Assert.Single(routingSlip.Itinerary).Name);
        Assert.Same(values, routingSlip.Itinerary[0].Arguments);
        Assert.Equal(executionId, Assert.Single(routingSlip.ActivityLogs).ExecutionId);
        Assert.Same(values, Assert.Single(routingSlip.CompensateLogs).Data);
        Assert.Same(exceptionInfo, Assert.Single(routingSlip.ActivityExceptions).ExceptionInfo);
        Assert.Same(values, routingSlip.Variables);
        Assert.Equal(trackingNumber, routingSlip.TrackingNumber);
        Assert.Equal(Timestamp, routingSlip.CreateTimestamp);
    }

    private static RoutingSlipRoutingSlip CreateRoutingSlip(
        Guid? trackingNumber = null,
        DateTimeOffset? createTimestamp = null,
        IEnumerable<IActivity>? activities = null,
        IEnumerable<IActivityLog>? activityLogs = null,
        IEnumerable<ICompensateLog>? compensateLogs = null,
        IEnumerable<IActivityException>? exceptions = null,
        IEnumerable<KeyValuePair<string, object>>? variables = null,
        IEnumerable<ISubscription>? subscriptions = null) =>
        new(
            trackingNumber ?? NewId.NextGuid(),
            createTimestamp ?? Timestamp,
            activities ?? [],
            activityLogs ?? [],
            compensateLogs ?? [],
            exceptions ?? [],
            variables ?? [],
            subscriptions ?? []);

    private static ExceptionInfo CreateExceptionInfo() =>
        new FaultExceptionInfo(new InvalidOperationException("expected"));

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.ThrowsAny<ArgumentException>(action).ParamName);

    private static void AssertReadOnlyDictionary(IReadOnlyDictionary<string, object> values)
    {
        IDictionary<string, object> dictionary = Assert.IsAssignableFrom<IDictionary<string, object>>(values);
        Assert.True(dictionary.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => dictionary["late"] = "value");
    }

    private static void AssertReadOnlyList<T>(IReadOnlyList<T> values)
    {
        IList<T> list = Assert.IsAssignableFrom<IList<T>>(values);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Clear());
    }

    private interface IReadProbe
    {
        int ReadCount { get; }
    }

    private sealed class ReadProbe<T>(T value) : IReadProbe
    {
        public int ReadCount { get; private set; }

        public T Read()
        {
            ReadCount++;
            if (ReadCount > 1)
                throw new InvalidOperationException("A materializer read the same source member more than once.");

            return value;
        }
    }

    private sealed class SingleReadActivity : IActivity
    {
        private readonly ReadProbe<string> _name = new("Execute");
        private readonly ReadProbe<Uri> _address = new(ExecuteAddress);
        private readonly ReadProbe<IReadOnlyDictionary<string, object>> _arguments = new(
            new Dictionary<string, object> { ["Value"] = 27 });

        public IReadOnlyList<IReadProbe> Probes => [_name, _address, _arguments];
        public string Name => _name.Read();
        public Uri Address => _address.Read();
        public IReadOnlyDictionary<string, object> Arguments => _arguments.Read();
    }

    private sealed class SingleReadActivityLog : IActivityLog
    {
        private readonly ReadProbe<Guid> _executionId = new(NewId.NextGuid());
        private readonly ReadProbe<string> _name = new("Execute");
        private readonly ReadProbe<DateTimeOffset> _timestamp = new(CourierRoutingSlipMessageModelDeepContractTests.Timestamp);
        private readonly ReadProbe<TimeSpan> _duration = new(TimeSpan.Zero);
        private readonly ReadProbe<HostInfo> _host = new(HostMetadataCache.Host);

        public IReadOnlyList<IReadProbe> Probes => [_executionId, _name, _timestamp, _duration, _host];
        public Guid ExecutionId => _executionId.Read();
        public string Name => _name.Read();
        public DateTimeOffset Timestamp => _timestamp.Read();
        public TimeSpan Duration => _duration.Read();
        public HostInfo Host => _host.Read();
    }

    private sealed class SingleReadActivityException : IActivityException
    {
        private readonly ReadProbe<Guid> _executionId = new(NewId.NextGuid());
        private readonly ReadProbe<DateTimeOffset> _timestamp = new(CourierRoutingSlipMessageModelDeepContractTests.Timestamp);
        private readonly ReadProbe<TimeSpan> _elapsed = new(TimeSpan.Zero);
        private readonly ReadProbe<string> _name = new("Execute");
        private readonly ReadProbe<HostInfo> _host = new(HostMetadataCache.Host);
        private readonly ReadProbe<ExceptionInfo> _exceptionInfo = new(CreateExceptionInfo());

        public IReadOnlyList<IReadProbe> Probes => [_executionId, _timestamp, _elapsed, _name, _host, _exceptionInfo];
        public Guid ExecutionId => _executionId.Read();
        public DateTimeOffset Timestamp => _timestamp.Read();
        public TimeSpan Elapsed => _elapsed.Read();
        public string Name => _name.Read();
        public HostInfo Host => _host.Read();
        public ExceptionInfo ExceptionInfo => _exceptionInfo.Read();
    }

    private sealed class SingleReadCompensateLog : ICompensateLog
    {
        private readonly ReadProbe<Guid> _executionId = new(NewId.NextGuid());
        private readonly ReadProbe<Uri> _address = new(CompensateAddress);
        private readonly ReadProbe<IReadOnlyDictionary<string, object>> _data = new(
            new Dictionary<string, object> { ["Receipt"] = "receipt" });

        public IReadOnlyList<IReadProbe> Probes => [_executionId, _address, _data];
        public Guid ExecutionId => _executionId.Read();
        public Uri Address => _address.Read();
        public IReadOnlyDictionary<string, object> Data => _data.Read();
    }

    private sealed class MutableActivity : IActivity
    {
        public string Name { get; set; } = "Execute";
        public Uri Address { get; set; } = ExecuteAddress;
        public IReadOnlyDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }

    private sealed class MutableActivityLog : IActivityLog
    {
        public Guid ExecutionId { get; set; } = NewId.NextGuid();
        public string Name { get; set; } = "Execute";
        public DateTimeOffset Timestamp { get; set; } = CourierRoutingSlipMessageModelDeepContractTests.Timestamp;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public HostInfo Host { get; set; } = HostMetadataCache.Host;
    }

    private sealed class MutableActivityException : IActivityException
    {
        public Guid ExecutionId { get; set; } = NewId.NextGuid();
        public DateTimeOffset Timestamp { get; set; } = CourierRoutingSlipMessageModelDeepContractTests.Timestamp;
        public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
        public string Name { get; set; } = "Execute";
        public HostInfo Host { get; set; } = HostMetadataCache.Host;
        public ExceptionInfo ExceptionInfo { get; set; } = CreateExceptionInfo();
    }

    private sealed class MutableCompensateLog : ICompensateLog
    {
        public Guid ExecutionId { get; set; } = NewId.NextGuid();
        public Uri Address { get; set; } = CompensateAddress;
        public IReadOnlyDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    private sealed class MutablePayload(string value)
    {
        public string Value { get; set; } = value;
    }
}
