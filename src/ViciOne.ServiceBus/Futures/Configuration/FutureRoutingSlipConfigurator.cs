using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Futures;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a future routing slip configurator implementation.
/// </summary>
/// <typeparam name="TCommand">The t command type.</typeparam>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="TFault">The t fault type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="routingSlipCompleted">The routing slip completed value.</param>
    /// <param name="routingSlipFaulted">The routing slip faulted value.</param>
    public FutureRoutingSlipConfigurator(IFutureStateMachineConfigurator configurator, Event<RoutingSlipCompleted> routingSlipCompleted,
        Event<RoutingSlipFaulted> routingSlipFaulted)
    {
        _configurator = configurator;
        Completed = routingSlipCompleted;
        Faulted = routingSlipFaulted;

        _executor = new PlanRoutingSlipExecutor<TInput>();

        OnRoutingSlipFaulted(fault => fault.SetFaultedUsingInitializer(context => RoutingSlipFaultedValueProvider(context)));
    }

    /// <summary>
    /// Gets or sets the completed id provider value.
    /// </summary>
    public PendingFutureIdProvider<RoutingSlipCompleted> CompletedIdProvider { get; private set; } = null!;
    /// <summary>
    /// Gets or sets the faulted id provider value.
    /// </summary>
    public PendingFutureIdProvider<RoutingSlipFaulted> FaultedIdProvider { get; private set; } = null!;
    /// <summary>
    /// Gets the completed value.
    /// </summary>
    public Event<RoutingSlipCompleted> Completed { get; }
    /// <summary>
    /// Gets the faulted value.
    /// </summary>
    public Event<RoutingSlipFaulted> Faulted { get; }

    /// <summary>
    /// Performs the on routing slip completed operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void OnRoutingSlipCompleted(Action<IFutureResultConfigurator<TResult, RoutingSlipCompleted>> configure)
    {
        _result ??= new FutureResult<TCommand, TResult, RoutingSlipCompleted>();

        var configurator = new FutureResultConfigurator<TCommand, TResult, RoutingSlipCompleted>(_result);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the on routing slip faulted operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void OnRoutingSlipFaulted(Action<IFutureFaultConfigurator<TFault, RoutingSlipFaulted>> configure)
    {
        _fault ??= new FutureFault<TCommand, TFault, RoutingSlipFaulted>();

        var configurator = new FutureFaultConfigurator<TCommand, TFault, RoutingSlipFaulted>(_fault);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the when routing slip completed operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void WhenRoutingSlipCompleted(
        Func<EventActivityBinder<FutureState, RoutingSlipCompleted>, EventActivityBinder<FutureState, RoutingSlipCompleted>> configure)
    {
        _configurator.DuringAnyWhen(Completed, configure);
    }

    /// <summary>
    /// Performs the when routing slip faulted operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void WhenRoutingSlipFaulted(
        Func<EventActivityBinder<FutureState, RoutingSlipFaulted>, EventActivityBinder<FutureState, RoutingSlipFaulted>> configure)
    {
        _configurator.DuringAnyWhen(Faulted, configure);
    }

    /// <summary>
    /// Performs the track pending routing slip operation.
    /// </summary>
    public void TrackPendingRoutingSlip()
    {
        _executor.TrackRoutingSlip = true;

        CompletedIdProvider = GetTrackingNumber;
        FaultedIdProvider = GetTrackingNumber;
    }

    /// <summary>
    /// Performs the build itinerary operation.
    /// </summary>
    /// <param name="buildItinerary">The build itinerary value.</param>
    public void BuildItinerary(BuildItineraryCallback<TInput> buildItinerary)
    {
        _executor = new BuildRoutingSlipExecutor<TInput>(buildItinerary);
    }

    /// <summary>
    /// Performs the build using itinerary planner operation.
    /// </summary>
    public void BuildUsingItineraryPlanner()
    {
        _executor = new PlanRoutingSlipExecutor<TInput>();
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
            FaultMessageTypes = MessageTypeCache<TCommand>.MessageTypeNames,
            Host = context.Message.ActivityExceptions.Select(x => x.Host).FirstOrDefault() ?? context.Host,
            context.Message.Timestamp,
            exceptions,
            message
        };
    }

    /// <summary>
    /// Determines whether the current value has result.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasResult(out FutureResult<TCommand, TResult, RoutingSlipCompleted> result)
    {
        result = _result;
        return result != null;
    }

    /// <summary>
    /// Determines whether the current value has fault.
    /// </summary>
    /// <param name="fault">The fault value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasFault(out FutureFault<TCommand, TFault, RoutingSlipFaulted> fault)
    {
        fault = _fault;
        return fault != null;
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
