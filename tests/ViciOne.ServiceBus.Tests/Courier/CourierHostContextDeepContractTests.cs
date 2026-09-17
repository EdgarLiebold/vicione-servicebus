using System.Runtime.Serialization;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Results;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierHostContextDeepContractTests
{
    private static readonly DateTimeOffset CreatedAt = new(2046, 7, 8, 9, 10, 11, TimeSpan.Zero);
    private static readonly Uri ExecuteAddress = new("loopback://localhost/deep-execute");
    private static readonly Uri CompensateAddress = new("loopback://localhost/deep-compensate");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "host-context-null-admission")]
    public void HostContexts_RejectMissingContextWithTheirDeclaredParameterName()
    {
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new HostExecuteContext<ArgumentValues>(null, null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new HostCompensateContext<LogValues>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensation-result-factory-symmetry-and-newest-log")]
    public async Task CompensateContext_BindsNewestLogAndExposesSymmetricResultFactoriesAsync()
    {
        Guid firstExecutionId = NewId.NextGuid();
        Guid newestExecutionId = NewId.NextGuid();
        var routingSlip = CreateRoutingSlip(
            activityLogs:
            [
                ActivityLog(firstExecutionId, "First"),
                ActivityLog(newestExecutionId, "Newest"),
            ],
            compensateLogs:
            [
                CompensateLog(firstExecutionId, new Dictionary<string, object> { [nameof(LogValues.Receipt)] = "first" }),
                CompensateLog(newestExecutionId, new Dictionary<string, object> { [nameof(LogValues.Receipt)] = "newest" }),
            ]);
        var outgoing = new OutgoingMessageRecorder();
        var context = new HostCompensateContext<LogValues>(CreateConsumeContext(routingSlip, outgoing));

        CompensationResult compensated = context.Compensated();
        CompensationResult compensatedWithValues = context.Compensated(new { Result = 27 });
        CompensationResult compensatedWithVariables = context.Compensated(
            new Dictionary<string, object> { ["Result"] = 27 });
        CompensationResult failed = context.Failed();
        var expectedException = new InvalidOperationException("expected");
        CompensationResult explicitlyFailed = context.Failed(expectedException);
        var activity = new object();
        CompensateActivityContext<object, LogValues> activityContext = context.CreateActivityContext(activity);

        Assert.Equal("Newest", context.ActivityName);
        Assert.Equal("newest", context.Log.Receipt);
        Assert.IsType<CompensatedCompensationResult<LogValues>>(compensated);
        Assert.IsType<CompensatedCompensationResult<LogValues>>(compensatedWithValues);
        Assert.IsType<CompensatedCompensationResult<LogValues>>(compensatedWithVariables);
        Assert.True(failed.IsFailed(out Exception? defaultException));
        Assert.IsType<RoutingSlipException>(defaultException);
        Assert.True(explicitlyFailed.IsFailed(out Exception? actualException));
        Assert.Same(expectedException, actualException);
        Assert.Same(activity, activityContext.Activity);

        context.Result = compensated;
        Assert.Same(compensated, context.Result);

        await compensatedWithValues.EvaluateAsync(TestContext.Current.CancellationToken);
        await compensatedWithVariables.EvaluateAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            outgoing.Messages,
            message => Assert.Equal("27", Assert.IsAssignableFrom<IRoutingSlipActivityCompensated>(message).Variables["result"].ToString()),
            message => Assert.Equal("27", Assert.IsAssignableFrom<IRoutingSlip>(message).Variables["result"].ToString()),
            message => Assert.Equal("27", Assert.IsAssignableFrom<IRoutingSlipActivityCompensated>(message).Variables["result"].ToString()),
            message => Assert.Equal("27", Assert.IsAssignableFrom<IRoutingSlip>(message).Variables["result"].ToString()));

        AssertParameter("values", () => context.Compensated((object)null!));
        AssertParameter("variables", () => context.Compensated((IDictionary<string, object>)null!));
        AssertParameter("exception", () => context.Failed(null!));
        AssertParameter("activity", () => context.CreateActivityContext<object>(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EXECUTION", "newest-compensation-log-requires-exactly-one-activity-log")]
    public void CompensateContext_RequiresExactlyOneActivityLogForTheNewestCompensation()
    {
        Guid executionId = NewId.NextGuid();
        ICompensateLog compensateLog = CompensateLog(
            executionId,
            new Dictionary<string, object> { [nameof(LogValues.Receipt)] = "receipt" });
        var missingMatch = CreateRoutingSlip(compensateLogs: [compensateLog]);
        var duplicateMatch = CreateRoutingSlip(
            activityLogs:
            [
                ActivityLog(executionId, "First"),
                ActivityLog(executionId, "Duplicate"),
            ],
            compensateLogs: [compensateLog]);

        RoutingSlipException missingFailure = Assert.Throws<RoutingSlipException>(() =>
            new HostCompensateContext<LogValues>(CreateConsumeContext(missingMatch)));
        RoutingSlipException duplicateFailure = Assert.Throws<RoutingSlipException>(() =>
            new HostCompensateContext<LogValues>(CreateConsumeContext(duplicateMatch)));

        Assert.Contains(executionId.ToString(), missingFailure.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", missingFailure.Message, StringComparison.Ordinal);
        Assert.Contains(executionId.ToString(), duplicateFailure.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", duplicateFailure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-ISOLATION", "sanitized-received-state-is-detached-and-cast-read-only")]
    public void SanitizedRoutingSlip_DetachesAndMakesEveryTopLevelCollectionReadOnly()
    {
        Guid executionId = NewId.NextGuid();
        var arguments = new Dictionary<string, object> { ["Argument"] = "original" };
        var compensationData = new Dictionary<string, object> { ["Receipt"] = "original" };
        var variables = new Dictionary<string, object> { ["Tenant"] = "original" };
        var activity = new MutableActivity("Execute", ExecuteAddress, arguments);
        var activityLog = ActivityLog(executionId, "Execute");
        var compensateLog = CompensateLog(executionId, compensationData);
        var activityException = new MutableActivityException(executionId, "Execute");
        var subscription = new MutableSubscription(ExecuteAddress);
        var source = CreateRoutingSlip(
            itinerary: [activity],
            activityLogs: [activityLog],
            compensateLogs: [compensateLog],
            variables: variables,
            activityExceptions: [activityException],
            subscriptions: [subscription]);

        var sanitized = new SanitizedRoutingSlip(CreateConsumeContext(source));

        activity.Name = "changed";
        arguments["Argument"] = "changed";
        activityLog.Name = "changed";
        compensationData["Receipt"] = "changed";
        variables["Tenant"] = "changed";
        activityException.Name = "changed";
        subscription.Address = CompensateAddress;
        source.Itinerary.Clear();
        source.ActivityLogs.Clear();
        source.CompensateLogs.Clear();
        source.Variables.Clear();
        source.ActivityExceptions.Clear();
        source.Subscriptions.Clear();

        Assert.Equal("Execute", Assert.Single(sanitized.Itinerary).Name);
        Assert.Equal("original", sanitized.Itinerary[0].Arguments["argument"]);
        Assert.Equal("Execute", Assert.Single(sanitized.ActivityLogs).Name);
        Assert.Equal("original", Assert.Single(sanitized.CompensateLogs).Data["receipt"]);
        Assert.Equal("original", sanitized.Variables["tenant"]);
        Assert.Equal("Execute", Assert.Single(sanitized.ActivityExceptions).Name);
        Assert.Equal(ExecuteAddress, Assert.Single(sanitized.Subscriptions).Address);

        AssertReadOnly(sanitized.Itinerary, activity);
        AssertReadOnly(sanitized.ActivityLogs, activityLog);
        AssertReadOnly(sanitized.CompensateLogs, compensateLog);
        AssertReadOnly(sanitized.ActivityExceptions, activityException);
        AssertReadOnly(sanitized.Subscriptions, subscription);
        IDictionary<string, object> readOnlyVariables =
            Assert.IsAssignableFrom<IDictionary<string, object>>(sanitized.Variables);
        Assert.True(readOnlyVariables.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => readOnlyVariables["added"] = "value");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ARGUMENTS", "case-insensitive-precedence-and-null-fallback")]
    public void HostContexts_MergeVariablesCaseInsensitivelyAndUseThemForNullActivityValues()
    {
        var variables = new Dictionary<string, object>
        {
            ["CaSeKeY"] = "variable",
            [nameof(ArgumentValues.Fallback)] = "fallback",
            ["MiXeDFallback"] = "mixed-fallback",
        };
        var activity = new MutableActivity(
            "Execute",
            ExecuteAddress,
            new Dictionary<string, object>
            {
                ["casekey"] = "activity",
                [nameof(ArgumentValues.Fallback)] = null!,
                ["mixedfallback"] = null!,
                [nameof(ArgumentValues.ExplicitNull)] = null!,
            });
        var executeContext = new HostExecuteContext<ArgumentValues>(
            null,
            CreateConsumeContext(CreateRoutingSlip(itinerary: [activity], variables: variables)));

        Guid executionId = NewId.NextGuid();
        var compensateContext = new HostCompensateContext<LogValues>(CreateConsumeContext(CreateRoutingSlip(
            activityLogs: [ActivityLog(executionId, "Compensate")],
            compensateLogs:
            [
                CompensateLog(executionId, new Dictionary<string, object>
                {
                    ["casekey"] = "log",
                    [nameof(LogValues.Fallback)] = null!,
                    ["mixedfallback"] = null!,
                    [nameof(LogValues.ExplicitNull)] = null!,
                    [nameof(LogValues.Receipt)] = "receipt",
                }),
            ],
            variables: variables)));

        Assert.Equal("activity", executeContext.Arguments.CaseKey);
        Assert.Equal("fallback", executeContext.Arguments.Fallback);
        Assert.Equal("mixed-fallback", executeContext.Arguments.MixedFallback);
        Assert.Null(executeContext.Arguments.ExplicitNull);
        Assert.Equal("log", compensateContext.Log.CaseKey);
        Assert.Equal("fallback", compensateContext.Log.Fallback);
        Assert.Equal("mixed-fallback", compensateContext.Log.MixedFallback);
        Assert.Null(compensateContext.Log.ExplicitNull);
        Assert.Equal("receipt", compensateContext.Log.Receipt);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ARGUMENTS", "execute-context-binds-first-itinerary-entry")]
    public void HostExecuteContext_BindsTheFirstItineraryEntryWhenMoreRemain()
    {
        var first = new MutableActivity(
            "First",
            ExecuteAddress,
            new Dictionary<string, object> { [nameof(ArgumentValues.CaseKey)] = "first" });
        var second = new MutableActivity(
            "Second",
            ExecuteAddress,
            new Dictionary<string, object> { [nameof(ArgumentValues.CaseKey)] = "second" });

        var context = new HostExecuteContext<ArgumentValues>(
            null,
            CreateConsumeContext(CreateRoutingSlip(itinerary: [first, second])));

        Assert.Equal("First", context.ActivityName);
        Assert.Equal("first", context.Arguments.CaseKey);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ARGUMENTS", "missing-entries-and-deserialization-faults")]
    public void HostContexts_RejectMissingEntriesAndWrapDeserializationFailures()
    {
        MutableRoutingSlip empty = CreateRoutingSlip();
        MutableRoutingSlip missingTrackingNumber = CreateRoutingSlip();
        missingTrackingNumber.TrackingNumber = Guid.Empty;
        MutableRoutingSlip missingCreateTimestamp = CreateRoutingSlip();
        missingCreateTimestamp.CreateTimestamp = default;
        var invalidActivity = new MutableActivity(
            "Execute",
            ExecuteAddress,
            new Dictionary<string, object> { [nameof(NumericArguments.Count)] = "not-a-number" });
        Guid compensationExecutionId = NewId.NextGuid();
        MutableRoutingSlip invalidCompensation = CreateRoutingSlip(
            activityLogs: [ActivityLog(compensationExecutionId, "Compensate")],
            compensateLogs:
            [
                CompensateLog(compensationExecutionId,
                    new Dictionary<string, object> { [nameof(NumericArguments.Count)] = "not-a-number" }),
            ]);

        Assert.Equal("context", Assert.Throws<ArgumentException>(() =>
            new HostExecuteContext<ArgumentValues>(null, CreateConsumeContext(empty))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentException>(() =>
            new HostCompensateContext<LogValues>(CreateConsumeContext(empty))).ParamName);
        Assert.Contains("tracking number", Assert.Throws<SerializationException>(() =>
            new SanitizedRoutingSlip(CreateConsumeContext(missingTrackingNumber))).Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("creation timestamp", Assert.Throws<SerializationException>(() =>
            new SanitizedRoutingSlip(CreateConsumeContext(missingCreateTimestamp))).Message, StringComparison.OrdinalIgnoreCase);

        RoutingSlipArgumentException failure = Assert.Throws<RoutingSlipArgumentException>(() =>
            new HostExecuteContext<NumericArguments>(null, CreateConsumeContext(CreateRoutingSlip(itinerary: [invalidActivity]))));
        RoutingSlipArgumentException compensationFailure = Assert.Throws<RoutingSlipArgumentException>(() =>
            new HostCompensateContext<NumericArguments>(CreateConsumeContext(invalidCompensation)));

        Assert.Contains("could not be read", failure.Message, StringComparison.Ordinal);
        Assert.NotNull(failure.InnerException);
        Assert.Contains("could not be read", compensationFailure.Message, StringComparison.Ordinal);
        Assert.NotNull(compensationFailure.InnerException);
    }

    private static MutableRoutingSlip CreateRoutingSlip(
        IReadOnlyList<IActivity>? itinerary = null,
        IReadOnlyList<IActivityLog>? activityLogs = null,
        IReadOnlyList<ICompensateLog>? compensateLogs = null,
        IReadOnlyDictionary<string, object>? variables = null,
        IReadOnlyList<IActivityException>? activityExceptions = null,
        IReadOnlyList<ISubscription>? subscriptions = null) =>
        new()
        {
            TrackingNumber = NewId.NextGuid(),
            CreateTimestamp = CreatedAt,
            Itinerary = itinerary?.ToList() ?? [],
            ActivityLogs = activityLogs?.ToList() ?? [],
            CompensateLogs = compensateLogs?.ToList() ?? [],
            Variables = variables is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(variables),
            ActivityExceptions = activityExceptions?.ToList() ?? [],
            Subscriptions = subscriptions?.ToList() ?? [],
        };

    private static MutableActivityLog ActivityLog(Guid executionId, string name) =>
        new(executionId, name);

    private static MutableCompensateLog CompensateLog(Guid executionId, IReadOnlyDictionary<string, object> data) =>
        new(executionId, data);

    private static ConsumeContext<IRoutingSlip> CreateConsumeContext(
        IRoutingSlip routingSlip,
        OutgoingMessageRecorder? outgoingMessages = null)
    {
        SerializerContext serializerContext = CreateSerializerContext(routingSlip);
        ConsumeContext<IRoutingSlip> context = InMemoryOutboxTestContextFactory.Create(
            routingSlip,
            TestContext.Current.CancellationToken,
            outgoingMessages: outgoingMessages,
            serializerContext: serializerContext);
        context.SetTimeProvider(new FakeTimeProvider(CreatedAt));
        return context;
    }

    private static SerializerContext CreateSerializerContext(IRoutingSlip routingSlip)
    {
        IObjectDeserializer deserializer = ServiceBusMetadataJson.ObjectDeserializer;
        var metadata = new EnvelopeMessageContext(new JsonMessageEnvelope(), deserializer);
        return new SystemTextJsonSerializerContext(
            deserializer,
            ServiceBusMetadataJson.Options,
            SystemTextJsonMessageSerializer.JsonContentType,
            metadata,
            [MessageUrn.ForTypeString<IRoutingSlip>()],
            message: routingSlip);
    }

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.ThrowsAny<ArgumentException>(action).ParamName);

    private static void AssertReadOnly<T>(IReadOnlyList<T> values, T replacement)
    {
        ICollection<T> collection = Assert.IsAssignableFrom<ICollection<T>>(values);
        IList<T> list = Assert.IsAssignableFrom<IList<T>>(values);

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list[0] = replacement);
    }

    private sealed class ArgumentValues
    {
        public string CaseKey { get; set; } = null!;
        public string Fallback { get; set; } = null!;
        public string MixedFallback { get; set; } = null!;
        public string? ExplicitNull { get; set; } = "sentinel";
    }

    private sealed class LogValues
    {
        public string? CaseKey { get; set; }
        public string? Fallback { get; set; }
        public string? MixedFallback { get; set; }
        public string? ExplicitNull { get; set; } = "sentinel";
        public string Receipt { get; set; } = null!;
    }

    private sealed class NumericArguments
    {
        public int Count { get; set; }
    }

    private sealed class MutableRoutingSlip : IRoutingSlip
    {
        public Guid TrackingNumber { get; set; }
        public DateTimeOffset CreateTimestamp { get; set; }
        public List<IActivity> Itinerary { get; set; } = [];
        public List<IActivityLog> ActivityLogs { get; set; } = [];
        public List<ICompensateLog> CompensateLogs { get; set; } = [];
        public Dictionary<string, object> Variables { get; set; } = [];
        public List<IActivityException> ActivityExceptions { get; set; } = [];
        public List<ISubscription> Subscriptions { get; set; } = [];

        IReadOnlyList<IActivity> IRoutingSlip.Itinerary => Itinerary;
        IReadOnlyList<IActivityLog> IRoutingSlip.ActivityLogs => ActivityLogs;
        IReadOnlyList<ICompensateLog> IRoutingSlip.CompensateLogs => CompensateLogs;
        IReadOnlyDictionary<string, object> IRoutingSlip.Variables => Variables;
        IReadOnlyList<IActivityException> IRoutingSlip.ActivityExceptions => ActivityExceptions;
        IReadOnlyList<ISubscription> IRoutingSlip.Subscriptions => Subscriptions;
    }

    private sealed class MutableActivity(string name, Uri address, IReadOnlyDictionary<string, object> arguments) : IActivity
    {
        public string Name { get; set; } = name;
        public Uri Address { get; set; } = address;
        public IReadOnlyDictionary<string, object> Arguments { get; set; } = arguments;
    }

    private sealed class MutableActivityLog(Guid executionId, string name) : IActivityLog
    {
        public Guid ExecutionId { get; set; } = executionId;
        public string Name { get; set; } = name;
        public DateTimeOffset Timestamp { get; set; } = CreatedAt;
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(1);
        public HostInfo Host { get; set; } = HostMetadataCache.Host;
    }

    private sealed class MutableCompensateLog(Guid executionId, IReadOnlyDictionary<string, object> data) : ICompensateLog
    {
        public Guid ExecutionId { get; set; } = executionId;
        public Uri Address { get; set; } = CompensateAddress;
        public IReadOnlyDictionary<string, object> Data { get; set; } = data;
    }

    private sealed class MutableActivityException(Guid executionId, string name) : IActivityException
    {
        public Guid ExecutionId { get; set; } = executionId;
        public DateTimeOffset Timestamp { get; set; } = CreatedAt;
        public TimeSpan Elapsed { get; set; } = TimeSpan.FromSeconds(1);
        public string Name { get; set; } = name;
        public HostInfo Host { get; set; } = HostMetadataCache.Host;
        public ExceptionInfo ExceptionInfo { get; set; } = new FaultExceptionInfo(new InvalidOperationException("expected"));
    }

    private sealed class MutableSubscription(Uri address) : ISubscription
    {
        public Uri Address { get; set; } = address;
        public RoutingSlipEvents Events { get; set; } = RoutingSlipEvents.Completed;
        public RoutingSlipEventContents Include { get; set; } = RoutingSlipEventContents.All;
        public string? ActivityName { get; set; }
        public MessageEnvelope? Message { get; set; }
    }
}
