using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Configures the send pipeline for a request message.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
public interface IRequestPipeConfigurator<TRequest> :
    IRequestPipeConfigurator,
    IPipeConfigurator<SendContext<TRequest>>
    where TRequest : class
{
}


/// <summary>Exposes identity and delivery settings for one pending request.</summary>
public interface IRequestPipeConfigurator
{
    /// <summary>Gets the identifier used to correlate the request with its response.</summary>
    Guid RequestId { get; }

    /// <summary>Sets the transport time-to-live, or clears it with <see cref="RequestTimeout.None" />.</summary>
    /// <remarks>The request response deadline is unchanged.</remarks>
    RequestTimeout TimeToLive { set; }
}
