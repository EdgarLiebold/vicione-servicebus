// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class RequestException :
        ViciOneServiceBusException
    {
        public RequestException(string message, Exception innerException, object response)
            : base(message, innerException)
        {
            Response = response;
        }

        public RequestException()
        {
        }

        public RequestException(string message)
            : base(message)
        {
        }

        public RequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if NET8_0_OR_GREATER
        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
#endif
        protected RequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        protected RequestException(string message, object response)
            : base(message)
        {
            Response = response;
        }

        public object? Response { get; }
    }
}
