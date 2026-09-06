using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by future request definition.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface IFutureRequestDefinition<TRequest>
    where TRequest : class
{
    /// <summary>Gets the request address.</summary>
    Uri RequestAddress { get; }
}
