using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Util;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Providers.Configuration;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Quartz.Consumers;

/// <summary>Creates or replaces Quartz triggers that forward serialized messages at their due times.</summary>
internal sealed class ScheduleMessageConsumer<TBus> :
    IConsumer<ScheduleMessage>,
    IConsumer<ScheduleRecurringMessage>
    where TBus : class, IBus
{
    private const string ScheduledMessageJobIdentity = "ViciOne.ServiceBus.Quartz.ScheduledMessage";

    readonly QuartzSchedulerBinding<TBus>? _binding;
    readonly RetryPolicy _deliveryRetryPolicy;
    readonly ISchedulerFactory? _schedulerFactory;
    readonly string _schedulerNamespace;
    readonly Func<string, TimeZoneInfo?>? _timeZoneResolver;

    /// <summary>Initializes the scheduling consumer with the scheduler binding owned by its bus.</summary>
    /// <param name="binding">The bus-specific scheduler binding.</param>
    public ScheduleMessageConsumer(QuartzSchedulerBinding<TBus> binding)
    {
        _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        _timeZoneResolver = binding.Settings.TimeZoneResolver;
        _schedulerNamespace = binding.Settings.SchedulerNamespace;
        _deliveryRetryPolicy = binding.Settings.DeliveryRetryPolicy;
    }

    /// <summary>Initializes the scheduling consumer for direct bus configuration.</summary>
    /// <param name="schedulerFactory">The factory that resolves the active Quartz scheduler.</param>
    /// <param name="timeZoneResolver">The optional fallback time-zone resolver.</param>
    /// <param name="schedulerNamespace">The Quartz job and trigger namespace.</param>
    /// <param name="deliveryRetryPolicy">The persistent delivery retry policy.</param>
    internal ScheduleMessageConsumer(
        ISchedulerFactory schedulerFactory,
        Func<string, TimeZoneInfo?>? timeZoneResolver,
        string schedulerNamespace,
        RetryPolicy deliveryRetryPolicy)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        _timeZoneResolver = timeZoneResolver;
        ArgumentException.ThrowIfNullOrWhiteSpace(schedulerNamespace);
        _schedulerNamespace = schedulerNamespace;
        _deliveryRetryPolicy = deliveryRetryPolicy ?? throw new ArgumentNullException(nameof(deliveryRetryPolicy));
    }

    /// <summary>Creates or replaces a one-time trigger identified by the scheduling token.</summary>
    /// <param name="context">The one-time scheduling command context.</param>
    /// <returns>A task that completes after Quartz stores the one-time trigger.</returns>
    public async Task ConsumeAsync(ConsumeContext<ScheduleMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scheduler = await GetSchedulerAsync(context.CancellationToken).ConfigureAwait(false);
        var jobKey = await EnsureJobExistsAsync(scheduler, _schedulerNamespace, context.CancellationToken).ConfigureAwait(false);

        var messageBody = context.Advanced().SerializerContext.GetMessageSerializer(context.Message.Payload, context.Message.PayloadType)
            .GetMessageBody(new MessageSendContext<ScheduleMessage>(context.Message, context.CancellationToken));

        var triggerKey = QuartzTriggerKey.ForOneTime(context.Message.TokenId, _schedulerNamespace);

        var builder = TriggerBuilder.Create()
            .ForJob(jobKey)
            .StartAt(context.Message.DueAt)
            .WithSchedule(SimpleScheduleBuilder.Create().WithMisfireInstruction(SimpleTriggerMisfireInstruction.FireNow))
            .WithRetryPolicy(_deliveryRetryPolicy)
            .WithIdentity(triggerKey);

        var trigger = PopulateTrigger(context.Advanced(), builder, messageBody, context.Message.Destination, context.Message.PayloadType, messageId: context.MessageId,
            messageIdSeed: NewId.NextGuid(), tokenId: context.Message.TokenId);

        await scheduler.ScheduleJob(trigger, ScheduleJobOptions.Replacing, context.CancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Scheduled: {Key} {Schedule}", trigger.Key, trigger.NextFireTimeUtc);
    }

    /// <summary>Creates or replaces a cron trigger identified by schedule group and identifier.</summary>
    /// <param name="context">The recurring scheduling command context.</param>
    /// <returns>A task that completes after Quartz stores the recurring trigger.</returns>
    public async Task ConsumeAsync(ConsumeContext<ScheduleRecurringMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scheduler = await GetSchedulerAsync(context.CancellationToken).ConfigureAwait(false);
        var jobKey = await EnsureJobExistsAsync(scheduler, _schedulerNamespace, context.CancellationToken).ConfigureAwait(false);

        var messageBody = context.Advanced().SerializerContext.GetMessageSerializer(context.Message.Payload, context.Message.PayloadType)
            .GetMessageBody(new MessageSendContext<ScheduleRecurringMessage>(context.Message, context.CancellationToken));

        var schedule = context.Message.Schedule;
        var triggerKey = QuartzTriggerKey.ForRecurring(schedule.ScheduleId, schedule.ScheduleGroup, _schedulerNamespace);

        TimeZoneInfo timeZone = TimeZoneInfo.Local;
        if (!string.IsNullOrWhiteSpace(schedule.TimeZoneId) && schedule.TimeZoneId != timeZone.Id)
            timeZone = ResolveTimeZone(schedule.TimeZoneId, _timeZoneResolver);

        var triggerBuilder = TriggerBuilder.Create()
            .ForJob(jobKey)
            .WithIdentity(triggerKey)
            .StartAt(schedule.StartTime)
            .WithDescription(schedule.Description)
            .WithRetryPolicy(_deliveryRetryPolicy)
            .WithCronSchedule(schedule.CronExpression, x =>
            {
                x.InTimeZone(timeZone);
                switch (schedule.MisfirePolicy)
                {
                    case MissedEventPolicy.Skip:
                        x.WithMisfireInstruction(CronTriggerMisfireInstruction.DoNothing);
                        break;

                    case MissedEventPolicy.Send:
                        x.WithMisfireInstruction(CronTriggerMisfireInstruction.FireAndProceed);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(schedule.MisfirePolicy),
                            schedule.MisfirePolicy,
                            "The recurring schedule misfire policy is not supported.");
                }
            });

        if (schedule.EndTime.HasValue)
            triggerBuilder.EndAt(schedule.EndTime);

        var trigger = PopulateTrigger(context.Advanced(), triggerBuilder, messageBody, context.Message.Destination, context.Message.PayloadType,
            messageId: default,
            messageIdSeed: NewId.NextGuid(),
            scheduleId: schedule.ScheduleId,
            scheduleGroup: schedule.ScheduleGroup);

        await scheduler.ScheduleJob(trigger, ScheduleJobOptions.Replacing, context.CancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Scheduled: {Key} {Schedule}", triggerKey, trigger.NextFireTimeUtc);
    }

    static TimeZoneInfo ResolveTimeZone(string id, Func<string, TimeZoneInfo?>? customResolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Exception platformFailure;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            platformFailure = exception;
        }

        return customResolver?.Invoke(id) ?? throw new TimeZoneNotFoundException(
            $"The time zone '{id}' could not be resolved by the platform or the configured resolver.",
            platformFailure);
    }

    static ITrigger PopulateTrigger(ConsumeContext context, TriggerBuilder<IJob> builder, MessageBody messageBody, Uri destination,
        string[] messageTypes, Guid? messageId = default, Guid? messageIdSeed = default, Guid? tokenId = default, string? scheduleId = default,
        string? scheduleGroup = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(messageBody);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(messageTypes);

        builder = builder
            .UsingJobData(QuartzJobDataKeys.DestinationAddress, destination.ToString())
            .UsingJobData(QuartzJobDataKeys.Body, messageBody.GetRequiredTransportText())
            .UsingJobData(QuartzJobDataKeys.ContentType, context.ReceiveContext.ContentType.ToString())
            .UsingJobData(QuartzJobDataKeys.MessageTypes, string.Join(";", messageTypes));

        builder = UseOptionalJobData(builder, QuartzJobDataKeys.MessageId, messageId?.ToString("D"));
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.MessageIdSeed, messageIdSeed?.ToString("D"));
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.CorrelationId, context.CorrelationId?.ToString("D"));
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.ConversationId, context.ConversationId?.ToString("D"));
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.InitiatorId, context.InitiatorId?.ToString("D"));
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.RequestId, context.RequestId?.ToString("D"));
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.SourceAddress, context.SourceAddress?.ToString());
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.ResponseAddress, context.ResponseAddress?.ToString());
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.FaultAddress, context.FaultAddress?.ToString());
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.ExpirationTime,
            context.ExpirationTime?.ToString("O", CultureInfo.InvariantCulture));
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.SchedulingTokenId, tokenId?.ToString("N"));
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.ScheduleId, scheduleId);
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.ScheduleGroup, scheduleGroup);
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.Headers, SerializeHeaders(context.Headers));
        builder = UseOptionalJobData(builder, QuartzJobDataKeys.TransportProperties, SerializeTransportProperties(context));

        return builder.Build();
    }

    static TriggerBuilder<IJob> UseOptionalJobData(TriggerBuilder<IJob> builder, string key, string? value)
    {
        return value is null ? builder : builder.UsingJobData(key, value);
    }

    static string? SerializeHeaders(Headers source)
    {
        List<KeyValuePair<string, object>> headers = source.GetAll().ToList();
        PreserveTraceHeader(source, headers, DiagnosticPropagationHeaders.ActivityId);
        PreserveTraceHeader(source, headers, DiagnosticPropagationHeaders.Baggage);
        PreserveTraceHeader(source, headers, DiagnosticPropagationHeaders.ParentMode);

        return headers.Count == 0
            ? null
            : JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options);
    }

    static string? SerializeTransportProperties(ConsumeContext context)
    {
        if (!context.ReceiveContext.TryGetPayload(out TransportReceiveContext? transportContext))
            return null;

        IDictionary<string, object>? properties = transportContext.GetTransportProperties();
        return properties is null
            ? null
            : JsonSerializer.Serialize(properties, ServiceBusMetadataJson.Options);
    }

    static void PreserveTraceHeader(Headers source, List<KeyValuePair<string, object>> destination, string key)
    {
        // Trace metadata crosses the scheduling boundary even when a serializer omits system headers.
        if (destination.Any(header => string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase)))
            return;

        if (source.TryGetHeader(key, out var value))
            destination.Add(new KeyValuePair<string, object>(key, value));
    }

    private ValueTask<IScheduler> GetSchedulerAsync(CancellationToken cancellationToken)
    {
        return _binding is not null
            ? _binding.GetSchedulerAsync(cancellationToken)
            : _schedulerFactory!.GetScheduler(cancellationToken);
    }

    static async Task<JobKey> EnsureJobExistsAsync(IScheduler scheduler, string schedulerNamespace, CancellationToken cancellationToken)
    {
        var jobKey = new JobKey(ScheduledMessageJobIdentity, schedulerNamespace);

        IJobDetail? existing = await scheduler.GetJobDetail(jobKey, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            ValidateJob(existing);
            return jobKey;
        }

        var jobDetail = JobBuilder.Create<QuartzScheduledMessageJob<TBus>>()
            .RequestRecovery()
            .StoreDurably()
            .WithIdentity(jobKey)
            .WithDescription("ViciOne.ServiceBus Quartz scheduled-message delivery")
            .Build();

        try
        {
            await scheduler.AddJob(jobDetail, default, cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectAlreadyExistsException)
        {
            existing = await scheduler.GetJobDetail(jobKey, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw;
            ValidateJob(existing);
        }

        return jobKey;
    }

    private static void ValidateJob(IJobDetail job)
    {
        if (job.JobType != typeof(QuartzScheduledMessageJob<TBus>) || !job.Durable || !job.RequestsRecovery)
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "Quartz scheduling",
                typeof(TBus).FullName ?? typeof(TBus).Name,
                $"Job '{job.Key}' is not the durable recovery-enabled {typeof(QuartzScheduledMessageJob<TBus>)} job required by this bus",
                "Remove or replace the incompatible Quartz job before starting message scheduling"));
        }
    }
}
