using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Accesses the current state as a string property
    /// </summary>
    class IntStateAccessor :
        IStateAccessor<TInstance>
    {
        readonly StateAccessorIndex _index;
        readonly IStateObserver<TInstance> _observer;
        readonly PropertyInfo _propertyInfo;
        readonly IReadProperty<TInstance, int> _read;
        readonly IWriteProperty<TInstance, int> _write;

        public IntStateAccessor(Expression<Func<TInstance, int>> currentStateExpression, StateAccessorIndex index, IStateObserver<TInstance> observer)
        {
            _index = index;
            _observer = observer;

            _propertyInfo = currentStateExpression.GetPropertyInfo();

            _read = ReadPropertyCache<TInstance>.GetProperty<int>(_propertyInfo);
            _write = WritePropertyCache<TInstance>.GetProperty<int>(_propertyInfo);
        }

        Task<State<TInstance>?> IStateAccessor<TInstance>.GetAsync(BehaviorContext<TInstance> context, CancellationToken cancellationToken)
        {
            var stateIndex = _read.Get(context.Saga);

            return Task.FromResult(_index[stateIndex]);
        }

        Task IStateAccessor<TInstance>.SetAsync(BehaviorContext<TInstance> context, State<TInstance> state, CancellationToken cancellationToken)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var stateIndex = _index[state.Name];

            var previousIndex = _read.Get(context.Saga);

            if (stateIndex == previousIndex)
                return Task.CompletedTask;

            _write.Set(context.Saga, stateIndex);

            State<TInstance>? previousState = _index[previousIndex];

            return _observer.StateChangedAsync(context, state, previousState);
        }

        public Expression<Func<TInstance, bool>> GetStateExpression(params State[] states)
        {
            if (states == null || states.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(states), "One or more states must be specified");

            var parameterExpression = Expression.Parameter(typeof(TInstance), "instance");

            var getMethod = _propertyInfo.GetMethod
                ?? throw new InvalidOperationException($"The state property '{_propertyInfo.Name}' does not have a getter.");
            var statePropertyExpression = Expression.Property(parameterExpression, getMethod);

            var stateExpression = states.Select(state => Expression.Equal(statePropertyExpression, Expression.Constant(_index[state.Name])))
                .Aggregate((left, right) => Expression.Or(left, right));

            return Expression.Lambda<Func<TInstance, bool>>(stateExpression, parameterExpression);
        }

        public void Probe(ProbeContext context)
        {
            context.Add("currentStateProperty", _propertyInfo.Name);
        }
    }
}
