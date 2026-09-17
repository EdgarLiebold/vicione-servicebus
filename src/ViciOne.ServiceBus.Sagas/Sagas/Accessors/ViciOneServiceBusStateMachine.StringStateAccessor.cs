using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Accesses the current state as a string property.</summary>
    class StringStateAccessor :
        IStateAccessor<TInstance>
    {
        readonly IStateMachine<TInstance> _machine;
        readonly IStateObserver<TInstance> _observer;
        readonly PropertyInfo _propertyInfo;
        readonly IReadProperty<TInstance, string> _read;
        readonly IWriteProperty<TInstance, string> _write;

        public StringStateAccessor(IStateMachine<TInstance> machine, Expression<Func<TInstance, string>> currentStateExpression,
            IStateObserver<TInstance> observer)
        {
            ArgumentNullException.ThrowIfNull(machine);
            ArgumentNullException.ThrowIfNull(currentStateExpression);
            ArgumentNullException.ThrowIfNull(observer);

            _machine = machine;
            _observer = observer;

            _propertyInfo = currentStateExpression.GetPropertyInfo();

            _read = ReadPropertyCache<TInstance>.GetProperty<string>(_propertyInfo);
            _write = WritePropertyCache<TInstance>.GetProperty<string>(_propertyInfo);
        }

        Task<IState<TInstance>?> IStateAccessor<TInstance>.GetAsync(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);

            var stateName = _read.Get(context.Saga);
            if (string.IsNullOrWhiteSpace(stateName))
                return Task.FromResult<IState<TInstance>?>(null);

            return Task.FromResult<IState<TInstance>?>(_machine.GetState(stateName));
        }

        Task IStateAccessor<TInstance>.SetAsync(IBehaviorContext<TInstance> context, IState<TInstance> state, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(state);

            var previous = _read.Get(context.Saga);
            if (state.Name.Equals(previous))
                return Task.CompletedTask;

            _write.Set(context.Saga, state.Name);

            IState<TInstance>? previousState = null;
            if (!string.IsNullOrWhiteSpace(previous))
                previousState = _machine.GetState(previous);

            return _observer.StateChangedAsync(context, state, previousState)
                ?? throw new InvalidOperationException("The state observer returned no notification task.");
        }

        public Expression<Func<TInstance, bool>> GetStateExpression(params IState[] states)
        {
            ArgumentNullException.ThrowIfNull(states);
            if (states.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(states), "One or more states must be specified");
            if (states.Any(state => state is null))
                throw new ArgumentException("States must not contain null values.", nameof(states));

            var parameterExpression = Expression.Parameter(typeof(TInstance), "instance");

            var getMethod = _propertyInfo.GetMethod
                ?? throw new InvalidOperationException($"The state property '{_propertyInfo.Name}' does not have a getter.");
            var statePropertyExpression = Expression.Property(parameterExpression, getMethod);

            var stateExpression = states.Select(state => Expression.Equal(statePropertyExpression, Expression.Constant(state.Name)))
                .Aggregate((left, right) => Expression.Or(left, right));

            return Expression.Lambda<Func<TInstance, bool>>(stateExpression, parameterExpression);
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            context.Add("currentStateProperty", _propertyInfo.Name);
        }
    }
}
