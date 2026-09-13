using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Exposes the destination selected by a consumer-backed future definition.</summary>
/// <typeparam name="TRequest">The request contract sent to the companion consumer.</typeparam>
internal interface IFutureRequestDefinition<TRequest>
    where TRequest : class
{
    /// <summary>Gets the companion consumer endpoint to which the future sends requests.</summary>
    Uri RequestAddress { get; }
}
