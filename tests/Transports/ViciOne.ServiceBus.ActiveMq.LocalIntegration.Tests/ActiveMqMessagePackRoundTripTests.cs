using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.MessagePack;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqMessagePackRoundTripTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("REQ-VSB-BODY-CROSS-OWNER", "activemq-messagepack-send-publish-forward-binary-roundtrip")]
    public async Task SendPublishAndForward_PreserveMessagePackBinaryBodiesAsync(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "messagepack");
        string inputQueueName = fixture.Name("input");
        string forwardQueueName = fixture.Name("forward");
        var sent = NewObservation<BodyObservation<SentMessage>>();
        var published = NewObservation<BodyObservation<PublishedMessage>>();
        var forwarded = NewObservation<BodyObservation<ForwardedMessage>>();
        Uri forwardAddress = new($"queue:{forwardQueueName}");

        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ClearSerialization();
            configurator.UseMessagePackSerializer();
            configurator.MessageTopology.GetMessageTopology<PublishedMessage>().SetEntityName(fixture.Name("published"));
            configurator.ReceiveEndpoint(inputQueueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<SentMessage>(context =>
                {
                    sent.TrySetResult(Capture(context));
                    return Task.CompletedTask;
                });
                endpoint.Handler<PublishedMessage>(context =>
                {
                    published.TrySetResult(Capture(context));
                    return Task.CompletedTask;
                });
                endpoint.Handler<ForwardedMessage>(context => context.ForwardAsync(forwardAddress));
            });
            configurator.ReceiveEndpoint(forwardQueueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<ForwardedMessage>(context =>
                {
                    forwarded.TrySetResult(Capture(context));
                    return Task.CompletedTask;
                });
            });
        });

        var sentMessage = new SentMessage(
            Guid.Parse("f8c097e2-b224-4163-8759-b290a5ee6533"),
            "sent",
            [0x00, 0x7f, 0x80, 0xff, 0x01]);
        var publishedMessage = new PublishedMessage(
            Guid.Parse("ba0c2dbe-538c-4ab6-a2b4-1083bdbd08e7"),
            "published",
            [0xff, 0x00, 0xfe, 0x80, 0x02]);
        var forwardedMessage = new ForwardedMessage(
            Guid.Parse("b432ba21-a46a-4225-9ff1-744f02c2a17e"),
            "forwarded",
            [0x80, 0x00, 0xff, 0x7f, 0xfd]);
        Guid sentMessageId = Guid.Parse("b3296160-f03c-4690-8de5-dfcb589f16f6");
        Guid publishedMessageId = Guid.Parse("3a27bad4-792e-4d90-841b-d6c9ec0b70c1");
        Guid forwardedMessageId = Guid.Parse("a25a2277-3355-49ce-8f9a-fb9b2c28a108");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{inputQueueName}"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            await input.SendAsync(sentMessage, context => context.MessageId = sentMessageId, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync(publishedMessage, context => context.MessageId = publishedMessageId, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.SendAsync(forwardedMessage, context => context.MessageId = forwardedMessageId, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            BodyObservation<SentMessage> sentResult = await sent.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            BodyObservation<PublishedMessage> publishedResult = await published.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            BodyObservation<ForwardedMessage> forwardedResult = await forwarded.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            AssertObservation(sentResult, sentMessageId, sentMessage);
            AssertObservation(publishedResult, publishedMessageId, publishedMessage);
            AssertObservation(forwardedResult, forwardedMessageId, forwardedMessage);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static BodyObservation<TMessage> Capture<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class
    {
        MessageBody body = context.Advanced().ReceiveContext.Body;
        byte[] first = body.ToArray();
        byte[] second = body.ToArray();
        bool hasTransportText = body.TryGetTransportText(out string? transportText);

        return new BodyObservation<TMessage>(
            context.MessageId,
            context.Message,
            context.Advanced().ReceiveContext.ContentType.MediaType,
            body.Length,
            first,
            second,
            hasTransportText,
            transportText);
    }

    private static void AssertObservation<TMessage>(
        BodyObservation<TMessage> observation,
        Guid expectedMessageId,
        BinaryContract expected)
        where TMessage : BinaryContract
    {
        Assert.Equal(expectedMessageId, observation.MessageId);
        Assert.Equal(expected.CorrelationId, observation.Message.CorrelationId);
        Assert.Equal(expected.Value, observation.Message.Value);
        Assert.Equal(expected.Binary, observation.Message.Binary);
        Assert.Equal(new MessagePackSerializerFactory().ContentType.MediaType, observation.ContentType);
        Assert.Equal(observation.FirstBody.LongLength, observation.Length);
        Assert.NotEmpty(observation.FirstBody);
        Assert.NotSame(observation.FirstBody, observation.SecondBody);
        Assert.Equal(observation.FirstBody, observation.SecondBody);
        Assert.False(observation.HasTransportText);
        Assert.Null(observation.TransportText);
        observation.FirstBody[0] ^= 0xff;
        Assert.NotEqual(observation.FirstBody, observation.SecondBody);
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private abstract record BinaryContract(Guid CorrelationId, string Value, byte[] Binary);
    private sealed record SentMessage(Guid CorrelationId, string Value, byte[] Binary)
        : BinaryContract(CorrelationId, Value, Binary);
    private sealed record PublishedMessage(Guid CorrelationId, string Value, byte[] Binary)
        : BinaryContract(CorrelationId, Value, Binary);
    private sealed record ForwardedMessage(Guid CorrelationId, string Value, byte[] Binary)
        : BinaryContract(CorrelationId, Value, Binary);

    private sealed record BodyObservation<TMessage>(
        Guid? MessageId,
        TMessage Message,
        string ContentType,
        long Length,
        byte[] FirstBody,
        byte[] SecondBody,
        bool HasTransportText,
        string? TransportText)
        where TMessage : class;
}
