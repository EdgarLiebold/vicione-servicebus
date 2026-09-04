using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

public sealed class AmazonSqsRawJsonTests
{
    private const string HeaderName = "ViciOne-Raw-Trace";
    private const string HeaderValue = "raw-json-over-sqs";

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0232", "raw-json-preserves-payload-header-and-transport-metadata")]
    public async Task RawJsonHeader_ReachesTheTransportConsumer()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("rawjson");
        string queueName = fixture.Name("input");
        Guid commandId = Guid.NewGuid();
        Guid messageId = NewId.NextGuid();
        Guid correlationId = Guid.NewGuid();
        Guid conversationId = Guid.NewGuid();
        var consumed = NewObservation<RawObservation>();
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseRawJsonSerializer(RawSerializerOptions.All);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<IRawCommand>(context =>
                {
                    consumed.TrySetResult(new RawObservation(
                        context.ReceiveContext.ContentType,
                        context.Message.CommandId,
                        context.Message.ItemNumber,
                        context.Headers.Get<string>(HeaderName),
                        context.MessageId,
                        context.CorrelationId,
                        context.ConversationId,
                        context.SentTime,
                        context.DestinationAddress,
                        context.SupportedMessageTypes.ToArray()));
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
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            await input.Send(
                    new RawCommandBody { CommandId = commandId, ItemNumber = "27" },
                    context =>
                    {
                        context.MessageId = messageId;
                        context.CorrelationId = correlationId;
                        context.ConversationId = conversationId;
                        context.Headers.Set(HeaderName, HeaderValue);
                        context.Serializer = new SystemTextJsonRawMessageSerializer(ServiceBusMetadataJson.Options, RawSerializerOptions.All);
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            RawObservation actual = await consumed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(SystemTextJsonRawMessageSerializer.JsonContentType, actual.ContentType);
            Assert.Equal((commandId, "27"), (actual.CommandId, actual.ItemNumber));
            Assert.Equal(HeaderValue, actual.HeaderValue);
            Assert.Equal((messageId, correlationId, conversationId),
                (actual.MessageId, actual.CorrelationId, actual.ConversationId));
            Assert.Equal(messageId.ToNewId().Timestamp, actual.SentTime);
            Assert.Equal(
                new Uri($"amazonsqs://{fixture.Region}/{fixture.Prefix}/{queueName}?durable=false&autodelete=true"),
                actual.DestinationAddress);
            Assert.Equal([MessageUrn.ForTypeString<RawCommandBody>()], actual.SupportedMessageTypes);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0233", "forwarded-raw-json-drops-user-headers-when-copy-disabled")]
    public Task ForwardedRawJson_DropsTransportHeadersWhenCopyDisabled() =>
        AssertForwardedRawJsonHeader(RawSerializerOptions.AnyMessageType | RawSerializerOptions.AddTransportHeaders, null);

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0234", "forwarded-raw-json-preserves-user-headers-when-copy-enabled")]
    public Task ForwardedRawJson_PreservesTransportHeadersWhenCopyEnabled() =>
        AssertForwardedRawJsonHeader(RawSerializerOptions.All, HeaderValue);

    private static async Task AssertForwardedRawJsonHeader(RawSerializerOptions options, string? expectedHeader)
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("rawforward");
        string inputQueue = fixture.Name("input");
        string forwardedQueue = fixture.Name("forwarded");
        Guid commandId = Guid.NewGuid();
        var rawConsumed = NewObservation<ForwardObservation>();
        var forwarded = NewObservation<ForwardObservation>();
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.UseRawJsonDeserializer(options);
                endpoint.Handler<IRawCommand>(async context =>
                {
                    try
                    {
                        rawConsumed.TrySetResult(new ForwardObservation(
                            context.ReceiveContext.ContentType,
                            context.Message.CommandId,
                            context.Headers.Get<string>(HeaderName)));
                        await context.Publish(new RawForwarded(context.Message.CommandId), context.CancellationToken);
                    }
                    catch (Exception exception)
                    {
                        rawConsumed.TrySetException(exception);
                        forwarded.TrySetException(exception);
                        throw;
                    }
                });
            });
            configurator.ReceiveEndpoint(forwardedQueue, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<RawForwarded>(context =>
                {
                    forwarded.TrySetResult(new ForwardObservation(
                        context.ReceiveContext.ContentType,
                        context.Message.CorrelationId,
                        context.Headers.Get<string>(HeaderName)));
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
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{inputQueue}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            await input.Send(
                    new RawCommandBody { CommandId = commandId, ItemNumber = "27" },
                    context =>
                    {
                        context.Headers.Set(HeaderName, HeaderValue);
                        context.Serializer = new SystemTextJsonRawMessageSerializer(ServiceBusMetadataJson.Options, options);
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ForwardObservation actualRaw = await rawConsumed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ForwardObservation actualForwarded = await forwarded.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(SystemTextJsonRawMessageSerializer.JsonContentType, actualRaw.ContentType);
            Assert.Equal((commandId, HeaderValue), (actualRaw.CorrelationId, actualRaw.HeaderValue));
            Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType, actualForwarded.ContentType);
            Assert.Equal((commandId, expectedHeader), (actualForwarded.CorrelationId, actualForwarded.HeaderValue));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public interface IRawCommand
    {
        Guid CommandId { get; }
        string ItemNumber { get; }
    }

    public sealed class RawCommandBody
    {
        public Guid CommandId { get; init; }
        public required string ItemNumber { get; init; }
    }

    private sealed record RawForwarded(Guid CorrelationId);

    private sealed record RawObservation(
        System.Net.Mime.ContentType ContentType,
        Guid CommandId,
        string ItemNumber,
        string? HeaderValue,
        Guid? MessageId,
        Guid? CorrelationId,
        Guid? ConversationId,
        DateTime? SentTime,
        Uri? DestinationAddress,
        string[] SupportedMessageTypes);

    private sealed record ForwardObservation(
        System.Net.Mime.ContentType ContentType,
        Guid CorrelationId,
        string? HeaderValue);
}
