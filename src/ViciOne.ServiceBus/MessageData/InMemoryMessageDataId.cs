using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.MessageData;

/// <summary>Represents the identifier for in memory message data.</summary>
public class InMemoryMessageDataId
{
    readonly NewId _id;

    /// <summary>Initializes a new instance.</summary>
    public InMemoryMessageDataId()
    {
        _id = NewId.Next();
    }

    /// <summary>Gets the uri.</summary>
    public Uri Uri => new Uri("urn:msgdata:" + FormatUtil.Formatter.Format(_id.ToByteArray()));
}
