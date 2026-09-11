using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Loads externalized values for transport-independent activity arguments and compensation logs.</summary>
internal sealed class ActivityMessageDataConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly bool _includeMessages;
    readonly IMessageDataRepository _repository;

    /// <summary>Connects message-data loading to one consume-pipeline configurator.</summary>
    /// <param name="configurator">The consume configurator to observe.</param>
    /// <param name="repository">The repository that owns activity references.</param>
    /// <param name="includeMessages">Whether ordinary message contracts are also transformed.</param>
    public ActivityMessageDataConfigurationObserver(IConsumePipeConfigurator configurator, IMessageDataRepository repository,
        bool includeMessages)
        : base(configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
        _includeMessages = includeMessages;
        Connect(this);
    }

    /// <inheritdoc />
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (!_includeMessages)
            return;

        IPipeSpecification<ConsumeContext<TMessage>> specification = new GetMessageDataTransformSpecification<TMessage>(_repository);
        configurator.AddPipeSpecification(specification);
    }

    /// <inheritdoc />
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);
        AddExecuteSpecification(configurator);
    }

    /// <inheritdoc />
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        AddExecuteSpecification(configurator);
    }

    /// <inheritdoc />
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var specification = new GetMessageDataTransformSpecification<TLog>(_repository);
        configurator.Log(pipe => pipe.AddPipeSpecification(specification));
    }

    void AddExecuteSpecification<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        var specification = new GetMessageDataTransformSpecification<TArguments>(_repository);
        configurator.Arguments(pipe => pipe.AddPipeSpecification(specification));
    }
}
