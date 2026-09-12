using Quartz;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessagePack;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzMessagePackSchedulingTests
{
    private static readonly Uri Destination = new("loopback://localhost/quartz-messagepack-destination");

    [Fact]
    [RequirementCoverage("REQ-VSB-BODY-CROSS-OWNER", "quartz-messagepack-carrier-store-rehydrate-deliver")]
    public async Task ScheduledMessagePackBody_IsStoredAsCanonicalBase64AndDeliveredExactlyAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        var delivered = new TaskCompletionSource<DeliveryObservation>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator =>
            {
                configurator.ClearSerialization();
                configurator.UseMessagePackSerializer();
                configurator.ReceiveEndpoint("quartz-messagepack-destination", endpoint =>
                    endpoint.Handler<MessagePackScheduledPayload>(context =>
                    {
                        MessageBody body = context.Advanced().ReceiveContext.Body;
                        delivered.TrySetResult(new DeliveryObservation(
                            context.MessageId,
                            context.Message,
                            context.Headers.Get<string>("ViciOne-MessagePack-Schedule"),
                            context.Advanced().ReceiveContext.ContentType.MediaType,
                            body.Length,
                            body.ToArray(),
                            body.ToArray(),
                            body.TryGetTransportText(out string? transportText),
                            transportText));
                        return Task.CompletedTask;
                    }));
            });
        var scheduler = new MessageScheduler(
            new EndpointScheduleMessageProvider(_ => Task.FromResult(fixture.SchedulerEndpoint)),
            fixture.Bus.Topology);
        var scheduledCommand = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
        using ConnectHandle scheduledObserver = fixture.Bus.ConnectConsumeObserver(scheduledCommand);
        var expected = new MessagePackScheduledPayload(
            Guid.Parse("0e228af3-a354-4052-b663-5afac1ed8bbf"),
            "quartz-messagepack",
            [0x00, 0x80, 0xff, 0x7f, 0xfe, 0x01]);
        Guid messageId = Guid.Parse("958f6324-cb66-4f66-ae62-4240e8856b1a");
        DateTimeOffset farFuture = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        ScheduledMessage<MessagePackScheduledPayload> scheduled = await scheduler.ScheduleSendAsync(
            Destination,
            farFuture,
            expected,
            Pipe.Execute<SendContext<MessagePackScheduledPayload>>(context =>
            {
                context.MessageId = messageId;
                context.Headers.Set("ViciOne-MessagePack-Schedule", "header-preserved");
            }),
            cancellationToken);
        await scheduledCommand.Completed.WaitAsync(timeout, cancellationToken);

        TriggerKey triggerKey = QuartzTriggerKey.ForOneTime(scheduled.TokenId, fixture.SchedulerNamespace);
        ITrigger storedTrigger = Assert.IsAssignableFrom<ITrigger>(
            await fixture.Scheduler.GetTrigger(triggerKey, cancellationToken));
        string persistedBody = Assert.IsType<string>(storedTrigger.JobDataMap[QuartzJobDataKeys.Body]);
        byte[] persistedBytes = Convert.FromBase64String(persistedBody);
        Assert.NotEmpty(persistedBytes);
        Assert.Equal(Convert.ToBase64String(persistedBytes), persistedBody);
        Assert.DoesNotContain(persistedBody, char.IsWhiteSpace);
        Assert.Equal(
            new MessagePackSerializerFactory().ContentType.MediaType,
            Assert.IsType<string>(storedTrigger.JobDataMap[QuartzJobDataKeys.ContentType]));

        ITrigger immediateTrigger = storedTrigger.GetTriggerBuilder()
            .StartNow()
            .Build();
        DateTimeOffset? nextFireTime = await fixture.Scheduler.RescheduleJob(
            triggerKey,
            immediateTrigger,
            cancellationToken);
        Assert.NotNull(nextFireTime);

        DeliveryObservation result = await delivered.Task.WaitAsync(timeout, cancellationToken);
        Assert.Equal(messageId, result.MessageId);
        Assert.Equal(expected.CorrelationId, result.Message.CorrelationId);
        Assert.Equal(expected.Value, result.Message.Value);
        Assert.Equal(expected.Binary, result.Message.Binary);
        Assert.Equal("header-preserved", result.HeaderValue);
        Assert.Equal(new MessagePackSerializerFactory().ContentType.MediaType, result.ContentType);
        Assert.Equal(result.FirstBody.LongLength, result.Length);
        Assert.NotEmpty(result.FirstBody);
        Assert.NotSame(result.FirstBody, result.SecondBody);
        Assert.Equal(result.FirstBody, result.SecondBody);
        Assert.False(result.HasTransportText);
        Assert.Null(result.TransportText);
        result.FirstBody[0] ^= 0xff;
        Assert.NotEqual(result.FirstBody, result.SecondBody);
    }

    private sealed record MessagePackScheduledPayload(Guid CorrelationId, string Value, byte[] Binary);

    private sealed record DeliveryObservation(
        Guid? MessageId,
        MessagePackScheduledPayload Message,
        string? HeaderValue,
        string ContentType,
        long Length,
        byte[] FirstBody,
        byte[] SecondBody,
        bool HasTransportText,
        string? TransportText);
}
