namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Represents the method that handles payload factory.
/// </summary>
/// <typeparam name="TPayload">The t payload type.</typeparam>
/// <returns>The result of the operation.</returns>
public delegate TPayload PayloadFactory<out TPayload>()
    where TPayload : class;
