using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Util;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.QuartzIntegration;

public class ScheduleMessageConsumer :
    IConsumer<ScheduleMessage>,
    IConsumer<ScheduleRecurringMessage>
{
    const string ScheduleMessageJobId = "ViciOneServiceBusScheduleMessageJob";

    readonly ISchedulerFactory _schedulerFactory;
    readonly Func<string, TimeZoneInfo?>? _timeZoneResolver;

    public ScheduleMessageConsumer(ISchedulerFactory schedulerFactory)
        : this(schedulerFactory, (Func<string, TimeZoneInfo?>?)null)
    {
    }

    public ScheduleMessageConsumer(ISchedulerFactory schedulerFactory, IOptions<QuartzEndpointOptions> options)
        : this(schedulerFactory, options?.Value.TimeZoneResolver)
    {
    }

    internal ScheduleMessageConsumer(ISchedulerFactory schedulerFactory, Func<string, TimeZoneInfo?>? timeZoneResolver)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        _timeZoneResolver = timeZoneResolver;
    }

    public async Task Consume(ConsumeContext<ScheduleMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var jobKey = await EnsureJobExists(context.CancellationToken).ConfigureAwait(false);

        var messageBody = context.SerializerContext.GetMessageSerializer(context.Message.Payload, context.Message.PayloadType)
            .GetMessageBody(new MessageSendContext<ScheduleMessage>(context.Message));

        var triggerKey = new TriggerKey(context.Message.TokenId.ToString("N"));

        var builder = TriggerBuilder.Create()
            .ForJob(jobKey)
            .StartAt(context.Message.ScheduledTime)
            .WithSchedule(SimpleScheduleBuilder.Create().WithMisfireInstruction(SimpleTriggerMisfireInstruction.FireNow))
            .WithIdentity(triggerKey);

        var trigger = PopulateTrigger(context, builder, messageBody, context.Message.Destination, context.Message.PayloadType, messageId: context.MessageId,
            tokenId: context.Message.TokenId);

        var scheduler = await _schedulerFactory.GetScheduler(context.CancellationToken).ConfigureAwait(false);

        if (await scheduler.Exists(trigger.Key, context.CancellationToken).ConfigureAwait(false))
            await scheduler.UnscheduleJob(trigger.Key, context.CancellationToken).ConfigureAwait(false);

        await scheduler.ScheduleJob(trigger, new ScheduleJobOptions(), context.CancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Scheduled: {Key} {Schedule}", trigger.Key, trigger.NextFireTimeUtc);
    }

    public async Task Consume(ConsumeContext<ScheduleRecurringMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var jobKey = await EnsureJobExists(context.CancellationToken).ConfigureAwait(false);

        var messageBody = context.SerializerContext.GetMessageSerializer(context.Message.Payload, context.Message.PayloadType)
            .GetMessageBody(new MessageSendContext<ScheduleRecurringMessage>(context.Message));

        var schedule = context.Message.Schedule;
        var triggerKey = QuartzTriggerKey.ForRecurring(schedule.ScheduleId, schedule.ScheduleGroup);

        var tz = TimeZoneInfo.Local;
        if (!string.IsNullOrWhiteSpace(schedule.TimeZoneId) && schedule.TimeZoneId != tz.Id)
            tz = JobService.Scheduling.TimeZoneUtil.FindTimeZoneById(schedule.TimeZoneId, _timeZoneResolver);

        var triggerBuilder = TriggerBuilder.Create()
            .ForJob(jobKey)
            .WithIdentity(triggerKey)
            .StartAt(schedule.StartTime)
            .WithDescription(schedule.Description)
            .WithCronSchedule(schedule.CronExpression, x =>
            {
                x.InTimeZone(tz);
                switch (schedule.MisfirePolicy)
                {
                    case MissedEventPolicy.Skip:
                        x.WithMisfireInstruction(CronTriggerMisfireInstruction.DoNothing);
                        break;

                    case MissedEventPolicy.Send:
                        x.WithMisfireInstruction(CronTriggerMisfireInstruction.FireAndProceed);
                        break;
                }
            });

        if (schedule.EndTime.HasValue)
            triggerBuilder.EndAt(schedule.EndTime);

        var trigger = PopulateTrigger(context, triggerBuilder, messageBody, context.Message.Destination, context.Message.PayloadType,
            messageId: default);

        var scheduler = await _schedulerFactory.GetScheduler(context.CancellationToken).ConfigureAwait(false);

        if (await scheduler.Exists(triggerKey, context.CancellationToken).ConfigureAwait(false))
            await scheduler.UnscheduleJob(triggerKey, context.CancellationToken).ConfigureAwait(false);

        await scheduler.ScheduleJob(trigger, new ScheduleJobOptions(), context.CancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Scheduled: {Key} {Schedule}", triggerKey, trigger.NextFireTimeUtc);
    }

    static ITrigger PopulateTrigger(ConsumeContext context, TriggerBuilder<IJob> builder, MessageBody messageBody, Uri destination,
        string[] messageTypes, Guid? messageId = default, Guid? tokenId = default)
    {
        builder = builder
            .UsingJobData("Destination", ToString(destination))
            .UsingJobData("Body", messageBody.GetString())
            .UsingJobData("ContentType", context.ReceiveContext.ContentType.ToString())
            .UsingJobData("MessageType", string.Join(";", messageTypes));

        if (messageId.HasValue)
            builder = builder.UsingJobData("MessageId", messageId.Value.ToString());

        if (context.CorrelationId.HasValue)
            builder = builder.UsingJobData("CorrelationId", context.CorrelationId.Value.ToString());

        if (context.ConversationId.HasValue)
            builder = builder.UsingJobData("ConversationId", context.ConversationId.Value.ToString());

        if (context.InitiatorId.HasValue)
            builder = builder.UsingJobData("InitiatorId", context.InitiatorId.Value.ToString());

        if (context.RequestId.HasValue)
            builder = builder.UsingJobData("RequestId", context.RequestId.Value.ToString());

        if (context.SourceAddress != null)
            builder = builder.UsingJobData("SourceAddress", context.SourceAddress.ToString());

        if (context.ResponseAddress != null)
            builder = builder.UsingJobData("ResponseAddress", context.ResponseAddress.ToString());

        if (context.FaultAddress != null)
            builder = builder.UsingJobData("FaultAddress", context.FaultAddress.ToString());

        if (context.ExpirationTime.HasValue)
            builder = builder.UsingJobData("ExpirationTime", context.ExpirationTime.Value.ToString("O"));

        if (tokenId.HasValue)
            builder = builder.UsingJobData("TokenId", tokenId.Value.ToString("N"));

        List<KeyValuePair<string, object>> headers = context.Headers.GetAll().ToList();
        PreserveTraceHeader(context.Headers, headers, DiagnosticHeaders.ActivityId);
        PreserveTraceHeader(context.Headers, headers, DiagnosticHeaders.ActivityCorrelationContext);
        PreserveTraceHeader(context.Headers, headers, DiagnosticHeaders.ActivityPropagation);
        if (headers.Any())
            builder = builder.UsingJobData("HeadersAsJson", JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options));

        if (context.ReceiveContext.TryGetPayload<TransportReceiveContext>(out var transportReceiveContext))
        {
            IDictionary<string, object>? properties = transportReceiveContext.GetTransportProperties();
            if (properties != null)
                builder = builder.UsingJobData("TransportProperties", JsonSerializer.Serialize(properties, ServiceBusMetadataJson.Options));
        }

        var trigger = builder
            .Build();

        return trigger;
    }

    static void PreserveTraceHeader(Headers source, List<KeyValuePair<string, object>> destination, string key)
    {
        // Raw serializers intentionally exclude VSB system headers from their user-header enumeration.
        // Scheduling is a temporal transport boundary, so trace propagation metadata must cross it explicitly.
        if (destination.Any(header => string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase)))
            return;

        if (source.TryGetHeader(key, out var value))
            destination.Add(new KeyValuePair<string, object>(key, value));
    }

    async Task<JobKey> EnsureJobExists(CancellationToken cancellationToken)
    {
        var jobKey = new JobKey(ScheduleMessageJobId);

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);

        if (await scheduler.Exists(jobKey, cancellationToken).ConfigureAwait(false))
            return jobKey;

        var jobDetail = JobBuilder.Create<ScheduledMessageJob>()
            .RequestRecovery()
            .StoreDurably()
            .WithIdentity(jobKey)
            .WithDescription("ViciOne.ServiceBus Scheduled Message Job")
            .Build();

        await scheduler.AddJob(jobDetail, AddJobOptions.Replacing, cancellationToken).ConfigureAwait(false);

        return jobKey;
    }

    static string ToString(Uri? uri)
    {
        return uri?.ToString() ?? "";
    }
}
