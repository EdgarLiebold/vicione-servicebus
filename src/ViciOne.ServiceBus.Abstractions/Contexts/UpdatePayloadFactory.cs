// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Update an existing payload, using the existing payload
    /// </summary>
    /// <param name="existing">The existing payload</param>
    /// <typeparam name="TPayload">The payload type</typeparam>
    public delegate TPayload UpdatePayloadFactory<TPayload>(TPayload existing)
        where TPayload : class;
}
