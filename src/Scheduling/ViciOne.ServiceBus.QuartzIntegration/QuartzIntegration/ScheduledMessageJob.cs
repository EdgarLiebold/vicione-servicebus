using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.QuartzIntegration;

public class ScheduledMessageJob :
    IJob
{
    internal const string BusContextKey = "ViciOne.ServiceBus.QuartzIntegration.Bus";
    internal const string TimeProviderContextKey = "ViciOne.ServiceBus.QuartzIntegration.TimeProvider";

    readonly IBus? _bus;
    readonly TimeProvider? _timeProvider;

    /// <summary>
    /// Creates a job for Quartz standalone schedulers. The bus and time provider are resolved from the scheduler context.
    /// </summary>
    public ScheduledMessageJob()
    {
    }

    public ScheduledMessageJob(IBus bus, TimeProvider timeProvider)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        IBus bus = _bus ?? GetSchedulerContextValue<IBus>(context, BusContextKey);
        TimeProvider timeProvider = _timeProvider ?? GetSchedulerContextValue<TimeProvider>(context, TimeProviderContextKey);
        var jobData = context.MergedJobDataMap;
        var messageContext = new JobDataMessageContext(context, ServiceBusMetadataJson.ObjectDeserializer);

        var contentType = new ContentType(jobData.GetString("ContentType")!);
        var destinationAddress = new Uri(jobData.GetString("Destination")!);
        var body = jobData.GetString("Body") ?? string.Empty;

        var supportedMessageTypes = (jobData.TryGetString("MessageType", out var text)
            ? text?.Split(';').ToArray()
            : default) ?? [];

        try
        {
            var pipe = new ForwardScheduledMessagePipe(
                contentType,
                messageContext,
                body,
                destinationAddress,
                supportedMessageTypes,
                timeProvider);

            var endpoint = await bus.GetSendEndpoint(destinationAddress).ConfigureAwait(false);

            await endpoint.Send(new SerializedMessageBody(), pipe, cancellationToken).ConfigureAwait(false);

            LogContext.Debug?.Log("Schedule Executed: {Key} {Schedule}", context.Trigger.Key, context.Trigger.NextFireTimeUtc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogContext.Error?.Log(ex, "Failed to send scheduled message: {MessageType} {DestinationAddress}", supportedMessageTypes, destinationAddress);

            throw new JobExecutionException(ex) { RefireImmediately = context.RefireCount < 5 };
        }
    }

    static T GetSchedulerContextValue<T>(IJobExecutionContext context, string key)
    {
        if (context.Scheduler.Context.TryGetValue(key, out var value) && value is T typed)
            return typed;

        throw new InvalidOperationException(
            $"Quartz scheduler context value '{key}' is missing. Configure the scheduler through UseInMemoryScheduler or register {nameof(ScheduledMessageJob)} with dependency injection.");
    }


    class ForwardScheduledMessagePipe :
        IPipe<SendContext>
    {
        readonly string _body;
        readonly ContentType? _contentType;
        readonly Uri? _destinationAddress;
        readonly string[] _supportedMessageTypes;
        readonly JobDataMessageContext _messageContext;
        readonly TimeProvider _timeProvider;

        public ForwardScheduledMessagePipe(ContentType? contentType, JobDataMessageContext messageContext, string body, Uri? destinationAddress,
            string[] supportedMessageTypes, TimeProvider timeProvider)
        {
            _contentType = contentType;
            _messageContext = messageContext;
            _body = body;
            _destinationAddress = destinationAddress;
            _supportedMessageTypes = supportedMessageTypes;
            _timeProvider = timeProvider;
        }

        public Task Send(SendContext context)
        {
            var deserializer = context.Serialization.GetMessageDeserializer(_contentType);

            var body = deserializer.GetMessageBody(_body);

            var serializerContext = deserializer.Deserialize(body, _messageContext, _destinationAddress);

            if (_messageContext.MessageId.HasValue)
                context.MessageId = _messageContext.MessageId;

            context.RequestId = _messageContext.RequestId;
            context.ConversationId = _messageContext.ConversationId;
            context.CorrelationId = _messageContext.CorrelationId;
            context.InitiatorId = _messageContext.InitiatorId;
            context.SourceAddress = _messageContext.SourceAddress;
            context.ResponseAddress = _messageContext.ResponseAddress;
            context.FaultAddress = _messageContext.FaultAddress;

            if (_supportedMessageTypes.Any())
                context.SupportedMessageTypes = _supportedMessageTypes;

            context.TimeToLive = ScheduledMessageExpiration.GetRemainingTimeToLive(_messageContext.ExpirationTime, _timeProvider);

            foreach (KeyValuePair<string, object> header in _messageContext.Headers.GetAll())
                context.Headers.Set(header.Key, header.Value);

            IReadOnlyDictionary<string, object>? transportProperties = _messageContext.TransportProperties;
            if (transportProperties != null && context is TransportSendContext transportSendContext)
                transportSendContext.ReadPropertiesFrom(transportProperties);

            context.Serializer = serializerContext.GetMessageSerializer();

            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
