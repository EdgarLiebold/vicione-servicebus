using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a message event correlation implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageEventCorrelation<TSaga, TMessage> :
    EventCorrelation<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly Lazy<bool> _includesInitial;
    readonly bool _insertOnInitial;
    readonly SagaStateMachine<TSaga> _machine;
    readonly IPipe<ConsumeContext<TMessage>> _missingPipe;
    readonly Lazy<ISagaPolicy<TSaga, TMessage>> _policy;
    readonly bool _readOnly;
    readonly ISagaFactory<TSaga, TMessage> _sagaFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="machine">The machine value.</param>
    /// <param name="event">The event value.</param>
    /// <param name="sagaFilterFactory">The saga filter factory value.</param>
    /// <param name="messageFilter">The message filter value.</param>
    /// <param name="missingPipe">The missing pipe value.</param>
    /// <param name="sagaFactory">The saga factory value.</param>
    /// <param name="insertOnInitial">The insert on initial value.</param>
    /// <param name="readOnly">The read only value.</param>
    /// <param name="configureConsumeTopology">The configure consume topology value.</param>
    public MessageEventCorrelation(SagaStateMachine<TSaga> machine, Event<TMessage> @event, SagaFilterFactory<TSaga, TMessage>? sagaFilterFactory,
        IFilter<ConsumeContext<TMessage>>? messageFilter, IPipe<ConsumeContext<TMessage>> missingPipe, ISagaFactory<TSaga, TMessage> sagaFactory,
        bool insertOnInitial, bool readOnly, bool configureConsumeTopology)
    {
        Event = @event;
        FilterFactory = sagaFilterFactory;
        MessageFilter = messageFilter;
        _missingPipe = missingPipe;
        _sagaFactory = sagaFactory;
        _insertOnInitial = insertOnInitial;
        _readOnly = readOnly;
        ConfigureConsumeTopology = configureConsumeTopology;
        _machine = machine;

        _policy = new Lazy<ISagaPolicy<TSaga, TMessage>>(GetSagaPolicy);
        _includesInitial = new Lazy<bool>(() => IncludesInitial());
    }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology { get; }

    /// <summary>
    /// Gets the filter factory value.
    /// </summary>
    public SagaFilterFactory<TSaga, TMessage>? FilterFactory { get; }

    /// <summary>
    /// Gets the event value.
    /// </summary>
    public Event<TMessage> Event { get; }

    /// <summary>
    /// Gets the data type value.
    /// </summary>
    public Type DataType => typeof(TMessage);

    /// <summary>
    /// Gets the message filter value.
    /// </summary>
    public IFilter<ConsumeContext<TMessage>>? MessageFilter { get; }

    /// <summary>
    /// Gets the policy value.
    /// </summary>
    public ISagaPolicy<TSaga, TMessage> Policy => _policy.Value;

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_insertOnInitial && _readOnly)
            yield return this.Failure("ReadOnly", "ReadOnly cannot be set when InsertOnInitial is true");

        if (_includesInitial.Value && _readOnly)
            yield return this.Failure("ReadOnly", "ReadOnly cannot be used for events in the initial state");
    }

    ISagaPolicy<TSaga, TMessage> GetSagaPolicy()
    {
        if (_includesInitial.Value)
            return new NewOrExistingSagaPolicy<TSaga, TMessage>(_sagaFactory, _insertOnInitial);

        return new AnyExistingSagaPolicy<TSaga, TMessage>(_missingPipe, _readOnly);
    }

    bool IncludesInitial()
    {
        return _machine.NextEvents(_machine.Initial).Contains(Event);
    }
}
