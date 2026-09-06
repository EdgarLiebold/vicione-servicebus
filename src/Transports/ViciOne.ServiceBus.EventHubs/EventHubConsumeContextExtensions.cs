using System;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Reads Event Hubs metadata from a consume context when that metadata is available.</summary>
public static class EventHubConsumeContextExtensions
{

    /// <summary>Returns the provider-defined partition offset for the consumed Event Hubs event.</summary>
    /// <param name="context">The consume context to inspect.</param>
    /// <returns>The partition offset, or <see langword="null" /> when the context did not originate from Event Hubs.</returns>
    public static string? OffsetString(this ConsumeContext context)
    {
        return context.TryGetPayload(out EventHubConsumeContext? consumeContext) ? consumeContext.OffsetString : null;
    }

    /// <summary>Returns the Event Hubs sequence number.</summary>
    /// <param name="context">The consume context to inspect.</param>
    /// <returns>The sequence number, or <see langword="null" /> when the context did not originate from Event Hubs.</returns>
    public static long? SequenceNumber(this ConsumeContext context)
    {
        return context.TryGetPayload(out EventHubConsumeContext? consumeContext) ? consumeContext.SequenceNumber : null;
    }
}
