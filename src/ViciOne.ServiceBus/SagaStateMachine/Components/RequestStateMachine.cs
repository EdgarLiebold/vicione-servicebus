namespace ViciOne.ServiceBus.Components
{
    using System;
    using Configuration;
    using Contracts;
    using SagaStateMachine;


    /// <summary>
    /// Tracks a request, which was sent to a saga, and the saga deferred until some operation
    /// is completed, after which it will produce an event to trigger the response.
    /// </summary>
    public class RequestStateMachine :
        ViciOneServiceBusStateMachine<RequestState>
    {
        public RequestStateMachine(Action<IMissingInstanceRedeliveryConfigurator> configureMissingInstanceRedelivery = null)
        {
            IRequestStateMachineMissingInstanceConfigurator missingInstanceConfigurator = configureMissingInstanceRedelivery == null
                ? null
                : new RedeliverRequestStateMachineSpecification(configureMissingInstanceRedelivery);

            InstanceState(x => x.CurrentState, Pending);

            Event(() => Started, x =>
            {
                x.CorrelateById(m => m.Message.RequestId);
            });

            Event(() => Completed, x =>
            {
                x.CorrelateById(m => m.SagaCorrelationId, i => i.Message.CorrelationId);

                if (missingInstanceConfigurator != null)
                    x.OnMissingInstance(m => missingInstanceConfigurator.Apply(m));
            });

            Event(() => Faulted, x =>
            {
                x.CorrelateById(m => m.SagaCorrelationId, i => i.Message.CorrelationId);

                if (missingInstanceConfigurator != null)
                    x.OnMissingInstance(m => missingInstanceConfigurator.Apply(m));
            });

            Initially(
                When(Started)
                    .Then(InitializeInstance)
                    .TransitionTo(Pending));

            During(Pending,
                When(Completed)
                    .Execute(x => new CompleteRequestActivity())
                    .Finalize(),
                When(Faulted)
                    .Execute(x => new FaultRequestActivity())
                    .Finalize());

            SetCompletedWhenFinalized();
        }

        //
        // ReSharper disable UnassignedGetOnlyAutoProperty
        // ReSharper disable MemberCanBePrivate.Global
        public State Pending { get; }

        public Event<RequestStarted> Started { get; }
        public Event<RequestCompleted> Completed { get; }
        public Event<RequestFaulted> Faulted { get; }

        static void InitializeInstance(BehaviorContext<RequestState, RequestStarted> context)
        {
            context.Saga.ConversationId = context.ConversationId;
            context.Saga.ResponseAddress = context.Message.ResponseAddress;
            context.Saga.FaultAddress = context.Message.FaultAddress;
            context.Saga.ExpirationTime = context.Message.ExpirationTime;

            context.Saga.SagaCorrelationId = context.Message.CorrelationId;
            context.Saga.SagaAddress = context.SourceAddress;
        }

    }
}
