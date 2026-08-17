namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class RequestTimeoutException :
        RequestException
    {
        public RequestTimeoutException()
        {
        }

        public RequestTimeoutException(string requestId)
            : base(FormatMessage(requestId))
        {
        }

        public RequestTimeoutException(string requestId, Exception innerException)
            : base(FormatMessage(requestId), innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected RequestTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        static string FormatMessage(string requestId)
        {
            return $"Timeout waiting for response, RequestId: {requestId}";
        }
    }
}
