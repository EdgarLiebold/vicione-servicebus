namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates a receive dispatcher for one specialized registration type.</summary>
public interface ITypeReceiveEndpointDispatcherFactory
{
    /// <summary>Creates the registration type's receive endpoint dispatcher.</summary>
    /// <param name="factory">The factory that owns dispatcher instances.</param>
    /// <param name="formatter">The formatter used to derive the endpoint name.</param>
    /// <returns>The dispatcher for the registration type.</returns>
    IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter);
}
