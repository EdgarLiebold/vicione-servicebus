using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class PayloadNotFoundException :
    PayloadException
{
    public PayloadNotFoundException()
    {
    }

    public PayloadNotFoundException(string message)
        : base(message)
    {
    }

    public PayloadNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
