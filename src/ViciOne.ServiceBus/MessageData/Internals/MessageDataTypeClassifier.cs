using System;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.MessageData.Internals;

/// <summary>Classifies reference types supported by object-valued message data.</summary>
internal static class MessageDataTypeClassifier
{
    /// <summary>Determines whether a type is a serializable, non-object reference message contract.</summary>
    /// <param name="type">The runtime type to classify.</param>
    /// <returns><see langword="true" /> when the type can be stored as object-valued message data; otherwise, <see langword="false" />.</returns>
    internal static bool IsSupported(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.IsInterfaceOrConcreteClass()
            && MessageTypeCache.IsValidMessageType(type)
            && !type.IsValueTypeOrObject();
    }
}
