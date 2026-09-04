using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class MessageDataException :
    ViciOneServiceBusException
{
    public MessageDataException()
    {
    }

    public MessageDataException(string message)
        : base(message)
    {
    }

    public MessageDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
