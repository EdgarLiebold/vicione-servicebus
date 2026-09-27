using System.Collections.Concurrent;
using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS.AMQP.Message;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqHeaderRoundTripTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [InlineData(ActiveMqBroker.ArtemisFlavor)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "native-groups-remain-isolated-across-reused-endpoint")]
    public async Task NativeGroups_RemainIsolatedAcrossReusedEndpointAsync(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "group-isolation");
        string queueName = fixture.Name("input");
        GroupMessage[] messages =
        [
            new(Guid.NewGuid(), "first grouped payload"),
            new(Guid.NewGuid(), "ungrouped successor"),
            new(Guid.NewGuid(), "independent second group"),
            new(Guid.NewGuid(), "explicit zero sequence"),
        ];
        string?[] groups = ["group-a", null, "group-b", "group-a"];
        int?[] sequences = [7, null, 19, 0];
        var observations = new ConcurrentQueue<GroupObservation>();
        var received = NewObservation<bool>();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<GroupMessage>(context =>
                {
                    try
                    {
                        var transport = Assert.IsType<ActiveMqReceiveContext>(context.Advanced().ReceiveContext);
                        (string? group, int sequence) = transport.TransportMessage switch
                        {
                            ActiveMQMessage native => (native.GroupID, native.GroupSequence),
                            NmsMessage native => (native.NMSXGroupId, native.NMSXGroupSeq),
                            _ => throw new InvalidOperationException("Expected a native OpenWire or AMQP message."),
                        };
                        observations.Enqueue(new GroupObservation(context.Message, context.MessageId,
                            transport.GroupId, transport.GroupSequence, group, sequence,
                            transport.GetTransportProperties()));
                        if (observations.Count >= messages.Length)
                            received.TrySetResult(true);
                        return Task.CompletedTask;
                    }
                    catch (Exception exception)
                    {
                        received.TrySetException(exception);
                        return Task.FromException(exception);
                    }
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            for (int index = 0; index < messages.Length; index++)
            {
                int current = index;
                await input.SendAsync(messages[current], context =>
                {
                    context.MessageId = messages[current].Id;
                    ActiveMqSendContext native = context.GetPayload<ActiveMqSendContext>();
                    native.GroupId = groups[current];
                    native.GroupSequence = sequences[current];
                }, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
            await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            Assert.Equal(messages.Length, observations.Count);
            for (int index = 0; index < messages.Length; index++)
            {
                GroupObservation actual = Assert.Single(observations, item => item.Message.Id == messages[index].Id);
                Assert.Equal(messages[index], actual.Message);
                Assert.Equal(messages[index].Id, actual.MessageId);
                Assert.Equal(groups[index], actual.GroupId);
                Assert.Equal(sequences[index] ?? 0, actual.GroupSequence);
                Assert.Equal(groups[index], actual.NativeGroupId);
                Assert.Equal(sequences[index] ?? 0, actual.NativeGroupSequence);
                if (groups[index] is null)
                    Assert.True(actual.Properties is null || !actual.Properties.ContainsKey("AMQ-GroupId"));
                else
                    Assert.Equal(groups[index], Assert.IsType<string>(actual.Properties!["AMQ-GroupId"]));
                if (sequences[index] is null or 0)
                    Assert.True(actual.Properties is null || !actual.Properties.ContainsKey("AMQ-GroupSequence"));
                else
                    Assert.Equal(sequences[index], Assert.IsType<int>(actual.Properties!["AMQ-GroupSequence"]));
            }
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "non-native-scalars-cross-classic-provider-boundaries")]
    public Task NonNativeScalars_CrossClassicProviderBoundariesAsync(string flavor) =>
        AssertHeaderRoundTripAsync(flavor);

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "non-native-scalars-cross-artemis-provider-boundary")]
    public Task NonNativeScalars_CrossArtemisProviderBoundaryAsync() =>
        AssertHeaderRoundTripAsync(ActiveMqBroker.ArtemisFlavor);

    private static async Task AssertHeaderRoundTripAsync(string flavor)
    {
        Guid identifier = Guid.NewGuid();
        byte[] bytes = [0, 127, 255];
        HeaderSnapshot actual = await SendAndReceiveAsync(flavor, identifier, bytes);

        Assert.Equal(identifier, actual.MessageId);
        Assert.Equal("12.5", Assert.IsType<string>(actual.Decimal));
        Assert.Equal(identifier.ToString(), Assert.IsType<string>(actual.Identifier));
        Assert.Equal(bool.FalseString, Assert.IsType<string>(actual.Boolean));
        Assert.False(actual.HasUnsupportedBytes);
    }

    private static async Task<HeaderSnapshot> SendAndReceiveAsync(string flavor, Guid identifier, byte[] bytes)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "headerrtrip");
        string queueName = fixture.Name("input");
        var received = NewObservation<HeaderSnapshot>();
        IBusControl bus = CreateBus(fixture, queueName, received);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpointAsync(
                new Uri($"queue:{queueName}"),
                cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.SendAsync(
                new HeaderMessage(identifier),
                context => SetHeaders(context, identifier, bytes),
                cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            return await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static IBusControl CreateBus(
        ActiveMqBroker fixture,
        string queueName,
        TaskCompletionSource<HeaderSnapshot> received) =>
        Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<HeaderMessage>(context => ObserveHeadersAsync(context, received));
            });
        });

    private static void SetHeaders(SendContext context, Guid identifier, byte[] bytes)
    {
        context.Headers.Set("APlusDecimal", 12.5m);
        context.Headers.Set("APlusIdentifier", identifier);
        context.Headers.Set("APlusBoolean", false);
        context.Headers.Set("APlusBytes", bytes);
    }

    private static Task ObserveHeadersAsync(
        ConsumeContext<HeaderMessage> context,
        TaskCompletionSource<HeaderSnapshot> received)
    {
        Headers headers = context.Advanced().ReceiveContext.TransportHeaders;
        try
        {
            received.TrySetResult(new HeaderSnapshot(
                context.Message.MessageId,
                GetRequiredHeader(headers, "APlusDecimal"),
                GetRequiredHeader(headers, "APlusIdentifier"),
                GetRequiredHeader(headers, "APlusBoolean"),
                headers.TryGetHeader("APlusBytes", out _)));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            received.TrySetException(exception);
            return Task.FromException(exception);
        }
    }

    private static object GetRequiredHeader(Headers headers, string key)
    {
        Assert.True(headers.TryGetHeader(key, out object? value), $"Missing transport header '{key}'.");
        return value;
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record HeaderMessage(Guid MessageId);

    private sealed record GroupMessage(Guid Id, string Text);

    private sealed record GroupObservation(
        GroupMessage Message,
        Guid? MessageId,
        string? GroupId,
        int GroupSequence,
        string? NativeGroupId,
        int NativeGroupSequence,
        IDictionary<string, object>? Properties);

    private sealed record HeaderSnapshot(
        Guid MessageId,
        object Decimal,
        object Identifier,
        object Boolean,
        bool HasUnsupportedBytes);
}
