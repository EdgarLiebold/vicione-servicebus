using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Sagas.Logging;

/// <summary>Adds tracing and metrics for saga and state-machine execution.</summary>
public static class SagaLogContextExtensions
{
    /// <summary>Starts a tracing activity for a saga message.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="logContext">The log context.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The started activity, or <see langword="null" /> when tracing is disabled.</returns>
    public static StartedActivity? StartSagaActivity<TSaga, TMessage>(this ILogContext logContext,
        SagaConsumeContext<TSaga, TMessage> context)
        where TSaga : class, ISaga
        where TMessage : class
    {
        return LogContextActivityExtensions.StartActivity(context, activity =>
        {
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.SagaId, context.Saga.CorrelationId.ToString("D"));
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, TypeCache<TSaga>.ShortName);
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<TMessage>.DiagnosticAddress);
        });
    }

    /// <summary>Starts a tracing activity for a saga state-machine message.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="logContext">The log context.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The started activity, or <see langword="null" /> when tracing is disabled.</returns>
    public static StartedActivity? StartSagaStateMachineActivity<TInstance, TMessage>(this ILogContext logContext,
        BehaviorContext<TInstance, TMessage> context)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return LogContextActivityExtensions.StartActivity(context, activity =>
        {
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.SagaId, context.Saga.CorrelationId.ToString("D"));
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.ConsumerType, context.StateMachine.Name);
            ActivityObservation.TrySetTag(activity, DiagnosticHeaders.PeerAddress, MessageTypeCache<TMessage>.DiagnosticAddress);
        });
    }

    /// <summary>Starts metrics collection for a saga message.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="logContext">The log context.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The active metric operation, or <see langword="null" /> when metrics are disabled.</returns>
    public static MetricOperation? StartSagaInstrument<TSaga, TMessage>(this ILogContext logContext,
        SagaConsumeContext<TSaga, TMessage> context)
        where TSaga : class, ISaga
        where TMessage : class =>
        LogContextInstrumentationExtensions.StartProcess(logContext, context, "saga", "saga");

    /// <summary>Starts metrics collection for a saga state-machine message.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="logContext">The log context.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The active metric operation, or <see langword="null" /> when metrics are disabled.</returns>
    public static MetricOperation? StartSagaStateMachineInstrument<TInstance, TMessage>(this ILogContext logContext,
        BehaviorContext<TInstance, TMessage> context)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class =>
        LogContextInstrumentationExtensions.StartProcess(logContext, context, "saga", "saga_state_machine");
}
