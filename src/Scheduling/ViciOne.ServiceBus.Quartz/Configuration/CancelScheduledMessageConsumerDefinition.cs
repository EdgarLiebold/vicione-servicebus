using ViciOne.ServiceBus.Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a cancel scheduled message consumer definition implementation.
/// </summary>
public class CancelScheduledMessageConsumerDefinition :
    ConsumerDefinition<CancelScheduledMessageConsumer>
{
    readonly QuartzEndpointDefinition _endpointDefinition;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointDefinition">The endpoint definition value.</param>
    public CancelScheduledMessageConsumerDefinition(QuartzEndpointDefinition endpointDefinition)
    {
        _endpointDefinition = endpointDefinition;

        EndpointDefinition = endpointDefinition;
    }

    /// <summary>
    /// Configures consumer.
    /// </summary>
    /// <param name="endpointConfigurator">The endpoint configurator value.</param>
    /// <param name="consumerConfigurator">The consumer configurator value.</param>
    /// <param name="context">The operation context.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CancelScheduledMessageConsumer> consumerConfigurator, IRegistrationContext context)
    {
        consumerConfigurator.Message<CancelScheduledMessage>(m => m.UsePartitioner(_endpointDefinition.Partition, p => p.Message.TokenId));

        consumerConfigurator.Message<CancelScheduledRecurringMessage>(m =>
        {
            m.UsePartitioner(_endpointDefinition.Partition, p => $"{p.Message.ScheduleGroup},{p.Message.ScheduleId}");
        });
    }
}
