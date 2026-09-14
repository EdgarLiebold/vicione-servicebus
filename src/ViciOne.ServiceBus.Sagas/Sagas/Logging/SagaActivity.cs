using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Monitoring;

namespace ViciOne.ServiceBus.Sagas.Logging;

/// <summary>Creates process activities for saga and state-machine message handling.</summary>
internal static class SagaActivity
{
    /// <summary>Starts a process activity for a message delivered to a saga.</summary>
    /// <typeparam name="TSaga">The saga state recorded as the processor.</typeparam>
    /// <typeparam name="TMessage">The message contract being processed.</typeparam>
    /// <param name="context">The saga consume context that supplies correlation and message metadata.</param>
    /// <returns>The started activity, or <see langword="null"/> when tracing is unavailable.</returns>
    public static StartedActivity? TryStart<TSaga, TMessage>(SagaConsumeContext<TSaga, TMessage> context)
        where TSaga : class, ISaga
        where TMessage : class =>
        MessageActivity.TryStartProcess(context, activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.SagaId, context.Saga.CorrelationId.ToString("D"));
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, TypeCache<TSaga>.ShortName);
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<TMessage>.DiagnosticAddress);
        });

    /// <summary>Starts a process activity for a message delivered to a saga state machine.</summary>
    /// <typeparam name="TInstance">The saga state recorded by the activity.</typeparam>
    /// <typeparam name="TMessage">The message contract being processed.</typeparam>
    /// <param name="context">The behavior context that supplies state-machine, correlation, and message metadata.</param>
    /// <returns>The started activity, or <see langword="null"/> when tracing is unavailable.</returns>
    public static StartedActivity? TryStartStateMachine<TInstance, TMessage>(IBehaviorContext<TInstance, TMessage> context)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class =>
        MessageActivity.TryStartProcess(context, activity =>
        {
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.SagaId, context.Saga.CorrelationId.ToString("D"));
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.ProcessorName, context.StateMachine.Name);
            ActivityObservation.TrySetTag(activity, ServiceBusTelemetry.Attributes.MessageContract, MessageTypeCache<TMessage>.DiagnosticAddress);
        });
}
