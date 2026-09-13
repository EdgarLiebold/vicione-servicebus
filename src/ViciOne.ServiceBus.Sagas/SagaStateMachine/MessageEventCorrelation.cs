using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Defines correlation for message event.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageEventCorrelation<TSaga, TMessage> :
    IEventCorrelation<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly Lazy<bool> _includesInitial;
    readonly bool _insertOnInitial;
    readonly ISagaStateMachine<TSaga> _machine;
    readonly IPipe<ConsumeContext<TMessage>> _missingPipe;
    readonly Lazy<ISagaPolicy<TSaga, TMessage>> _policy;
    readonly bool _readOnly;
    readonly ISagaFactory<TSaga, TMessage> _sagaFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    /// <param name="sagaFilterFactory">The saga filter factory.</param>
    /// <param name="messageFilter">The message filter.</param>
    /// <param name="missingPipe">The missing pipe.</param>
    /// <param name="sagaFactory">The saga factory.</param>
    /// <param name="insertOnInitial">The insert on initial.</param>
    /// <param name="readOnly">The read only.</param>
    /// <param name="configureConsumeTopology">The configure consume topology.</param>
    public MessageEventCorrelation(ISagaStateMachine<TSaga> machine, IEvent<TMessage> @event, SagaFilterFactory<TSaga, TMessage>? sagaFilterFactory,
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

    /// <summary>Gets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology { get; }

    /// <summary>Gets the filter factory.</summary>
    public SagaFilterFactory<TSaga, TMessage>? FilterFactory { get; }

    /// <summary>Gets the event.</summary>
    public IEvent<TMessage> Event { get; }

    /// <summary>Gets the data type.</summary>
    public Type DataType => typeof(TMessage);

    /// <summary>Gets the message filter.</summary>
    public IFilter<ConsumeContext<TMessage>>? MessageFilter { get; }

    /// <summary>Gets the policy.</summary>
    public ISagaPolicy<TSaga, TMessage> Policy => _policy.Value;

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
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
