using System;

#nullable enable
namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides extension methods for event hub consume context.
/// </summary>
public static class EventHubConsumeContextExtensions
{

    /// <summary>
    /// Performs the offset string operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static string? OffsetString(this ConsumeContext context)
    {
        return context.TryGetPayload(out EventHubConsumeContext? consumeContext) ? consumeContext.OffsetString : null;
    }

    /// <summary>
    /// Performs the sequence number operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static long? SequenceNumber(this ConsumeContext context)
    {
        return context.TryGetPayload(out EventHubConsumeContext? consumeContext) ? consumeContext.SequenceNumber : null;
    }
}
