using ViciOne.ServiceBus.Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a schedule message consumer definition implementation.
/// </summary>
public class ScheduleMessageConsumerDefinition :
    ConsumerDefinition<ScheduleMessageConsumer>
{
    readonly QuartzEndpointDefinition _endpointDefinition;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointDefinition">The endpoint definition value.</param>
    public ScheduleMessageConsumerDefinition(QuartzEndpointDefinition endpointDefinition)
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
        IConsumerConfigurator<ScheduleMessageConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseTechnicalMessageRetry();

        consumerConfigurator.Message<ScheduleMessage>(m => m.UsePartitioner(_endpointDefinition.Partition, p => p.Message.TokenId));

        consumerConfigurator.Message<ScheduleRecurringMessage>(m =>
        {
            m.UsePartitioner(_endpointDefinition.Partition, p => $"{p.Message.Schedule?.ScheduleGroup},{p.Message.Schedule?.ScheduleId}");
        });
    }
}
