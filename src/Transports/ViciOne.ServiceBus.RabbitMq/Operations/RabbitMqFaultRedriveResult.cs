namespace ViciOne.ServiceBus.RabbitMq;


/// <summary>Outcome of one bounded RabbitMQ fault-redrive operation.</summary>
public sealed record RabbitMqFaultRedriveResult(
    string EndpointQueueName,
    string SourceQueueName,
    int Scanned,
    int Matched,
    int Redriven,
    bool SourceExhausted,
    bool ScanLimitReached);
