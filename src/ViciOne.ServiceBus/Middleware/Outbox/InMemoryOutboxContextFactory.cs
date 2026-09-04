using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>
/// Provides an in memory outbox context factory implementation.
/// </summary>
public class InMemoryOutboxContextFactory :
    IOutboxContextFactory<InMemoryOutboxMessageRepository>
{
    readonly InMemoryOutboxMessageRepository _messageRepository;
    readonly IServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageRepository">The message repository value.</param>
    /// <param name="provider">The service provider.</param>
    public InMemoryOutboxContextFactory(InMemoryOutboxMessageRepository messageRepository, IServiceProvider provider)
    {
        _messageRepository = messageRepository;
        _provider = provider;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="options">The options value.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested(); var updateDeliveryCount = true;
        var continueProcessing = true;

        var messageId = context.GetOriginalMessageId() ?? throw new MessageException(typeof(T), "MessageId required to use the outbox");

        while (continueProcessing)
        {
            await _messageRepository.MarkInUseAsync(context.CancellationToken).ConfigureAwait(false);

            InMemoryInboxMessage? inboxMessage = null;
            try
            {
                inboxMessage = await _messageRepository.LockAsync(messageId, options.ConsumerId, context.CancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _messageRepository.Release();
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
                            // The awaited delivery already exposed this failure. Drain the attempt-local
                            // pending task so that it cannot poison the retry, while the outer throw keeps
                            // the original delivery exception and stack.
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

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("inMemoryOutboxContextFactory");
    }
}
