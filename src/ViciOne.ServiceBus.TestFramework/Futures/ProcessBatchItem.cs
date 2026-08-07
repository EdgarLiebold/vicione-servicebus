// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Futures;

using System;


public interface ProcessBatchItem
{
    public Guid CorrelationId { get; }
    public string JobNumber { get; }
}
