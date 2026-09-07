using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class EntityFrameworkReliableInboxContext<TBus, TDbContext, TMessage> :
    OutboxConsumeContextProxy<TMessage>
    where TBus : class, IBus
    where TDbContext : DbContext
    where TMessage : class
{
    readonly TDbContext _dbContext;
    readonly ReliableInboxRecord _inbox;
    readonly EntityFrameworkScopedBusContext<TBus, TDbContext> _outbox;
    readonly TimeProvider _timeProvider;

    public EntityFrameworkReliableInboxContext(
        ConsumeContext<TMessage> context,
        OutboxConsumeOptions options,
        IServiceProvider provider,
        TDbContext dbContext,
        ReliableInboxRecord inbox,
        EntityFrameworkScopedBusContext<TBus, TDbContext> outbox,
        TimeProvider timeProvider)
        : base(
            context ?? throw new ArgumentNullException(nameof(context)),
            options ?? throw new ArgumentNullException(nameof(options)),
            provider ?? throw new ArgumentNullException(nameof(provider)))
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public override Guid? MessageId => _inbox.MessageId;

    public override bool ContinueProcessing { get; set; }

    public override bool IsMessageConsumed => false;

    public override bool IsOutboxDelivered => true;

    public override int ReceiveCount => _inbox.Attempts;

    public override long? LastSequenceNumber => null;

    public override async Task SetConsumedAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = ResolveOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();
        _inbox.Status = ReliableInboxStatus.Consumed;
        _inbox.CompletedAt = _timeProvider.GetUtcNow().UtcDateTime;
        _inbox.DueAt = null;
        _inbox.LeaseToken = null;
        _inbox.LeaseExpiresAt = null;
        _inbox.FailedAt = null;
        _inbox.FailureType = null;
        _dbContext.Update(_inbox);

        // The scoped outbox owns SaveChanges so business state, the consumed fence and every outgoing intent are
        // persisted by this DbContext while the enclosing provider transaction is still active.
        await _outbox.CommitAsync(operationCancellationToken).ConfigureAwait(false);
    }

    public override Task SetDeliveredAsync(CancellationToken cancellationToken = default) =>
        CompletedOrCanceledAsync(ResolveOperationCancellationToken(cancellationToken));

    public override Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = ResolveOperationCancellationToken(cancellationToken);
        return operationCancellationToken.IsCancellationRequested
            ? Task.FromCanceled<List<OutboxMessageContext>>(operationCancellationToken)
            : Task.FromResult(new List<OutboxMessageContext>());
    }

    public override Task NotifyOutboxMessageDeliveredAsync(
        OutboxMessageContext message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return CompletedOrCanceledAsync(ResolveOperationCancellationToken(cancellationToken));
    }

    public override Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default) =>
        CompletedOrCanceledAsync(ResolveOperationCancellationToken(cancellationToken));

    public override Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        CancellationToken operationCancellationToken = cancellationToken.CanBeCanceled
            ? cancellationToken
            : context.CancellationToken.CanBeCanceled
                ? context.CancellationToken
                : CancellationToken;
        return _outbox.AddSendAsync(context, operationCancellationToken);
    }

    static Task CompletedOrCanceledAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;

    CancellationToken ResolveOperationCancellationToken(CancellationToken cancellationToken) =>
        cancellationToken.CanBeCanceled ? cancellationToken : CancellationToken;
}
