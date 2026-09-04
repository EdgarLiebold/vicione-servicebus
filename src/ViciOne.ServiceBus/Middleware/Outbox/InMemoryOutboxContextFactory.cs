using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

public class InMemoryOutboxContextFactory :
    IOutboxContextFactory<InMemoryOutboxMessageRepository>
{
    readonly InMemoryOutboxMessageRepository _messageRepository;
    readonly IServiceProvider _provider;

    public InMemoryOutboxContextFactory(InMemoryOutboxMessageRepository messageRepository, IServiceProvider provider)
    {
        _messageRepository = messageRepository;
        _provider = provider;
    }

    public async Task Send<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next)
        where T : class
    {
        var updateDeliveryCount = true;
        var continueProcessing = true;

        var messageId = context.GetOriginalMessageId() ?? throw new MessageException(typeof(T), "MessageId required to use the outbox");

        while (continueProcessing)
        {
            await _messageRepository.MarkInUse(context.CancellationToken).ConfigureAwait(false);

            InMemoryInboxMessage inboxMessage = null;
            try
            {
                inboxMessage = await _messageRepository.Lock(messageId, options.ConsumerId, context.CancellationToken).ConfigureAwait(false);
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
                    await next.Send(outboxContext).ConfigureAwait(false);
                }
                catch
                {
                    if (!outboxContext.IsMessageConsumed)
                        await outboxContext.DiscardPendingConsumerMessages().ConfigureAwait(false);
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

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("inMemoryOutboxContextFactory");
    }
}
