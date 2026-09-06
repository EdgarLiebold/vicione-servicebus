using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Futures;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures future routing slip.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TFault">The fault type.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class FutureRoutingSlipConfigurator<TCommand, TResult, TFault, TInput> :
    FutureRoutingSlipHandle,
    IFutureRoutingSlipConfigurator<TResult, TFault, TInput>,
    ISpecification
    where TCommand : class
    where TResult : class
    where TInput : class
    where TFault : class

{
    readonly IFutureStateMachineConfigurator _configurator;
    IRoutingSlipExecutor<TInput> _executor;
    FutureFault<TCommand, TFault, RoutingSlipFaulted> _fault = null!;
    FutureResult<TCommand, TResult, RoutingSlipCompleted> _result = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="routingSlipCompleted">The routing slip completed.</param>
    /// <param name="routingSlipFaulted">The routing slip faulted.</param>
    public FutureRoutingSlipConfigurator(IFutureStateMachineConfigurator configurator, Event<RoutingSlipCompleted> routingSlipCompleted,
        Event<RoutingSlipFaulted> routingSlipFaulted)
    {
        _configurator = configurator;
        Completed = routingSlipCompleted;
        Faulted = routingSlipFaulted;

        _executor = new PlanRoutingSlipExecutor<TInput>();

        OnRoutingSlipFaulted(fault => fault.SetFaultedUsingInitializer(context => RoutingSlipFaultedValueProvider(context)));
    }

    /// <summary>Gets or sets the completed id provider.</summary>
    public PendingFutureIdProvider<RoutingSlipCompleted> CompletedIdProvider { get; private set; } = null!;
    /// <summary>Gets or sets the faulted id provider.</summary>
    public PendingFutureIdProvider<RoutingSlipFaulted> FaultedIdProvider { get; private set; } = null!;
    /// <summary>Gets the completed.</summary>
    public Event<RoutingSlipCompleted> Completed { get; }
    /// <summary>Gets the faulted.</summary>
    public Event<RoutingSlipFaulted> Faulted { get; }

    /// <summary>Reports that on routing slip has completed.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void OnRoutingSlipCompleted(Action<IFutureResultConfigurator<TResult, RoutingSlipCompleted>> configure)
    {
        _result ??= new FutureResult<TCommand, TResult, RoutingSlipCompleted>();

        var configurator = new FutureResultConfigurator<TCommand, TResult, RoutingSlipCompleted>(_result);

        configure?.Invoke(configurator);
    }

    /// <summary>Reports that on routing slip has faulted.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void OnRoutingSlipFaulted(Action<IFutureFaultConfigurator<TFault, RoutingSlipFaulted>> configure)
    {
        _fault ??= new FutureFault<TCommand, TFault, RoutingSlipFaulted>();

        var configurator = new FutureFaultConfigurator<TCommand, TFault, RoutingSlipFaulted>(_fault);

        configure?.Invoke(configurator);
    }

    /// <summary>Reports that when routing slip has completed.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void WhenRoutingSlipCompleted(
        Func<EventActivityBinder<FutureState, RoutingSlipCompleted>, EventActivityBinder<FutureState, RoutingSlipCompleted>> configure)
    {
        _configurator.DuringAnyWhen(Completed, configure);
    }

    /// <summary>Reports that when routing slip has faulted.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void WhenRoutingSlipFaulted(
        Func<EventActivityBinder<FutureState, RoutingSlipFaulted>, EventActivityBinder<FutureState, RoutingSlipFaulted>> configure)
    {
        _configurator.DuringAnyWhen(Faulted, configure);
    }

    /// <summary>Tracks pending routing slip.</summary>
    public void TrackPendingRoutingSlip()
    {
        _executor.TrackRoutingSlip = true;

        CompletedIdProvider = GetTrackingNumber;
        FaultedIdProvider = GetTrackingNumber;
    }

    /// <summary>Builds itinerary.</summary>
    /// <param name="buildItinerary">The build itinerary.</param>
    public void BuildItinerary(BuildItineraryCallback<TInput> buildItinerary)
    {
        _executor = new BuildRoutingSlipExecutor<TInput>(buildItinerary);
    }

    /// <summary>Builds using itinerary planner.</summary>
    public void BuildUsingItineraryPlanner()
    {
        _executor = new PlanRoutingSlipExecutor<TInput>();
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
                yield return this.Failure("RoutingSlip", "Result", "WhenCompleted or TrackingPendingRoutingSlip must be configured");
            if (FaultedIdProvider == null)
                yield return this.Failure("RoutingSlip", "Fault", "WhenFaulted or TrackingPendingRoutingSlip must be configured");
        }

        if (_fault != null)
        {
            foreach (var result in _fault.Validate())
                yield return result.WithParentKey("RoutingSlip");
        }

        if (_executor == null)
            yield return this.Failure("RoutingSlip", "Build", "BuildItinerary or BuildUsingItineraryPlanner must be specified");
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

    /// <summary>Determines whether the current value has result.</summary>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasResult(out FutureResult<TCommand, TResult, RoutingSlipCompleted> result)
    {
        result = _result;
        return result != null;
    }

    /// <summary>Determines whether the current value has fault.</summary>
    /// <param name="fault">Receives the fault produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasFault(out FutureFault<TCommand, TFault, RoutingSlipFaulted> fault)
    {
        fault = _fault;
        return fault != null;
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
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
