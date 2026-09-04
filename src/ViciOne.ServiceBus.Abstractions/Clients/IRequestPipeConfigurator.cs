using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for request pipe configurator.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
public interface IRequestPipeConfigurator<TRequest> :
    IRequestPipeConfigurator,
    IPipeConfigurator<SendContext<TRequest>>
    where TRequest : class
{
}


/// <summary>
/// Defines the contract for request pipe configurator.
/// </summary>
public interface IRequestPipeConfigurator
{
    /// <summary>
    /// The RequestId assigned to the request, and used in the header for the outgoing request message
    /// </summary>
    Guid RequestId { get; }

    /// <summary>
    /// Set the request message time to live, which by default is equal to the request timeout. Clearing this value
    /// will prevent any TimeToLive value from being specified.
    /// </summary>
    RequestTimeout TimeToLive { set; }
}
