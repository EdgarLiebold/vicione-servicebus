using System;

namespace ViciOne.ServiceBus.Futures.Contracts;

/// <summary>Requests the durable outcome of a future instance identified by its correlation identifier.</summary>
/// <typeparam name="TFuture">The command contract used by the future whose outcome is requested.</typeparam>
public interface IGet<TFuture> :
    ICorrelatedBy<Guid>
    where TFuture : class
{
}
