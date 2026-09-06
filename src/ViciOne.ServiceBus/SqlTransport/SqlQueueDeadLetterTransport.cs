using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Transports sql queue dead letter messages.</summary>
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(SqlTransportMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
