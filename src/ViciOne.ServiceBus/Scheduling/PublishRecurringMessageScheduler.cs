using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;

#nullable enable
namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a publish recurring message scheduler implementation.
/// </summary>
public class PublishRecurringMessageScheduler :
    IRecurringMessageScheduler
{
    readonly IBusTopology? _busTopology;
    readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishEndpoint">The publish endpoint value.</param>
    /// <param name="busTopology">The bus topology value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public PublishRecurringMessageScheduler(IPublishEndpoint publishEndpoint, IBusTopology? busTopology = null, TimeProvider? timeProvider = null)
    {
        _publishEndpoint = publishEndpoint;
        _busTopology = busTopology;
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message,
        CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return ScheduleAsync(destinationAddress, schedule, message, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return ScheduleAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return ScheduleAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(Uri destinationAddress, RecurringSchedule schedule, object message,
        CancellationToken cancellationToken)
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(Uri destinationAddress, RecurringSchedule schedule, object message,
        Type messageType, CancellationToken cancellationToken)
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(Uri destinationAddress, RecurringSchedule schedule, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var messageType = message.GetType();

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(Uri destinationAddress, RecurringSchedule schedule, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule,
        object values, CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (values == null)
            throw new ArgumentNullException(nameof(values));

        global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await MessageInitializerCache<T>.InitializeMessageAsync(values, cancellationToken).ConfigureAwait(false);

        return await ScheduleAsync(destinationAddress, schedule, send.Message, send.Pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule,
        object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (values == null)
            throw new ArgumentNullException(nameof(values));

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        return await ScheduleAsync(destinationAddress, schedule, send.Message, send.Pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Schedules recurring send.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule,
        object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (values == null)
            throw new ArgumentNullException(nameof(values));

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        return await ScheduleAsync(destinationAddress, schedule, send.Message, send.Pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, T message,
        CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var destinationAddress = GetPublishAddress<T>();

        return ScheduleAsync(destinationAddress, schedule, message, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var destinationAddress = GetPublishAddress<T>();

        return ScheduleAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var destinationAddress = GetPublishAddress<T>();

        return ScheduleAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringPublishAsync(RecurringSchedule schedule, object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        var destinationAddress = GetPublishAddress(messageType);

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringPublishAsync(RecurringSchedule schedule, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        var destinationAddress = GetPublishAddress(messageType);

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringPublishAsync(RecurringSchedule schedule, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var messageType = message.GetType();

        var destinationAddress = GetPublishAddress(messageType);

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringPublishAsync(RecurringSchedule schedule, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var destinationAddress = GetPublishAddress(messageType);

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, object values,
        CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var destinationAddress = GetPublishAddress<T>();

        global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await MessageInitializerCache<T>.InitializeMessageAsync(values, cancellationToken).ConfigureAwait(false);

        return await ScheduleAsync(destinationAddress, schedule, send.Message, send.Pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var destinationAddress = GetPublishAddress<T>();

        global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        return await ScheduleAsync(destinationAddress, schedule, send.Message, send.Pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Schedules recurring publish.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var destinationAddress = GetPublishAddress<T>();

        global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        return await ScheduleAsync(destinationAddress, schedule, send.Message, send.Pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether the current value can cel scheduled recurring send.
    /// </summary>
    /// <param name="scheduleId">The schedule id value.</param>
    /// <param name="scheduleGroup">The schedule group value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CancelScheduledRecurringSendAsync(string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        var command = new CancelScheduledRecurringMessageCommand(scheduleId, scheduleGroup, TimeProvider.GetUtcNow().UtcDateTime);

        return _publishEndpoint.PublishAsync<CancelScheduledRecurringMessage>(command, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the pause scheduled recurring send operation.
    /// </summary>
    /// <param name="scheduleId">The schedule id value.</param>
    /// <param name="scheduleGroup">The schedule group value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PauseScheduledRecurringSendAsync(string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        var command = new PauseScheduledRecurringMessageCommand(scheduleId, scheduleGroup, TimeProvider.GetUtcNow().UtcDateTime);

        return _publishEndpoint.PublishAsync<PauseScheduledRecurringMessage>(command, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the resume scheduled recurring send operation.
    /// </summary>
    /// <param name="scheduleId">The schedule id value.</param>
    /// <param name="scheduleGroup">The schedule group value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ResumeScheduledRecurringSendAsync(string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        var command = new ResumeScheduledRecurringMessageCommand(scheduleId, scheduleGroup, TimeProvider.GetUtcNow().UtcDateTime);

        return _publishEndpoint.PublishAsync<ResumeScheduledRecurringMessage>(command, cancellationToken: cancellationToken);
    }

    async Task<ScheduledRecurringMessage<T>> ScheduleAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message, CancellationToken cancellationToken)
        where T : class
    {
        var command = CreateCommand(destinationAddress, schedule, message);

        await _publishEndpoint.PublishAsync(command, cancellationToken).ConfigureAwait(false);

        return new ScheduledRecurringMessageHandle<T>(schedule, command.Destination, message);
    }

    async Task<ScheduledRecurringMessage<T>> ScheduleAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        var command = CreateCommand(destinationAddress, schedule, message);

        await _publishEndpoint.PublishAsync(command, pipe, cancellationToken).ConfigureAwait(false);

        return new ScheduledRecurringMessageHandle<T>(schedule, command.Destination, message);
    }

    async Task<ScheduledRecurringMessage<T>> ScheduleAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        var command = CreateCommand(destinationAddress, schedule, message);

        var scheduleMessagePipe = new ScheduleRecurringMessageContextPipe<T>(message, pipe);

        await _publishEndpoint.PublishAsync(command, scheduleMessagePipe, cancellationToken).ConfigureAwait(false);

        return new ScheduledRecurringMessageHandle<T>(schedule, command.Destination, message);
    }

    static ScheduleRecurringMessage CreateCommand<T>(Uri destinationAddress, RecurringSchedule schedule, T message)
        where T : class
    {
        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        return new ScheduleRecurringMessageCommand<T>(schedule, destinationAddress, message);
    }

    Uri GetPublishAddress<T>()
        where T : class
    {
        if (_busTopology == null)
            throw new InvalidOperationException("The bus topology is required to use ScheduleRecurringPublish.");

        if (_busTopology.TryGetPublishAddress<T>(out var address))
            return address;

        throw new ArgumentException($"The publish address for the specified type was not returned: {TypeCache<T>.ShortName}");
    }

    Uri GetPublishAddress(Type messageType)
    {
        if (_busTopology == null)
            throw new InvalidOperationException("The bus topology is required to use ScheduleRecurringPublish.");

        if (_busTopology.TryGetPublishAddress(messageType, out var address))
            return address;

        throw new ArgumentException($"The publish address for the specified type was not returned: {TypeCache.GetShortName(messageType)}");
    }


    class ScheduleRecurringMessageContextPipe<T> :
        IPipe<PublishContext<ScheduleRecurringMessage>>
        where T : class
    {
        readonly T _payload;
        readonly IPipe<PublishContext<T>>? _pipe;

        public ScheduleRecurringMessageContextPipe(T payload, IPipe<PublishContext<T>>? pipe)
        {
            _payload = payload;
            _pipe = pipe;
        }

        public async Task SendAsync(PublishContext<ScheduleRecurringMessage> context)
        {
            if (_pipe.IsNotEmpty())
            {
                var proxy = new PublishContextProxy<T>(context, _payload);

                await _pipe!.SendAsync(proxy).ConfigureAwait(false);
            }
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            _pipe?.Probe(context);
        }
    }
}
