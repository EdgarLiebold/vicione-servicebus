using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>Defines correlation for uncorrelated event.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    public class UncorrelatedEventCorrelation<TData> :
        EventCorrelation<TInstance, TData>
        where TData : class
    {
        /// <summary>Initializes a new instance.</summary>
        /// <param name="event">The event.</param>
        public UncorrelatedEventCorrelation(Event<TData> @event)
        {
            Event = @event;
        }

        /// <summary>Gets the filter factory.</summary>
        public SagaFilterFactory<TInstance, TData>? FilterFactory => null;

        /// <summary>Gets the event.</summary>
        public Event<TData> Event { get; }

        Type EventCorrelation.DataType => typeof(TData);

        /// <summary>Gets the configure consume topology.</summary>
        public bool ConfigureConsumeTopology => false;

        /// <summary>Gets the message filter.</summary>
        public IFilter<ConsumeContext<TData>>? MessageFilter => null;

        /// <summary>Gets the policy.</summary>
        public ISagaPolicy<TInstance, TData>? Policy => null;

        /// <summary>Validates the current configuration.</summary>
        /// <returns>The validation failures.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            yield return this.Failure(Event.Name, "Correlation", "was not specified");
        }
    }
}
