using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Loads externalized values for transport-independent activity arguments and compensation logs.</summary>
public sealed class ActivityMessageDataConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly bool _includeMessages;
    readonly IMessageDataRepository _repository;

    /// <summary>Initializes the observer.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="repository">The repository.</param>
    /// <param name="includeMessages">The include messages.</param>
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
        if (!_includeMessages)
            return;

        IPipeSpecification<ConsumeContext<TMessage>> specification = new GetMessageDataTransformSpecification<TMessage>(_repository);
        configurator.AddPipeSpecification(specification);
    }

    /// <inheritdoc />
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
    {
        AddExecuteSpecification(configurator);
    }

    /// <inheritdoc />
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        AddExecuteSpecification(configurator);
    }

    /// <inheritdoc />
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
    {
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
