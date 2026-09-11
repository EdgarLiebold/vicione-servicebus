using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Logging.Diagnostics;

/// <summary>Provides extension methods for log context activity.</summary>
internal static class LogContextActivityExtensions
{
    /// <summary>Starts send activity.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="logContext">The log context.</param>
    /// <param name="transportContext">The transport context.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="tags">The tags.</param>
    /// <returns>The started activity produced by the operation.</returns>
    public static StartedActivity? StartSendActivity<T>(this ILogContext logContext, SendTransportContext transportContext, SendContext<T> context,
        params (string Key, object? Value)[] tags)
        where T : class
    {
        var currentActivity = System.Diagnostics.Activity.Current;
        var parentActivityContext = currentActivity == null
            ? GetParentActivityContext(context.Headers)
            : default;

        var activity = ActivityObservation.TryCreate(Cached.Source, transportContext.ActivityName, ActivityKind.Producer, parentActivityContext);
        if (activity == null)
        {
            PropagateActivity(context, currentActivity);
            return null;
        }

        SetOperation(activity, "send");
        ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessagingSystem, transportContext.ActivitySystem);
        ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.DestinationName, transportContext.ActivityDestination);

        return PopulateSendActivity(context, activity, currentActivity, tags);
    }

    /// <summary>Starts outbox send activity.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="logContext">The log context.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The started activity produced by the operation.</returns>
    public static StartedActivity? StartOutboxSendActivity<T>(this ILogContext logContext, SendContext<T> context)
        where T : class
    {
        var currentActivity = System.Diagnostics.Activity.Current;
        var parentActivityContext = currentActivity == null
            ? GetParentActivityContext(context.Headers)
            : default;

        var activity = ActivityObservation.TryCreate(Cached.Source, "outbox send", ActivityKind.Producer, parentActivityContext);
        if (activity == null)
        {
            PropagateActivity(context, currentActivity);
            return null;
        }

        SetOperation(activity, "send");

        return PopulateSendActivity(context, activity, currentActivity);
    }

    /// <summary>Starts outbox deliver activity.</summary>
    /// <param name="logContext">The log context.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The started activity produced by the operation.</returns>
    public static StartedActivity? StartOutboxDeliverActivity(this ILogContext logContext, OutboxMessageContext context)
    {
        var parentActivityContext = GetParentActivityContext(context.Headers);

        var activity = ActivityObservation.TryCreate(Cached.Source, "outbox process", ActivityKind.Client, parentActivityContext);
        if (activity == null)
            return null;

        SetOperation(activity, "process");
        if (!ActivityObservation.TryStart(activity))
            return null;

        return new StartedActivity(activity);
    }

    /// <summary>Starts receive activity.</summary>
    /// <param name="logContext">The log context.</param>
    /// <param name="name">The name.</param>
    /// <param name="inputAddress">The input address.</param>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The started activity produced by the operation.</returns>
    public static StartedActivity? StartReceiveActivity(this ILogContext logContext, string name, string inputAddress, string endpointName,
        ReceiveContext context)
    {
        var parentActivityContext = GetParentActivityContext(context.TransportHeaders, true);

        var activity = context.TransportHeaders.TryGetHeader(DiagnosticPropagationHeaders.ParentMode, out var linkTypeValue) switch
        {
            true => linkTypeValue switch
            {
                "Link" => ActivityObservation.TryCreate(Cached.Source, name, ActivityKind.Consumer, default,
                    [new ActivityLink(parentActivityContext)]),
                "New" => ActivityObservation.TryCreate(Cached.Source, name, ActivityKind.Consumer),
                _ => ActivityObservation.TryCreate(Cached.Source, name, ActivityKind.Consumer, parentActivityContext)
            },
            false => ActivityObservation.TryCreate(Cached.Source, name, ActivityKind.Consumer, parentActivityContext)
        };

        if (activity == null)
            return null;

        SetOperation(activity, "receive");
        ActivityObservation.TrySetTag(
            activity,
            ServiceBusTelemetry.Attributes.MessagingSystem,
            LogContextInstrumentationExtensions.SystemName(context));
        ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.DestinationName, endpointName);

        if (activity.IsAllDataRequested)
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.InputAddress, inputAddress);

            if ((context.TransportHeaders.TryGetHeader(MessageHeaders.TransportMessageId, out var messageIdHeader)
                    || context.TransportHeaders.TryGetHeader(MessageHeaders.MessageId, out messageIdHeader))
                && messageIdHeader is string text)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageId, text);
        }

        if (!ActivityObservation.TryStart(activity))
            return null;

        return new StartedActivity(activity, context.GetTimeProvider());
    }

    /// <summary>Starts consumer activity.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="logContext">The log context.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The started activity produced by the operation.</returns>
    public static StartedActivity? StartConsumerActivity<TConsumer, T>(this ILogContext logContext, ConsumeContext<T> context)
        where T : class
    {
        return StartActivity((ConsumeContext)context, activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, TypeCache<TConsumer>.ShortName);
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<T>.DiagnosticAddress);
        });
    }

    /// <summary>Starts handler activity.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="logContext">The log context.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The started activity produced by the operation.</returns>
    public static StartedActivity? StartHandlerActivity<T>(this ILogContext logContext, ConsumeContext<T> context)
        where T : class
    {
        return StartActivity((ConsumeContext)context, activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, "Handler");
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<T>.DiagnosticAddress);
        });
    }

    /// <summary>Starts generic activity.</summary>
    /// <param name="logContext">The log context.</param>
    /// <param name="operationName">The operation name.</param>
    /// <returns>The started activity produced by the operation.</returns>
    public static StartedActivity? StartGenericActivity(this ILogContext logContext, string operationName)
    {
        var activity = ActivityObservation.TryCreate(Cached.Source, operationName, ActivityKind.Client);
        if (activity == null)
            return null;

        if (!ActivityObservation.TryStart(activity))
            return null;

        return new StartedActivity(activity);
    }

    internal static void AddConsumeContextTags(this System.Diagnostics.Activity activity, ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(context);

        if (activity.IsAllDataRequested)
        {
            if (context.MessageId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageId, context.MessageId.Value.ToString("D"));
            if (context.ConversationId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ConversationId, context.ConversationId.Value.ToString("D"));
            if (context.CorrelationId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.CorrelationId, context.CorrelationId.Value.ToString("D"));
            if (context.InitiatorId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.InitiatorId, context.InitiatorId.Value.ToString("D"));
            if (context.RequestId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.RequestId, context.RequestId.Value.ToString("D"));
            if (context.SourceAddress is not null)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.SourceAddress, context.SourceAddress.ToString());
            if (context.DestinationAddress is not null)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.DestinationAddress, context.DestinationAddress.ToString());

            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContracts, string.Join(",", context.SupportedMessageTypes));
        }

        if (context.CorrelationId.HasValue)
            ActivityObservation.TrySetBaggage(activity, ServiceBusTelemetry.Attributes.CorrelationId, context.CorrelationId.Value.ToString("D"));
        if (context.ConversationId.HasValue)
            ActivityObservation.TrySetBaggage(activity, ServiceBusTelemetry.Attributes.ConversationId, context.ConversationId.Value.ToString("D"));

        if (!context.TryGetHeader(DiagnosticPropagationHeaders.Baggage, out IEnumerable<KeyValuePair<string, object>>? baggage))
            return;

        foreach (KeyValuePair<string, object> value in baggage)
        {
            if (value.Value is string text && !string.IsNullOrWhiteSpace(text))
                ActivityObservation.TrySetBaggage(activity, value.Key, text);
        }
    }

    static StartedActivity? PopulateSendActivity(SendContext context, System.Diagnostics.Activity activity,
        System.Diagnostics.Activity? parentActivity, params (string Key, object? Value)[] tags)
    {
        CopyParentTraceState(parentActivity, activity);
        AddSendBaggage(context, activity);
        AddSendTags(context, activity, tags);

        if (!ActivityObservation.TryStart(activity))
        {
            PropagateActivity(context, parentActivity);
            return null;
        }

        PropagateActivity(context, activity);
        return new StartedActivity(activity, context.GetTimeProvider());
    }

    static void CopyParentTraceState(System.Diagnostics.Activity? parentActivity, System.Diagnostics.Activity activity)
    {
        if (!string.IsNullOrWhiteSpace(parentActivity?.TraceStateString)
            && string.IsNullOrWhiteSpace(activity.TraceStateString))
            ActivityObservation.TrySetTraceState(activity, parentActivity.TraceStateString);
    }

    static void AddSendBaggage(SendContext context, System.Diagnostics.Activity activity)
    {
        if (context.CorrelationId is { } correlationId)
            ActivityObservation.TrySetBaggage(activity, ServiceBusTelemetry.Attributes.CorrelationId, correlationId.ToString("D"));
        if (context.ConversationId is { } conversationId)
            ActivityObservation.TrySetBaggage(activity, ServiceBusTelemetry.Attributes.ConversationId, conversationId.ToString("D"));
    }

    static void AddSendTags(SendContext context, System.Diagnostics.Activity activity, (string Key, object? Value)[] tags)
    {
        if (!activity.IsAllDataRequested)
            return;

        AddSendIdentifierTags(context, activity);
        AddSendAddressTags(context, activity);
        ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContracts, string.Join(",", context.SupportedMessageTypes));
        AddCustomTags(activity, tags);
    }

    static void AddSendIdentifierTags(SendContext context, System.Diagnostics.Activity activity)
    {
        if (context.MessageId is { } messageId)
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageId, messageId.ToString("D"));
        if (context.ConversationId is { } conversationId)
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ConversationId, conversationId.ToString("D"));
        if (context.CorrelationId is { } correlationId)
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.CorrelationId, correlationId.ToString("D"));
        if (context.RequestId is { } requestId)
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.RequestId, requestId.ToString("D"));
        if (context.InitiatorId is { } initiatorId)
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.InitiatorId, initiatorId.ToString("D"));
    }

    static void AddSendAddressTags(SendContext context, System.Diagnostics.Activity activity)
    {
        if (context.SourceAddress is { } sourceAddress)
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.SourceAddress, sourceAddress.ToString());
        if (context.DestinationAddress is { } destinationAddress)
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.DestinationAddress, destinationAddress.ToString());
    }

    static void AddCustomTags(System.Diagnostics.Activity activity, (string Key, object? Value)[] tags)
    {
        foreach ((string key, object? value) in tags)
        {
            if (value is not null)
                ActivityObservation.TrySetTag(activity, key, value.ToString());
        }
    }

    static void PropagateActivity(SendContext context, System.Diagnostics.Activity? activity)
    {
        if (activity is null)
            return;

        if (activity.Id is { } activityId)
            context.Headers.Set(DiagnosticPropagationHeaders.ActivityId, activityId);

        if (!string.IsNullOrWhiteSpace(activity.TraceStateString))
            context.Headers.Set(DiagnosticPropagationHeaders.TraceState, activity.TraceStateString);

        List<KeyValuePair<string, string?>>? baggage = null;
        foreach (KeyValuePair<string, string?> pair in activity.Baggage)
        {
            if (pair.Key.Equals(ServiceBusTelemetry.Attributes.ConversationId, StringComparison.Ordinal)
                || pair.Key.Equals(ServiceBusTelemetry.Attributes.CorrelationId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(pair.Value))
                continue;

            baggage ??= new List<KeyValuePair<string, string?>>(1);
            baggage.Add(pair);
        }

        if (baggage is not null)
            context.Headers.Set(DiagnosticPropagationHeaders.Baggage, baggage);
    }

    static string? GetTraceState(Headers headers)
    {
        return headers.TryGetHeader(DiagnosticPropagationHeaders.TraceState, out var value) ? value as string : null;
    }

    static System.Diagnostics.ActivityContext GetParentActivityContext(Headers headers, bool isRemote = false)
    {
        if (headers.TryGetHeader(DiagnosticPropagationHeaders.ActivityId, out var headerValue)
            && headerValue is string activityId
            && System.Diagnostics.ActivityContext.TryParse(activityId, GetTraceState(headers), out var activityContext))
        {
            if (isRemote && System.Diagnostics.Activity.Current == null)
                return new System.Diagnostics.ActivityContext(activityContext.TraceId, activityContext.SpanId, activityContext.TraceFlags,
                    activityContext.TraceState, true);

            return activityContext;
        }

        return default;
    }

    internal static StartedActivity? StartActivity(ConsumeContext context, Action<System.Diagnostics.Activity> started)
    {
        var currentActivity = System.Diagnostics.Activity.Current;
        if (currentActivity == null)
            return null;

        string operationName = GetProcessOperationName(currentActivity.OperationName);

        var activity = ActivityObservation.TryCreate(Cached.Source, operationName, ActivityKind.Consumer);
        if (activity == null)
            return null;

        SetOperation(activity, "process");
        ActivityObservation.TrySetTag(
            activity,
            ServiceBusTelemetry.Attributes.MessagingSystem,
            LogContextInstrumentationExtensions.SystemName(context.ReceiveContext));

        if (activity.IsAllDataRequested)
        {
            if (context.MessageId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageId, context.MessageId.Value.ToString("D"));
            if (context.ConversationId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ConversationId, context.ConversationId.Value.ToString("D"));
            if (context.CorrelationId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.CorrelationId, context.CorrelationId.Value.ToString("D"));
            if (context.RequestId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.RequestId, context.RequestId.Value.ToString("D"));
            if (context.InitiatorId.HasValue)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.InitiatorId, context.InitiatorId.Value.ToString("D"));
            if (context.SourceAddress != null)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.SourceAddress, context.SourceAddress.ToString());
            if (context.DestinationAddress != null)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.DestinationAddress, context.DestinationAddress.ToString());

            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContracts, string.Join(",", context.SupportedMessageTypes));

            started(activity);
        }

        if (!ActivityObservation.TryStart(activity))
            return null;

        return new StartedActivity(activity, context.GetTimeProvider());
    }

    static string GetProcessOperationName(string operationName)
    {
        const string receiveSuffix = " receive";

        if (operationName.EndsWith(receiveSuffix, StringComparison.Ordinal))
            return string.Concat(operationName.AsSpan(0, operationName.Length - receiveSuffix.Length), " process");

        return operationName;
    }

    static void SetOperation(System.Diagnostics.Activity activity, string operation)
    {
        ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.OperationName, operation);
        ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.OperationType, operation);
    }


    static class Cached
    {
        internal static readonly Lazy<ActivitySource> Source = new Lazy<ActivitySource>(() =>
            new ActivitySource(ServiceBusTelemetry.ActivitySourceName, HostMetadataCache.Host.ViciOneServiceBusVersion));

    }
}
