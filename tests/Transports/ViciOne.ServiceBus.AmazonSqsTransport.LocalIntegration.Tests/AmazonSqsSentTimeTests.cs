using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

public sealed class AmazonSqsSentTimeTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0243", "envelope-carries-exact-utc-sent-time-and-provider-timestamp")]
    public Task Envelope_CarriesExactUtcSentTimeAsync() => AssertSentTimeAsync(useRawJson: false);

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0244", "raw-json-carries-exact-utc-sent-time-and-provider-timestamp")]
    public Task RawJson_CarriesExactUtcSentTimeAsync() => AssertSentTimeAsync(useRawJson: true);

    private static async Task AssertSentTimeAsync(bool useRawJson)
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create(useRawJson ? "rawsent" : "senttime");
        string queueName = fixture.Name("input");
        Guid? sentMessageId = null;
        DateTimeOffset? sentTime = null;
        var consumed = new TaskCompletionSource<SentTimeObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            if (useRawJson)
                configurator.UseRawJsonSerializer(RawSerializerOptions.All);

            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<SentTimeMessage>(context =>
                {
                    consumed.TrySetResult(new SentTimeObservation(
                        context.MessageId,
                        context.SentTime,
                        context.Advanced().ReceiveContext.GetSentTime(),
                        context.Advanced().ReceiveContext.ContentType));
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
            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.SendAsync(
                    new SentTimeMessage(Guid.NewGuid()),
                    context =>
                    {
                        sentMessageId = context.MessageId;
                        sentTime = context.SentTime;
                        if (useRawJson)
                            context.Serializer = new SystemTextJsonRawMessageSerializer(ServiceBusMetadataJson.Options, RawSerializerOptions.All);
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            SentTimeObservation actual = await consumed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            DateTimeOffset providerSentTime = Assert.IsType<DateTimeOffset>(actual.ProviderSentTime);

            Assert.Equal(Assert.IsType<Guid>(sentMessageId), actual.MessageId);
            Assert.Equal(Assert.IsType<DateTimeOffset>(sentTime), actual.EnvelopeSentTime);
            Assert.Equal(TimeSpan.Zero, Assert.IsType<DateTimeOffset>(actual.EnvelopeSentTime).Offset);
            Assert.Equal(TimeSpan.Zero, providerSentTime.Offset);
            Assert.True(providerSentTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(0, providerSentTime.Ticks % TimeSpan.TicksPerMillisecond);
            Assert.Equal(
                useRawJson ? SystemTextJsonRawMessageSerializer.JsonContentType : SystemTextJsonMessageSerializer.JsonContentType,
                actual.ContentType);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private sealed record SentTimeMessage(Guid Id);

    private sealed record SentTimeObservation(
        Guid? MessageId,
        DateTimeOffset? EnvelopeSentTime,
        DateTimeOffset? ProviderSentTime,
        System.Net.Mime.ContentType ContentType);
}
