namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for type receive endpoint dispatcher factory.
/// </summary>
public interface ITypeReceiveEndpointDispatcherFactory
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter);
}
