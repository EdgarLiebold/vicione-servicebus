using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed class InMemoryReliableInboxContextFactory<TBus> :
    IOutboxContextFactory<InMemoryReliableInboxScope<TBus>>
    where TBus : class, IBus
{
    readonly IMessageContractCatalog _contracts;
    readonly ReliableMessagingPolicy<TBus> _policy;
    readonly IServiceProvider _provider;
    readonly InMemoryReliableStore<TBus> _store;
    readonly TimeProvider _timeProvider;

    public InMemoryReliableInboxContextFactory(
        InMemoryReliableStore<TBus> store,
        IMessageContractCatalog contracts,
        ReliableMessagingPolicy<TBus> policy,
        IServiceProvider provider,
        TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task SendAsync<TMessage>(
        ConsumeContext<TMessage> context,
        OutboxConsumeOptions options,
        IPipe<OutboxConsumeContext<TMessage>> next,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(next);
        CancellationToken deliveryCancellationToken = context.CancellationToken;
        using CancellationTokenSource? linkedCancellation = cancellationToken.CanBeCanceled
            && deliveryCancellationToken.CanBeCanceled
            && cancellationToken != deliveryCancellationToken
                ? CancellationTokenSource.CreateLinkedTokenSource(deliveryCancellationToken, cancellationToken)
                : null;
        CancellationToken operationCancellationToken = linkedCancellation?.Token
            ?? (cancellationToken.CanBeCanceled ? cancellationToken : deliveryCancellationToken);
        deliveryCancellationToken.ThrowIfCancellationRequested();
        cancellationToken.ThrowIfCancellationRequested();
        Guid messageId = context.GetOriginalMessageId()
            ?? throw new MessageException(typeof(TMessage), "MessageId required to use reliable messaging");
        var key = new ReliableInboxKey(messageId, options.ConsumerId).Validate();

        try
        {
            await SendWhileAvailableAsync(
                context, options, next, key, deliveryCancellationToken,
                cancellationToken, operationCancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (linkedCancellation is not null
            && operationCancellationToken.IsCancellationRequested
            && exception.CancellationToken != deliveryCancellationToken
            && exception.CancellationToken != cancellationToken)
        {
            deliveryCancellationToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
    }

    async Task SendWhileAvailableAsync<TMessage>(
        ConsumeContext<TMessage> context,
        OutboxConsumeOptions options,
        IPipe<OutboxConsumeContext<TMessage>> next,
        ReliableInboxKey key,
        CancellationToken deliveryCancellationToken,
        CancellationToken cancellationToken,
        CancellationToken operationCancellationToken)
        where TMessage : class
    {
        while (true)
        {
            deliveryCancellationToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            DateTimeOffset now = _timeProvider.GetUtcNow();
            ReliableInboxAcquireResult acquisition = await _store.AcquireAsync(
                key,
                now,
                _policy.LeaseDuration,
                operationCancellationToken).ConfigureAwait(false);
            acquisition = ReliableMessagingProviderGuard.ValidateAcquisition(key, acquisition);

            if (acquisition.Disposition is ReliableInboxAcquireDisposition.AlreadyConsumed
                or ReliableInboxAcquireDisposition.Unavailable)
            {
                return;
            }

            if (acquisition.Disposition is ReliableInboxAcquireDisposition.Busy
                or ReliableInboxAcquireDisposition.NotDue)
            {
                await Task.Delay(_policy.PollInterval, _timeProvider, operationCancellationToken).ConfigureAwait(false);
                continue;
            }

            ReliableInboxLease lease = acquisition.Lease
                ?? throw new InvalidOperationException($"Reliable inbox '{key}' was acquired without a lease.");
            await SendWithLeaseAsync(
                context, options, next, key, lease, acquisition.Attempt,
                operationCancellationToken).ConfigureAwait(false);
            return;
        }
    }

    async Task SendWithLeaseAsync<TMessage>(
        ConsumeContext<TMessage> context,
        OutboxConsumeOptions options,
        IPipe<OutboxConsumeContext<TMessage>> next,
        ReliableInboxKey key,
        ReliableInboxLease lease,
        int attempt,
        CancellationToken operationCancellationToken)
        where TMessage : class
    {
        var reliableContext = new InMemoryReliableInboxContext<TBus, TMessage>(
            context, options, _provider, _store, _contracts, _policy.Limits,
            key, lease, attempt, _timeProvider);
        try
        {
            await next.SendAsync(reliableContext).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (operationCancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            DateTimeOffset failedAt = _timeProvider.GetUtcNow();
            string failureType = GetFailureTypeName(exception);
            if (attempt >= _policy.MaximumDeliveryAttempts)
            {
                bool quarantined = await _store.QuarantineAsync(
                        key, lease, failureType, failedAt, CancellationToken.None)
                    .ConfigureAwait(false);
                if (quarantined)
                    return;

                throw;
            }

            DateTimeOffset dueAt = failedAt + CalculateRetryDelay(attempt);
            bool retained = await _store.ScheduleRetryAsync(
                key, lease, dueAt, failureType, failedAt, CancellationToken.None).ConfigureAwait(false);
            if (!retained)
                throw;

            throw new ReliableInboxRetryRequiredException(exception);
        }
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateFilterScope("reliableInbox");
        scope.Add("provider", "inMemory");
    }

    TimeSpan CalculateRetryDelay(int attempt)
    {
        long ticks = _policy.InitialRetryDelay.Ticks;
        long maximumTicks = _policy.MaximumRetryDelay.Ticks;
        for (var index = 1; index < attempt && ticks < maximumTicks; index++)
            ticks = ticks > maximumTicks / 2 ? maximumTicks : Math.Min(maximumTicks, ticks * 2);
        return TimeSpan.FromTicks(ticks);
    }

    static string GetFailureTypeName(Exception exception)
    {
        Type exceptionType = exception.GetType();
        string typeName = exceptionType.FullName ?? exceptionType.Name;
        return typeName.Length <= 512 ? typeName : typeName[..512];
    }
}
