namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes partition message configuration events.</summary>
public class PartitionMessageConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly IPartitioner _partitioner;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="partitioner">The partitioner.</param>
    public PartitionMessageConfigurationObserver(IConsumePipeConfigurator configurator, IPartitioner partitioner)
        : base(configurator)
    {
        _partitioner = partitioner;

        Connect(this);
    }

    /// <summary>Reports that message has been configured.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new PartitionMessageSpecification<TMessage>(_partitioner);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Reports that batch consumer has been configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
    {
        var specification = new PartitionMessageSpecification<Batch<TMessage>>(_partitioner);

        configurator.Message(m => m.AddPipeSpecification(specification));
    }
}
