namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.SlowConcurrentSaga
{
    using System.Threading.Tasks;
    using DataAccess;
    using Events;


    public class SlowConcurrentSagaStateMachine : ViciOneServiceBusStateMachine<SlowConcurrentSaga>
    {
        public SlowConcurrentSagaStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => Begin, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => IncrementCounterSlowly, x => x.CorrelateById(context => context.Message.CorrelationId));

            Initially(
                When(Begin)
                    .Then(context => context.Saga.Counter = 1)
                    .TransitionTo(Started));

            During(Started,
                When(IncrementCounterSlowly)
                    .ThenAsync(async context =>
                    {
                        await Task.Delay(5000);
                        context.Saga.Counter++;
                    })
                    .TransitionTo(DidIncrement));
        }

        public Event<Begin> Begin { get; set; }

        public Event<IncrementCounterSlowly> IncrementCounterSlowly { get; set; }

        public State Started { get; set; }

        public State DidIncrement { get; set; }
    }
}
