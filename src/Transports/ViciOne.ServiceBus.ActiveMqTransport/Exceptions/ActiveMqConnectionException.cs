namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class ActiveMqConnectionException :
        ConnectionException
    {
        public ActiveMqConnectionException()
        {
        }

        public ActiveMqConnectionException(string message)
            : base(message)
        {
        }

        public ActiveMqConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected ActiveMqConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
