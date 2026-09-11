using System;
using ViciOne.ServiceBus.MessageData.Values;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates message-data values for assignment to strongly typed message contracts.</summary>
public static class MessageData
{
    /// <summary>Creates a populated value that will be stored according to the configured message-data policy when sent.</summary>
    /// <typeparam name="T">The reference type carried by the message-data property.</typeparam>
    /// <param name="value">The value to attach to the outgoing message.</param>
    /// <returns>A populated message-data value whose repository address is assigned during send processing.</returns>
    public static Serialization.MessageData<T> FromValue<T>(T value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        return new PutMessageData<T>(value);
    }

    /// <summary>Returns an empty message-data value.</summary>
    /// <typeparam name="T">The reference type carried by the message-data property.</typeparam>
    /// <returns>A value whose <see cref="Serialization.IMessageData.HasValue" /> property is <see langword="false" />.</returns>
    public static Serialization.MessageData<T> Empty<T>()
        where T : class => EmptyMessageData<T>.Instance;
}
