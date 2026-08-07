// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Futures.Contracts
{
    using System;


    /// <summary>
    /// Sent by a client to get a future value by type/ID
    /// </summary>
    /// <typeparam name="TFuture">The future type</typeparam>
    public interface Get<TFuture> :
        CorrelatedBy<Guid>
        where TFuture : class
    {
    }
}
