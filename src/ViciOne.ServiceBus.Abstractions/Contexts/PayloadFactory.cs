namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents the method that handles payload factory.</summary>
/// <typeparam name="TPayload">The payload type.</typeparam>
/// <returns>The value produced by the operation.</returns>
public delegate TPayload PayloadFactory<out TPayload>()
    where TPayload : class;
