using System;

#nullable enable
namespace ViciOne.ServiceBus;

public static class EventHubConsumeContextExtensions
{

    public static string? OffsetString(this ConsumeContext context)
    {
        return context.TryGetPayload(out EventHubConsumeContext? consumeContext) ? consumeContext.OffsetString : null;
    }

    public static long? SequenceNumber(this ConsumeContext context)
    {
        return context.TryGetPayload(out EventHubConsumeContext? consumeContext) ? consumeContext.SequenceNumber : null;
    }
}
