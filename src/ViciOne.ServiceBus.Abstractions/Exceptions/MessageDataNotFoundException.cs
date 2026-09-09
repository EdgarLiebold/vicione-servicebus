using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a message-data repository does not contain the requested address.</summary>
public sealed class MessageDataNotFoundException :
    MessageDataException
{
    /// <summary>Creates a not-found exception without a message-data address.</summary>
    public MessageDataNotFoundException()
    {
    }

    /// <summary>Creates a not-found exception for the specified message-data address.</summary>
    /// <param name="address">The address that the repository could not resolve.</param>
    public MessageDataNotFoundException(Uri address)
        : base(FormatMessage(address))
    {
        Address = address;
    }

    /// <summary>Gets the message-data address that was not found, when one was supplied.</summary>
    public Uri? Address { get; }

    static string FormatMessage(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return $"The message data was not found: {address}";
    }
}
