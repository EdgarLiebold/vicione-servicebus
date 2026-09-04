using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>
/// Provides a courier message data configuration observer implementation.
/// </summary>
public class CourierMessageDataConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly bool _includeMessages;
    readonly IMessageDataRepository _repository;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="repository">The repository value.</param>
    /// <param name="includeMessages">The include messages value.</param>
    public CourierMessageDataConfigurationObserver(IConsumePipeConfigurator configurator, IMessageDataRepository repository, bool includeMessages)
        : base(configurator)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        _repository = repository;
        _includeMessages = includeMessages;

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
        if (!_includeMessages)
            return;

        IPipeSpecification<ConsumeContext<TMessage>> specification = new GetMessageDataTransformSpecification<TMessage>(_repository);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        IPipeSpecification<ExecuteContext<TArguments>> specification = new GetMessageDataTransformSpecification<TArguments>(_repository);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
    {
        IPipeSpecification<ExecuteContext<TArguments>> specification = new GetMessageDataTransformSpecification<TArguments>(_repository);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
    {
        IPipeSpecification<CompensateContext<TLog>> specification = new GetMessageDataTransformSpecification<TLog>(_repository);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }
}
