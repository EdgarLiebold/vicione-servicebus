using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Builds catch behavior components.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class CatchBehaviorBuilder<TSaga> :
    IBehaviorBuilder<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly List<IStateMachineActivity<TSaga>> _activities;
    readonly Lazy<IBehavior<TSaga>> _behavior;

    /// <summary>Initializes a new instance.</summary>
    public CatchBehaviorBuilder()
    {
        _activities = new List<IStateMachineActivity<TSaga>>();
        _behavior = new Lazy<IBehavior<TSaga>>(CreateBehavior);
    }

    /// <summary>Gets the behavior.</summary>
    public IBehavior<TSaga> Behavior => _behavior.Value;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    public void Add(IStateMachineActivity<TSaga> activity)
    {
        if (_behavior.IsValueCreated)
            throw new SagaStateMachineException("The behavior was already built, additional activities cannot be added.");

        _activities.Add(activity);
    }

    IBehavior<TSaga> CreateBehavior()
    {
        if (_activities.Count == 0)
            return SagaStateMachine.Behavior.Empty<TSaga>();

        IBehavior<TSaga> current = new LastCatchBehavior(_activities[_activities.Count - 1]);

        for (var i = _activities.Count - 2; i >= 0; i--)
            current = new ActivityBehavior<TSaga>(_activities[i], current);

        return current;
    }


    class LastCatchBehavior :
        IBehavior<TSaga>
    {
        readonly IStateMachineActivity<TSaga> _activity;

        public LastCatchBehavior(IStateMachineActivity<TSaga> activity)
        {
            _activity = activity;
        }

        public void Accept(IStateMachineVisitor visitor)
        {
            _activity.Accept(visitor);
        }

        public void Probe(ProbeContext context)
        {
            _activity.Probe(context);
        }

        public Task ExecuteAsync(IBehaviorContext<TSaga> context)
        {
            return _activity.ExecuteAsync(context, SagaStateMachine.Behavior.Empty<TSaga>());
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context)
            where T : class
        {
            return _activity.ExecuteAsync(context, SagaStateMachine.Behavior.Empty<TSaga, T>());
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context)
            where T : class
            where TException : Exception
        {
            return _activity.FaultedAsync(context, SagaStateMachine.Behavior.Empty<TSaga, T>());
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context)
            where TException : Exception
        {
            return _activity.FaultedAsync(context, SagaStateMachine.Behavior.Empty<TSaga>());
        }
    }
}
