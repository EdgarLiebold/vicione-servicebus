// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Abstractions.Tests.Usage
{
    using System;


    public interface ProcessOrderLog
    {
        Guid OrderId { get; }
        Guid ShipmentId { get; }
    }
}
