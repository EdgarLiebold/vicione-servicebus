using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Futures.Contracts;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// A future is a deterministic durable service that, given a command, executes any number
/// of requests, routing slips, functions, etc. to produce a result. Once the result has been set,
/// it is available to any subsequent commands and requests for the result.
/// </summary>
/// <typeparam name="TCommand">The command type that creates the future.</typeparam>
/// <typeparam name="TResult">The result type that completes the future.</typeparam>
/// <typeparam name="TFault">The fault type that faults the future.</typeparam>
public abstract class Future<TCommand, TResult, TFault> :
    ViciOneServiceBusStateMachine<FutureState>,
    IFutureStateMachineConfigurator
    where TCommand : class
    where TResult : class
    where TFault : class
{
    readonly FutureFault<TFault> _fault = new();
    readonly FutureResult<TCommand, TResult> _result = new();

    /// <summary>Initializes the durable lifecycle and default terminal fault mapping for the future.</summary>
    protected Future()
    {
        InstanceState(x => x.CurrentState, WaitingForCompletion, Completed, Faulted);

        Event(() => ResultRequested, e =>
        {
            e.CorrelateById(x => x.Message.CorrelationId);
            e.OnMissingInstance(x => x.Execute(context => throw new FutureNotFoundException(typeof(TCommand), context.Message.CorrelationId)));
            e.ConfigureConsumeTopology = false;
        });

        Initially(
            When(CommandReceived)
                .InitializeFuture()
                .TransitionTo(WaitingForCompletion)
        );

        During(WaitingForCompletion,
            When(CommandReceived)
                .AddSubscription(),
            When(ResultRequested)
                .AddSubscription()
        );

        During(Completed,
            When(CommandReceived)
                .RespondAwaited(x => GetResultAsync(x)),
            When(ResultRequested)
                .RespondAwaited(x => GetResultAsync(x))
        );

        During(Faulted,
            When(CommandReceived)
                .RespondAwaited(x => GetFaultAsync(x)),
            When(ResultRequested)
                .RespondAwaited(x => GetFaultAsync(x))
        );

        WhenAnyFaulted(x => x.SetFaultInitializer(context =>
        {
            var message = context.GetCommand<TCommand>();

            List<Fault> faults = [.. context.Saga.Faults.Select(fault => context.ToObject<Fault>(fault.Value)).OfType<Fault>()];

            var faulted = faults.First();

            ExceptionInfo[] exceptions = [.. faults.SelectMany(fault => fault.Exceptions)];

            return new
            {
                faulted.FaultId,
                faulted.FaultedMessageId,
                Timestamp = context.Saga.Faulted,
                Exceptions = exceptions,
                faulted.Host,
                faulted.FaultMessageTypes,
                Message = message
            };
        }));
    }

    /// <summary>Gets the state in which one or more operations are still pending.</summary>
    public IState WaitingForCompletion { get; protected set; } = null!;
    /// <summary>Gets the terminal state for a successful future.</summary>
    public IState Completed { get; protected set; } = null!;
    /// <summary>Gets the terminal state for a faulted future.</summary>
    public IState Faulted { get; protected set; } = null!;

    /// <summary>
    /// Initiates and correlates the command to the future. Subsequent commands received while waiting for completion
    /// are added as subscribers.
    /// </summary>
    public IEvent<TCommand> CommandReceived { get; protected set; } = null!;
    /// <summary>Gets the event that requests the stored terminal outcome of an existing future.</summary>
    public IEvent<IGet<TCommand>> ResultRequested { get; protected set; } = null!;
    /// <summary>Configures correlation and topology behavior for the initiating command.</summary>
    /// <param name="configure">The callback that configures the command event.</param>
    protected void ConfigureCommand(Action<IEventCorrelationConfigurator<FutureState, TCommand>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Event(() => CommandReceived, configurator =>
        {
            configure(configurator);
        });
    }

    /// <summary>Dispatches one request initialized from the command when the future starts.</summary>
    /// <typeparam name="TRequest">The outbound request contract.</typeparam>
    /// <param name="configure">The optional callback that customizes request, response, and fault behavior.</param>
    /// <returns>A handle for configuring accepted responses.</returns>
    protected IFutureRequestHandle<TCommand, TResult, TFault, TRequest>
        SendRequest<TRequest>(Action<IFutureRequestConfigurator<TFault, TCommand, TRequest>>? configure = default)
        where TRequest : class
    {
        FutureRequestConfigurator<TCommand, TResult, TFault, TCommand, TRequest> request = CreateFutureRequest(configure);

        Initially(
            When(CommandReceived)
                .ThenAwaited(context => request.SendAsync(context, context.CancellationToken))
        );

        return request;
    }

    /// <summary>Dispatches one request initialized from a selected command value when the future starts.</summary>
    /// <typeparam name="TInput">The selected input value type.</typeparam>
    /// <typeparam name="TRequest">The outbound request contract.</typeparam>
    /// <param name="inputSelector">The selector that extracts the request input from the command.</param>
    /// <param name="configure">The optional callback that customizes request, response, and fault behavior.</param>
    /// <returns>A handle for configuring accepted responses.</returns>
    protected IFutureRequestHandle<TCommand, TResult, TFault, TRequest> SendRequest<TInput, TRequest>(Func<TCommand, TInput> inputSelector,
        Action<IFutureRequestConfigurator<TFault, TInput, TRequest>>? configure = default)
        where TInput : class
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(inputSelector);
        FutureRequestConfigurator<TCommand, TResult, TFault, TInput, TRequest> request = CreateFutureRequest(configure);

        Initially(
            When(CommandReceived)
                .ThenAwaited(context => request.SendAsync(context, inputSelector(context.Message), context.CancellationToken))
        );

        return request;
    }

    /// <summary>Dispatches one request for every selected command value when the future starts.</summary>
    /// <typeparam name="TInput">The selected input value type.</typeparam>
    /// <typeparam name="TRequest">The outbound request contract.</typeparam>
    /// <param name="inputSelector">The selector that extracts the request inputs from the command.</param>
    /// <param name="configure">The callback that configures request, response, and fault behavior.</param>
    /// <returns>A handle for configuring accepted responses.</returns>
    protected IFutureRequestHandle<TCommand, TResult, TFault, TRequest> SendRequests<TInput, TRequest>(Func<TCommand, IEnumerable<TInput>> inputSelector,
        Action<IFutureRequestConfigurator<TFault, TInput, TRequest>> configure)
        where TInput : class
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(inputSelector);
        ArgumentNullException.ThrowIfNull(configure);
        FutureRequestConfigurator<TCommand, TResult, TFault, TInput, TRequest> request = CreateFutureRequest(configure);

        Initially(
            When(CommandReceived)
                .ThenAwaited(context => request.SendRangeAsync(context, inputSelector(context.Message), context.CancellationToken))
        );

        return request;
    }

    /// <summary>Builds and executes a routing slip when the future starts.</summary>
    /// <param name="configure">The callback that configures itinerary construction and terminal events.</param>
    /// <returns>A handle exposing the configured routing-slip terminal events.</returns>
    protected IFutureRoutingSlipHandle ExecuteRoutingSlip(Action<IFutureRoutingSlipConfigurator<TResult, TFault, TCommand>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        FutureRoutingSlipConfigurator<TCommand, TResult, TFault, TCommand> routingSlip = CreateFutureRoutingSlip(configure);

        Initially(
            When(CommandReceived)
                .ThenAwaited(context => routingSlip.ExecuteAsync(context, context.CancellationToken))
        );

        return routingSlip;
    }

    FutureRequestConfigurator<TCommand, TResult, TFault, TInput, TRequest> CreateFutureRequest<TInput, TRequest>(
        Action<IFutureRequestConfigurator<TFault, TInput, TRequest>>? configure)
        where TInput : class
        where TRequest : class
    {
        IEvent<Fault<TRequest>> requestFaulted = Event<Fault<TRequest>>(FormatEventName<TRequest>() + "Faulted", x =>
        {
            x.CorrelateById(m => RequestIdOrFault(m));
            x.OnMissingInstance(m => m.Execute(context => throw new FutureNotFoundException(GetType(), RequestIdOrDefault(context))));
            x.ConfigureConsumeTopology = false;
        });

        var request = new FutureRequestConfigurator<TCommand, TResult, TFault, TInput, TRequest>(this, requestFaulted);

        configure?.Invoke(request);

        request.Validate().ThrowIfContainsFailure($"The future request was not configured correctly: {TypeCache.GetShortName(GetType())}");

        if (request.PendingRequestIdProvider != null)
            FaultPendingRequest(requestFaulted, request.PendingRequestIdProvider);
        else
            SetFaulted(requestFaulted, context => request.TrySetFaultedAsync(context, context.CancellationToken));

        return request;
    }

    FutureRoutingSlipConfigurator<TCommand, TResult, TFault, TInput> CreateFutureRoutingSlip<TInput>(
        Action<IFutureRoutingSlipConfigurator<TResult, TFault, TInput>> configure)
        where TInput : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        IEvent<IRoutingSlipCompleted> routingSlipCompleted = Event<IRoutingSlipCompleted>(FormatEventName<IRoutingSlipCompleted>(), x =>
        {
            x.CorrelateById(m => FutureIdOrFault(m.Advanced(), m.Message.Variables));
            x.OnMissingInstance(m => m
                .Execute(context => throw new FutureNotFoundException(GetType(), FutureIdOrDefault(context.Advanced(), context.Message.Variables))));
            x.ConfigureConsumeTopology = false;
        });

        IEvent<IRoutingSlipFaulted> routingSlipFaulted = Event<IRoutingSlipFaulted>(FormatEventName<IRoutingSlipFaulted>(), x =>
        {
            x.CorrelateById(m => FutureIdOrFault(m.Advanced(), m.Message.Variables));
            x.OnMissingInstance(m => m
                .Execute(context => throw new FutureNotFoundException(GetType(), FutureIdOrDefault(context.Advanced(), context.Message.Variables))));
            x.ConfigureConsumeTopology = false;
        });

        var routingSlip = new FutureRoutingSlipConfigurator<TCommand, TResult, TFault, TInput>(this, routingSlipCompleted, routingSlipFaulted);

        configure(routingSlip);

        routingSlip.Validate().ThrowIfContainsFailure($"The future routing slip was not configured correctly: {TypeCache.GetShortName(GetType())}");

        if (routingSlip.FaultedIdProvider != null)
            FaultPendingRoutingSlip(routingSlipFaulted);
        else
        {
            if (routingSlip.HasFault(out FutureFault<TCommand, TFault, IRoutingSlipFaulted>? fault))
                SetFaulted(routingSlipFaulted, context => fault.TrySetFaultedAsync(context, context.CancellationToken));
            else
                SetFaulted(routingSlipFaulted, context => _fault.TrySetFaultedAsync(context, context.CancellationToken));
        }

        if (routingSlip.CompletedIdProvider != null)
            CompletePending(routingSlipCompleted, routingSlip.CompletedIdProvider);
        else if (routingSlip.HasResult(out FutureResult<TCommand, TResult, IRoutingSlipCompleted>? result))
            SetResult(routingSlipCompleted, context => result.SetResultAsync(context, context.CancellationToken));

        return routingSlip;
    }

    IEvent<T> IFutureStateMachineConfigurator.CreateResponseEvent<T>()
        where T : class
    {
        IEvent<T> requestCompleted = Event<T>(FormatEventName<T>(), x =>
        {
            x.CorrelateById(m => RequestIdOrFault(m));
            x.OnMissingInstance(m => m.Execute(context => throw new FutureNotFoundException(GetType(), RequestIdOrDefault(context))));
            x.ConfigureConsumeTopology = false;
        });

        return requestCompleted;
    }

    void IFutureStateMachineConfigurator.CompletePendingRequest<T>(IEvent<T> requestCompleted, PendingFutureIdProvider<T> pendingIdProvider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(requestCompleted);
        ArgumentNullException.ThrowIfNull(pendingIdProvider);
        CompletePending(requestCompleted, pendingIdProvider);
    }

    void IFutureStateMachineConfigurator.DuringAnyWhen<T>(IEvent<T> whenEvent, Func<IEventActivityBinder<FutureState, T>,
        IEventActivityBinder<FutureState, T>> configure)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(whenEvent);
        ArgumentNullException.ThrowIfNull(configure);
        IEventActivityBinder<FutureState, T> binder = When(whenEvent);

        binder = configure(binder) ?? throw new InvalidOperationException("The future event configuration callback returned null.");

        DuringAny(binder);
    }

    void CompletePending<T>(IEvent<T> completedEvent, PendingFutureIdProvider<T> pendingIdProvider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(completedEvent);
        ArgumentNullException.ThrowIfNull(pendingIdProvider);
        DuringAny(
            When(completedEvent)
                .SetResult(x => pendingIdProvider(x.Message), x => x.Message)
                .IfElse(context => context.Saga.Completed.HasValue,
                    completed => completed
                        .ThenAwaited(context => _result.SetResultAsync(context, context.CancellationToken))
                        .TransitionTo(Completed),
                    notCompleted => notCompleted.If(context => context.Saga.Faulted.HasValue,
                        faulted => faulted.IfAwaited(
                            context => _fault.TrySetFaultedAsync(context, context.CancellationToken),
                            terminal => terminal.TransitionTo(Faulted))))
        );
    }

    /// <summary>Stores a request fault and completes its pending operation identifier.</summary>
    /// <typeparam name="T">The failed request contract.</typeparam>
    /// <param name="requestFaulted">The request-fault event.</param>
    /// <param name="pendingIdProvider">The selector for the pending request identifier.</param>
    void FaultPendingRequest<T>(IEvent<Fault<T>> requestFaulted, PendingFutureIdProvider<T> pendingIdProvider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(requestFaulted);
        ArgumentNullException.ThrowIfNull(pendingIdProvider);
        DuringAny(
            When(requestFaulted)
                .SetFault(x => pendingIdProvider(x.Message.Message), x => x.Message)
                .If(context => context.Saga.Faulted.HasValue,
                    faulted => faulted.IfAwaited(
                        context => _fault.TrySetFaultedAsync(context, context.CancellationToken),
                        terminal => terminal.TransitionTo(Faulted)))
        );
    }

    void FaultPendingRoutingSlip(IEvent<IRoutingSlipFaulted> requestFaulted)
    {
        ArgumentNullException.ThrowIfNull(requestFaulted);
        DuringAny(
            When(requestFaulted)
                .SetFault(x => x.Message)
                .If(context => context.Saga.Faulted.HasValue,
                    faulted => faulted.IfAwaited(
                        context => _fault.TrySetFaultedAsync(context, context.CancellationToken),
                        terminal => terminal.TransitionTo(Faulted)))
        );
    }

    void IFutureStateMachineConfigurator.SetResult<T>(IEvent<T> responseReceived,
        Func<IBehaviorContext<FutureState, T>, Task> callback)
        where T : class
    {
        SetResult(responseReceived, callback);
    }

    void SetResult<T>(IEvent<T> resultEvent, Func<IBehaviorContext<FutureState, T>, Task> callback)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(resultEvent);
        ArgumentNullException.ThrowIfNull(callback);
        DuringAny(
            When(resultEvent)
                .ThenAwaited(context => callback(context))
                .TransitionTo(Completed)
        );
    }

    void IFutureStateMachineConfigurator.SetFaulted<T>(IEvent<T> requestCompleted,
        Func<IBehaviorContext<FutureState, T>, Task<bool>> callback)
    {
        SetFaulted(requestCompleted, callback);
    }

    /// <summary>Configures an event to create the terminal fault and transition the future to faulted.</summary>
    /// <typeparam name="T">The event contract that triggers the fault.</typeparam>
    /// <param name="faultEvent">The event to configure.</param>
    /// <param name="callback">The asynchronous callback that reports whether the terminal fault was emitted.</param>
    void SetFaulted<T>(IEvent<T> faultEvent, Func<IBehaviorContext<FutureState, T>, Task<bool>> callback)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(faultEvent);
        ArgumentNullException.ThrowIfNull(callback);
        DuringAny(
            When(faultEvent)
                .IfAwaited(
                    context => callback(context),
                    terminal => terminal.TransitionTo(Faulted))
        );
    }

    static string FormatEventName<T>()
        where T : class
    {
        return DefaultEndpointNameFormatter.Instance.Message<T>();
    }

    /// <summary>Returns the request identifier or propagates its fault.</summary>
    /// <param name="context">The message context that carries the request identifier.</param>
    /// <returns>The required request identifier.</returns>
    protected static Guid RequestIdOrFault(MessageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.RequestId ?? throw new RequestException("The request identifier is required but was not present.");
    }

    /// <summary>Returns the request identifier, or the default value.</summary>
    /// <param name="context">The message context that may carry a request identifier.</param>
    /// <returns>The request identifier, or <see cref="Guid.Empty" /> when absent.</returns>
    protected static Guid RequestIdOrDefault(MessageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.RequestId ?? default;
    }

    /// <summary>Returns the future identifier or propagates its fault.</summary>
    /// <param name="context">The consume context that provides variable conversion.</param>
    /// <param name="variables">The routing-slip variables expected to contain the future identifier.</param>
    /// <returns>The required future identifier.</returns>
    protected static Guid FutureIdOrFault(ConsumeContext context, IReadOnlyDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(variables);
        Guid? correlationId = context.SerializerContext.GetValue<Guid>(variables, MessageHeaders.FutureId);
        if (correlationId.HasValue)
            return correlationId.Value;

        throw new RequestException("The routing slip does not contain the required future identifier.");
    }

    /// <summary>Returns the future identifier, or the default value.</summary>
    /// <param name="context">The consume context that provides variable conversion.</param>
    /// <param name="variables">The routing-slip variables that may contain the future identifier.</param>
    /// <returns>The future identifier, or <see cref="Guid.Empty" /> when absent.</returns>
    protected static Guid FutureIdOrDefault(ConsumeContext context, IReadOnlyDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(variables);
        return context.SerializerContext.GetValue<Guid>(variables, MessageHeaders.FutureId) ?? default;
    }

    /// <summary>Configures the successful result produced after all tracked operations complete.</summary>
    /// <param name="configure">The callback that configures the terminal result.</param>
    protected void WhenAllCompleted(Action<IFutureResultConfigurator<TResult>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var configurator = new FutureResultConfigurator<TCommand, TResult>(_result);

        configure(configurator);
    }

    /// <summary>Configures the terminal fault produced as soon as any tracked operation faults.</summary>
    /// <param name="configure">The callback that configures the terminal fault.</param>
    protected void WhenAnyFaulted(Action<IFutureFaultConfigurator<TFault>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var configurator = new FutureFaultConfigurator<TFault>(_fault);

        configure(configurator);
    }

    /// <summary>Configures the terminal fault produced after every tracked operation has completed or faulted.</summary>
    /// <param name="configure">The callback that configures the terminal fault.</param>
    protected void WhenAllCompletedOrFaulted(Action<IFutureFaultConfigurator<TFault>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _fault.WaitForPending = true;
        var configurator = new FutureFaultConfigurator<TFault>(_fault);

        configure(configurator);
    }

    static Task<TResult> GetResultAsync(IBehaviorContext<FutureState> context)
    {
        if (context.TryGetResult(context.Saga.CorrelationId, out TResult? completed))
            return Task.FromResult(completed);

        throw new InvalidOperationException("The future is completed, but its result is not available.");
    }

    static Task<TFault> GetFaultAsync(IBehaviorContext<FutureState> context)
    {
        if (context.TryGetFault(context.Saga.CorrelationId, out TFault? faulted))
            return Task.FromResult(faulted);

        throw new InvalidOperationException("The future is faulted, but its fault is not available.");
    }
}


/// <summary>Coordinates a future command and its eventual result.</summary>
/// <typeparam name="TCommand">The command contract that creates the future.</typeparam>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
public abstract class Future<TCommand, TResult> :
    Future<TCommand, TResult, Fault<TCommand>>
    where TCommand : class
    where TResult : class
{
}
