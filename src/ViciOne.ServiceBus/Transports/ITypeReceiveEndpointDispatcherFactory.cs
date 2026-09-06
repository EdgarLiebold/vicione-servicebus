namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates type receive endpoint dispatcher instances.</summary>
public interface ITypeReceiveEndpointDispatcherFactory
{
    /// <summary>Creates the requested value.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The newly created instance.</returns>
    IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter);
}
