using System;

namespace ViciOneServiceBusBenchmark.BusOutbox;

public record BusOutboxMessage(Guid CorrelationId, string Payload);
