namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.PublishContracts;

public interface FirstPublishedContract
{
    Guid CorrelationId { get; }
}

public interface SecondPublishedContract
{
    Guid CorrelationId { get; }
}
