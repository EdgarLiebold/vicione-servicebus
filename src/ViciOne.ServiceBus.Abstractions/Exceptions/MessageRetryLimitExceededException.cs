namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class MessageRetryLimitExceededException :
        TransportException
    {
        public MessageRetryLimitExceededException()
        {
        }

        public MessageRetryLimitExceededException(Uri uri)
            : base(uri)
        {
        }

        public MessageRetryLimitExceededException(Uri uri, string message)
            : base(uri, message)
        {
        }

        public MessageRetryLimitExceededException(Uri uri, string message, Exception innerException)
            : base(uri, message, innerException)
        {
        }
    }
}
