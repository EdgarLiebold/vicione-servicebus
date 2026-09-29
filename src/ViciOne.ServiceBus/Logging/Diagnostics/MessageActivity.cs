using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Logging.Diagnostics;

/// <summary>Creates message-flow activities and propagates their trace context across transport boundaries.</summary>
internal static class MessageActivity
{
    /// <summary>Starts a producer activity for a transport send and injects its trace context into the message.</summary>
    /// <typeparam name="T">The message contract being sent.</typeparam>
    /// <param name="transportContext">The transport identity used to name and describe the activity.</param>
    /// <param name="context">The send context that receives propagation headers and message metadata tags.</param>
    /// <returns>The started activity, or <see langword="null"/> when the source is not sampled or observation fails.</returns>
    public static StartedActivity? TryStartSend<T>(SendTransportContext transportContext, SendContext<T> context)
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

        return PopulateSendActivity(context, activity, currentActivity);
    }

    /// <summary>Starts an outbox producer activity and injects its trace context into the deferred message.</summary>
    /// <typeparam name="T">The message contract being enqueued.</typeparam>
    /// <param name="context">The outbox send context that receives propagation headers and message metadata tags.</param>
    /// <returns>The started activity, or <see langword="null"/> when the source is not sampled or observation fails.</returns>
    public static StartedActivity? TryStartOutboxSend<T>(SendContext<T> context)
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

    /// <summary>Starts a client activity for delivery of a message retained by an outbox.</summary>
    /// <param name="context">The retained message context that supplies the parent trace identity.</param>
    /// <returns>The started activity, or <see langword="null"/> when the source is not sampled or observation fails.</returns>
    public static StartedActivity? TryStartOutboxDelivery(OutboxMessageContext context)
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

    /// <summary>Starts the consumer activity that represents transport delivery into a receive pipeline.</summary>
    /// <param name="name">The low-cardinality activity name supplied by the transport.</param>
    /// <param name="inputAddress">The receive address recorded when full activity data is requested.</param>
    /// <param name="endpointName">The bounded destination name used by the receive endpoint.</param>
    /// <param name="context">The receive context that supplies remote trace headers, transport identity, and timing.</param>
    /// <returns>The started activity, or <see langword="null"/> when the source is not sampled or observation fails.</returns>
    public static StartedActivity? TryStartReceive(string name, string inputAddress, string endpointName,
        ReceiveContext context)
    {
        var parentActivityContext = GetParentActivityContext(context.TransportHeaders, true);
        bool newRoot = context.TransportHeaders.TryGetHeader(DiagnosticPropagationHeaders.ParentMode, out var parentMode)
            && parentMode is "Link" or "New";

        var activity = context.TransportHeaders.TryGetHeader(DiagnosticPropagationHeaders.ParentMode, out var linkTypeValue) switch
        {
            true => linkTypeValue switch
            {
                "Link" => ActivityObservation.TryCreate(Cached.Source, name, ActivityKind.Consumer, default,
                    [new ActivityLink(parentActivityContext)], newRoot: true),
                "New" => ActivityObservation.TryCreate(Cached.Source, name, ActivityKind.Consumer, newRoot: true),
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
            LogContextMetricsExtensions.SystemName(context));
        ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.DestinationName, endpointName);

        if (activity.IsAllDataRequested)
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.InputAddress, inputAddress);

            if ((context.TransportHeaders.TryGetHeader(MessageHeaders.TransportMessageId, out var messageIdHeader)
                    || context.TransportHeaders.TryGetHeader(MessageHeaders.MessageId, out messageIdHeader))
                && messageIdHeader is string text)
                ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageId, text);
        }

        if (!ActivityObservation.TryStart(activity, newRoot))
            return null;

        return new StartedActivity(activity, context.GetTimeProvider());
    }

    /// <summary>Starts a process activity for a message delivered to a consumer.</summary>
    /// <typeparam name="TConsumer">The consumer implementation recorded as the processor.</typeparam>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The consume context that supplies the receive parent and message metadata.</param>
    /// <returns>The started activity, or <see langword="null"/> when no receive activity is current or observation fails.</returns>
    public static StartedActivity? TryStartConsumer<TConsumer, T>(ConsumeContext<T> context)
        where T : class
    {
        return TryStartProcess((ConsumeContext)context, activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, TypeCache<TConsumer>.ShortName);
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<T>.DiagnosticAddress);
        });
    }

    /// <summary>Starts a process activity for a message handled by a delegate.</summary>
    /// <typeparam name="T">The handled message contract.</typeparam>
    /// <param name="context">The consume context that supplies the receive parent and message metadata.</param>
    /// <returns>The started activity, or <see langword="null"/> when no receive activity is current or observation fails.</returns>
    public static StartedActivity? TryStartHandler<T>(ConsumeContext<T> context)
        where T : class
    {
        return TryStartProcess((ConsumeContext)context, activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, "Handler");
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<T>.DiagnosticAddress);
        });
    }

    /// <summary>Starts a client activity for an infrastructure operation.</summary>
    /// <param name="operationName">The low-cardinality operation name.</param>
    /// <returns>The started activity, or <see langword="null"/> when the source is not sampled or observation fails.</returns>
    public static StartedActivity? TryStart(string operationName)
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
        System.Diagnostics.Activity? parentActivity)
    {
        AddSendBaggage(context, activity);
        AddSendTags(context, activity);

        if (!ActivityObservation.TryStart(activity))
        {
            PropagateActivity(context, parentActivity);
            return null;
        }

        PropagateActivity(context, activity);
        return new StartedActivity(activity, context.GetTimeProvider());
    }

    static void AddSendBaggage(SendContext context, System.Diagnostics.Activity activity)
    {
        if (context.CorrelationId is { } correlationId)
            ActivityObservation.TrySetBaggage(activity, ServiceBusTelemetry.Attributes.CorrelationId, correlationId.ToString("D"));
        if (context.ConversationId is { } conversationId)
            ActivityObservation.TrySetBaggage(activity, ServiceBusTelemetry.Attributes.ConversationId, conversationId.ToString("D"));
    }

    static void AddSendTags(SendContext context, System.Diagnostics.Activity activity)
    {
        if (!activity.IsAllDataRequested)
            return;

        AddSendIdentifierTags(context, activity);
        AddSendAddressTags(context, activity);
        ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContracts, string.Join(",", context.SupportedMessageTypes));
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

    internal static System.Diagnostics.ActivityContext GetParentActivityContext(Headers headers, bool isRemote = false)
    {
        if (headers.TryGetHeader(DiagnosticPropagationHeaders.ActivityId, out var headerValue)
            && headerValue is string activityId
            && System.Diagnostics.ActivityContext.TryParse(activityId, GetTraceState(headers), out var activityContext))
        {
            if (isRemote)
                return new System.Diagnostics.ActivityContext(activityContext.TraceId, activityContext.SpanId, activityContext.TraceFlags,
                    activityContext.TraceState, true);

            return activityContext;
        }

        return default;
    }

    internal static StartedActivity? TryStartProcess(ConsumeContext context, Action<System.Diagnostics.Activity> configure)
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
            LogContextMetricsExtensions.SystemName(context.ReceiveContext));

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

            configure(activity);
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
