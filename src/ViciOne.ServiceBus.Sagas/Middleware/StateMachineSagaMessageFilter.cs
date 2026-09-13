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
    where TInstance : class, ISaga, SagaStateMachineInstance
    where TMessage : class
{
    readonly string _activityName;
    readonly Event<TMessage> _event;
    readonly SagaStateMachine<TInstance> _machine;

    /// <summary>Creates a filter for one state machine and correlated event.</summary>
    /// <param name="machine">The state machine that owns the event.</param>
    /// <param name="event">The event raised for each consumed message.</param>
    public StateMachineSagaMessageFilter(SagaStateMachine<TInstance> machine, Event<TMessage> @event)
    {
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        _event = @event ?? throw new ArgumentNullException(nameof(@event));

        _activityName = $"{_machine.Name} process";
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("sagaStateMachine");
        scope.Set(new
        {
            Event = _event.Name,
            DataType = TypeCache<TMessage>.ShortName,
            InstanceType = TypeCache<TInstance>.ShortName
        });

        List<State<TInstance>> states = _machine.States.Cast<State<TInstance>>().Where(x => x.Events.Contains(_event)).ToList();
        if (states.Any())
            scope.Add("states", states.Select(x => x.Name).ToArray());

        _machine.Probe(context);
    }

    /// <summary>Executes the correlated event against the selected saga instance.</summary>
    /// <param name="context">The saga instance and consumed message.</param>
    /// <param name="next">The composed pipeline continuation retained by the filter contract.</param>
    /// <returns>A task that completes after event execution and terminal-state evaluation.</returns>
    public async Task SendAsync(SagaConsumeContext<TInstance, TMessage> context, IPipe<SagaConsumeContext<TInstance, TMessage>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        context.CancellationToken.ThrowIfCancellationRequested();

        BehaviorContext<TInstance, TMessage> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy<TMessage>(_machine, context, context, _event);

        StartedActivity? activity = LogContext.Current?.StartSagaStateMachineActivity(behaviorContext);
        var instrument = LogContext.Current?.StartSagaStateMachineInstrument(behaviorContext);

        try
        {
            if (activity is { Activity: { IsAllDataRequested: true } })
            {
                State<TInstance>? beginState = await behaviorContext.StateMachine.Accessor
                    .GetAsync(behaviorContext, context.CancellationToken)
                    .ConfigureAwait(false);
                if (beginState != null)
                    activity?.SetTag(ServiceBusTelemetry.Attributes.SagaStateBefore, beginState.Name);
            }

            await _machine.RaiseEventAsync(behaviorContext, context.CancellationToken).ConfigureAwait(false);

            if (await _machine.IsCompletedAsync(behaviorContext, context.CancellationToken).ConfigureAwait(false))
                await context.SetCompletedAsync(context.CancellationToken).ConfigureAwait(false);
        }
        catch (UnhandledEventException ex)
        {
            State<TInstance>? currentState = await _machine.Accessor
                .GetAsync(behaviorContext, context.CancellationToken)
                .ConfigureAwait(false);

            var stateMachineException = new NotAcceptedStateMachineException(typeof(TInstance), typeof(TMessage),
                context.CorrelationId ?? Guid.Empty, currentState?.Name ?? "(not initialized)", ex);

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
            if (activity is { } startedActivity)
            {
                if (startedActivity.Activity.IsAllDataRequested)
                {
                    State<TInstance>? endState = await behaviorContext.StateMachine.Accessor
                        .GetAsync(behaviorContext, CancellationToken.None)
                        .ConfigureAwait(false);
                    if (endState != null)
                        startedActivity.SetTag(ServiceBusTelemetry.Attributes.SagaStateAfter, endState.Name);
                }

                startedActivity.Stop();
            }

            instrument?.Complete();
        }
    }
}
