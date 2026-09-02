namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AmazonSqsSentTimeTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0243", "envelope-carries-exact-utc-sent-time-and-provider-timestamp")]
    public Task Envelope_CarriesExactUtcSentTime() => AssertSentTime(useRawJson: false);

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0244", "raw-json-carries-exact-utc-sent-time-and-provider-timestamp")]
    public Task RawJson_CarriesExactUtcSentTime() => AssertSentTime(useRawJson: true);

    private static async Task AssertSentTime(bool useRawJson)
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create(useRawJson ? "rawsent" : "senttime");
        string queueName = fixture.Name("input");
        Guid? sentMessageId = null;
        DateTime? sentTime = null;
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
                        context.ReceiveContext.GetSentTime(),
                        context.ReceiveContext.ContentType));
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
            DateTime providerSentTime = Assert.IsType<DateTime>(actual.ProviderSentTime);

            Assert.Equal(Assert.IsType<Guid>(sentMessageId), actual.MessageId);
            Assert.Equal(Assert.IsType<DateTime>(sentTime), actual.EnvelopeSentTime);
            Assert.Equal(DateTimeKind.Utc, Assert.IsType<DateTime>(actual.EnvelopeSentTime).Kind);
            Assert.Equal(DateTimeKind.Utc, providerSentTime.Kind);
            Assert.True(providerSentTime > DateTime.UnixEpoch);
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
        DateTime? EnvelopeSentTime,
        DateTime? ProviderSentTime,
        System.Net.Mime.ContentType ContentType);
}
