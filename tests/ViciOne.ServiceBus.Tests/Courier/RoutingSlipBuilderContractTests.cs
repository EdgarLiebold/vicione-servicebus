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
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-CLOCK", "create-timestamp-from-injected-clock")]
    public void Builder_UsesTheInjectedClockForTheExactCreateTimestamp()
    {
        DateTimeOffset now = new(2042, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(now));
        builder.AddActivity("Clock", new Uri("loopback://localhost/clock"));

        RoutingSlip routingSlip = builder.Build();

        Assert.Equal(now.UtcDateTime, routingSlip.CreateTimestamp);
        Assert.Equal(DateTimeKind.Utc, routingSlip.CreateTimestamp.Kind);
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
    public async Task CyclicArgumentGraph_FailsWithTheSerializationCause()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-cycle");
        await harness.Start(cancellationToken);

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
                harness.Bus.Execute(builder.Build(), cancellationToken).WaitAsync(timeout, cancellationToken));

            Assert.IsType<JsonException>(exception.InnerException);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SERIALIZATION", "transport-round-trip-with-subscription")]
    public async Task RoutingSlipTransport_RoundTripsItsActivityAndSubscriptionExactly()
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
        await harness.Start(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity("Serialized", new Uri(harness.BaseAddress, "execute-serialized"), new { Value = 27 });
            builder.AddSubscription(
                new Uri(harness.BaseAddress, "events"),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted,
                RoutingSlipEventContents.All);

            await harness.InputQueueSendEndpoint.Send(builder.Build(), cancellationToken);
            RoutingSlip actual = (await received.Task.WaitAsync(timeout, cancellationToken)).Message;

            Assert.Equal(trackingNumber, actual.TrackingNumber);
            Assert.Equal("Serialized", Assert.Single(actual.Itinerary).Name);
            Subscription subscription = Assert.Single(actual.Subscriptions);
            Assert.Equal(RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted, subscription.Events);
            Assert.Equal(RoutingSlipEventContents.All, subscription.Include);
        }
        finally
        {
            await harness.Stop();
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
}
