namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class EventHubConnectionException :
        ConnectionException
    {
        public EventHubConnectionException()
        {
        }

        public EventHubConnectionException(string message)
            : base(message)
        {
        }

        public EventHubConnectionException(string message, Exception innerException)
            : base(message, innerException, IsExceptionTransient(innerException))
        {
        }

        static bool IsExceptionTransient(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException _ => false,
                _ => true
            };
        }
    }
}
