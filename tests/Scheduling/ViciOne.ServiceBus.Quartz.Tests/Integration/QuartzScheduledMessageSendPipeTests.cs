using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Quartz;
using Quartz.Extensibility;
using Quartz.Impl;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

public sealed class QuartzScheduledMessageSendPipeTests
{
    private static readonly DateTimeOffset CurrentTime = new(2038, 7, 6, 5, 4, 3, TimeSpan.Zero);
    private static readonly Uri DestinationAddress = new("loopback://localhost/scheduled-destination");

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-DELIVERY", "transport-context-rehydration")]
    public async Task SendAsync_RestoresMessageAndTransportMetadataAsync()
    {
        Guid messageId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5fa1");
        Guid requestId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5fa2");
        Guid correlationId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5fa3");
        Guid conversationId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5fa4");
        Guid initiatorId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5fa5");
        var sourceAddress = new Uri("loopback://localhost/scheduled-source");
        var responseAddress = new Uri("loopback://localhost/scheduled-response");
        var faultAddress = new Uri("loopback://localhost/scheduled-fault");
        DateTimeOffset expirationTime = CurrentTime.AddMinutes(3);
        var headers = new[]
        {
            new KeyValuePair<string, object>("tenant", "factory-a"),
        };
        var transportProperties = new Dictionary<string, object>
        {
            ["Durable"] = false,
            ["Mandatory"] = true,
            ["Delay"] = "00:00:07",
            ["RoutingKey"] = "north",
        };
        var jobData = new JobDataMap
        {
            [QuartzJobDataKeys.MessageId] = messageId.ToString("D"),
            [QuartzJobDataKeys.RequestId] = requestId.ToString("D"),
            [QuartzJobDataKeys.CorrelationId] = correlationId.ToString("D"),
            [QuartzJobDataKeys.ConversationId] = conversationId.ToString("D"),
            [QuartzJobDataKeys.InitiatorId] = initiatorId.ToString("D"),
            [QuartzJobDataKeys.SourceAddress] = sourceAddress.ToString(),
            [QuartzJobDataKeys.DestinationAddress] = DestinationAddress.ToString(),
            [QuartzJobDataKeys.ResponseAddress] = responseAddress.ToString(),
            [QuartzJobDataKeys.FaultAddress] = faultAddress.ToString(),
            [QuartzJobDataKeys.ExpirationTime] = expirationTime.ToString("O"),
            [QuartzJobDataKeys.Headers] = JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options),
            [QuartzJobDataKeys.TransportProperties] = JsonSerializer.Serialize(transportProperties, ServiceBusMetadataJson.Options),
        };
        var messageContext = new QuartzScheduledMessageContext(
            CreateExecutionContext(jobData),
            ServiceBusMetadataJson.ObjectDeserializer);
        ISerialization serialization = new SerializationConfiguration().CreateSerializerCollection();
        var sourceContext = new MessageSendContext<ScheduledPayload>(new ScheduledPayload("scheduled-value"))
        {
            Serializer = serialization.GetMessageSerializer(),
        };
        string body = sourceContext.Body.GetRequiredTransportText();
        string expectedMessageType = MessageUrn.ForTypeString<ScheduledPayload>();
        string[] supportedMessageTypes = [expectedMessageType];
        var timeProvider = new FakeTimeProvider(CurrentTime);
        var pipe = new QuartzScheduledMessageSendPipe(
            sourceContext.ContentType!,
            messageContext,
            body,
            DestinationAddress,
            supportedMessageTypes,
            timeProvider);
        supportedMessageTypes[0] = "urn:message:mutated";
        var sendContext = new RoutingSendContext<SerializedTransportMessage>(SerializedTransportMessage.Instance)
        {
            Serialization = serialization,
        };

        await pipe.SendAsync(sendContext);

