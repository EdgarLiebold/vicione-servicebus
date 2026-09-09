namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds correlation-based partitioning to every non-batch message type configured on a consume pipeline.</summary>
internal sealed class PartitionMessageConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly IPartitioner _partitioner;

    /// <summary>Creates and connects an observer backed by one shared partitioner.</summary>
    /// <param name="configurator">The consume pipeline whose message configurations are observed.</param>
    /// <param name="partitioner">The partitioner shared by all observed message types.</param>
    public PartitionMessageConfigurationObserver(IConsumePipeConfigurator configurator, IPartitioner partitioner)
        : base(configurator)
    {
        _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));

        Connect(this);
    }

    /// <summary>Adds correlation-based partitioning to a newly configured message pipeline.</summary>
    /// <typeparam name="TMessage">The configured message type.</typeparam>
    /// <param name="configurator">The consume pipeline that owns the message pipeline.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var specification = new PartitionMessageSpecification<TMessage>(_partitioner);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Leaves a batch pipeline unpartitioned because a batch has no single element correlation key.</summary>
    /// <typeparam name="TConsumer">The batch consumer implementation.</typeparam>
    /// <typeparam name="TMessage">The message type contained by the batch.</typeparam>
    /// <param name="configurator">The batch consumer pipeline left unchanged.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
        where TConsumer : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
    }
}
