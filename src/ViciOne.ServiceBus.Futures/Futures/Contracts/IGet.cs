using System;

namespace ViciOne.ServiceBus.Futures.Contracts;

/// <summary>Requests the durable outcome of a future instance identified by its correlation identifier.</summary>
/// <typeparam name="TFuture">The future state machine whose result is requested.</typeparam>
public interface IGet<TFuture> :
    ICorrelatedBy<Guid>
    where TFuture : class
{
}
