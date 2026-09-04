using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.MessageData;

public class InMemoryMessageDataId
{
    readonly NewId _id;

    public InMemoryMessageDataId()
    {
        _id = NewId.Next();
    }

    public Uri Uri => new Uri("urn:msgdata:" + FormatUtil.Formatter.Format(_id.ToByteArray()));
}
