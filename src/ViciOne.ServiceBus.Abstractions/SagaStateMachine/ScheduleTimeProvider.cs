using System;

namespace ViciOne.ServiceBus;

public delegate DateTimeOffset ScheduleTimeProvider<TSaga>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance;


public delegate DateTimeOffset ScheduleTimeProvider<TSaga, in TMessage>(BehaviorContext<TSaga, TMessage> context)
    where TMessage : class
    where TSaga : class, SagaStateMachineInstance;
