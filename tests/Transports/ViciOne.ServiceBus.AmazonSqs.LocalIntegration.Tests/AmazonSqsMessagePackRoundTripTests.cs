using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.MessagePack;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

public sealed class AmazonSqsMessagePackRoundTripTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-MESSAGEPACK", "queue-send-and-sns-envelope-publish-roundtrip-binary-payload")]
    public async Task QueueSendAndSnsPublish_RoundTripTypedBinaryMessagesAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("messagepack");
        string directQueueName = fixture.Name("direct");
        string notificationQueueName = fixture.Name("notification");
        var sent = NewObservation();
        var published = NewObservation();
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ClearSerialization();
            configurator.UseMessagePackSerializer();
            configurator.ReceiveEndpoint(directQueueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<BinaryRoundTripMessage>(context =>
                {
                    sent.TrySetResult(Observe(context));
                    return Task.CompletedTask;
                });
            });
            configurator.ReceiveEndpoint(notificationQueueName, endpoint =>
            {
                endpoint.RequireSnsNotificationEnvelope();
                endpoint.Handler<BinaryRoundTripMessage>(context =>
                {
                    published.TrySetResult(Observe(context));
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{directQueueName}"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            var sentMessage = new BinaryRoundTripMessage(Guid.NewGuid(), "send", [0x00, 0x7f, 0x80, 0xff]);
            var publishedMessage = new BinaryRoundTripMessage(Guid.NewGuid(), "publish", [0xff, 0x80, 0x7f, 0x00]);

            await endpoint.SendAsync(sentMessage, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync(publishedMessage, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            AssertObservation(sentMessage, await sent.Task.WaitAsync(fixture.OperationTimeout, cancellationToken), false);
            AssertObservation(publishedMessage, await published.Task.WaitAsync(fixture.OperationTimeout, cancellationToken), true);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    static void AssertObservation(BinaryRoundTripMessage expected, BodyObservation actual, bool expectedSnsEnvelope)
    {
        Assert.Equal(expected.Id, actual.Message.Id);
        Assert.Equal(expected.Kind, actual.Message.Kind);
        Assert.Equal(expected.Payload, actual.Message.Payload);
        TransportTextMessageBody body = Assert.IsAssignableFrom<TransportTextMessageBody>(actual.Body);
        Assert.Equal("application/vnd.vicione.servicebus+msgpack", actual.ContentType);
        Assert.NotEmpty(actual.FirstCopy);
        Assert.Equal(actual.FirstCopy, actual.SecondCopy);
        Assert.NotSame(actual.FirstCopy, actual.SecondCopy);
        string transportText = body.GetRequiredTransportText();
        Assert.Equal(Encoding.UTF8.GetBytes(transportText), actual.FirstCopy);
        Assert.Equal(expectedSnsEnvelope, actual.TopicArn is not null);

        string messagePackCarrier;
        if (expectedSnsEnvelope)
        {
            using JsonDocument notification = JsonDocument.Parse(transportText);
            Assert.Equal("Notification", notification.RootElement.GetProperty("Type").GetString());
            Assert.Equal(actual.TopicArn, notification.RootElement.GetProperty("TopicArn").GetString());
            messagePackCarrier = notification.RootElement.GetProperty("Message").GetString()
                ?? throw new InvalidDataException("The Amazon SNS notification did not contain its Message carrier.");
        }
        else
            messagePackCarrier = transportText;

        Assert.NotEmpty(Convert.FromBase64String(messagePackCarrier));
    }

    static BodyObservation Observe(ConsumeContext<BinaryRoundTripMessage> context)
    {
        ReceiveContext receiveContext = context.Advanced().ReceiveContext;
        MessageBody body = receiveContext.Body;
        return new BodyObservation(
            context.Message,
            body,
            body.ToArray(),
            body.ToArray(),
            receiveContext.ContentType.MediaType,
            receiveContext.TransportHeaders.Get<string>("TopicArn"));
    }

    static TaskCompletionSource<BodyObservation> NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    sealed record BinaryRoundTripMessage(Guid Id, string Kind, byte[] Payload);

    sealed record BodyObservation(
        BinaryRoundTripMessage Message,
        MessageBody Body,
        byte[] FirstCopy,
        byte[] SecondCopy,
        string ContentType,
        string? TopicArn);
}
