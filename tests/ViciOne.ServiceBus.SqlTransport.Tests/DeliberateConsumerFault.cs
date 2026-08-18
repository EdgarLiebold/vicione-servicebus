namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;


/// <summary>
/// The fault a spec throws on purpose to drive redelivery or dead lettering. It lives here rather than
/// being borrowed from another test project, so nothing in this suite depends on a type that belongs to
/// a fixture somewhere else.
/// </summary>
public class DeliberateConsumerFault :
    Exception
{
    public DeliberateConsumerFault(string message)
        : base(message)
    {
    }
}
