using System.Net.Mime;
using System.Reflection;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Quartz.Consumers;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

public sealed class QuartzScheduleSerializationBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "persisted-content-type-comes-from-body-serializer")]
    public async Task Scheduling_PersistsTheContentTypeProducedByTheSelectedSerializerAsync()
    {
        var receivedContentType = new ContentType("application/vnd.vicione.received");
        var producedContentType = new ContentType("application/vnd.vicione.scheduled");
        ISchedulerFactory schedulerFactory = QuartzSchedulingExtensions.CreateInMemorySchedulerFactory();
        IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken);
        const string schedulerNamespace = "quartz-serialization-boundary";
        Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5fa8");
        var command = new ScheduleMessageCommand
        {
            TokenId = tokenId,
            DueAt = new DateTimeOffset(2041, 2, 3, 4, 5, 6, TimeSpan.Zero),
            Destination = new Uri("loopback://localhost/quartz-serialization-boundary"),
            Payload = new SerializationPayload("value"),
            PayloadType = ["urn:message:serialization-boundary;with-delimiter"],
        };
        ITestScheduleContext context = CreateContext(
            command,
            receivedContentType,
            new FixedSerializer(producedContentType));
        var consumer = new ScheduleMessageConsumer<IBus>(
            schedulerFactory,
            timeZoneResolver: null,
            schedulerNamespace,
            RetryPolicy.Fixed(1, TimeSpan.Zero));

        try
        {
            await consumer.ConsumeAsync(context);

            ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await scheduler.GetTrigger(
                QuartzTriggerKey.ForOneTime(tokenId, schedulerNamespace),
                TestContext.Current.CancellationToken));
            Assert.Equal(producedContentType.ToString(), trigger.JobDataMap.GetString(QuartzJobDataKeys.ContentType));
            Assert.NotEqual(receivedContentType.ToString(), trigger.JobDataMap.GetString(QuartzJobDataKeys.ContentType));
            Assert.Equal(command.PayloadType, QuartzMessageTypeList.Deserialize(
                Assert.IsType<string>(trigger.JobDataMap[QuartzJobDataKeys.MessageTypes])));
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true, TestContext.Current.CancellationToken);
            await Assert.IsAssignableFrom<IAsyncDisposable>(schedulerFactory).DisposeAsync();
        }
    }

    private static ITestScheduleContext CreateContext(
        ScheduleMessage message,
        ContentType receivedContentType,
        IMessageSerializer serializer)
    {
        SerializerContext serializerContext = DispatchProxy.Create<SerializerContext, SerializerContextProxy>();
        ((SerializerContextProxy)(object)serializerContext).Serializer = serializer;
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)receiveContext).ContentType = receivedContentType;
        ITestScheduleContext context = DispatchProxy.Create<ITestScheduleContext, ScheduleContextProxy>();
        var proxy = (ScheduleContextProxy)(object)context;
        proxy.Message = message;
        proxy.SerializerContext = serializerContext;
        proxy.ReceiveContext = receiveContext;
        return context;
    }

    private interface ITestScheduleContext : ConsumeContext<ScheduleMessage>, ConsumeContext;

    private class ScheduleContextProxy : DispatchProxy
    {
        public ScheduleMessage? Message { get; set; }
        public ReceiveContext? ReceiveContext { get; set; }
        public SerializerContext? SerializerContext { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Message" => Message ?? throw new InvalidOperationException("The test message was not configured."),
                "get_CancellationToken" => TestContext.Current.CancellationToken,
                "get_ReceiveContext" => ReceiveContext ?? throw new InvalidOperationException("The receive context was not configured."),
                "get_SerializerContext" => SerializerContext
                    ?? throw new InvalidOperationException("The serializer context was not configured."),
                "get_Headers" => new DictionarySendHeaders(),
                "get_MessageId" or "get_RequestId" or "get_CorrelationId" or "get_ConversationId" or "get_InitiatorId"
                    or "get_ExpirationTime" or "get_SourceAddress" or "get_ResponseAddress" or "get_FaultAddress" => null,
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
        }
    }

    private class SerializerContextProxy : DispatchProxy
    {
        public IMessageSerializer? Serializer { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(SerializerContext.GetMessageSerializer) && args is { Length: 2 })
                return Serializer ?? throw new InvalidOperationException("The serializer was not configured.");

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        public ContentType? ContentType { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_ContentType")
                return ContentType ?? throw new InvalidOperationException("The content type was not configured.");
            if (targetMethod?.Name == nameof(PipeContext.TryGetPayload))
            {
                args![0] = null;
                return false;
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private sealed class FixedSerializer(ContentType contentType) : IMessageSerializer
    {
        public ContentType ContentType { get; } = contentType;

        public MessageBody GetMessageBody<T>(SendContext<T> context)
            where T : class
        {
            return new StringMessageBody("{\"value\":\"serialized\"}");
        }
    }

    private sealed record SerializationPayload(string Value);
}
