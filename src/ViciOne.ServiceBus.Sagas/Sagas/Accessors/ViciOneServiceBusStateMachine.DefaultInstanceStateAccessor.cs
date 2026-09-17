using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>
    /// Lazily selects the saga's single public, non-indexed instance property whose type is exactly
    /// <see cref="IState"/> and which has both a getter and a setter, including non-public accessors.
    /// </summary>
    /// <remarks>
    /// The first read, write, predicate or probe selects the property. Selection throws a
    /// <see cref="SagaStateMachineException"/> if no property or multiple properties qualify.
    /// A read initializes a null state through the machine's initial-state transition behavior.
    /// </remarks>
    class DefaultInstanceStateAccessor :
        IStateAccessor<TInstance>
    {
        readonly Lazy<IStateAccessor<TInstance>> _accessor;
        readonly IState<TInstance> _initialState;
        readonly IStateMachine<TInstance> _machine;
        readonly IStateObserver<TInstance> _observer;

        public DefaultInstanceStateAccessor(IStateMachine<TInstance> machine, IState<TInstance> initialState, IStateObserver<TInstance> observer)
        {
            ArgumentNullException.ThrowIfNull(machine);
            ArgumentNullException.ThrowIfNull(initialState);
            ArgumentNullException.ThrowIfNull(observer);

            _machine = machine;
            _initialState = initialState;
            _observer = observer;
            _accessor = new Lazy<IStateAccessor<TInstance>>(CreateDefaultAccessor);
        }

        Task<IState<TInstance>?> IStateAccessor<TInstance>.GetAsync(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            return _accessor.Value.GetAsync(context, cancellationToken: cancellationToken);
        }

        Task IStateAccessor<TInstance>.SetAsync(IBehaviorContext<TInstance> context, IState<TInstance> state, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(state);
            return _accessor.Value.SetAsync(context, state, cancellationToken: cancellationToken);
        }

        public Expression<Func<TInstance, bool>> GetStateExpression(params IState[] states)
        {
            ArgumentNullException.ThrowIfNull(states);
            return _accessor.Value.GetStateExpression(states);
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _accessor.Value.Probe(context);
        }

        IStateAccessor<TInstance> CreateDefaultAccessor()
        {
            List<PropertyInfo> states = typeof(TInstance)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.PropertyType == typeof(IState))
                .Where(x => x.GetIndexParameters().Length == 0)
                .Where(x => x.GetGetMethod(true) != null)
                .Where(x => x.GetSetMethod(true) != null)
                .ToList();

            if (states.Count > 1)
            {
                throw new SagaStateMachineException(
                    "The InstanceState was not configured, and could not be automatically identified as multiple State properties exist.");
            }

            if (states.Count == 0)
            {
                throw new SagaStateMachineException(
                    "The InstanceState was not configured, and no public State property exists.");
            }

            var instance = Expression.Parameter(typeof(TInstance), "instance");
            var memberExpression = Expression.Property(instance, states[0]);

            Expression<Func<TInstance, IState?>> expression = Expression.Lambda<Func<TInstance, IState?>>(memberExpression,
                instance);

            return new InitialIfNullStateAccessor(_initialState, new RawStateAccessor(_machine, expression, _observer));
        }
    }
}
