using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Sends recurring-schedule commands through a dedicated scheduler endpoint.</summary>
public sealed class EndpointRecurringMessageScheduler :
    IRecurringMessageScheduler
{
    readonly IBusTopology? _busTopology;
    readonly Func<Task<ISendEndpoint>> _schedulerEndpoint;

    /// <summary>Creates a scheduler that resolves its command endpoint from a provider.</summary>
    /// <param name="sendEndpointProvider">Resolves the scheduler endpoint.</param>
    /// <param name="schedulerAddress">The scheduler command address.</param>
    /// <param name="busTopology">The topology used to resolve recurring-publish destinations.</param>
    /// <param name="timeProvider">The clock used to timestamp control commands.</param>
    public EndpointRecurringMessageScheduler(ISendEndpointProvider sendEndpointProvider, Uri schedulerAddress, IBusTopology? busTopology = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(sendEndpointProvider);
        ArgumentNullException.ThrowIfNull(schedulerAddress);

        _busTopology = busTopology;
        _schedulerEndpoint = () => sendEndpointProvider.GetSendEndpointAsync(schedulerAddress);
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Creates a scheduler that sends commands through an existing endpoint.</summary>
    /// <param name="sendEndpoint">The scheduler command endpoint.</param>
    /// <param name="busTopology">The topology used to resolve recurring-publish destinations.</param>
    /// <param name="timeProvider">The clock used to timestamp control commands.</param>
    public EndpointRecurringMessageScheduler(ISendEndpoint sendEndpoint, IBusTopology? busTopology = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(sendEndpoint);

        _busTopology = busTopology;
        _schedulerEndpoint = () => Task.FromResult(sendEndpoint);
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets the clock used to timestamp control commands.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Schedules recurring send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
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

    /// <summary>Schedules recurring send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
    public Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message,
        IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
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

    /// <summary>Schedules recurring send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
    public Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message,
        IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
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

    /// <summary>Schedules recurring send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
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

    /// <summary>Schedules recurring send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(Uri destinationAddress, RecurringSchedule schedule, object message,
        Type messageType,
        CancellationToken cancellationToken)
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, cancellationToken);
    }

    /// <summary>Schedules recurring send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
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

    /// <summary>Schedules recurring send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
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

    /// <summary>Schedules recurring send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
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

    /// <summary>Schedules recurring send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
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

    /// <summary>Schedules recurring send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring send outcome.</returns>
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

    /// <summary>Schedules recurring publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
    public Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, T message,
        CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var destinationAddress = GetPublishAddress<T>();

        return ScheduleAsync(destinationAddress, schedule, message, cancellationToken);
    }

    /// <summary>Schedules recurring publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
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

    /// <summary>Schedules recurring publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
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

    /// <summary>Schedules recurring publish.</summary>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
    public Task<ScheduledRecurringMessage> ScheduleRecurringPublishAsync(RecurringSchedule schedule, object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        var destinationAddress = GetPublishAddress(messageType);

        return MessageSchedulerConverterCache.ScheduleRecurringSendAsync(this, destinationAddress, schedule, message, messageType, cancellationToken);
    }

    /// <summary>Schedules recurring publish.</summary>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
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

    /// <summary>Schedules recurring publish.</summary>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
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

    /// <summary>Schedules recurring publish.</summary>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
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

    /// <summary>Schedules recurring publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="schedule">The schedule.</param>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
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

    /// <summary>Schedules recurring publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="schedule">The schedule.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
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

    /// <summary>Schedules recurring publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="schedule">The schedule.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule recurring publish outcome.</returns>
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

    /// <summary>Cancels a recurring schedule.</summary>
    /// <param name="scheduleId">The schedule id.</param>
    /// <param name="scheduleGroup">The schedule group.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CancelScheduledRecurringSendAsync(string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleGroup);

        var command = new CancelScheduledRecurringMessageCommand(scheduleId, scheduleGroup, TimeProvider.GetUtcNow().UtcDateTime);

        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync<CancelScheduledRecurringMessage>(command, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Pauses scheduled recurring send.</summary>
    /// <param name="scheduleId">The schedule id.</param>
    /// <param name="scheduleGroup">The schedule group.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PauseScheduledRecurringSendAsync(string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleGroup);

        var command = new PauseScheduledRecurringMessageCommand(scheduleId, scheduleGroup, TimeProvider.GetUtcNow().UtcDateTime);

        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync<PauseScheduledRecurringMessage>(command, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resumes scheduled recurring send.</summary>
    /// <param name="scheduleId">The schedule id.</param>
    /// <param name="scheduleGroup">The schedule group.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ResumeScheduledRecurringSendAsync(string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleGroup);

        var command = new ResumeScheduledRecurringMessageCommand(scheduleId, scheduleGroup, TimeProvider.GetUtcNow().UtcDateTime);

        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync<ResumeScheduledRecurringMessage>(command, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    async Task<ScheduledRecurringMessage<T>> ScheduleAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message,
        CancellationToken cancellationToken)
        where T : class
    {
        var command = CreateCommand(destinationAddress, schedule, message);

        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync(command, cancellationToken).ConfigureAwait(false);

        return new ScheduledRecurringMessageHandle<T>(schedule, command.Destination, message);
    }

    async Task<ScheduledRecurringMessage<T>> ScheduleAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        var command = CreateCommand(destinationAddress, schedule, message);

        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync(command, pipe, cancellationToken).ConfigureAwait(false);

        return new ScheduledRecurringMessageHandle<T>(schedule, command.Destination, message);
    }

    async Task<ScheduledRecurringMessage<T>> ScheduleAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        var command = CreateCommand(destinationAddress, schedule, message);

        var scheduleMessagePipe = new ScheduleRecurringMessageContextPipe<T>(message, pipe);

        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync(command, scheduleMessagePipe, cancellationToken).ConfigureAwait(false);

        return new ScheduledRecurringMessageHandle<T>(schedule, command.Destination, message);
    }

    static ScheduleRecurringMessage CreateCommand<T>(Uri destinationAddress, RecurringSchedule schedule, T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(message);

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
        IPipe<SendContext<ScheduleRecurringMessage>>
        where T : class
    {
        readonly T _payload;
        readonly IPipe<SendContext<T>>? _pipe;

        public ScheduleRecurringMessageContextPipe(T payload, IPipe<SendContext<T>>? pipe)
        {
            _payload = payload;
            _pipe = pipe;
        }

        public async Task SendAsync(SendContext<ScheduleRecurringMessage> context)
        {
            if (_pipe.IsNotEmpty())
            {
                SendContext<T> proxy = context.CreateProxy(_payload);

                await _pipe!.SendAsync(proxy).ConfigureAwait(false);
            }
        }

        public void Probe(ProbeContext context)
        {
            _pipe?.Probe(context);
        }
    }
}