        Assert.Equal(messageId, sendContext.MessageId);
        Assert.Equal(requestId, sendContext.RequestId);
        Assert.Equal(correlationId, sendContext.CorrelationId);
        Assert.Equal(conversationId, sendContext.ConversationId);
        Assert.Equal(initiatorId, sendContext.InitiatorId);
        Assert.Equal(sourceAddress, sendContext.SourceAddress);
        Assert.Equal(responseAddress, sendContext.ResponseAddress);
        Assert.Equal(faultAddress, sendContext.FaultAddress);
        Assert.Equal(TimeSpan.FromMinutes(3), sendContext.TimeToLive);
        Assert.Equal(expectedMessageType, Assert.Single(sendContext.SupportedMessageTypes));
        Assert.Equal("factory-a", sendContext.Headers.Get<string>("tenant"));
        Assert.False(sendContext.Durable);
        Assert.True(sendContext.Mandatory);
        Assert.Equal(TimeSpan.FromSeconds(7), sendContext.Delay);
        Assert.Equal("north", sendContext.RoutingKey);
        Assert.NotNull(sendContext.Serializer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-DELIVERY", "send-pipe-null-guards")]
    public async Task ConstructorAndSendAsync_RejectMissingDependenciesAsync()
    {
        var jobData = new JobDataMap
        {
            [QuartzJobDataKeys.DestinationAddress] = DestinationAddress.ToString(),
            [QuartzJobDataKeys.MessageIdSeed] = "018f6738-7d4a-7b21-86e2-bdfbb3ed5f90",
        };
        var messageContext = new QuartzScheduledMessageContext(
            CreateExecutionContext(jobData),
            ServiceBusMetadataJson.ObjectDeserializer);
        var contentType = new ContentType("application/json");
        string[] messageTypes = [];

        Assert.Equal("contentType", Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageSendPipe(null!, messageContext, "{}", DestinationAddress, messageTypes, TimeProvider.System)).ParamName);
        Assert.Equal("messageContext", Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageSendPipe(contentType, null!, "{}", DestinationAddress, messageTypes, TimeProvider.System)).ParamName);
        Assert.Equal("body", Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageSendPipe(contentType, messageContext, null!, DestinationAddress, messageTypes, TimeProvider.System)).ParamName);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageSendPipe(contentType, messageContext, "{}", null!, messageTypes, TimeProvider.System)).ParamName);
        Assert.Equal("supportedMessageTypes", Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageSendPipe(contentType, messageContext, "{}", DestinationAddress, null!, TimeProvider.System)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageSendPipe(contentType, messageContext, "{}", DestinationAddress, messageTypes, null!)).ParamName);

        var pipe = new QuartzScheduledMessageSendPipe(
            contentType,
            messageContext,
            "{}",
            DestinationAddress,
            messageTypes,
            TimeProvider.System);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => pipe.SendAsync(null!))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => pipe.Probe(null!)).ParamName);
        pipe.Probe(DispatchProxy.Create<ProbeContext, NoOpDispatchProxy>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "invalid-serialized-body-is-terminal")]
    public async Task SendAsync_ClassifiesInvalidPersistedContentAsTerminalDataFailureAsync()
    {
        var jobData = new JobDataMap
        {
            [QuartzJobDataKeys.DestinationAddress] = DestinationAddress.ToString(),
            [QuartzJobDataKeys.MessageIdSeed] = "018f6738-7d4a-7b21-86e2-bdfbb3ed5f90",
        };
        var messageContext = new QuartzScheduledMessageContext(
            CreateExecutionContext(jobData),
            ServiceBusMetadataJson.ObjectDeserializer);
        var pipe = new QuartzScheduledMessageSendPipe(
            SystemTextJsonMessageSerializer.JsonContentType,
            messageContext,
            "{not-json",
            DestinationAddress,
            [],
            TimeProvider.System);
        var sendContext = new RoutingSendContext<SerializedTransportMessage>(SerializedTransportMessage.Instance)
        {
            Serialization = new SerializationConfiguration().CreateSerializerCollection(),
        };

        InvalidScheduledMessageDataException exception = await Assert.ThrowsAsync<InvalidScheduledMessageDataException>(() =>
            pipe.SendAsync(sendContext));

        Assert.NotNull(exception.InnerException);
    }

    private static JobExecutionContextImpl CreateExecutionContext(JobDataMap data)
    {
        IJobDetail job = JobBuilder.Create<NoOpJob>()
            .WithIdentity("scheduled-message-job")
            .StoreDurably()
            .Build();
        IOperableTrigger trigger = (IOperableTrigger)TriggerBuilder.Create()
            .WithIdentity("scheduled-message-trigger")
            .ForJob(job.Key)
            .UsingJobData(data)
            .StartAt(CurrentTime)
            .Build();
        var bundle = new TriggerFiredBundle
        {
            JobDetail = job,
            Trigger = trigger,
            Calendar = null,
            Recovering = false,
            FireTimeUtc = CurrentTime,
            ScheduledFireTimeUtc = CurrentTime,
            PreviousFireTimeUtc = null,
            NextFireTimeUtc = null,
        };

        return new JobExecutionContextImpl(null!, bundle, new NoOpJob());
    }

    private sealed class NoOpJob : global::Quartz.IJob
    {
        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? ValueTask.FromCanceled(cancellationToken)
                : ValueTask.CompletedTask;
        }
    }

    private sealed record ScheduledPayload(string Value);

    private sealed class RoutingSendContext<TMessage>(TMessage message) :
        MessageSendContext<TMessage>(message),
        RoutingKeySendContext
        where TMessage : class
    {
        public string? RoutingKey { get; set; }

        public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
        {
            base.ReadPropertiesFrom(properties);
            RoutingKey = ReadString(properties, "RoutingKey");
        }
    }

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
