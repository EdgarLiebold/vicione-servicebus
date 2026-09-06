using System;

namespace ViciOne.ServiceBus.Futures.Contracts;

/// <summary>Requests the current result of a future instance identified by its correlation identifier.</summary>
/// <typeparam name="TFuture">The future state machine whose result is requested.</typeparam>
public interface Get<TFuture> :
    CorrelatedBy<Guid>
    where TFuture : class
{
}
