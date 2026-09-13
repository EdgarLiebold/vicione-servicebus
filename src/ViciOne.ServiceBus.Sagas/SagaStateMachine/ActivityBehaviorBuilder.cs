using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Builds activity behavior components.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ActivityBehaviorBuilder<TSaga> :
    IBehaviorBuilder<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly List<IStateMachineActivity<TSaga>> _activities;
    readonly Lazy<IBehavior<TSaga>> _behavior;

    /// <summary>Initializes a new instance.</summary>
    public ActivityBehaviorBuilder()
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

        IBehavior<TSaga> current = new LastBehavior<TSaga>(_activities[_activities.Count - 1]);

        for (var i = _activities.Count - 2; i >= 0; i--)
            current = new ActivityBehavior<TSaga>(_activities[i], current);

        return current;
    }
}
