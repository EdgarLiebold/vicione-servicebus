// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOneServiceBusBenchmark.BusOutbox;

using System;


public record BusOutboxMessage(Guid CorrelationId, string Payload);
