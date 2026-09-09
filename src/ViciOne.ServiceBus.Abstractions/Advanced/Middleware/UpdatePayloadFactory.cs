namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates a replacement for an existing context payload.</summary>
/// <typeparam name="TPayload">The payload type.</typeparam>
/// <param name="existing">The existing payload.</param>
/// <returns>The replacement payload.</returns>
public delegate TPayload UpdatePayloadFactory<TPayload>(TPayload existing)
    where TPayload : class;
