#nullable enable
// ReSharper disable once CheckNamespace
namespace ViciOne.ServiceBus.Logging
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Courier.Contracts;
    using Metadata;
    using Middleware;
    using Transports;


    public static class LogContextActivityExtensions
    {
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

            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.Operation, "send");
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.System, transportContext.ActivitySystem);
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.DestinationName, transportContext.ActivityDestination);

            return PopulateSendActivity<T>(context, activity, currentActivity, tags);
        }

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

            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.Operation, "send");

            return PopulateSendActivity<T>(context, activity, currentActivity);
        }

        public static StartedActivity? StartOutboxDeliverActivity(this ILogContext logContext, OutboxMessageContext context)
        {
            var parentActivityContext = GetParentActivityContext(context.Headers);

            var activity = ActivityObservation.TryCreate(Cached.Source, "outbox process", ActivityKind.Client, parentActivityContext);
            if (activity == null)
                return null;

            if (!ActivityObservation.TryStart(activity))
                return null;

            return new StartedActivity(activity);
        }

        public static StartedActivity? StartReceiveActivity(this ILogContext logContext, string name, string inputAddress, string endpointName,
            ReceiveContext context)
        {
            var parentActivityContext = GetParentActivityContext(context.TransportHeaders, true);

            var activity = context.TransportHeaders.TryGetHeader(DiagnosticHeaders.ActivityPropagation, out var linkTypeValue) switch
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

            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.Operation, "receive");
            ActivityObservation.TrySetTag(
                activity,
                DiagnosticHeaders.Messaging.System,
                LogContextInstrumentationExtensions.SystemName(context));
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.DestinationName, endpointName);

            if (activity.IsAllDataRequested)
            {
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.InputAddress, inputAddress);

                if ((context.TransportHeaders.TryGetHeader(MessageHeaders.TransportMessageId, out var messageIdHeader)
                        || context.TransportHeaders.TryGetHeader(MessageHeaders.MessageId, out messageIdHeader))
                    && messageIdHeader is string text)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.TransportMessageId, text);
            }

            if (!ActivityObservation.TryStart(activity))
                return null;

            return new StartedActivity(activity);
        }

        public static StartedActivity? StartConsumerActivity<TConsumer, T>(this ILogContext logContext, ConsumeContext<T> context)
            where T : class
        {
            return StartActivity(context, activity =>
            {
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, TypeCache<TConsumer>.ShortName);
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<T>.DiagnosticAddress);
            });
        }

        public static StartedActivity? StartHandlerActivity<T>(this ILogContext logContext, ConsumeContext<T> context)
            where T : class
        {
            return StartActivity(context, activity =>
            {
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, "Handler");
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<T>.DiagnosticAddress);
            });
        }

        public static StartedActivity? StartSagaActivity<TSaga, T>(this ILogContext logContext, SagaConsumeContext<TSaga, T> context)
            where TSaga : class, ISaga
            where T : class
        {
            return StartActivity(context, activity =>
            {
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.SagaId, context.Saga.CorrelationId.ToString("D"));
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, TypeCache<TSaga>.ShortName);
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<T>.DiagnosticAddress);
            });
        }

        public static StartedActivity? StartSagaStateMachineActivity<TSaga, T>(this ILogContext logContext, BehaviorContext<TSaga, T> context)
            where TSaga : class, SagaStateMachineInstance
            where T : class
        {
            return StartActivity(context, activity =>
            {
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.SagaId, context.Saga.CorrelationId.ToString("D"));
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, context.StateMachine.Name);
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<T>.DiagnosticAddress);
            });
        }

        public static StartedActivity? StartExecuteActivity<TActivity, TArguments>(this ILogContext logContext, ConsumeContext<RoutingSlip> context)
            where TActivity : IExecuteActivity<TArguments>
            where TArguments : class
        {
            return StartActivity(context, activity =>
            {
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.TrackingNumber, context.Message.TrackingNumber.ToString("D"));
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, TypeCache<TActivity>.ShortName);
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<TArguments>.DiagnosticAddress);
            });
        }

        public static StartedActivity? StartCompensateActivity<TActivity, TLog>(this ILogContext logContext, ConsumeContext<RoutingSlip> context)
            where TActivity : ICompensateActivity<TLog>
            where TLog : class
        {
            return StartActivity(context, activity =>
            {
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.TrackingNumber, context.Message.TrackingNumber.ToString("D"));
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, TypeCache<TActivity>.ShortName);
                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<TLog>.DiagnosticAddress);
            });
        }

        public static StartedActivity? StartGenericActivity(this ILogContext logContext, string operationName)
        {
            var activity = ActivityObservation.TryCreate(Cached.Source, operationName, ActivityKind.Client);
            if (activity == null)
                return null;

            if (!ActivityObservation.TryStart(activity))
                return null;

            return new StartedActivity(activity);
        }

        static StartedActivity? PopulateSendActivity<T>(SendContext context, System.Diagnostics.Activity activity,
            System.Diagnostics.Activity? parentActivity, params (string Key, object? Value)[] tags)
            where T : class
        {
            var conversationId = context.ConversationId?.ToString("D");

            if (context.CorrelationId.HasValue)
                ActivityObservation.TrySetBaggage(activity, DiagnosticHeaders.CorrelationId, context.CorrelationId.Value.ToString("D"));
            if (conversationId != null)
                ActivityObservation.TrySetBaggage(activity, DiagnosticHeaders.Messaging.ConversationId, conversationId);

            if (activity.IsAllDataRequested)
            {
                if (context.MessageId.HasValue)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.MessageId, context.MessageId.Value.ToString("D"));
                if (conversationId != null)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.ConversationId, conversationId);
                if (context.CorrelationId.HasValue)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.CorrelationId, context.CorrelationId.Value.ToString("D"));
                if (context.RequestId.HasValue)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.RequestId, context.RequestId.Value.ToString("D"));
                if (context.InitiatorId.HasValue)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.InitiatorId, context.InitiatorId.Value.ToString("D"));
                if (context.SourceAddress != null)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.SourceAddress, context.SourceAddress.ToString());
                if (context.DestinationAddress != null)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.DestinationAddress, context.DestinationAddress.ToString());

                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.MessageTypes, string.Join(",", context.SupportedMessageTypes));

                for (var i = 0; i < tags.Length; i++)
                {
                    if (tags[i].Value != null)
                        ActivityObservation.TrySetTag(activity, tags[i].Key, tags[i].Value?.ToString());
                }
            }

            if (!ActivityObservation.TryStart(activity))
            {
                PropagateActivity(context, parentActivity);
                return null;
            }

            List<KeyValuePair<string, string?>>? baggage = null;
            foreach (KeyValuePair<string, string?> pair in activity.Baggage)
            {
                if (pair.Key.Equals(DiagnosticHeaders.Messaging.ConversationId, StringComparison.Ordinal)
                    || pair.Key.Equals(DiagnosticHeaders.CorrelationId, StringComparison.Ordinal))
                    continue;

                if (string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                baggage ??= new List<KeyValuePair<string, string?>>(1);
                baggage.Add(pair);
            }

            if (activity.Id != null)
                context.Headers.Set(DiagnosticHeaders.ActivityId, activity.Id);

            if (!string.IsNullOrWhiteSpace(activity.TraceStateString))
                context.Headers.Set(DiagnosticHeaders.ActivityTraceState, activity.TraceStateString);

            if (baggage != null)
                context.Headers.Set(DiagnosticHeaders.ActivityCorrelationContext, baggage);

            return new StartedActivity(activity);
        }

        static void PropagateActivity(SendContext context, System.Diagnostics.Activity? activity)
        {
            if (activity is null)
                return;

            if (activity.Id is { } activityId)
                context.Headers.Set(DiagnosticHeaders.ActivityId, activityId);

            if (!string.IsNullOrWhiteSpace(activity.TraceStateString))
                context.Headers.Set(DiagnosticHeaders.ActivityTraceState, activity.TraceStateString);

            List<KeyValuePair<string, string?>>? baggage = null;
            foreach (KeyValuePair<string, string?> pair in activity.Baggage)
            {
                if (pair.Key.Equals(DiagnosticHeaders.Messaging.ConversationId, StringComparison.Ordinal)
                    || pair.Key.Equals(DiagnosticHeaders.CorrelationId, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                baggage ??= new List<KeyValuePair<string, string?>>(1);
                baggage.Add(pair);
            }

            if (baggage is not null)
                context.Headers.Set(DiagnosticHeaders.ActivityCorrelationContext, baggage);
        }

        static string? GetTraceState(Headers headers)
        {
            return headers.TryGetHeader(DiagnosticHeaders.ActivityTraceState, out var value) ? value as string : null;
        }

        static ActivityContext GetParentActivityContext(Headers headers, bool isRemote = false)
        {
            if (headers.TryGetHeader(DiagnosticHeaders.ActivityId, out var headerValue)
                && headerValue is string activityId
                && ActivityContext.TryParse(activityId, GetTraceState(headers), out var activityContext))
            {
                if (isRemote && System.Diagnostics.Activity.Current == null)
                    return new ActivityContext(activityContext.TraceId, activityContext.SpanId, activityContext.TraceFlags, activityContext.TraceState, true);

                return activityContext;
            }

            return default;
        }

        static StartedActivity? StartActivity(ConsumeContext context, Action<System.Diagnostics.Activity> started)
        {
            var currentActivity = System.Diagnostics.Activity.Current;
            if (currentActivity == null)
                return null;

            var operationName = Cached.OperationNames.GetOrAdd(currentActivity.OperationName, add =>
            {
                if (add.EndsWith(" receive"))
                    return add.Substring(0, add.Length - 8) + " process";
                if (add.EndsWith(" process"))
                    return add;

                return currentActivity.OperationName;
            });

            var activity = ActivityObservation.TryCreate(Cached.Source, operationName, ActivityKind.Consumer);
            if (activity == null)
                return null;

            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.Operation, "process");
            ActivityObservation.TrySetTag(
                activity,
                DiagnosticHeaders.Messaging.System,
                LogContextInstrumentationExtensions.SystemName(context.ReceiveContext));

            if (activity.IsAllDataRequested)
            {
                if (context.MessageId.HasValue)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.MessageId, context.MessageId.Value.ToString("D"));
                if (context.ConversationId.HasValue)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.Messaging.ConversationId, context.ConversationId.Value.ToString("D"));
                if (context.CorrelationId.HasValue)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.CorrelationId, context.CorrelationId.Value.ToString("D"));
                if (context.RequestId.HasValue)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.RequestId, context.RequestId.Value.ToString("D"));
                if (context.InitiatorId.HasValue)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.InitiatorId, context.InitiatorId.Value.ToString("D"));
                if (context.SourceAddress != null)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.SourceAddress, context.SourceAddress.ToString());
                if (context.DestinationAddress != null)
                    ActivityObservation.TrySetTag(activity, DiagnosticHeaders.DestinationAddress, context.DestinationAddress.ToString());

                ActivityObservation.TrySetTag(activity, DiagnosticHeaders.MessageTypes, string.Join(",", context.SupportedMessageTypes));

                started(activity);
            }

            if (!ActivityObservation.TryStart(activity))
                return null;

            return new StartedActivity(activity);
        }


        static class Cached
        {
            internal static readonly Lazy<ActivitySource> Source = new Lazy<ActivitySource>(() =>
                new ActivitySource(DiagnosticHeaders.DefaultListenerName, HostMetadataCache.Host.ViciOneServiceBusVersion));

            internal static readonly ConcurrentDictionary<string, string> OperationNames = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        }
    }
}
