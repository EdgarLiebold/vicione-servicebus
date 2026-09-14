using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Monitoring;

namespace ViciOne.ServiceBus.Sagas.Logging;

/// <summary>Starts saga metric scopes through the meter bound to a logging context.</summary>
internal static class SagaLogContextExtensions
{
    /// <summary>Starts metrics collection for a saga message.</summary>
    /// <typeparam name="TSaga">The saga state that handles the message.</typeparam>
    /// <typeparam name="TMessage">The message contract being processed.</typeparam>
    /// <param name="logContext">The logging context bound to the active meter.</param>
    /// <param name="context">The saga delivery measured by the operation.</param>
    /// <returns>The active metric operation, or <see langword="null" /> when metrics are disabled.</returns>
    public static MetricOperation? TryStartSagaMetrics<TSaga, TMessage>(this ILogContext logContext,
        SagaConsumeContext<TSaga, TMessage> context)
        where TSaga : class, ISaga
        where TMessage : class =>
        LogContextMetricsExtensions.StartProcess(logContext, context, "saga", "saga");

    /// <summary>Starts metrics collection for a saga state-machine message.</summary>
    /// <typeparam name="TInstance">The state-machine saga instance handling the message.</typeparam>
    /// <typeparam name="TMessage">The message contract being processed.</typeparam>
    /// <param name="logContext">The logging context bound to the active meter.</param>
    /// <param name="context">The state-machine behavior measured by the operation.</param>
    /// <returns>The active metric operation, or <see langword="null" /> when metrics are disabled.</returns>
    public static MetricOperation? TryStartSagaStateMachineMetrics<TInstance, TMessage>(this ILogContext logContext,
        IBehaviorContext<TInstance, TMessage> context)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class =>
        LogContextMetricsExtensions.StartProcess(logContext, context, "saga", "saga_state_machine");
}
