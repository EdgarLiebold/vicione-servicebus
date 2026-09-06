using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a db context outbox consume context implementation.
/// </summary>
/// <typeparam name="TDbContext">The t db context type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class DbContextOutboxConsumeContext<TDbContext, TMessage> :
    OutboxConsumeContextProxy<TMessage>,
    DbTransactionContext
    where TDbContext : DbContext
    where TMessage : class
{
    readonly TDbContext _dbContext;
    readonly InboxState _inboxState;
    readonly TimeProvider _timeProvider;
    readonly IDbContextTransaction _transaction;
    readonly EntityFrameworkOutboxWriteCoordinator _writeCoordinator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="options">The options value.</param>
    /// <param name="provider">The service provider.</param>
    /// <param name="dbContext">The db context value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="inboxState">The inbox state value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public DbContextOutboxConsumeContext(ConsumeContext<TMessage> context, OutboxConsumeOptions options, IServiceProvider provider, TDbContext dbContext,
        IDbContextTransaction transaction, InboxState inboxState, TimeProvider timeProvider)
        : base(context, options, provider)
    {
        _dbContext = dbContext;
        _transaction = transaction;
        _inboxState = inboxState;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _writeCoordinator = new EntityFrameworkOutboxWriteCoordinator();
    }

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public override Guid? MessageId => _inboxState.MessageId;

    /// <summary>
    /// Gets or sets the continue processing value.
    /// </summary>
    public override bool ContinueProcessing { get; set; } = true;

    /// <summary>
    /// Gets the is message consumed value.
    /// </summary>
    public override bool IsMessageConsumed => _inboxState.Consumed.HasValue;
    /// <summary>
    /// Gets the is outbox delivered value.
    /// </summary>
    public override bool IsOutboxDelivered => _inboxState.Delivered.HasValue;
    /// <summary>
    /// Gets the receive count value.
    /// </summary>
    public override int ReceiveCount => _inboxState.ReceiveCount;
    /// <summary>
    /// Gets the last sequence number value.
    /// </summary>
    public override long? LastSequenceNumber => _inboxState.LastSequenceNumber;

    /// <summary>
    /// Gets the transaction id value.
    /// </summary>
    public Guid TransactionId => _transaction.TransactionId;

    /// <summary>
    /// Sets consumed.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task SetConsumedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); _inboxState.Consumed = _timeProvider.GetUtcNow().UtcDateTime;
        _dbContext.Update(_inboxState);

        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Outbox Consumed: {MessageId} {Consumed}", MessageId, _inboxState.Consumed);
    }

    /// <summary>
    /// Sets delivered.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task SetDeliveredAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); _inboxState.Delivered = _timeProvider.GetUtcNow().UtcDateTime;
        _dbContext.Update(_inboxState);

        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Outbox Delivered: {MessageId} {Delivered}", MessageId, _inboxState.Delivered);
    }

    /// <summary>
    /// Performs the load outbox messages operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var lastSequenceNumber = LastSequenceNumber ?? 0;

        List<OutboxMessage> messages = await _dbContext.Set<OutboxMessage>()
            .Where(x => x.InboxMessageId == MessageId && x.InboxConsumerId == ConsumerId && x.SequenceNumber > lastSequenceNumber)
            .OrderBy(x => x.SequenceNumber)
            .Take(Options.MessageDeliveryLimit + 1)
            .AsNoTracking()
            .ToListAsync(CancellationToken).ConfigureAwait(false);

        for (var i = 0; i < messages.Count; i++)
            messages[i].Deserialize(SerializerContext);

        return messages.Cast<OutboxMessageContext>().ToList();
    }

    /// <summary>
    /// Performs the notify outbox message delivered operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _inboxState.LastSequenceNumber = message.SequenceNumber;
        _dbContext.Update(_inboxState);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the remove outbox messages operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var count = await _dbContext.Set<OutboxMessage>()
                    .Where(x => x.InboxMessageId == MessageId && x.InboxConsumerId == ConsumerId)
                    .ExecuteDeleteAsync(CancellationToken).ConfigureAwait(false);

        if (count > 0)
            LogContext.Debug?.Log("Outbox removed {Count} messages: {MessageId}", count, MessageId);
    }

    /// <summary>
    /// Adds send to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); OutboxMessage message = OutboxMessageFactory.Create(
                    context,
                    SerializerContext,
                    _timeProvider,
                    MessageId,
                    ConsumerId);
        return _writeCoordinator.ExecuteAsync(() =>
        {
            _dbContext.Add(message);
            return Task.CompletedTask;
        }, context.CancellationToken);
    }
}
