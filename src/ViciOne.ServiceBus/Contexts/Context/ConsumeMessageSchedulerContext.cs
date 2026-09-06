using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for consume message scheduler operations.</summary>
public class ConsumeMessageSchedulerContext :
    MessageSchedulerContext
{
    readonly Uri _inputAddress;
    readonly Lazy<IMessageScheduler> _scheduler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="schedulerFactory">The scheduler factory.</param>
    public ConsumeMessageSchedulerContext(ConsumeContext consumeContext, MessageSchedulerFactory schedulerFactory)
    {
        _inputAddress = consumeContext.ReceiveContext.InputAddress;

        SchedulerFactory = schedulerFactory;

        _scheduler = new Lazy<IMessageScheduler>(() => schedulerFactory(consumeContext));
    }

    /// <summary>Gets the scheduler factory.</summary>
    public MessageSchedulerFactory SchedulerFactory { get; }

    /// <summary>Gets the time provider.</summary>
    public TimeProvider TimeProvider => _scheduler.Value.Advanced().TimeProvider;

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync<T>(destinationAddress, dueAt, values, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync<T>(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync<T>(_inputAddress, dueAt, values, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync<T>(_inputAddress, dueAt, values, pipe, cancellationToken);
    }

    Task Advanced.IAdvancedMessageScheduler.CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().CancelScheduledSendAsync(destinationAddress, tokenId, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync<T>(dueAt, values, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync<T>(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Determines whether the current value can cel scheduled publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledPublishAsync<T>(Guid tokenId, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().CancelScheduledPublishAsync<T>(tokenId, cancellationToken);
    }

    /// <summary>Determines whether the current value can cel scheduled publish.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledPublishAsync(Type messageType, Guid tokenId, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().CancelScheduledPublishAsync(messageType, tokenId, cancellationToken);
    }
}
