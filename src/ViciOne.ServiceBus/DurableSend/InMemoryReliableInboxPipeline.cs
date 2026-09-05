using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed class InMemoryReliableInboxScope<TBus>
    where TBus : class, IBus;

internal sealed class InMemoryReliableInboxEndpointConfiguration<TBus>(
    IRegistrationContext context,
    int maximumDeliveryAttempts) :
    IConfigureReceiveEndpoint
    where TBus : class, IBus
{
    readonly IRegistrationContext _context = context ?? throw new ArgumentNullException(nameof(context));
    readonly int _maximumDeliveryAttempts = maximumDeliveryAttempts > 0
        ? maximumDeliveryAttempts
        : throw new ArgumentOutOfRangeException(nameof(maximumDeliveryAttempts));

    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (_maximumDeliveryAttempts > 1)
        {
            _ = new MessageRetryConfigurationObserver(
                configurator,
                CancellationToken.None,
                retry =>
                {
                    // Consumer fault notifications happen before the durable inbox factory converts the failure into
                    // its retained-retry signal. Handling the original exception here defers Fault<T> publication;
                    // the inbox row still owns the retry decision, due time and terminal quarantine state.
                    retry.Handle<Exception>();
                    retry.Immediate(_maximumDeliveryAttempts - 1);
                });
        }

        var observer = new OutboxConsumePipeSpecificationObserver<InMemoryReliableInboxScope<TBus>>(
            configurator,
            _context);
        configurator.ConnectConsumerConfigurationObserver(observer);
        configurator.ConnectSagaConfigurationObserver(observer);
    }
}

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

    public async Task SendAsync<T>(
        ConsumeContext<T> context,
        OutboxConsumeOptions options,
        IPipe<OutboxConsumeContext<T>> next,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(next);
        Guid messageId = context.GetOriginalMessageId()
            ?? throw new MessageException(typeof(T), "MessageId required to use reliable messaging");
        var key = new ReliableInboxKey(messageId, options.ConsumerId).Validate();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DateTimeOffset now = _timeProvider.GetUtcNow();
            ReliableInboxAcquireResult acquisition = await _store.AcquireAsync(
                key,
                now,
                _policy.LeaseDuration,
                cancellationToken).ConfigureAwait(false);

            if (acquisition.Disposition is ReliableInboxAcquireDisposition.AlreadyConsumed
                or ReliableInboxAcquireDisposition.Unavailable)
            {
                return;
            }

            if (acquisition.Disposition is ReliableInboxAcquireDisposition.Busy
                or ReliableInboxAcquireDisposition.NotDue)
            {
                await Task.Delay(_policy.PollInterval, _timeProvider, cancellationToken).ConfigureAwait(false);
                continue;
            }

            ReliableInboxLease lease = acquisition.Lease
                ?? throw new InvalidOperationException($"Reliable inbox '{key}' was acquired without a lease.");
            var reliableContext = new InMemoryReliableInboxContext<TBus, T>(
                context,
                options,
                _provider,
                _store,
                _contracts,
                _policy.Limits,
                key,
                lease,
                acquisition.Attempt,
                _timeProvider);
            try
            {
                await next.SendAsync(reliableContext).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                DateTimeOffset failedAt = _timeProvider.GetUtcNow();
                string failureType = Describe(exception);
                if (acquisition.Attempt >= _policy.MaximumDeliveryAttempts)
                {
                    bool quarantined = await _store.QuarantineAsync(
                            key,
                            lease,
                            failureType,
                            failedAt,
                            CancellationToken.None)
                        .ConfigureAwait(false);
                    if (quarantined)
                        return;

                    throw;
                }

                DateTimeOffset dueAt = failedAt + CalculateRetryDelay(acquisition.Attempt);
                bool retained = await _store.ScheduleRetryAsync(
                    key,
                    lease,
                    dueAt,
                    failureType,
                    failedAt,
                    CancellationToken.None).ConfigureAwait(false);
                if (!retained)
                    throw;

                throw new ReliableInboxRetryRequiredException(exception);
            }
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

    static string Describe(Exception exception)
    {
        string description;
        try
        {
            description = exception.ToString();
        }
        catch
        {
            description = exception.GetType().FullName ?? exception.GetType().Name;
        }
        return description.Length <= 512 ? description : description[..512];
    }
}

