using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Provides an uncorrelated event correlation implementation.
    /// </summary>
    /// <typeparam name="TData">The t data type.</typeparam>
    public class UncorrelatedEventCorrelation<TData> :
        EventCorrelation<TInstance, TData>
        where TData : class
    {
        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="event">The event value.</param>
        public UncorrelatedEventCorrelation(Event<TData> @event)
        {
            Event = @event;
        }

        /// <summary>
        /// Gets the filter factory value.
        /// </summary>
        public SagaFilterFactory<TInstance, TData>? FilterFactory => null;

        /// <summary>
        /// Gets the event value.
        /// </summary>
        public Event<TData> Event { get; }

        Type EventCorrelation.DataType => typeof(TData);

        /// <summary>
        /// Gets the configure consume topology value.
        /// </summary>
        public bool ConfigureConsumeTopology => false;

        /// <summary>
        /// Gets the message filter value.
        /// </summary>
        public IFilter<ConsumeContext<TData>>? MessageFilter => null;

        /// <summary>
        /// Gets the policy value.
        /// </summary>
        public ISagaPolicy<TInstance, TData>? Policy => null;

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            yield return this.Failure(Event.Name, "Correlation", "was not specified");
        }
    }
}
