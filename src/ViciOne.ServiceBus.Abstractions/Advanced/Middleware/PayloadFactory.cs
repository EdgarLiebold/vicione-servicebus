namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates a context payload.</summary>
/// <typeparam name="TPayload">The payload type.</typeparam>
/// <returns>The created payload.</returns>
public delegate TPayload PayloadFactory<out TPayload>()
    where TPayload : class;
