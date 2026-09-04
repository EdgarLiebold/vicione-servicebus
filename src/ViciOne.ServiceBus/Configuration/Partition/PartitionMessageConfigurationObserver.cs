namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a partition message configuration observer implementation.
/// </summary>
public class PartitionMessageConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly IPartitioner _partitioner;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="partitioner">The partitioner value.</param>
    public PartitionMessageConfigurationObserver(IConsumePipeConfigurator configurator, IPartitioner partitioner)
        : base(configurator)
    {
        _partitioner = partitioner;

        Connect(this);
    }

    /// <summary>
    /// Performs the message configured operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new PartitionMessageSpecification<TMessage>(_partitioner);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the batch consumer configured operation.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
    {
        var specification = new PartitionMessageSpecification<Batch<TMessage>>(_partitioner);

        configurator.Message(m => m.AddPipeSpecification(specification));
    }
}
