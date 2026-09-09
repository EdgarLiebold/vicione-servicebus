using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Coordinates one receive-side EF Core inbox transaction and its ordered outgoing messages.</summary>
/// <typeparam name="TDbContext">The EF Core context containing the inbox and outbox entity sets.</typeparam>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
internal sealed class DbContextOutboxConsumeContext<TDbContext, TMessage> :
    OutboxConsumeContextProxy<TMessage>,
    IDbTransactionContext,
    IDisposable
    where TDbContext : DbContext
    where TMessage : class
{
    readonly TDbContext _dbContext;
    readonly InboxState _inboxState;
    readonly TimeProvider _timeProvider;
    readonly IDbContextTransaction _transaction;
    readonly EntityFrameworkOutboxWriteCoordinator _writeCoordinator;

    /// <summary>Initializes a context for an existing inbox row and database transaction.</summary>
    /// <param name="context">The active message-consumption context.</param>
    /// <param name="options">The receive-side outbox limits.</param>
    /// <param name="provider">The scoped service provider exposed to the pipeline.</param>
    /// <param name="dbContext">The DbContext that owns inbox and outbox changes.</param>
    /// <param name="transaction">The transaction that fences this inbox row.</param>
    /// <param name="inboxState">The tracked inbox row for the message and consumer.</param>
    /// <param name="timeProvider">The source for consumed and delivered timestamps.</param>
    public DbContextOutboxConsumeContext(ConsumeContext<TMessage> context, OutboxConsumeOptions options, IServiceProvider provider, TDbContext dbContext,
        IDbContextTransaction transaction, InboxState inboxState, TimeProvider timeProvider)
        : base(
            context ?? throw new ArgumentNullException(nameof(context)),
            options ?? throw new ArgumentNullException(nameof(options)),
            provider ?? throw new ArgumentNullException(nameof(provider)))
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _inboxState = inboxState ?? throw new ArgumentNullException(nameof(inboxState));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _writeCoordinator = new EntityFrameworkOutboxWriteCoordinator();
    }

    /// <summary>Gets the original message identifier recorded by the inbox.</summary>
    public override Guid? MessageId => _inboxState.MessageId;

    /// <summary>Gets or sets whether the outbox pipeline must perform another delivery pass.</summary>
    public override bool ContinueProcessing { get; set; } = true;

    /// <summary>Gets whether the inbox row records successful consumption.</summary>
    public override bool IsMessageConsumed => _inboxState.Consumed.HasValue;
    /// <summary>Gets whether every outgoing message for this inbox row has been delivered.</summary>
    public override bool IsOutboxDelivered => _inboxState.Delivered.HasValue;
    /// <summary>Gets the number of times the inbox row has been received.</summary>
    public override int ReceiveCount => _inboxState.ReceiveCount;
    /// <summary>Gets the last outgoing sequence number recorded as delivered.</summary>
    public override long? LastSequenceNumber => _inboxState.LastSequenceNumber;

    /// <summary>Gets the identifier of the active EF Core transaction.</summary>
    public Guid TransactionId => _transaction.TransactionId;

    /// <summary>Releases the write coordinator owned by this pipeline context.</summary>
    public void Dispose()
    {
        _writeCoordinator.Dispose();
    }

    /// <summary>Persists the current time as the inbox consumption timestamp.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the consumption timestamp has been persisted.</returns>
    public override async Task SetConsumedAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = ResolveOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();
        _inboxState.Consumed = _timeProvider.GetUtcNow().UtcDateTime;
        _dbContext.Update(_inboxState);

        await _dbContext.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Outbox Consumed: {MessageId} {Consumed}", MessageId, _inboxState.Consumed);
    }

    /// <summary>Persists the current time as the completed outbox-delivery timestamp.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the delivery timestamp has been persisted.</returns>
    public override async Task SetDeliveredAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = ResolveOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();
        _inboxState.Delivered = _timeProvider.GetUtcNow().UtcDateTime;
        _dbContext.Update(_inboxState);

        await _dbContext.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Outbox Delivered: {MessageId} {Delivered}", MessageId, _inboxState.Delivered);
    }

    /// <summary>Loads the next ordered batch of outgoing messages after the recorded sequence.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The ordered outgoing messages, bounded by the configured delivery limit plus one look-ahead row.</returns>
    public override async Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = ResolveOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();
        var lastSequenceNumber = LastSequenceNumber ?? 0;

        List<OutboxMessage> messages = await _dbContext.Set<OutboxMessage>()
            .Where(x => x.InboxMessageId == MessageId && x.InboxConsumerId == ConsumerId && x.SequenceNumber > lastSequenceNumber)
            .OrderBy(x => x.SequenceNumber)
            .Take(Options.MessageDeliveryLimit + 1)
            .AsNoTracking()
            .ToListAsync(operationCancellationToken).ConfigureAwait(false);

        for (var i = 0; i < messages.Count; i++)
            messages[i].Deserialize(SerializerContext);

        return messages.Cast<OutboxMessageContext>().ToList();
    }

    /// <summary>Advances the tracked last-delivered sequence to the supplied message.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after the tracked inbox state has advanced.</returns>
    public override Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        CancellationToken operationCancellationToken = ResolveOperationCancellationToken(cancellationToken);
        if (operationCancellationToken.IsCancellationRequested)
            return Task.FromCanceled(operationCancellationToken);

        _inboxState.LastSequenceNumber = message.SequenceNumber;
        _dbContext.Update(_inboxState);

        return Task.CompletedTask;
    }

    /// <summary>Deletes every outgoing message associated with this inbox row.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the associated outbox rows have been deleted.</returns>
    public override async Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = ResolveOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();
        var count = await _dbContext.Set<OutboxMessage>()
            .Where(x => x.InboxMessageId == MessageId && x.InboxConsumerId == ConsumerId)
            .ExecuteDeleteAsync(operationCancellationToken).ConfigureAwait(false);

        if (count > 0)
            LogContext.Debug?.Log("Outbox removed {Count} messages: {MessageId}", count, MessageId);
    }

    /// <summary>Stages an outgoing message in the same DbContext as the inbox row.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The send context to serialize and persist.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after the serialized message has been staged in the DbContext.</returns>
    public override Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        CancellationToken operationCancellationToken = cancellationToken.CanBeCanceled
            ? cancellationToken
            : context.CancellationToken.CanBeCanceled
                ? context.CancellationToken
                : CancellationToken;
        if (operationCancellationToken.IsCancellationRequested)
            return Task.FromCanceled(operationCancellationToken);

        OutboxMessage message = OutboxMessageFactory.Create(
            context,
            SerializerContext,
            _timeProvider,
            MessageId,
            ConsumerId);
        return _writeCoordinator.ExecuteAsync(() =>
        {
            _dbContext.Add(message);
            return Task.CompletedTask;
        }, operationCancellationToken);
    }

    CancellationToken ResolveOperationCancellationToken(CancellationToken cancellationToken) =>
        cancellationToken.CanBeCanceled ? cancellationToken : CancellationToken;
}
