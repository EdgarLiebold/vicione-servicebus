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

    public Task Send(ReceiveContext context, string? reason)
    {
        void PreSend(SqlTransportMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return Move(context, PreSend);
    }
}
