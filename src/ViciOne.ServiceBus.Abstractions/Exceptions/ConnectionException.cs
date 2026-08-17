namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ConnectionException :
        ViciOneServiceBusException
    {
        public ConnectionException()
        {
        }

        public ConnectionException(bool isTransient)
        {
            IsTransient = isTransient;
        }

        public ConnectionException(string message, bool isTransient = false)
            : base(message)
        {
            IsTransient = isTransient;
        }

        public ConnectionException(string message, Exception innerException, bool isTransient = true)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }

        public bool IsTransient { get; }
    }
}
