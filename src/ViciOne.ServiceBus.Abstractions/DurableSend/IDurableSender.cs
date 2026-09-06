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
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the send outcome.</returns>
    Task<DurableSendReceipt> SendAsync<TMessage>(
        TMessage message,
        DurableSendOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Uses an explicit destination while retaining the owning bus's normal send-context, serializer, MessageData,
    /// payload-admission and contract-catalog path. A successful receipt confirms only durable persistence commit.
    /// </summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="destinationAddress">The destination address used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the send outcome.</returns>
    Task<DurableSendReceipt> SendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        DurableSendOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
