namespace ViciOne.ServiceBus.RabbitMq;


/// <summary>Outcome of one bounded RabbitMQ fault-redrive operation.</summary>
/// <param name="EndpointQueueName">The destination receive-endpoint queue.</param>
/// <param name="SourceQueueName">The error queue scanned for faulted messages.</param>
/// <param name="Scanned">The number of source deliveries inspected.</param>
/// <param name="Matched">The number of deliveries matching every supplied filter.</param>
/// <param name="Redriven">The number of matching deliveries republished and acknowledged.</param>
/// <param name="SourceExhausted">Whether the broker returned no further immediately available delivery.</param>
/// <param name="ScanLimitReached">Whether scanning stopped at <see cref="RabbitMqFaultRedriveRequest.MaxScanCount" /> before the source was exhausted.</param>
public sealed record RabbitMqFaultRedriveResult(
    string EndpointQueueName,
    string SourceQueueName,
    int Scanned,
    int Matched,
    int Redriven,
    bool SourceExhausted,
    bool ScanLimitReached);
