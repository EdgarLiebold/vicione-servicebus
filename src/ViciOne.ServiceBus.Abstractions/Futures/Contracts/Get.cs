using System;

namespace ViciOne.ServiceBus.Futures.Contracts;

/// <summary>Sent by a client to get a future value by type/ID.</summary>
/// <typeparam name="TFuture">The future type.</typeparam>
public interface Get<TFuture> :
    CorrelatedBy<Guid>
    where TFuture : class
{
}
