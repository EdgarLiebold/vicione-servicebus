using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqHeaderRoundTripTests
{
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

    private sealed record HeaderSnapshot(
        Guid MessageId,
        object Decimal,
        object Identifier,
        object Boolean,
        bool HasUnsupportedBytes);
}
