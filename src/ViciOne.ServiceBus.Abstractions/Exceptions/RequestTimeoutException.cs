namespace ViciOne.ServiceBus
{
    using System;


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

        static string FormatMessage(string requestId)
        {
            return $"Timeout waiting for response, RequestId: {requestId}";
        }
    }
}
