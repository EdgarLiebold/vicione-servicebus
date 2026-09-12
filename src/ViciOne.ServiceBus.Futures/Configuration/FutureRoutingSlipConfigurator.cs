using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Futures;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures routing-slip execution and terminal events for a future input.</summary>
/// <typeparam name="TCommand">The command contract stored by the future.</typeparam>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TFault">The terminal future fault contract.</typeparam>
/// <typeparam name="TInput">The future event contract used to build the routing slip.</typeparam>
internal sealed class FutureRoutingSlipConfigurator<TCommand, TResult, TFault, TInput> :
    IFutureRoutingSlipHandle,
    IFutureRoutingSlipConfigurator<TResult, TFault, TInput>,
    ISpecification
    where TCommand : class
    where TResult : class
    where TInput : class
    where TFault : class

{
    readonly IFutureStateMachineConfigurator _configurator;
    IRoutingSlipExecutor<TInput> _executor;
    FutureFault<TCommand, TFault, RoutingSlipFaulted>? _fault;
    FutureResult<TCommand, TResult, RoutingSlipCompleted>? _result;
    bool _trackRoutingSlip;

    /// <summary>Creates routing-slip execution and terminal-event configuration for a future.</summary>
    /// <param name="configurator">The future state-machine configurator.</param>
    /// <param name="routingSlipCompleted">The routing-slip completion event.</param>
    /// <param name="routingSlipFaulted">The routing-slip fault event.</param>
    public FutureRoutingSlipConfigurator(IFutureStateMachineConfigurator configurator, Event<RoutingSlipCompleted> routingSlipCompleted,
        Event<RoutingSlipFaulted> routingSlipFaulted)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(routingSlipCompleted);
        ArgumentNullException.ThrowIfNull(routingSlipFaulted);
        _configurator = configurator;
        Completed = routingSlipCompleted;
        Faulted = routingSlipFaulted;

        _executor = new PlanRoutingSlipExecutor<TInput>();

        OnRoutingSlipFaulted(fault => fault.SetFaultInitializer(RoutingSlipFaultedValueProvider));
    }

    /// <summary>Gets the selector that completes the pending routing-slip identifier.</summary>
    public PendingFutureIdProvider<RoutingSlipCompleted>? CompletedIdProvider { get; private set; }
    /// <summary>Gets the selector that faults the pending routing-slip identifier.</summary>
    public PendingFutureIdProvider<RoutingSlipFaulted>? FaultedIdProvider { get; private set; }
    /// <summary>Gets the routing-slip completion event.</summary>
    public Event<RoutingSlipCompleted> Completed { get; }
    /// <summary>Gets the routing-slip fault event.</summary>
    public Event<RoutingSlipFaulted> Faulted { get; }

    /// <summary>Configures how routing-slip completion creates the successful future result.</summary>
    /// <param name="configure">The callback that configures the future result message.</param>
    public void OnRoutingSlipCompleted(Action<IFutureResultConfigurator<TResult, RoutingSlipCompleted>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _result ??= new FutureResult<TCommand, TResult, RoutingSlipCompleted>();

        var configurator = new FutureResultConfigurator<TCommand, TResult, RoutingSlipCompleted>(_result);

        configure(configurator);
    }

    /// <summary>Configures how a routing-slip fault creates the terminal future fault.</summary>
    /// <param name="configure">The callback that configures the future fault message.</param>
    public void OnRoutingSlipFaulted(Action<IFutureFaultConfigurator<TFault, RoutingSlipFaulted>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _fault ??= new FutureFault<TCommand, TFault, RoutingSlipFaulted>();

        var configurator = new FutureFaultConfigurator<TCommand, TFault, RoutingSlipFaulted>(_fault);

        configure(configurator);
    }

    /// <summary>Adds state-machine activities executed when the routing slip completes.</summary>
    /// <param name="configure">The callback that adds activities to the completion event.</param>
    public void WhenRoutingSlipCompleted(
        Func<EventActivityBinder<FutureState, RoutingSlipCompleted>, EventActivityBinder<FutureState, RoutingSlipCompleted>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configurator.DuringAnyWhen(Completed, configure);
    }

    /// <summary>Adds state-machine activities executed when the routing slip faults.</summary>
    /// <param name="configure">The callback that adds activities to the fault event.</param>
    public void WhenRoutingSlipFaulted(
        Func<EventActivityBinder<FutureState, RoutingSlipFaulted>, EventActivityBinder<FutureState, RoutingSlipFaulted>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configurator.DuringAnyWhen(Faulted, configure);
    }

    /// <summary>Keeps the routing slip pending until its completion or fault event is consumed.</summary>
    public void TrackPendingRoutingSlip()
    {
        _trackRoutingSlip = true;
        _executor.TrackRoutingSlip = true;

        CompletedIdProvider = GetTrackingNumber;
        FaultedIdProvider = GetTrackingNumber;
    }

    /// <summary>Uses a callback to build each routing-slip itinerary.</summary>
    /// <param name="buildItinerary">The callback that adds activities and variables to the itinerary.</param>
    public void BuildItinerary(BuildItineraryCallback<TInput> buildItinerary)
    {
        ArgumentNullException.ThrowIfNull(buildItinerary);
        _executor = new BuildRoutingSlipExecutor<TInput>(buildItinerary) { TrackRoutingSlip = _trackRoutingSlip };
    }

    /// <summary>Uses the registered itinerary planner to build each routing slip.</summary>
    public void BuildUsingItineraryPlanner()
    {
        _executor = new PlanRoutingSlipExecutor<TInput> { TrackRoutingSlip = _trackRoutingSlip };
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_result != null)
        {
            foreach (var result in _result.Validate())
                yield return result.WithParentKey("RoutingSlip");
        }
        else
        {
            if (CompletedIdProvider == null)
                yield return this.Failure("RoutingSlip", "Result", "OnRoutingSlipCompleted or TrackPendingRoutingSlip must be configured");
            if (FaultedIdProvider == null)
                yield return this.Failure("RoutingSlip", "Fault", "OnRoutingSlipFaulted or TrackPendingRoutingSlip must be configured");
        }

        if (_fault != null)
        {
            foreach (var result in _fault.Validate())
                yield return result.WithParentKey("RoutingSlip");
        }

    }

    static object RoutingSlipFaultedValueProvider(BehaviorContext<FutureState, RoutingSlipFaulted> context)
    {
        var message = context.GetCommand<TCommand>();

        IEnumerable<ExceptionInfo> exceptions = context.Message.ActivityExceptions.Select(x => x.ExceptionInfo);

        return new
        {
            FaultId = context.MessageId ?? NewId.NextGuid(),
            FaultedMessageId = context.Message.TrackingNumber,
            FaultMessageTypes = MessageTypeCache<TCommand>.MessageTypeNames.ToArray(),
            Host = context.Message.ActivityExceptions.Select(x => x.Host).FirstOrDefault() ?? context.Host,
            context.Message.Timestamp,
            exceptions,
            message
        };
    }

    /// <summary>Returns the configured routing-slip result producer when present.</summary>
    /// <param name="result">Receives the configured result producer.</param>
    /// <returns><see langword="true" /> when result production is configured; otherwise, <see langword="false" />.</returns>
    public bool HasResult([NotNullWhen(true)] out FutureResult<TCommand, TResult, RoutingSlipCompleted>? result)
    {
        result = _result;
        return result != null;
    }

    /// <summary>Returns the configured routing-slip fault producer when present.</summary>
    /// <param name="fault">Receives the configured fault producer.</param>
    /// <returns><see langword="true" /> when fault production is configured; otherwise, <see langword="false" />.</returns>
    public bool HasFault([NotNullWhen(true)] out FutureFault<TCommand, TFault, RoutingSlipFaulted>? fault)
    {
        fault = _fault;
        return fault != null;
    }

    /// <summary>Builds and executes the routing slip for the current future event.</summary>
    /// <param name="context">The future event context used to build and execute the routing slip.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _executor.ExecuteAsync(context, cancellationToken: cancellationToken);
    }

    static Guid GetTrackingNumber(RoutingSlipCompleted message)
    {
        return message.TrackingNumber;
    }

    static Guid GetTrackingNumber(RoutingSlipFaulted message)
    {
        return message.TrackingNumber;
    }
}
