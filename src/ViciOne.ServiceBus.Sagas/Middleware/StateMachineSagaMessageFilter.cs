using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Monitoring;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Raises the event correlated with a consumed message and completes terminal saga instances.</summary>
/// <typeparam name="TInstance">The state-machine saga type.</typeparam>
/// <typeparam name="TMessage">The correlated message type.</typeparam>
internal sealed class StateMachineSagaMessageFilter<TInstance, TMessage> :
    ISagaMessageFilter<TInstance, TMessage>
    where TInstance : class, ISaga, ISagaStateMachineInstance
    where TMessage : class
{
    readonly IEvent<TMessage> _event;
    readonly ISagaStateMachine<TInstance> _machine;

    /// <summary>Creates a filter for one state machine and correlated event.</summary>
    /// <param name="machine">The state machine that owns the event.</param>
    /// <param name="event">The event raised for each consumed message.</param>
    public StateMachineSagaMessageFilter(ISagaStateMachine<TInstance> machine, IEvent<TMessage> @event)
    {
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        _event = @event ?? throw new ArgumentNullException(nameof(@event));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("sagaStateMachine");
        scope.Set(new
        {
            Event = _event.Name,
            DataType = TypeCache<TMessage>.ShortName,
            InstanceType = TypeCache<TInstance>.ShortName
        });

        List<IState<TInstance>> states = _machine.States.Cast<IState<TInstance>>().Where(x => x.Events.Contains(_event)).ToList();
        if (states.Any())
            scope.Add("states", states.Select(x => x.Name).ToArray());

        _machine.Probe(scope);
    }

    /// <summary>Executes the correlated event against the selected saga instance.</summary>
    /// <param name="context">The saga instance and consumed message.</param>
    /// <param name="next">The composed pipeline continuation retained by the filter contract but not invoked by this terminal filter.</param>
    /// <returns>A task that completes after event execution and terminal-state evaluation.</returns>
    /// <remarks>The state-machine event is the terminal operation at the saga-message layer.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is null.</exception>
    /// <exception cref="OperationCanceledException">The delivery is cancelled, or an invoked state-machine or saga-completion collaborator propagates cancellation.</exception>
    /// <exception cref="InvalidOperationException">A state-machine or saga-completion collaborator returns a null task.</exception>
    public async Task SendAsync(SagaConsumeContext<TInstance, TMessage> context, IPipe<SagaConsumeContext<TInstance, TMessage>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        context.CancellationToken.ThrowIfCancellationRequested();

        IBehaviorContext<TInstance, TMessage> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy<TMessage>(_machine, context, context, _event);

        StartedActivity? activity = SagaActivity.TryStartStateMachine(behaviorContext);
        var instrument = LogContext.Current?.TryStartSagaStateMachineMetrics(behaviorContext);

        try
        {
            await ExecuteStateMachineAsync(context, behaviorContext, activity).ConfigureAwait(false);
        }
        catch (UnhandledEventException ex)
        {
            string currentState = GetCurrentStateName(behaviorContext, ex);

            var stateMachineException = new NotAcceptedStateMachineException(typeof(TInstance), typeof(TMessage),
                context.CorrelationId ?? Guid.Empty, currentState, ex);

            activity?.AddExceptionEvent(stateMachineException);
            instrument?.RecordException(ex);

            throw stateMachineException;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            activity?.AddExceptionEvent(exception);
            instrument?.RecordException(exception);

            throw;
        }
        finally
        {
            CompleteTelemetry(activity, instrument, behaviorContext);
        }
    }

    async Task ExecuteStateMachineAsync(
        SagaConsumeContext<TInstance, TMessage> context,
        IBehaviorContext<TInstance, TMessage> behaviorContext,
        StartedActivity? activity)
    {
        if (activity is { Activity: { IsAllDataRequested: true } })
            TrySetStateTag(activity, behaviorContext, ServiceBusTelemetry.Attributes.SagaStateBefore, context.CancellationToken);

        context.CancellationToken.ThrowIfCancellationRequested();

        Task eventTask = _machine.RaiseEventAsync(behaviorContext, context.CancellationToken)
            ?? throw new InvalidOperationException("The state machine returned a null task from RaiseEventAsync.");
        await eventTask.ConfigureAwait(false);

        context.CancellationToken.ThrowIfCancellationRequested();

        Task<bool> completionTask = _machine.IsCompletedAsync(behaviorContext, context.CancellationToken)
            ?? throw new InvalidOperationException("The state machine returned a null task from IsCompletedAsync.");
        bool isCompleted = await completionTask.ConfigureAwait(false);

        context.CancellationToken.ThrowIfCancellationRequested();

        if (!isCompleted)
            return;

        Task setCompletedTask = context.SetCompletedAsync(context.CancellationToken)
            ?? throw new InvalidOperationException("The saga consume context returned a null task from SetCompletedAsync.");
        await setCompletedTask.ConfigureAwait(false);
    }

    static void CompleteTelemetry(
        StartedActivity? activity,
        MetricOperation? instrument,
        IBehaviorContext<TInstance, TMessage> behaviorContext)
    {
        if (activity is { } startedActivity)
        {
            if (startedActivity.Activity.IsAllDataRequested)
                TrySetStateTag(startedActivity, behaviorContext, ServiceBusTelemetry.Attributes.SagaStateAfter, CancellationToken.None);

            startedActivity.Stop();
        }

        instrument?.Complete();
    }

    static string GetCurrentStateName(
        IBehaviorContext<TInstance, TMessage> behaviorContext,
        UnhandledEventException exception)
    {
        if (!string.IsNullOrWhiteSpace(exception.StateName))
            return exception.StateName;

        try
        {
            Task<IState<TInstance>?> stateTask = behaviorContext.StateMachine.Accessor
                .GetAsync(behaviorContext, CancellationToken.None);
            if (stateTask is null)
                return "(not initialized)";

            if (!stateTask.IsCompletedSuccessfully)
            {
                ObserveFault(stateTask);
                return "(not initialized)";
            }

            IState<TInstance>? state = stateTask.GetAwaiter().GetResult();
            return string.IsNullOrWhiteSpace(state?.Name) ? "(not initialized)" : state.Name;
        }
        catch (Exception)
        {
            return "(not initialized)";
        }
    }

    static void TrySetStateTag(
        StartedActivity activity,
        IBehaviorContext<TInstance, TMessage> behaviorContext,
        string tag,
        CancellationToken cancellationToken)
    {
        try
        {
            Task<IState<TInstance>?> stateTask = behaviorContext.StateMachine.Accessor
                .GetAsync(behaviorContext, cancellationToken);
            if (stateTask is null)
                return;

            if (!stateTask.IsCompletedSuccessfully)
            {
                ObserveFault(stateTask);
                return;
            }

            IState<TInstance>? state = stateTask.GetAwaiter().GetResult();
            if (state != null)
                activity.SetTag(tag, state.Name);
        }
        catch (Exception)
        {
            // State tags are observational and cannot change event, completion, or cancellation outcomes.
        }
    }

    static void ObserveFault(Task task)
    {
        _ = task.ContinueWith(
            static completedTask => _ = completedTask.Exception,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
