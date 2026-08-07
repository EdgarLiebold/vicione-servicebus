// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

public interface IAmazonSqsConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IAmazonSqsConsumeTopology
{
    new IAmazonSqsMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    void AddSpecification(IAmazonSqsConsumeTopologySpecification specification);
}
