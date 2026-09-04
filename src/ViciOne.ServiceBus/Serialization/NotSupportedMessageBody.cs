using System;
using System.IO;

namespace ViciOne.ServiceBus.Serialization;

public class NotSupportedMessageBody :
    MessageBody
{
    public long? Length => throw new NotSupportedException();

    public Stream GetStream()
    {
        throw new NotSupportedException();
    }

    public byte[] GetBytes()
    {
        throw new NotSupportedException();
    }

    public string GetString()
    {
        throw new NotSupportedException();
    }
}
