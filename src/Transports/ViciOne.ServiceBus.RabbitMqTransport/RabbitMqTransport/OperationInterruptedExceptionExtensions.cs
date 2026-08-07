// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using RabbitMQ.Client.Exceptions;


    public static class OperationInterruptedExceptionExtensions
    {
        public static bool ChannelShouldBeClosed(this OperationInterruptedException ex)
        {
            if (ex.ShutdownReason == null)
                return true;

            return ex.ShutdownReason?.ReplyCode >= 300;
        }
    }
}
