using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class MessageDataNotFoundException :
    MessageDataException
{
    public MessageDataNotFoundException()
    {
    }

    public MessageDataNotFoundException(Uri address)
        : base($"The message data was not found: {address}")
    {
    }
}
