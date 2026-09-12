using ViciOne.ServiceBus.MessagePack;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerMessagePackRoundTripTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQLSERVER-MESSAGEPACK", "send-and-publish-use-binary-storage-roundtrip")]
    public async Task SendAndPublish_RoundTripTypedMessagesThroughBinaryStorageAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "messagepack-binary",
            cancellationToken);
        string queueName = fixture.Name("messagepack-input");
        var sent = NewObservation();
        var published = NewObservation();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ClearSerialization();
            configurator.UseMessagePackSerializer();
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<BinaryRoundTripMessage>(context =>
            {
                MessageBody body = context.Advanced().ReceiveContext.Body;
                var observation = new BodyObservation(
                    context.Message,
                    body,
                    body.ToArray(),
                    body.ToArray(),
                    context.Advanced().ReceiveContext.ContentType.MediaType);
                (context.Message.Kind == "send" ? sent : published).TrySetResult(observation);
                return Task.CompletedTask;
            }));
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            var sentMessage = new BinaryRoundTripMessage(Guid.NewGuid(), "send", [0x00, 0x7f, 0x80, 0xff]);
            var publishedMessage = new BinaryRoundTripMessage(Guid.NewGuid(), "publish", [0xff, 0x80, 0x7f, 0x00]);

            await endpoint.SendAsync(sentMessage, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync(publishedMessage, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            AssertObservation(sentMessage, await sent.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            AssertObservation(publishedMessage, await published.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    static void AssertObservation(BinaryRoundTripMessage expected, BodyObservation actual)
    {
        Assert.Equal(expected.Id, actual.Message.Id);
        Assert.Equal(expected.Kind, actual.Message.Kind);
        Assert.Equal(expected.Payload, actual.Message.Payload);
        Assert.IsType<BinaryMessageBody>(actual.Body);
        Assert.Equal("application/vnd.vicione.servicebus+msgpack", actual.ContentType);
        Assert.NotEmpty(actual.FirstCopy);
        Assert.Equal(actual.FirstCopy, actual.SecondCopy);
        Assert.NotSame(actual.FirstCopy, actual.SecondCopy);
    }

    static TaskCompletionSource<BodyObservation> NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    sealed record BinaryRoundTripMessage(Guid Id, string Kind, byte[] Payload);

    sealed record BodyObservation(
        BinaryRoundTripMessage Message,
        MessageBody Body,
        byte[] FirstCopy,
        byte[] SecondCopy,
        string ContentType);
}
