namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.PublishContracts;

public interface FirstPublishedContract
{
    Guid CorrelationId { get; }
}

public interface SecondPublishedContract
{
    Guid CorrelationId { get; }
}
