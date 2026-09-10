using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Moves faulted SQL transport messages to their error queue.</summary>
public class SqlQueueErrorTransport :
    SqlQueueMoveTransport,
    IErrorTransport
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="queueType">The runtime queue type used by the operation.</param>
    public SqlQueueErrorTransport(string queueName, SqlQueueType queueType)
        : base(queueName, queueType)
    {
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        void AddExceptionDetails(SqlTransportMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);

            if (message.ExpirationTime.HasValue)
                message.ExpirationTime = context.GetTimeProvider().GetUtcNow().UtcDateTime + SqlTransportDefaults.ErrorQueueTimeToLive;
        }

        return MoveAsync(context, AddExceptionDetails, cancellationToken);
    }
}
