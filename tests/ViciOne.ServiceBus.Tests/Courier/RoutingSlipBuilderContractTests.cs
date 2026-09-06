using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier.Contracts;
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
        Assert.Throws<ArgumentException>(() => builder.AddVariable("", "value"));
        Assert.Throws<ArgumentException>(() => builder.AddVariable(" ", new object()));
        Assert.Throws<ArgumentNullException>(() => builder.SetVariables((object)null!));
        Assert.Throws<ArgumentNullException>(() => builder.SetVariables((IEnumerable<KeyValuePair<string, object>>)null!));
        Assert.Throws<ArgumentNullException>(() => builder.AddSubscription(null!, RoutingSlipEvents.All));
        Assert.Throws<ArgumentNullException>(() => builder.AddSubscription(null!, RoutingSlipEvents.All, RoutingSlipEventContents.All));
        Assert.Throws<ArgumentException>(() => builder.AddSubscription(address, RoutingSlipEvents.All, RoutingSlipEventContents.All, " "));
        Assert.Throws<ArgumentNullException>(() => builder.AddActivityLog(null!, "Activity", NewId.NextGuid(), DateTimeOffset.UtcNow, TimeSpan.Zero));
        Assert.Throws<ArgumentNullException>(() => builder.AddCompensateLog(NewId.NextGuid(), null!, new Dictionary<string, object>()));
        Assert.Throws<ArgumentNullException>(() => builder.AddCompensateLog(NewId.NextGuid(), address, null!));
        Assert.Throws<ArgumentNullException>(() => builder.AddActivityException((ActivityException)null!));
        Assert.Throws<ArgumentNullException>(() => RoutingSlipBuilder.GetObjectAsDictionary(null!));
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
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
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
}
