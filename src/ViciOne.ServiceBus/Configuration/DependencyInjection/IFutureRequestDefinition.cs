using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for future request definition.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
public interface IFutureRequestDefinition<TRequest>
    where TRequest : class
{
    /// <summary>
    /// Gets the request address value.
    /// </summary>
    Uri RequestAddress { get; }
}
