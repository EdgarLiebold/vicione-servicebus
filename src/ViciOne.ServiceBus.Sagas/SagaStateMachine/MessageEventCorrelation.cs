using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Associates a state-machine message event with correlation filters and lazily selected saga repository policy.</summary>
/// <typeparam name="TSaga">The saga state handled by the state machine.</typeparam>
/// <typeparam name="TMessage">The message contract carried by the correlated event.</typeparam>
public class MessageEventCorrelation<TSaga, TMessage> :
    IEventCorrelation<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly Lazy<bool> _includesInitial;
    readonly bool _insertOnInitial;
    readonly ISagaStateMachine<TSaga> _machine;
    readonly IPipe<ConsumeContext<TMessage>>? _missingPipe;
    readonly Lazy<ISagaPolicy<TSaga, TMessage>> _policy;
    readonly bool _readOnly;
    readonly ISagaFactory<TSaga, TMessage> _sagaFactory;

    /// <summary>Stores event correlation configuration and defers state-machine policy selection until it is needed.</summary>
    /// <param name="machine">The state machine determining whether the event is handled in its initial state.</param>
    /// <param name="event">The event receiving the correlated message.</param>
    /// <param name="sagaFilterFactory">The optional factory composing correlated repository dispatch.</param>
    /// <param name="messageFilter">The optional message filter applied before repository dispatch.</param>
    /// <param name="missingPipe">The optional message pipeline used when existing-instance dispatch finds no saga.</param>
    /// <param name="sagaFactory">The factory creating saga instances for initial-state dispatch.</param>
    /// <param name="insertOnInitial">Whether initial-state dispatch requests saga creation before repository dispatch.</param>
    /// <param name="readOnly">Whether existing-instance dispatch uses a read-only repository policy.</param>
    /// <param name="configureConsumeTopology">Whether connecting the event's message pipeline configures consume topology.</param>
    /// <exception cref="ArgumentNullException"><paramref name="machine" />, <paramref name="event" />, or
    /// <paramref name="sagaFactory" /> is null.</exception>
    public MessageEventCorrelation(ISagaStateMachine<TSaga> machine, IEvent<TMessage> @event, SagaFilterFactory<TSaga, TMessage>? sagaFilterFactory,
        IFilter<ConsumeContext<TMessage>>? messageFilter, IPipe<ConsumeContext<TMessage>>? missingPipe, ISagaFactory<TSaga, TMessage> sagaFactory,
        bool insertOnInitial, bool readOnly, bool configureConsumeTopology)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(@event, "event");
        ArgumentNullException.ThrowIfNull(sagaFactory);

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

    /// <summary>Gets whether connecting the event's message pipeline configures consume topology.</summary>
    public bool ConfigureConsumeTopology { get; }

    /// <summary>Gets the optional factory composing correlated saga repository dispatch.</summary>
    public SagaFilterFactory<TSaga, TMessage>? FilterFactory { get; }

    /// <summary>Gets the state-machine event receiving correlated messages.</summary>
    public IEvent<TMessage> Event { get; }

    /// <summary>Gets the message contract carried by the event.</summary>
    public Type DataType => typeof(TMessage);

    /// <summary>Gets the optional filter applied to messages before saga repository dispatch.</summary>
    public IFilter<ConsumeContext<TMessage>>? MessageFilter { get; }

    /// <summary>Gets a creation-capable policy for initial-state events or an existing-instance policy for other events.</summary>
    public ISagaPolicy<TSaga, TMessage> Policy => _policy.Value;

    /// <summary>Rejects read-only configuration combined with pre-insertion or initial-state event handling.</summary>
    /// <returns>The failures for incompatible read-only policy settings.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_insertOnInitial && _readOnly)
            yield return this.Failure("ReadOnly", "ReadOnly cannot be set when InsertOnInitial is true");

        if (_readOnly && _includesInitial.Value)
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
