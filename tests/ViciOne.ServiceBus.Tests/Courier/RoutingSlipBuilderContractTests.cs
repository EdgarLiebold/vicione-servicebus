using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipBuilderContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "constructor-boundaries")]
    public void Constructors_RejectInvalidIdentityAndNullStateInputs()
    {
        RoutingSlip routingSlip = new RoutingSlipBuilder(NewId.NextGuid()).Build();

        Assert.Throws<ArgumentException>(() => new RoutingSlipBuilder(Guid.Empty));
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipBuilder(null!, activities => activities));
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipBuilder(routingSlip, (Func<IEnumerable<Activity>, IEnumerable<Activity>>)null!));
        Assert.Throws<InvalidOperationException>(() => new RoutingSlipBuilder(routingSlip, _ => null!));
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipBuilder(routingSlip, null!, []));
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipBuilder(routingSlip, [], null!));
        Assert.Throws<ArgumentNullException>(() => new RoutingSlipBuilder(routingSlip, (IEnumerable<CompensateLog>)null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "mutation-boundaries")]
    public void MutationMethods_RejectInvalidRequiredInputs()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        var address = new Uri("loopback://localhost/courier-boundary");

        Assert.Throws<ArgumentException>(() => builder.AddActivity(" ", address));
        Assert.Throws<ArgumentNullException>(() => builder.AddActivity("Activity", null!));
        Assert.Throws<ArgumentNullException>(() => builder.AddActivity("Activity", address, (object)null!));
        Assert.Throws<ArgumentNullException>(() => builder.AddActivity("Activity", address, (IDictionary<string, object>)null!));
        Assert.Throws<ArgumentException>(() => builder.SetVariable("", "value"));
        Assert.Throws<ArgumentException>(() => builder.SetVariable(" ", new object()));
        Assert.Throws<ArgumentNullException>(() => builder.SetVariables((object)null!));
        Assert.Throws<ArgumentNullException>(() => builder.SetVariables((IEnumerable<KeyValuePair<string, object>>)null!));
        Assert.Throws<ArgumentNullException>(() => builder.AddSubscription(null!, RoutingSlipEvents.All));
        Assert.Throws<ArgumentNullException>(() => builder.AddSubscription(null!, RoutingSlipEvents.All, RoutingSlipEventContents.All));
        Assert.Throws<ArgumentException>(() => builder.AddSubscription(address, RoutingSlipEvents.All, RoutingSlipEventContents.All, " "));
        Assert.Throws<ArgumentNullException>(() => builder.AddActivityLog(null!, "Activity", NewId.NextGuid(), DateTimeOffset.UtcNow, TimeSpan.Zero));
        Assert.Equal("activityTrackingNumber", Assert.Throws<ArgumentException>(() =>
            builder.AddActivityLog(HostMetadataCache.Host, "Activity", Guid.Empty, DateTimeOffset.UtcNow, TimeSpan.Zero)).ParamName);
        Assert.Equal("duration", Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.AddActivityLog(HostMetadataCache.Host, "Activity", NewId.NextGuid(), DateTimeOffset.UtcNow, TimeSpan.FromTicks(-1))).ParamName);
        Assert.Equal("activityTrackingNumber", Assert.Throws<ArgumentException>(() =>
            builder.AddCompensateLog(Guid.Empty, address, new Dictionary<string, object>())).ParamName);
        Assert.Throws<ArgumentNullException>(() => builder.AddCompensateLog(NewId.NextGuid(), null!, new Dictionary<string, object>()));
        Assert.Throws<ArgumentNullException>(() => builder.AddCompensateLog(NewId.NextGuid(), address, null!));
        Assert.Equal("activityTrackingNumber", Assert.Throws<ArgumentException>(() => builder.AddActivityException(
            HostMetadataCache.Host, "Activity", Guid.Empty, DateTimeOffset.UtcNow, TimeSpan.Zero, new InvalidOperationException())).ParamName);
        Assert.Equal("elapsed", Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddActivityException(
            HostMetadataCache.Host, "Activity", NewId.NextGuid(), DateTimeOffset.UtcNow, TimeSpan.FromTicks(-1),
            new FaultExceptionInfo(new InvalidOperationException()))).ParamName);
        Assert.Throws<ArgumentNullException>(() => builder.AddActivityException((ActivityException)null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-ISOLATION", "build-produces-detached-read-only-state")]
    public void Build_ProducesADetachedReadOnlySnapshotWhileTheBuilderRemainsMutable()
    {
        var firstAddress = new Uri("loopback://localhost/first");
        var secondAddress = new Uri("loopback://localhost/second");
        var arguments = new Dictionary<string, object> { ["state"] = "original" };
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddActivity("First", firstAddress, arguments);
        builder.SetVariable("tenant", "north");
        builder.AddSubscription(firstAddress, RoutingSlipEvents.Completed);

        RoutingSlip first = builder.Build();
        arguments["state"] = "caller-mutated";
        builder.SetVariable("tenant", "south");
        builder.AddActivity("Second", secondAddress);
        builder.AddSubscription(secondAddress, RoutingSlipEvents.Faulted);

        Activity activity = Assert.Single(first.Itinerary);
        Assert.Equal("original", activity.Arguments["STATE"]);
        Assert.Equal("north", first.Variables["TENANT"]);
        Assert.Single(first.Subscriptions);
        Assert.True(first.Itinerary.IsReadOnly);
        Assert.True(first.ActivityLogs.IsReadOnly);
        Assert.True(first.CompensateLogs.IsReadOnly);
        Assert.True(first.ActivityExceptions.IsReadOnly);
        Assert.True(first.Subscriptions.IsReadOnly);
        Assert.True(Assert.IsAssignableFrom<ICollection<KeyValuePair<string, object>>>(first.Variables).IsReadOnly);
        Assert.True(Assert.IsAssignableFrom<ICollection<KeyValuePair<string, object>>>(activity.Arguments).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => first.Itinerary.Clear());
        Assert.Throws<NotSupportedException>(() => first.Variables.Add("late", 1));
        Assert.Throws<NotSupportedException>(() => activity.Arguments.Add("late", 1));

        RoutingSlip second = builder.Build();
        Assert.Equal(2, second.Itinerary.Count);
        Assert.Equal(2, second.Subscriptions.Count);
        Assert.Equal("south", second.Variables["tenant"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ARGUMENTS", "object-defaults-filtered-explicit-dictionary-defaults-preserved")]
    public void ActivityArguments_FilterObjectDefaultsButPreserveExplicitDictionaryDefaults()
    {
        var address = new Uri("loopback://localhost/default-arguments");
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddActivity("Projected", address, new
        {
            Null = (string?)null,
            EmptyId = Guid.Empty,
            Count = 0,
            Enabled = false,
            Timestamp = default(DateTime),
            Present = 27,
        });
        builder.AddActivity("Explicit", address, new Dictionary<string, object>
        {
            ["EmptyId"] = Guid.Empty,
            ["Count"] = 0,
            ["Enabled"] = false,
            ["Timestamp"] = default(DateTime),
        });

        RoutingSlip routingSlip = builder.Build();

        Activity projected = routingSlip.Itinerary[0];
        Assert.Equal(27, Assert.Single(projected.Arguments).Value);
        Assert.Equal("present", Assert.Single(projected.Arguments).Key, ignoreCase: true);

        Activity explicitArguments = routingSlip.Itinerary[1];
        Assert.Equal(Guid.Empty, explicitArguments.Arguments["EmptyId"]);
        Assert.Equal(0, explicitArguments.Arguments["Count"]);
        Assert.Equal(false, explicitArguments.Arguments["Enabled"]);
        Assert.Equal(default(DateTime), explicitArguments.Arguments["Timestamp"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "variable-sequences-validate-atomically")]
    public void VariableSequences_RejectInvalidKeysWithoutApplyingEarlierEntries()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        KeyValuePair<string, object>[] values =
        [
            new("accepted", 1),
            new(" ", 2),
        ];

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.SetVariables(values));

        Assert.Equal("values", exception.ParamName);
        Assert.Empty(builder.Build().Variables);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "async-subscription-boundaries")]
    public async Task AsyncSubscriptions_ValidateInputsOwnCancellationAndRejectNullTasksAsync()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        var address = new Uri("loopback://localhost/courier-subscription");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var callbackCalled = false;

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            builder.AddSubscriptionAsync(
                null!, RoutingSlipEvents.All, _ => Task.CompletedTask, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            builder.AddSubscriptionAsync(
                address, RoutingSlipEvents.All, null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.AddSubscriptionAsync(
                address, RoutingSlipEvents.All, _ => null!, TestContext.Current.CancellationToken));

        Task canceled = builder.AddSubscriptionAsync(
            address,
            RoutingSlipEvents.All,
            _ =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            },
            cancellation.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.False(callbackCalled);
    }

    [Theory]
    [InlineData((int)RoutingSlipEvents.None)]
    [InlineData((int)RoutingSlipEvents.Supplemental)]
    [InlineData(0x20000)]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-FLAGS", "all-overloads-reject-empty-or-undefined-event-selections")]
    public void SubscriptionOverloads_RejectEmptyOrUndefinedEventSelectionsBeforeMutationOrCallback(int rawEvents)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        var address = new Uri("loopback://localhost/courier-subscription");
        RoutingSlipEvents events = (RoutingSlipEvents)rawEvents;
        var callbackCalled = false;
        Func<ISendEndpoint, Task> callback = _ =>
        {
            callbackCalled = true;
            return Task.CompletedTask;
        };

        AssertInvalidEvents(() => builder.AddSubscription(address, events));
        AssertInvalidEvents(() => builder.AddSubscription(address, events, RoutingSlipEventContents.All));
        AssertInvalidEvents(() => builder.AddSubscription(address, events, RoutingSlipEventContents.All, "ChargeCard"));
        AssertInvalidEvents(() => builder.AddSubscriptionAsync(address, events, callback));
        AssertInvalidEvents(() => builder.AddSubscriptionAsync(address, events, RoutingSlipEventContents.All, callback));
        AssertInvalidEvents(() => builder.AddSubscriptionAsync(address, events, RoutingSlipEventContents.All, "ChargeCard", callback));

        Assert.False(callbackCalled);
        Assert.Empty(builder.Build().Subscriptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-FLAGS", "all-content-overloads-reject-undefined-flags")]
    public void SubscriptionContentOverloads_RejectUndefinedFlagsBeforeMutationOrCallback()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        var address = new Uri("loopback://localhost/courier-subscription");
        var contents = (RoutingSlipEventContents)0x10;
        var callbackCalled = false;
        Func<ISendEndpoint, Task> callback = _ =>
        {
            callbackCalled = true;
            return Task.CompletedTask;
        };

        AssertInvalidContents(() => builder.AddSubscription(address, RoutingSlipEvents.Completed, contents));
        AssertInvalidContents(() => builder.AddSubscription(address, RoutingSlipEvents.Completed, contents, "ChargeCard"));
        AssertInvalidContents(() => builder.AddSubscriptionAsync(address, RoutingSlipEvents.Completed, contents, callback));
        AssertInvalidContents(() => builder.AddSubscriptionAsync(address, RoutingSlipEvents.Completed, contents, "ChargeCard", callback));

        Assert.False(callbackCalled);
        Assert.Empty(builder.Build().Subscriptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-FLAGS", "received-contract-state-is-revalidated")]
    public void ReceivedSubscriptions_RejectInvalidEventContentAndActivitySelections()
    {
        var address = new Uri("loopback://localhost/courier-subscription");

        SerializationException events = Assert.Throws<SerializationException>(() => new RoutingSlipSubscription(
            new StubSubscription(address, RoutingSlipEvents.None, RoutingSlipEventContents.All, null)));
        SerializationException contents = Assert.Throws<SerializationException>(() => new RoutingSlipSubscription(
            new StubSubscription(address, RoutingSlipEvents.Completed, (RoutingSlipEventContents)0x10, null)));
        SerializationException activity = Assert.Throws<SerializationException>(() => new RoutingSlipSubscription(
            new StubSubscription(address, RoutingSlipEvents.Completed, RoutingSlipEventContents.All, " ")));

        Assert.IsType<ArgumentOutOfRangeException>(events.InnerException);
        Assert.IsType<ArgumentOutOfRangeException>(contents.InnerException);
        Assert.IsType<ArgumentException>(activity.InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-ISOLATION", "no-public-mutable-empty-sentinel")]
    public void Builder_ExposesNoPublicMutableEmptyArgumentSentinel()
    {
        FieldInfo? field = typeof(RoutingSlipBuilder).GetField(
            "NoArguments",
            BindingFlags.Public | BindingFlags.Static);

        Assert.Null(field);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-API", "internal-transition-members-are-not-public")]
    public void Builder_PublicSurfaceExcludesInternalRoutingSlipTransitionMembers()
    {
        ConstructorInfo constructor = Assert.Single(typeof(RoutingSlipBuilder).GetConstructors());
        ParameterInfo[] parameters = constructor.GetParameters();

        Assert.Equal([typeof(Guid), typeof(TimeProvider)], parameters.Select(parameter => parameter.ParameterType));
        Assert.Null(typeof(RoutingSlipBuilder).GetProperty("SourceItinerary", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(RoutingSlipBuilder).GetMethod("AddActivityLog", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(RoutingSlipBuilder).GetMethod("AddCompensateLog", BindingFlags.Public | BindingFlags.Instance));
        Assert.DoesNotContain(
            typeof(RoutingSlipBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance),
            method => method.Name == "AddActivityException");
        Assert.Null(typeof(RoutingSlipBuilder).GetMethod("GetObjectAsDictionary", BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-ISOLATION", "no-argument-activities-are-immutable")]
    public void NoArgumentActivities_CannotContaminateAnotherBuilder()
    {
        string poisonKey = $"poison-{Guid.NewGuid():N}";
        IDictionary<string, object> arguments = BuildNoArgumentActivity().Arguments;
        Exception? mutationFailure = null;
        bool secondBuilderWasContaminated = false;

        try
        {
            mutationFailure = Record.Exception(() => arguments.Add(poisonKey, 27));
            if (mutationFailure is null)
                secondBuilderWasContaminated = BuildNoArgumentActivity().Arguments.ContainsKey(poisonKey);
        }
        finally
        {
            if (mutationFailure is null)
                arguments.Remove(poisonKey);
        }

        Assert.IsType<NotSupportedException>(mutationFailure);
        Assert.False(secondBuilderWasContaminated);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-CLOCK", "create-timestamp-from-injected-clock")]
    public void Builder_UsesTheInjectedClockForTheExactCreateTimestamp()
    {
        DateTimeOffset now = new(2042, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(now));
        builder.AddActivity("Clock", new Uri("loopback://localhost/clock"));

        RoutingSlip routingSlip = builder.Build();

        Assert.Equal(now, routingSlip.CreateTimestamp);
        Assert.Equal(TimeSpan.Zero, routingSlip.CreateTimestamp.Offset);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "interface-valued-argument")]
    public void InterfaceValuedArgument_IsMappedWithoutLosingTheConcreteValue()
    {
        Guid trackingNumber = NewId.NextGuid();
        var builder = new RoutingSlipBuilder(trackingNumber);
        var arguments = new InterfaceArguments
        {
            Name = "contract",
            Result = new ConcreteResult("mapped"),
        };

        builder.AddActivity("Interface", new Uri("loopback://localhost/execute-interface"), arguments);
        RoutingSlip routingSlip = builder.Build();

        Activity activity = Assert.Single(routingSlip.Itinerary);
        Assert.Equal("Interface", activity.Name);
        Assert.Equal("contract", Assert.IsType<string>(activity.Arguments["name"]));
        Assert.Equal("mapped", Assert.IsAssignableFrom<IResult>(activity.Arguments["result"]).Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "cyclic-object-graph-rejected")]
    public async Task CyclicArgumentGraph_FailsWithTheSerializationCauseAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-cycle");
        await harness.StartAsync(cancellationToken);

        try
        {
            var outer = new Outer();
            outer.Children = [new Child("first", outer), new Child("second", outer)];
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(
                "Cycle",
                new Uri(harness.BaseAddress, "execute-cycle"),
                new { Content = outer });

            SerializationException exception = await Assert.ThrowsAsync<SerializationException>(() =>
                harness.Bus.ExecuteAsync(builder.Build(), cancellationToken).WaitAsync(timeout, cancellationToken));

            Assert.IsType<JsonException>(exception.InnerException);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SERIALIZATION", "transport-round-trip-with-subscription")]
    public async Task RoutingSlipTransport_RoundTripsItsActivityAndSubscriptionExactlyAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<ConsumeContext<RoutingSlip>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-serialization");
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
            endpoint.Handler<RoutingSlip>(context =>
            {
                received.TrySetResult(context);
                return Task.CompletedTask;
            });
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity("Serialized", new Uri(harness.BaseAddress, "execute-serialized"), new { Value = 27 });
            builder.AddSubscription(
                new Uri(harness.BaseAddress, "events"),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted,
                RoutingSlipEventContents.All);

            await harness.InputQueueSendEndpoint.SendAsync(builder.Build(), cancellationToken);
            RoutingSlip actual = (await received.Task.WaitAsync(timeout, cancellationToken)).Message;

            Assert.Equal(trackingNumber, actual.TrackingNumber);
            Assert.Equal("Serialized", Assert.Single(actual.Itinerary).Name);
            Subscription subscription = Assert.Single(actual.Subscriptions);
            Assert.Equal(RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted, subscription.Events);
            Assert.Equal(RoutingSlipEventContents.All, subscription.Include);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    public interface IResult
    {
        string Value { get; }
    }

    public sealed record ConcreteResult(string Value) : IResult;

    public sealed class InterfaceArguments
    {
        public required string Name { get; init; }
        public required IResult Result { get; init; }
    }

    public sealed class Outer
    {
        public Child[] Children { get; set; } = [];
    }

    public sealed record Child(string Value, Outer Parent);

    private static Activity BuildNoArgumentActivity()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddActivity("NoArguments", new Uri("loopback://localhost/no-arguments"));

        return Assert.Single(builder.Build().Itinerary);
    }

    private static void AssertInvalidEvents(Action action)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal("events", exception.ParamName);
    }

    private static void AssertInvalidContents(Action action)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal("contents", exception.ParamName);
    }

    private sealed record StubSubscription(
        Uri Address,
        RoutingSlipEvents Events,
        RoutingSlipEventContents Include,
        string? ActivityName) : Subscription
    {
        public ViciOne.ServiceBus.Serialization.MessageEnvelope? Message => null;
    }
}
