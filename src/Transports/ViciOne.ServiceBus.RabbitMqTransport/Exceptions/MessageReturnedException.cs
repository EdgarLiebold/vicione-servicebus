namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// Published when a RabbitMQ channel is closed and the message was not confirmed by the broker.
    /// </summary>
    [Serializable]
    public class MessageReturnedException :
        ViciOneServiceBusException
    {
        public MessageReturnedException()
        {
        }

        public MessageReturnedException(string message)
            : base(message)
        {
        }

        public MessageReturnedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
