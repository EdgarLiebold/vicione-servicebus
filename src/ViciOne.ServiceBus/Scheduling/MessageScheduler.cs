using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;

#nullable enable annotations
namespace ViciOne.ServiceBus.Scheduling;

public class MessageScheduler :
    Advanced.IAdvancedMessageScheduler
{
    readonly IBusTopology _busTopology;
    readonly IScheduleMessageProvider _provider;

    public MessageScheduler(IScheduleMessageProvider provider, IBusTopology busTopology, TimeProvider? timeProvider = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _busTopology = busTopology ?? throw new ArgumentNullException(nameof(busTopology));
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    public TimeProvider TimeProvider { get; }

    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return _provider.ScheduleSendAsync(destinationAddress, dueAt, message, Pipe.Empty<SendContext>(), cancellationToken);
    }

    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return _provider.ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return _provider.ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken)
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        var messageType = message.GetType();

        return MessageSchedulerConverterCache.ScheduleSendAsync(this, destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return MessageSchedulerConverterCache.ScheduleSendAsync(this, destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var messageType = message.GetType();

        return MessageSchedulerConverterCache.ScheduleSendAsync(this, destinationAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return MessageSchedulerConverterCache.ScheduleSendAsync(this, destinationAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        SendTuple<T> send = await MessageInitializerCache<T>.InitializeMessageAsync(values, cancellationToken).ConfigureAwait(false);

        return await _provider.ScheduleSendAsync(destinationAddress, dueAt, send.Message, send.Pipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        SendTuple<T> send = await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        return await _provider.ScheduleSendAsync(destinationAddress, dueAt, send.Message, send.Pipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (destinationAddress == null)
            throw new ArgumentNullException(nameof(destinationAddress));
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        SendTuple<T> send = await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        return await _provider.ScheduleSendAsync(destinationAddress, dueAt, send.Message, send.Pipe, cancellationToken).ConfigureAwait(false);
    }

    public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        return _provider.CancelScheduledSendAsync(destinationAddress, tokenId, cancellationToken);
    }

    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        var destinationAddress = GetPublishAddress<T>();

        return ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken);
    }

    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var destinationAddress = GetPublishAddress<T>();

        return ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var destinationAddress = GetPublishAddress<T>();

        return ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        var destinationAddress = GetPublishAddress(messageType);

        return ScheduleSendAsync(destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, CancellationToken cancellationToken = default)
    {
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        var destinationAddress = GetPublishAddress(messageType);

        return ScheduleSendAsync(destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        var destinationAddress = GetPublishAddress(messageType);

        return ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        var destinationAddress = GetPublishAddress(messageType);

        return ScheduleSendAsync(destinationAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken = default)
        where T : class
    {
        var destinationAddress = GetPublishAddress<T>();

        return ScheduleSendAsync<T>(destinationAddress, dueAt, values, cancellationToken);
    }

    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var destinationAddress = GetPublishAddress<T>();

        return ScheduleSendAsync(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var destinationAddress = GetPublishAddress<T>();

        return ScheduleSendAsync<T>(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    public Task CancelScheduledPublishAsync<T>(Guid tokenId, CancellationToken cancellationToken)
        where T : class
    {
        var destinationAddress = GetPublishAddress<T>();

        return CancelScheduledSendAsync(destinationAddress, tokenId, cancellationToken);
    }

    public Task CancelScheduledPublishAsync(Type messageType, Guid tokenId, CancellationToken cancellationToken)
    {
        var destinationAddress = GetPublishAddress(messageType);

        return CancelScheduledSendAsync(destinationAddress, tokenId, cancellationToken);
    }

    Uri GetPublishAddress<T>()
        where T : class
    {
        if (_busTopology.TryGetPublishAddress<T>(out var address))
            return address;

        throw new ArgumentException($"The publish address for the specified type was not returned: {TypeCache<T>.ShortName}");
    }

    Uri GetPublishAddress(Type messageType)
    {
        if (_busTopology.TryGetPublishAddress(messageType, out var address))
            return address;

        throw new ArgumentException($"The publish address for the specified type was not returned: {TypeCache.GetShortName(messageType)}");
    }
}
