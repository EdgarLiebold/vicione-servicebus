// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ReliableMessaging
{
    using ViciOne.ServiceBus.TestFramework;


    public class ReliableStateMachine :
        ViciOneServiceBusStateMachine<ReliableState>
    {
        public ReliableStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Initially(
                When(CreateState)
                    .TransitionTo(Created)
                    .Send(context => context.ReceiveContext.InputAddress, context => new StateVerified() { CorrelationId = context.Saga.CorrelationId },
                        (x, sendContext) => sendContext.Headers.Set("ViciOne-ServiceBus-Fail-Delivery", x.Message.FailMessageDelivery && x.GetRetryAttempt() == 0))
                    .If(context => context.Message.FailOnFirstAttempt && context.GetRetryAttempt() == 0,
                        fail => fail.Then(context => throw new IntentionalTestException()))
            );

            During(Created,
                When(StateVerified)
                    .TransitionTo(Verified)
            );
        }

        //
        // ReSharper disable UnassignedGetOnlyAutoProperty
        // ReSharper disable MemberCanBePrivate.Global
        public State Created { get; }
        public State Verified { get; }
        public Event<CreateState> CreateState { get; }
        public Event<StateVerified> StateVerified { get; }
    }
}
