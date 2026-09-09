using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds a concurrency limit filter for each message type configured on the consume pipe.</summary>
internal sealed class ConcurrencyLimitConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly bool _excludeManagementCommands;

    /// <summary>Creates an observer that applies one shared concurrency budget to configured message types.</summary>
    /// <param name="configurator">The consume pipeline whose message configurations are observed.</param>
    /// <param name="concurrencyLimit">The positive initial shared limit.</param>
    /// <param name="limiterId">The optional identifier used by management commands.</param>
    /// <param name="excludeManagementCommands">Whether concurrency-adjustment commands bypass the budget they modify.</param>
    public ConcurrencyLimitConfigurationObserver(
        IConsumePipeConfigurator configurator,
        int concurrencyLimit,
        string? limiterId = null,
        bool excludeManagementCommands = false)
        : base(configurator)
    {
        _excludeManagementCommands = excludeManagementCommands;
        Limiter = new ConcurrencyLimiter(concurrencyLimit, limiterId);

        Connect(this);
    }

    /// <summary>Gets the limiter shared by the observed message pipelines.</summary>
    public IConcurrencyLimiter Limiter { get; }

    /// <summary>Adds the shared limiter to a newly configured message pipeline.</summary>
    /// <typeparam name="TMessage">The configured message type.</typeparam>
    /// <param name="configurator">The consume pipeline that owns the message pipeline.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (_excludeManagementCommands && typeof(TMessage) == typeof(SetConcurrencyLimit))
            return;

        var specification = new ConcurrencyLimitConsumePipeSpecification<TMessage>(Limiter);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds the shared endpoint budget to a configured batch-consumer pipeline.</summary>
    /// <typeparam name="TConsumer">The batch consumer implementation.</typeparam>
    /// <typeparam name="TMessage">The message type contained by the batch.</typeparam>
    /// <param name="configurator">The configured batch-consumer pipeline.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
        where TConsumer : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var specification = new ConcurrencyLimitConsumePipeSpecification<Batch<TMessage>>(Limiter);
        configurator.Message(message => message.AddPipeSpecification(specification));
    }
}
