using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Scheduling;


namespace ViciOne.ServiceBus.Configuration;

internal sealed class ReliableMessageScheduler<TBus> : IMessageScheduler
    where TBus : class, IBus
{
    readonly TBus _bus;
    readonly IScheduleStore<TBus> _store;
    readonly IDurableSender<TBus> _sender;
    readonly TimeProvider _timeProvider;

    public ReliableMessageScheduler(
        TBus bus,
        IDurableSender<TBus> sender,
        IScheduleStore<TBus> store,
        TimeProvider timeProvider)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public TimeProvider TimeProvider => _timeProvider;

    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(
        Uri destination,
        DateTimeOffset dueAt,
        T message,
        CancellationToken cancellationToken = default)
        where T : class
        => await ScheduleSendCoreAsync(destination, dueAt, message, null, cancellationToken).ConfigureAwait(false);

    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(
        Uri destination,
        TimeSpan delay,
        T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ValidateRelativeSend(destination, message);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ScheduledMessage<T>>(cancellationToken);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(this, delay);
        return ScheduleSendAsync(destination, dueAt, message, cancellationToken);
    }

    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(
        Uri destination,
        DateTimeOffset dueAt,
        T message,
        ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(options);
        return await ScheduleSendCoreAsync(destination, dueAt, message, options, cancellationToken).ConfigureAwait(false);
    }

    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(
        Uri destination,
        TimeSpan delay,
        T message,
        ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ValidateRelativeSend(destination, message);
        ArgumentNullException.ThrowIfNull(options);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ScheduledMessage<T>>(cancellationToken);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(this, delay);
        return ScheduleSendAsync(destination, dueAt, message, options, cancellationToken);
    }

    async Task<ScheduledMessage<T>> ScheduleSendCoreAsync<T>(
        Uri destination,
        DateTimeOffset dueAt,
        T message,
        ScheduleOptions? options,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        if (!destination.IsAbsoluteUri)
            throw new ArgumentException("A scheduled destination must be an absolute URI.", nameof(destination));

        cancellationToken.ThrowIfCancellationRequested();
        var id = new DurableSendId(Guid.NewGuid());
        await _sender.SendAsync(
                destination,
                message,
                new DurableSendOptions
                {
                    IdempotencyKey = id,
                    CorrelationId = options?.CorrelationId,
                    DueAt = dueAt,
                    ScheduledMessageOptions = options,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return new ScheduledMessageHandle<T>(id.Value, dueAt, destination, message);
    }

    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(
        DateTimeOffset dueAt,
        T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        Uri destination = EndpointConvention.GetDestinationAddress<T>(_bus);
        return ScheduleSendAsync(destination, dueAt, message, cancellationToken);
    }

    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(
        TimeSpan delay,
        T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ScheduledMessage<T>>(cancellationToken);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(this, delay);
        return SchedulePublishAsync(dueAt, message, cancellationToken);
    }

    public async Task CancelScheduledSendAsync(
        ScheduledMessage scheduled,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scheduled);
        ReliableMessagingOperationResult result = await _store
            .CancelAsync(new DurableSendId(scheduled.TokenId), cancellationToken)
            .ConfigureAwait(false);
        if (result.Disposition == ReliableMessagingOperationDisposition.NotFound)
            throw new InvalidOperationException($"Scheduled message '{scheduled.TokenId}' was not found.");
        if (result.Disposition == ReliableMessagingOperationDisposition.InvalidState)
            throw new InvalidOperationException($"Scheduled message '{scheduled.TokenId}' can no longer be cancelled.");
    }

    static void ValidateRelativeSend<T>(Uri destination, T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        if (!destination.IsAbsoluteUri)
            throw new ArgumentException("The scheduled destination must be an absolute URI.", nameof(destination));
    }
}
