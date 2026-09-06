using ViciOne.ServiceBus.Initializers.Variables;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates deferred values shared within a message-initialization operation.</summary>
public static class InVar
{
    /// <summary>Gets a deferred UTC timestamp that remains consistent within one initialized message.</summary>
    public static TimestampVariable Timestamp => new();

    /// <summary>Gets a deferred identifier inferred for an <c>Id</c> property and shared within one initialized message.</summary>
    public static IdVariable Id => new();

    /// <summary>Gets a deferred identifier inferred for a <c>CorrelationId</c> property and shared within one initialized message.</summary>
    public static IdVariable CorrelationId => new();
}
