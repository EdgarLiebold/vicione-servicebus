// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System;
using System.Runtime.Serialization;


[Serializable]
public class AmazonSqsConnectException :
    AmazonSqsConnectionException
{
    public AmazonSqsConnectException()
    {
    }

    public AmazonSqsConnectException(string message)
        : base(message)
    {
    }

    public AmazonSqsConnectException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

#if NET8_0_OR_GREATER
    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
#endif
    protected AmazonSqsConnectException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
