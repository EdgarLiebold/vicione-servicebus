using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Exposes Azure Service Bus-specific properties on an outgoing message.</summary>
public interface ServiceBusSendContext :
    SendContext,
    PartitionKeySendContext
{
    /// <summary>Sets the UTC instant at which Azure Service Bus should enqueue the message.</summary>
    DateTimeOffset? ScheduledEnqueueTimeUtc { set; }

    /// <summary>Sets the session identifier and partition key for the message.</summary>
    string? SessionId { set; }

    /// <summary>Sets the session identifier expected on replies.</summary>
    string? ReplyToSessionId { set; }

    /// <summary>Sets the reply destination entity path.</summary>
    string? ReplyTo { set; }

    /// <summary>Sets the application-specific subject label.</summary>
    string? Label { set; }
}


/// <summary>Exposes Azure Service Bus-specific properties on an outgoing typed message.</summary>
/// <typeparam name="T">The message contract.</typeparam>
public interface ServiceBusSendContext<out T> :
    SendContext<T>,
    ServiceBusSendContext
    where T : class
{
}
