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
    class RawStateAccessor :
        IStateAccessor<TInstance>
    {
        readonly IStateMachine<TInstance> _machine;
        readonly IStateObserver<TInstance> _observer;
        readonly PropertyInfo _propertyInfo;
        readonly IReadProperty<TInstance, IState> _read;
        readonly IWriteProperty<TInstance, IState> _write;

        public RawStateAccessor(IStateMachine<TInstance> machine, Expression<Func<TInstance, IState?>> currentStateExpression,
            IStateObserver<TInstance> observer)
        {
            _machine = machine;
            _observer = observer;

            _propertyInfo = currentStateExpression.GetPropertyInfo();

            _read = ReadPropertyCache<TInstance>.GetProperty<IState>(_propertyInfo);
            _write = WritePropertyCache<TInstance>.GetProperty<IState>(_propertyInfo);
        }

        Task<IState<TInstance>?> IStateAccessor<TInstance>.GetAsync(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
        {
            var state = _read.Get(context.Saga);
            if (state == null)
                return Task.FromResult<IState<TInstance>?>(null);

            return Task.FromResult<IState<TInstance>?>(_machine.GetState(state.Name));
        }

        Task IStateAccessor<TInstance>.SetAsync(IBehaviorContext<TInstance> context, IState<TInstance> state, CancellationToken cancellationToken)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var previous = _read.Get(context.Saga);
            if (state.Equals(previous))
                return Task.CompletedTask;

            _write.Set(context.Saga, state);

            IState<TInstance>? previousState = null;
            if (previous != null)
                previousState = _machine.GetState(previous.Name);

            return _observer.StateChangedAsync(context, state, previousState);
        }

        public Expression<Func<TInstance, bool>> GetStateExpression(params IState[] states)
        {
            if (states == null || states.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(states), "One or more states must be specified");

            var parameterExpression = Expression.Parameter(typeof(TInstance), "instance");

            var getMethod = _propertyInfo.GetMethod
                ?? throw new InvalidOperationException($"The state property '{_propertyInfo.Name}' does not have a getter.");
            var statePropertyExpression = Expression.Property(parameterExpression, getMethod);

            var stateExpression = states.Select(state => Expression.Equal(statePropertyExpression,
                Expression.Constant(state, typeof(IState)))).Aggregate((left, right) => Expression.Or(left, right));

            return Expression.Lambda<Func<TInstance, bool>>(stateExpression, parameterExpression);
        }

        public void Probe(ProbeContext context)
        {
            context.Add("currentStateProperty", _propertyInfo.Name);
        }
    }
}
