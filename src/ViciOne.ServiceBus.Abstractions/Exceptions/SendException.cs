namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class SendException :
        AbstractUriException
    {
        public SendException()
        {
        }

        public SendException(Type messageType, Uri uri)
            : base(uri)
        {
            MessageType = messageType;
        }

        public SendException(Type messageType, Uri uri, string message)
            : base(uri, message)
        {
            MessageType = messageType;
        }

        public SendException(Type messageType, Uri uri, string message, Exception innerException)
            : base(uri, message, innerException)
        {
            MessageType = messageType;
        }

        public Type? MessageType { get; protected set; }
    }
}
