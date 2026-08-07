// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.SqlTransport
{
    using System.Threading.Tasks;
    using Transports;


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
}
