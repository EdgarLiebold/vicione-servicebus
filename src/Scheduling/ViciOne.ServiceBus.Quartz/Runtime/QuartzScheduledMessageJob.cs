using System;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Quartz.Runtime;

/// <summary>Forwards a persisted scheduled message through its bus when the Quartz trigger fires.</summary>
internal sealed class QuartzScheduledMessageJob<TBus> : global::Quartz.IJob
    where TBus : class, IBus
{
    readonly IBus? _bus;
    readonly TimeProvider? _timeProvider;

    /// <summary>Creates a job whose dependencies are supplied through the scheduler context.</summary>
    public QuartzScheduledMessageJob()
    {
    }

    /// <summary>Creates a job with explicit bus and time-source dependencies.</summary>
    /// <param name="bus">The bus used to resolve the destination endpoint.</param>
    /// <param name="timeProvider">The clock used to derive the remaining message time to live.</param>
    internal QuartzScheduledMessageJob(IBus bus, TimeProvider timeProvider)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    ValueTask global::Quartz.IJob.Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, cancellationToken);
    }

    /// <summary>Reconstructs and sends the persisted message for the fired trigger.</summary>
    /// <param name="context">The fired Quartz job context and merged job data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A value task that completes when the scheduled message has been accepted by the configured send transport.</returns>
    internal async ValueTask ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        IBus bus = _bus ?? GetSchedulerContextValue<IBus>(context, QuartzSchedulerContextKeys.Bus);
        TimeProvider timeProvider = _timeProvider
            ?? GetSchedulerContextValue<TimeProvider>(context, QuartzSchedulerContextKeys.TimeProvider);

        QuartzScheduledMessageSendPipe pipe;
        Uri destinationAddress;
        string[] supportedMessageTypes;
        try
        {
            JobDataMap jobData = context.MergedJobDataMap;
            var messageContext = new QuartzScheduledMessageContext(context, ServiceBusMetadataJson.ObjectDeserializer);
            var contentType = new ContentType(GetRequiredString(jobData, QuartzJobDataKeys.ContentType));
            destinationAddress = messageContext.DestinationAddress ?? throw new InvalidOperationException(
                $"Quartz job data value '{QuartzJobDataKeys.DestinationAddress}' is required to deliver a scheduled message.");
            string body = GetRequiredString(jobData, QuartzJobDataKeys.Body, allowEmpty: true);
            supportedMessageTypes = jobData.TryGetString(QuartzJobDataKeys.MessageTypes, out string? text)
                ? text?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []
                : [];

            pipe = new QuartzScheduledMessageSendPipe(
                contentType,
                messageContext,
                body,
                destinationAddress,
                supportedMessageTypes,
                timeProvider);
        }
        catch (Exception exception) when (IsPermanentPayloadFailure(exception))
        {
            LogContext.Error?.Log(
                exception,
                "Rejected permanently invalid Quartz scheduled-message data for trigger {TriggerKey}",
                context.Trigger.Key);
            throw new JobExecutionException("The persisted scheduled-message data is invalid.", exception)
            {
                UnscheduleFiringTrigger = true,
            };
        }

        try
        {
            var endpoint = await bus.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);
            await endpoint.SendAsync(SerializedTransportMessage.Instance, pipe, cancellationToken).ConfigureAwait(false);
            LogContext.Debug?.Log("Schedule Executed: {Key} {Schedule}", context.Trigger.Key, context.Trigger.NextFireTimeUtc);
        }
        catch (InvalidScheduledMessageDataException exception)
        {
            LogContext.Error?.Log(
                exception,
                "Rejected permanently invalid Quartz scheduled-message data for trigger {TriggerKey}",
                context.Trigger.Key);
            throw new JobExecutionException("The persisted scheduled-message data is invalid.", exception)
            {
                UnscheduleFiringTrigger = true,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(
                exception,
                "Scheduled-message delivery failed and will follow trigger retry policy: {MessageType} {DestinationAddress} {RetryAttempt}",
                supportedMessageTypes,
                destinationAddress,
                context.Trigger.RetryAttempt);
            throw new JobExecutionException(exception);
        }
    }

    private static string GetRequiredString(JobDataMap jobData, string key, bool allowEmpty = false)
    {
        string? value = jobData.GetString(key);
        if (value is null || (!allowEmpty && string.IsNullOrWhiteSpace(value)))
        {
            throw new InvalidOperationException(
                $"Quartz job data value '{key}' is required to deliver a scheduled message.");
        }

        return value;
    }

    private static T GetSchedulerContextValue<T>(IJobExecutionContext context, string key)
    {
        if (context.Scheduler.Context.TryGetValue(key, out var value) && value is T typed)
            return typed;

        throw new InvalidOperationException(
            $"Quartz scheduler context value '{key}' is missing. Configure the scheduler through {nameof(QuartzSchedulingExtensions.ConfigureQuartzScheduler)} or a bus registration Quartz adapter.");
    }

    private static bool IsPermanentPayloadFailure(Exception exception)
    {
        return exception is FormatException
            or InvalidOperationException
            or JsonException
            or UriFormatException;
    }
}
