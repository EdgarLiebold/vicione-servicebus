using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

public class SqlQueueErrorTransport :
    SqlQueueMoveTransport,
    IErrorTransport
{
    public SqlQueueErrorTransport(string queueName, SqlQueueType queueType)
        : base(queueName, queueType)
    {
    }

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
