using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Application API for admitting typed messages to producer-side durable storage.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IDurableSender<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Resolves the message route owned by <typeparamref name="TBus"/>, runs the configured send-context,
    /// serialization, MessageData and payload-admission path, and commits the resulting intent to durable storage.
    /// A successful receipt confirms only that persistence commit; it does not claim transport or consumer completion.
    /// </summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="message">The message to persist for delivery.</param>
    /// <param name="options">The durable identity and optional delivery metadata.</param>
    /// <param name="cancellationToken">The token used to cancel admission.</param>
    /// <returns>A task containing the durable-persistence receipt.</returns>
    Task<DurableSendReceipt> SendAsync<TMessage>(
        TMessage message,
        DurableSendOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Uses an explicit destination while retaining the owning bus's normal send-context, serializer, MessageData,
    /// payload-admission and contract-catalog path. A successful receipt confirms only durable persistence commit.
    /// </summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="destinationAddress">The explicit absolute transport destination.</param>
    /// <param name="message">The message to persist for delivery.</param>
    /// <param name="options">The durable identity and optional delivery metadata.</param>
    /// <param name="cancellationToken">The token used to cancel admission.</param>
    /// <returns>A task containing the durable-persistence receipt.</returns>
    Task<DurableSendReceipt> SendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        DurableSendOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
