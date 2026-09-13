using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Carries state for state machine.</summary>
    public class StateMachineState :
        IState<TInstance>,
        IEquatable<IState>
    {
        readonly Dictionary<IEvent, ActivityBehaviorBuilder<TInstance>> _behaviors;
        readonly Dictionary<IEvent, IStateEventFilter<TInstance>> _ignoredEvents;
        readonly IEventObserver<TInstance> _observer;
        readonly HashSet<IState<TInstance>> _subStates;
        readonly StateMachineUnhandledEventCallback<TInstance> _unhandledEventCallback;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="unhandledEventCallback">The unhandled event callback.</param>
        /// <param name="name">The name.</param>
        /// <param name="observer">The observer to connect.</param>
        /// <param name="superState">The super state.</param>
        public StateMachineState(StateMachineUnhandledEventCallback<TInstance> unhandledEventCallback, string name, IEventObserver<TInstance> observer,
            IState<TInstance>? superState = null)
        {
            _unhandledEventCallback = unhandledEventCallback;
            Name = name;
            _observer = observer;

            _behaviors = new Dictionary<IEvent, ActivityBehaviorBuilder<TInstance>>();
            _ignoredEvents = new Dictionary<IEvent, IStateEventFilter<TInstance>>();

            Enter = new TriggerEvent(name + ".Enter");
            Ignore(Enter);
            Leave = new TriggerEvent(name + ".Leave");
            Ignore(Leave);

            BeforeEnter = new MessageEvent<IState>(name + ".BeforeEnter");
            Ignore(BeforeEnter);
            AfterLeave = new MessageEvent<IState>(name + ".AfterLeave");
            Ignore(AfterLeave);

            _subStates = new HashSet<IState<TInstance>>();

            SuperState = superState;
            superState?.AddSubstate(this);
        }

        /// <summary>Determines whether this instance equals the supplied value.</summary>
        /// <param name="other">The other.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool Equals(IState? other)
        {
            return string.CompareOrdinal(Name, other?.Name ?? "") == 0;
        }

        /// <inheritdoc />
        public IState<TInstance>? SuperState { get; }
        /// <summary>Gets the name.</summary>
        public string Name { get; }

        /// <summary>Gets the enter.</summary>
        public IEvent Enter { get; }
        /// <summary>Gets the leave.</summary>
        public IEvent Leave { get; }
        /// <summary>Gets the before enter.</summary>
        public IEvent<IState> BeforeEnter { get; }
        /// <summary>Gets the after leave.</summary>
        public IEvent<IState> AfterLeave { get; }

        /// <summary>Accepts the supplied value.</summary>
        /// <param name="visitor">The visitor.</param>
        public void Accept(IStateMachineVisitor visitor)
        {
            visitor.Visit(this, _ =>
            {
                foreach (KeyValuePair<IEvent, ActivityBehaviorBuilder<TInstance>> behavior in _behaviors)
                {
                    behavior.Key.Accept(visitor);
                    behavior.Value.Behavior.Accept(visitor);
                }
            });
        }

        /// <summary>Writes diagnostic information to the probe context.</summary>
        /// <param name="context">The context associated with the operation.</param>
        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("state");
            scope.Add("name", Name);

            if (_subStates.Any())
            {
                var subStateScope = scope.CreateScope("substates");
                foreach (IState<TInstance> subState in _subStates)
                    subStateScope.Add("name", subState.Name);
            }

            if (_behaviors.Any())
            {
                foreach (KeyValuePair<IEvent, ActivityBehaviorBuilder<TInstance>> behavior in _behaviors)
                {
                    var eventScope = scope.CreateScope("event");
                    behavior.Key.Probe(eventScope);

                    behavior.Value.Behavior.Probe(eventScope.CreateScope("behavior"));
                }
            }

            List<KeyValuePair<IEvent, IStateEventFilter<TInstance>>> ignored = _ignoredEvents.Where(x => IsRealEvent(x.Key)).ToList();
            if (ignored.Any())
            {
                foreach (KeyValuePair<IEvent, IStateEventFilter<TInstance>> ignoredEvent in ignored)
                    ignoredEvent.Key.Probe(scope.CreateScope("event-ignored"));
            }
        }

        async Task IState<TInstance>.RaiseAsync(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            if (!_behaviors.TryGetValue(context.Event, out ActivityBehaviorBuilder<TInstance>? activities))
            {
                if (_ignoredEvents.TryGetValue(context.Event, out IStateEventFilter<TInstance>? filter) && filter.Filter(context))
                    return;

                if (SuperState != null)
                {
                    try
                    {
                        await SuperState.RaiseAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (UnhandledEventException)
                    {
                        // The substate owns the more specific unhandled-event decision.
                    }
                }

                await _unhandledEventCallback(context, this).ConfigureAwait(false);
                return;
            }

            try
            {
                await _observer.PreExecuteAsync(context).ConfigureAwait(false);

                await activities.Behavior.ExecuteAsync(context).ConfigureAwait(false);

                await _observer.PostExecuteAsync(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _observer.ExecuteFaultAsync(context, ex).ConfigureAwait(false);

                throw;
            }
        }

        async Task IState<TInstance>.RaiseAsync<T>(IBehaviorContext<TInstance, T> context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            if (!_behaviors.TryGetValue(context.Event, out ActivityBehaviorBuilder<TInstance>? activities))
            {
                if (_ignoredEvents.TryGetValue(context.Event, out IStateEventFilter<TInstance>? filter) && filter.Filter(context))
                    return;

                if (SuperState != null)
                {
                    try
                    {
                        await SuperState.RaiseAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (UnhandledEventException)
                    {
                        // The substate owns the more specific unhandled-event decision.
                    }
                }

                await _unhandledEventCallback(context, this).ConfigureAwait(false);
                return;
            }

            try
            {
                await _observer.PreExecuteAsync(context).ConfigureAwait(false);

                await activities.Behavior.ExecuteAsync(context).ConfigureAwait(false);

                await _observer.PostExecuteAsync(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _observer.ExecuteFaultAsync(context, ex).ConfigureAwait(false);

                throw;
            }
        }

        /// <summary>Binds the configured entities.</summary>
        /// <param name="event">The event.</param>
        /// <param name="activity">The activity.</param>
        public void Bind(IEvent @event, IStateMachineActivity<TInstance> activity)
        {
            if (!_behaviors.TryGetValue(@event, out ActivityBehaviorBuilder<TInstance>? builder))
            {
                builder = new ActivityBehaviorBuilder<TInstance>();
                _behaviors.Add(@event, builder);
            }

            builder.Add(activity);
        }

        /// <summary>Ignores the selected event or message.</summary>
        /// <param name="event">The event.</param>
        public void Ignore(IEvent @event)
        {
            _ignoredEvents[@event] = new AllStateEventFilter<TInstance>();
        }

        /// <summary>Ignores the selected event or message.</summary>
        /// <typeparam name="T">The message contract carried by the event.</typeparam>
        /// <param name="event">The event.</param>
        /// <param name="filter">The filter to add to the pipeline.</param>
        public void Ignore<T>(IEvent<T> @event, StateMachineCondition<TInstance, T> filter)
            where T : class
        {
            _ignoredEvents[@event] = new SelectedStateEventFilter<TInstance, T>(filter);
        }

        /// <summary>Adds substate to the configuration.</summary>
        /// <param name="subState">The sub state.</param>
        public void AddSubstate(IState<TInstance> subState)
        {
            if (subState == null)
                throw new ArgumentNullException(nameof(subState));

            if (Name.Equals(subState.Name))
                throw new ArgumentException("A state cannot be a substate of itself", nameof(subState));

            _subStates.Add(subState);
        }

        /// <summary>Determines whether the current value has state.</summary>
        /// <param name="state">The state.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool HasState(IState<TInstance> state)
        {
            return Name.Equals(state.Name) || _subStates.Any(s => s.HasState(state));
        }

        /// <summary>Determines whether state of.</summary>
        /// <param name="state">The state.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool IsStateOf(IState<TInstance> state)
        {
            return Name.Equals(state.Name) || (SuperState != null && SuperState.IsStateOf(state));
        }

        /// <inheritdoc />
        public IEnumerable<IEvent> Events => SuperState != null ? SuperState.Events.Union(GetStateEvents()).Distinct() : GetStateEvents();

        /// <inheritdoc />
        public IEnumerable<IEvent> DeclaredEvents => _behaviors.Keys
            .Union(_ignoredEvents.Keys.Where(IsRealEvent))
            .Distinct();

        /// <summary>Compares this instance with the supplied value.</summary>
        /// <param name="other">The other.</param>
        /// <returns>The int produced by the operation.</returns>
        public int CompareTo(IState? other)
        {
            return other == null ? 1 : string.CompareOrdinal(Name, other.Name);
        }

        bool IsRealEvent(IEvent @event)
        {
            if (Equals(@event, Enter) || Equals(@event, Leave) || Equals(@event, BeforeEnter) || Equals(@event, AfterLeave))
                return false;

            return true;
        }

        IEnumerable<IEvent> GetStateEvents()
        {
            return _behaviors.Keys
                .Union(_ignoredEvents.Keys)
                .Where(IsRealEvent)
                .Distinct();
        }

        /// <summary>Determines whether this instance equals the supplied value.</summary>
        /// <param name="obj">The obj.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as IState;
            return other != null && Equals(other);
        }

        /// <summary>Gets hash code.</summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }

        /// <summary>Applies the <c>==</c> operator.</summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(IState<TInstance> left, StateMachineState right)
        {
            return Equals(left, right);
        }

        /// <summary>Applies the <c>!=</c> operator.</summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(IState<TInstance> left, StateMachineState right)
        {
            return !Equals(left, right);
        }

        /// <summary>Applies the <c>==</c> operator.</summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(StateMachineState left, IState<TInstance> right)
        {
            return Equals(left, right);
        }

        /// <summary>Applies the <c>!=</c> operator.</summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(StateMachineState left, IState<TInstance> right)
        {
            return !Equals(left, right);
        }

        /// <summary>Applies the <c>==</c> operator.</summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(StateMachineState left, StateMachineState right)
        {
            return Equals(left, right);
        }

        /// <summary>Applies the <c>!=</c> operator.</summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(StateMachineState left, StateMachineState right)
        {
            return !Equals(left, right);
        }

        /// <summary>Returns the string representation of this instance.</summary>
        /// <returns>The converted string.</returns>
        public override string ToString()
        {
            return $"{Name} (State)";
        }
    }
}
