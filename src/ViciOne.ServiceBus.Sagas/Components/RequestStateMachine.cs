using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Components;

/// <summary>
/// Tracks a request, which was sent to a saga, and the saga deferred until some operation
/// is completed, after which it will produce an event to trigger the response.
/// </summary>
public class RequestStateMachine :
    ViciOneServiceBusStateMachine<RequestState>
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="configureMissingInstanceRedelivery">The configure missing instance redelivery.</param>
    public RequestStateMachine(Action<IMissingInstanceRedeliveryConfigurator>? configureMissingInstanceRedelivery = null)
    {
        IRequestStateMachineMissingInstanceConfigurator? missingInstanceConfigurator = configureMissingInstanceRedelivery == null
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

    /// <summary>Gets the pending.</summary>
    public State Pending { get; } = null!;

    /// <summary>Gets the started.</summary>
    public Event<RequestStarted> Started { get; } = null!;
    /// <summary>Gets the completed.</summary>
    public Event<RequestCompleted> Completed { get; } = null!;
    /// <summary>Gets the faulted.</summary>
    public Event<RequestFaulted> Faulted { get; } = null!;
    static void InitializeInstance(BehaviorContext<RequestState, RequestStarted> context)
    {
        context.Saga.ConversationId = context.ConversationId;
        context.Saga.ResponseAddress = context.Message.ResponseAddress;
        context.Saga.FaultAddress = context.Message.FaultAddress;
        context.Saga.ExpirationTime = context.Message.ExpirationTime;

        context.Saga.SagaCorrelationId = context.Message.CorrelationId;
        context.Saga.SagaAddress = context.SourceAddress
            ?? throw new InvalidOperationException("A source address is required when a saga request is started.");
    }

}
