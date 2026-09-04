using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

public class SqlQueueDeadLetterTransport :
    SqlQueueMoveTransport,
    IDeadLetterTransport
{
    public SqlQueueDeadLetterTransport(string queueName, SqlQueueType queueType)
        : base(queueName, queueType)
    {
    }

    public Task SendAsync(ReceiveContext context, string? reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(SqlTransportMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
