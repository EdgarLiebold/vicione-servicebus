namespace ViciOne.ServiceBus.Advanced;

/// <summary>Update an existing payload, using the existing payload.</summary>
/// <typeparam name="TPayload">The payload type.</typeparam>
/// <param name="existing">The existing payload.</param>
/// <returns>The value produced by the operation.</returns>
public delegate TPayload UpdatePayloadFactory<TPayload>(TPayload existing)
    where TPayload : class;
