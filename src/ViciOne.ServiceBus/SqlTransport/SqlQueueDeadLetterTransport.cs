using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Moves undeliverable SQL transport messages to their dead-letter queue.</summary>
public class SqlQueueDeadLetterTransport :
    SqlQueueMoveTransport,
    IDeadLetterTransport
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="queueType">The runtime queue type used by the operation.</param>
    public SqlQueueDeadLetterTransport(string queueName, SqlQueueType queueType)
        : base(queueName, queueType)
    {
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ReceiveContext context, string? reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        void AddDeadLetterReason(SqlTransportMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, AddDeadLetterReason, cancellationToken);
    }
}
