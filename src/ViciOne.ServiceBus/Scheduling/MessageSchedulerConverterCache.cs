using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Dispatches runtime-typed scheduling operations to their generic implementations.</summary>
internal sealed class MessageSchedulerConverterCache
{
    readonly ConcurrentDictionary<Type, Lazy<IMessageSchedulerConverter>> _types = new ConcurrentDictionary<Type, Lazy<IMessageSchedulerConverter>>();

    IMessageSchedulerConverter this[Type type] => _types.GetOrAdd(type, CreateTypeConverter).Value;

    internal static Task<ScheduledMessage> ScheduleSendAsync(IMessageScheduler scheduler, Uri destinationAddress, DateTimeOffset dueAt, object message,
        Type messageType, CancellationToken cancellationToken)
    {
        return Cached.Converters.Value[messageType].ScheduleSendAsync(scheduler, destinationAddress, dueAt, message, cancellationToken);
    }

    internal static Task<ScheduledMessage> ScheduleSendAsync(IMessageScheduler scheduler, Uri destinationAddress, DateTimeOffset dueAt, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return Cached.Converters.Value[messageType].ScheduleSendAsync(scheduler, destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    internal static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(IRecurringMessageScheduler scheduler, Uri destinationAddress,
        RecurringSchedule schedule, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return Cached.Converters.Value[messageType].ScheduleRecurringSendAsync(scheduler, destinationAddress, schedule, message, pipe, cancellationToken);
    }

    internal static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(IRecurringMessageScheduler scheduler, Uri destinationAddress,
        RecurringSchedule schedule, object message,
        Type messageType, CancellationToken cancellationToken)
    {
        return Cached.Converters.Value[messageType].ScheduleRecurringSendAsync(scheduler, destinationAddress, schedule, message, cancellationToken);
    }

    static Lazy<IMessageSchedulerConverter> CreateTypeConverter(Type type)
    {
        return new Lazy<IMessageSchedulerConverter>(() => CreateConverter(type));
    }

    static IMessageSchedulerConverter CreateConverter(Type type)
    {
        var converterType = typeof(MessageSchedulerConverter<>).MakeGenericType(type);

        return (IMessageSchedulerConverter)(Activator.CreateInstance(converterType) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
    }


    interface IMessageSchedulerConverter
    {
        Task<ScheduledMessage> ScheduleSendAsync(IMessageScheduler scheduler, Uri destinationAddress, DateTimeOffset dueAt, object message,
            CancellationToken cancellationToken);

        Task<ScheduledMessage> ScheduleSendAsync(IMessageScheduler scheduler, Uri destinationAddress, DateTimeOffset dueAt, object message,
            IPipe<SendContext> pipe,
            CancellationToken cancellationToken);

        Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(IRecurringMessageScheduler scheduler, Uri destinationAddress, RecurringSchedule schedule,
            object message, IPipe<SendContext> pipe,
            CancellationToken cancellationToken);

        Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(IRecurringMessageScheduler scheduler, Uri destinationAddress,
            RecurringSchedule schedule, object message, CancellationToken cancellationToken);
    }


    sealed class MessageSchedulerConverter<T> :
        IMessageSchedulerConverter
        where T : class
    {
        public async Task<ScheduledMessage> ScheduleSendAsync(IMessageScheduler scheduler, Uri destinationAddress, DateTimeOffset dueAt, object message,
            CancellationToken cancellationToken = default)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message is T msg)
                return await scheduler.ScheduleSendAsync(destinationAddress, dueAt, msg, cancellationToken).ConfigureAwait(false);

            throw new ArgumentException("Unexpected message type: " + TypeCache.GetShortName(message.GetType()));
        }

        public async Task<ScheduledMessage> ScheduleSendAsync(IMessageScheduler scheduler, Uri destinationAddress, DateTimeOffset dueAt, object message,
            IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (pipe == null)
                throw new ArgumentNullException(nameof(pipe));

            if (message is T msg)
                return await scheduler.Advanced().ScheduleSendAsync(destinationAddress, dueAt, msg, pipe, cancellationToken).ConfigureAwait(false);

            throw new ArgumentException("Unexpected message type: " + TypeCache.GetShortName(message.GetType()));
        }

        public async Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(IRecurringMessageScheduler scheduler, Uri destinationAddress,
            RecurringSchedule schedule, object message, CancellationToken cancellationToken)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message is T msg)
                return await scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, msg, cancellationToken).ConfigureAwait(false);

            throw new ArgumentException("Unexpected message type: " + TypeCache.GetShortName(message.GetType()));
        }

        public async Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(IRecurringMessageScheduler scheduler, Uri destinationAddress,
            RecurringSchedule schedule, object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (pipe == null)
                throw new ArgumentNullException(nameof(pipe));

            if (message is T msg)
                return await scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, msg, pipe, cancellationToken).ConfigureAwait(false);

            throw new ArgumentException("Unexpected message type: " + TypeCache.GetShortName(message.GetType()));
        }
    }


    static class Cached
    {
        internal static readonly Lazy<MessageSchedulerConverterCache> Converters =
            new Lazy<MessageSchedulerConverterCache>(() => new MessageSchedulerConverterCache());
    }
}
