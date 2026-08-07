// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport
{
    using System;
    using System.Threading.Tasks;
    using Transports;


    public class SqlQueueErrorTransport :
        SqlQueueMoveTransport,
        IErrorTransport
    {
        public SqlQueueErrorTransport(string queueName, SqlQueueType queueType)
            : base(queueName, queueType)
        {
        }

        public Task Send(ExceptionReceiveContext context)
        {
            void PreSend(SqlTransportMessage message, SendHeaders headers)
            {
                headers.CopyFrom(context.ExceptionHeaders);

                if (message.ExpirationTime.HasValue)
                    message.ExpirationTime = DateTime.UtcNow + Defaults.ErrorQueueTimeToLive;
            }

            return Move(context, PreSend);
        }
    }
}
