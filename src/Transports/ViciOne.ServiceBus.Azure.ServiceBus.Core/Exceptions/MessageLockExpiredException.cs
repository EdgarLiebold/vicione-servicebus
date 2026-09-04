using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class MessageLockExpiredException :
    TransportException
{
    public MessageLockExpiredException()
    {
    }

    public MessageLockExpiredException(Uri uri)
        : base(uri)
    {
    }

    public MessageLockExpiredException(Uri uri, string message)
        : base(uri, message)
    {
    }

    public MessageLockExpiredException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
