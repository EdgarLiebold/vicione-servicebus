using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class MessageNotConsumedException :
    TransportException
{
    public MessageNotConsumedException()
    {
    }

    public MessageNotConsumedException(Uri uri)
        : base(uri)
    {
    }

    public MessageNotConsumedException(Uri uri, string message)
        : base(uri, message)
    {
    }

    public MessageNotConsumedException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
