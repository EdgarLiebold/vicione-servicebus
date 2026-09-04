using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// When added to a message type (class, record, or interface), prevents
/// ViciOne.ServiceBus from creating an exchange or topic on the broker for the message
/// type when it is an inherited type (such as IMessage, IEvent, etc.).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class ExcludeFromTopologyAttribute :
    Attribute
{
}
