using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.MessageData;

/// <summary>
/// Provides an in memory message data id implementation.
/// </summary>
public class InMemoryMessageDataId
{
    readonly NewId _id;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public InMemoryMessageDataId()
    {
        _id = NewId.Next();
    }

    /// <summary>
    /// Gets the uri value.
    /// </summary>
    public Uri Uri => new Uri("urn:msgdata:" + FormatUtil.Formatter.Format(_id.ToByteArray()));
}
