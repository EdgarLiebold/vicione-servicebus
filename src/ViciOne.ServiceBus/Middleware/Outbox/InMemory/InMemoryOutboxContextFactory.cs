using System;
using System.Threading.Tasks;

using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.Middleware.Outbox.InMemory;

/// <summary>Coordinates process-local inbox locking and outbox-aware pipeline execution.</summary>
internal sealed class InMemoryOutboxContextFactory :
    IOutboxContextFactory<InMemoryOutboxMessageRepository>
{
    readonly InMemoryOutboxMessageRepository _messageRepository;
    readonly IServiceProvider _provider;

    /// <summary>Initializes the factory over a shared inbox repository and a consume scope.</summary>
    /// <param name="messageRepository">The repository that owns process-local inbox state.</param>
    /// <param name="provider">The scoped service provider exposed to outbox send operations.</param>
    public InMemoryOutboxContextFactory(InMemoryOutboxMessageRepository messageRepository, IServiceProvider provider)
    {
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Runs the consume pipeline under the inbox lock until outbox processing is complete.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The incoming message context.</param>
    /// <param name="options">The inbox identity and outbox delivery settings.</param>
    /// <param name="next">The outbox-aware consume pipeline.</param>
    /// <param name="cancellationToken">The token that cancels inbox lock acquisition.</param>
    /// <returns>A task that completes after consumption and captured-message delivery finish or fail.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next, CancellationToken cancellationToken = default)
        where T : class
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

        var updateDeliveryCount = true;
        var continueProcessing = true;

        var messageId = context.GetOriginalMessageId() ?? throw new MessageException(typeof(T), "MessageId required to use the outbox");

        while (continueProcessing)
        {
            deliveryCancellationToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            InMemoryInboxMessage inboxMessage;
            try
            {
                inboxMessage = await _messageRepository.LockAsync(
                    messageId, options.ConsumerId, operationCancellationToken).ConfigureAwait(false);
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

            try
            {
                if (updateDeliveryCount)
                    inboxMessage.ReceiveCount++;

                updateDeliveryCount = false;

                var outboxContext = new InMemoryOutboxConsumeContext<T>(context, options, _provider, inboxMessage);

                try
                {
                    await next.SendAsync(outboxContext).ConfigureAwait(false);
                }
                catch
                {
                    if (!outboxContext.IsMessageConsumed)
                        outboxContext.DiscardPendingConsumerMessages();
                    else
                    {
                        try
                        {
                            await outboxContext.ConsumeCompleted.ConfigureAwait(false);
                        }
                        catch
                        {
                            // Observe the completion failure without replacing the delivery exception
                            // that the enclosing catch rethrows.
                        }
                    }

                    throw;
                }

                continueProcessing = outboxContext.ContinueProcessing;
            }
            finally
            {
                inboxMessage.Release();
            }
        }
    }

    /// <summary>Adds the in-memory inbox/outbox component to a probe result.</summary>
    /// <param name="context">The probe context to enrich.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateFilterScope("inMemoryOutboxContextFactory");
    }
}
