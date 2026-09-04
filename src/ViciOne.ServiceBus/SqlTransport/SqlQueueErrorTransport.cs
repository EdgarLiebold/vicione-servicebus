using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a sql queue error transport implementation.
/// </summary>
public class SqlQueueErrorTransport :
    SqlQueueMoveTransport,
    IErrorTransport
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="queueType">The queue type value.</param>
    public SqlQueueErrorTransport(string queueName, SqlQueueType queueType)
        : base(queueName, queueType)
    {
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(SqlTransportMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);

            if (message.ExpirationTime.HasValue)
                message.ExpirationTime = context.GetTimeProvider().GetUtcNow().UtcDateTime + Defaults.ErrorQueueTimeToLive;
        }

        return MoveAsync(context, PreSend);
    }
}
