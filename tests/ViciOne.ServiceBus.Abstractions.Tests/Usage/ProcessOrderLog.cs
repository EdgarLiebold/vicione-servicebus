namespace ViciOne.ServiceBus.Abstractions.Tests.Usage
{
    using System;


    public interface ProcessOrderLog
    {
        Guid OrderId { get; }
        Guid ShipmentId { get; }
    }
}