internal sealed class InMemoryReliableInboxContext<TBus, TMessage> :
    OutboxConsumeContextProxy<TMessage>
    where TBus : class, IBus
    where TMessage : class
{
    readonly List<SerializedDurableSend> _messages = [];
    readonly Lock _messagesLock = new();
    readonly IMessageContractCatalog _contracts;
    readonly DurableSendStoreLimits _limits;
    readonly ReliableInboxKey _key;
    readonly ReliableInboxLease _lease;
    readonly int _receiveCount;
    readonly InMemoryReliableStore<TBus> _store;
    readonly TimeProvider _timeProvider;

    public InMemoryReliableInboxContext(
        ConsumeContext<TMessage> context,
        OutboxConsumeOptions options,
        IServiceProvider provider,
        InMemoryReliableStore<TBus> store,
        IMessageContractCatalog contracts,
        DurableSendStoreLimits limits,
        ReliableInboxKey key,
        ReliableInboxLease lease,
        int receiveCount,
        TimeProvider timeProvider)
        : base(context, options, provider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _limits = limits;
        _key = key;
        _lease = lease;
        _receiveCount = receiveCount;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public override Guid? MessageId => _key.MessageId;

    public override bool ContinueProcessing { get; set; }

    public override bool IsMessageConsumed => false;

    public override bool IsOutboxDelivered => true;

    public override int ReceiveCount => _receiveCount;

    public override long? LastSequenceNumber => null;

    public override Task SetConsumedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SerializedDurableSend[] messages;
        lock (_messagesLock)
            messages = _messages.ToArray();
        _store.CompleteConsumer(_key, _lease, messages, _limits, _timeProvider.GetUtcNow());
        return Task.CompletedTask;
    }

    public override Task SetDeliveredAsync(CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

    public override Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<List<OutboxMessageContext>>(cancellationToken)
            : Task.FromResult(new List<OutboxMessageContext>());

    public override Task NotifyOutboxMessageDeliveredAsync(
        OutboxMessageContext message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }

    public override Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

    public override Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.MessageId.HasValue)
            throw new MessageException(typeof(T), "The SendContext MessageId must be present");
        Uri destination = context.DestinationAddress
            ?? throw new MessageException(typeof(T), "The SendContext DestinationAddress must be present");
        DateTimeOffset now = _timeProvider.GetUtcNow();
        var message = new SerializedDurableSend
        {
            Id = new DurableSendId(context.MessageId.Value),
            ContractIdentity = _contracts.GetIdentity(typeof(T)),
            DestinationAddress = destination,
            ContentType = context.ContentType?.ToString() ?? context.Serialization.DefaultContentType.ToString(),
            Body = context.Serializer.GetMessageBody(context).GetBytes(),
            Metadata = ReliableEnvelopeMetadataCodec.Capture(context, now),
            MessageId = context.MessageId,
            CorrelationId = context.CorrelationId,
            DueAt = context.Delay.HasValue ? now + context.Delay.Value : null,
        }.Validate();

        lock (_messagesLock)
            _messages.Add(message);
        return Task.CompletedTask;
    }
}

internal static class InMemoryReliableInboxRegistration
{
    public static void Add<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        services.AddScoped<IOutboxContextFactory<InMemoryReliableInboxScope<TBus>>,
            InMemoryReliableInboxContextFactory<TBus>>();
        services.AddSingleton(provider => Bind<TBus>.Create<IConfigureReceiveEndpoint>(
            new InMemoryReliableInboxEndpointConfiguration<TBus>(
                provider.GetRequiredService<Bind<TBus, IBusRegistrationContext>>().Value,
                provider.GetRequiredService<ReliableMessagingPolicy<TBus>>().MaximumDeliveryAttempts)));
    }
}
