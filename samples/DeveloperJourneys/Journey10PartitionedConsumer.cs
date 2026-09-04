namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey10PartitionedConsumer
{
    public sealed class PartitionedSubmitOrderDefinition : ConsumerDefinition<SubmitOrderConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpoint,
            IConsumerConfigurator<SubmitOrderConsumer> consumer,
            IRegistrationContext context) =>
            consumer.UsePartitionedConcurrency<SubmitOrder, Guid>(
                partitionCount: 16,
                static message => message.CustomerId);
    }
}
