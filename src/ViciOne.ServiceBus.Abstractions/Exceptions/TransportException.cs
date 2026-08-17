namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class TransportException :
        AbstractUriException
    {
        public TransportException()
        {
        }

        public TransportException(Uri uri)
            : base(uri)
        {
        }

        public TransportException(Uri uri, string message)
            : base(uri, message)
        {
        }

        public TransportException(Uri uri, string message, Exception innerException)
            : base(uri, message, innerException)
        {
        }
    }
}
