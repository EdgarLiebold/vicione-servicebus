using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a sql queue dead letter transport implementation.
/// </summary>
public class SqlQueueDeadLetterTransport :
    SqlQueueMoveTransport,
    IDeadLetterTransport
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="queueType">The queue type value.</param>
    public SqlQueueDeadLetterTransport(string queueName, SqlQueueType queueType)
        : base(queueName, queueType)
    {
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="reason">The reason value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ReceiveContext context, string? reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(SqlTransportMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
