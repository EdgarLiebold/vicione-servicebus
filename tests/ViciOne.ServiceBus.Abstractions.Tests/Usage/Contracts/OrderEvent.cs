// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Abstractions.Tests.Usage
{
    using System;


    [ExcludeFromTopology]
    public interface OrderEvent :
        CorrelatedBy<Guid>
    {
        Guid OrderId { get; }
        DateTime Timestamp { get; }
    }
}
